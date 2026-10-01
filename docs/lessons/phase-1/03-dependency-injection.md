# Dependency injection

*Lifetimes, the bugs each one causes, and where DI stops being free.*

**Phase:** ROADMAP Phase 1 — ASP.NET Core Web API & Architecture
**Target framework:** .NET 10 (ADR-0003)
**Audience assumption:** senior .NET developer. You know what constructor injection
is; this is about lifetimes, failure modes and composition.

> **ROADMAP says "then Lamar".** Lamar is **dropped** (PROJECT-CONTEXT §1) — it was
> a job-spec requirement, not a skill goal. This project uses the built-in
> container unless convention scanning later earns its place. §7 covers what a
> third-party container would add, because that is the interview question.

**Contents**

0. [Ten-second recall](#0-ten-second-recall)
1. [The three lifetimes](#1-the-three-lifetimes)
2. [Captive dependencies](#2-captive-dependencies-the-defining-bug)
3. [Registration mechanics](#3-registration-mechanics)
4. [Scopes outside a request](#4-scopes-outside-a-request)
5. [`IDisposable` and who owns disposal](#5-idisposable-and-who-owns-disposal)
6. [Where DI meets Clean Architecture](#6-where-di-meets-clean-architecture)
7. [Built-in container vs the alternatives](#7-built-in-container-vs-the-alternatives)
8. [**Interview question bank**](#8-interview-question-bank)
9. [Exercises](#9-exercises)
10. [What this lesson does not settle](#10-what-this-lesson-does-not-settle)

---

## 0. Ten-second recall

| Thing | The one sentence that matters |
|---|---|
| Transient | New instance every resolution — even twice in one request |
| Scoped | One instance per scope; in a web app, per request |
| Singleton | One instance for the application lifetime |
| Captive dependency | A shorter lifetime captured by a longer one. The defining DI bug |
| `ValidateScopes` | Catches it — **Development only** by default |
| `ValidateOnBuild` | Catches missing registrations at startup, not first request |
| Last registration wins | For a single resolve; all are returned for `IEnumerable<T>` |
| `TryAdd*` | Register only if absent — what libraries should use |
| Background service | A **singleton**: create a scope manually to use scoped services |
| Disposal | The container disposes what it creates — not instances you hand it |
| Singleton holding `DbContext` | Corrupted change tracking; in multi-tenant, a data leak |
| Service locator | `IServiceProvider.GetService` in business code is an anti-pattern |
| Constructor over-injection | 8 dependencies is a design smell, not a DI problem |

---

## 1. The three lifetimes

| Lifetime | Instances | Typical use |
|---|---|---|
| **Transient** | New on every resolution | Cheap, stateless services |
| **Scoped** | One per scope — per HTTP request | `DbContext`, unit of work, tenant context |
| **Singleton** | One per application | Caches, configuration, `HttpClient` factories, expensive stateless services |

The non-obvious parts:

**Transient is not "safe by default".** Resolving a transient twice in one request
gives two instances, so any state they share is lost. If two collaborators must see
the same unit of work, transient silently breaks it.

**Scoped means "per scope", not "per request".** A request is just the most common
scope. In a message consumer or a background job, *you* decide the scope boundary —
and that is where tenant context most often leaks (Block E8).

**Singleton means thread-safe is your problem.** The container does not synchronise
anything. A singleton holding a non-concurrent collection is a race under load.

---

## 2. Captive dependencies — the defining bug

> A service of a **longer** lifetime holding a reference to a **shorter**-lived one
> captures it. The shorter service effectively becomes the lifetime of its captor.

```csharp
// Singleton capturing Scoped — the bug
public sealed class BookingCache                       // registered Singleton
{
    private readonly BookingDbContext _db;             // Scoped — now captive
    public BookingCache(BookingDbContext db) => _db = db;
}
```

Every request now shares **one** `DbContext`: shared change tracking, cross-request
entity state, `InvalidOperationException` under concurrent access, and — in a
multi-tenant system — **one tenant's data served to another tenant**, which is the
severe version.

### Why it reaches production

`ValidateScopes` detects it, and it is enabled by default **only in Development**:

```csharp
builder.Host.UseDefaultServiceProvider((ctx, options) =>
{
    options.ValidateScopes = true;    // catch captive dependencies everywhere
    options.ValidateOnBuild = true;   // catch missing registrations at startup
});
```

Turning both on in all environments is a cheap, high-value hardening step. The cost
of `ValidateOnBuild` is a slightly slower startup; the benefit is that a missing
registration fails at boot instead of on the first request that happens to hit that
path.

### The valid escape hatches

- Inject `IServiceScopeFactory` and create a scope per unit of work
- Inject a factory delegate (`Func<T>`) rather than the instance
- Reconsider the lifetime — often the singleton did not need to be one

---

## 3. Registration mechanics

### Resolution rules

```csharp
services.AddScoped<INotifier, EmailNotifier>();
services.AddScoped<INotifier, SmsNotifier>();

// Resolving INotifier        → SmsNotifier (last wins)
// Resolving IEnumerable<INotifier> → both, in registration order
```

`IEnumerable<T>` is the clean way to implement a pipeline or a fan-out of handlers.

### `TryAdd*` and `Replace`

```csharp
services.TryAddScoped<IClock, SystemClock>();          // only if not already present
services.Replace(ServiceDescriptor.Scoped<IClock, FakeClock>());  // swap in tests
```

A library's `AddXyz()` should use `TryAdd*` so it never silently overwrites the
host's choice. This matters directly for `AddBookingModule()` — a module should
register its own internals with `Add*` but any shared abstraction with `TryAdd*`.

### Keyed services

```csharp
services.AddKeyedScoped<IPaymentGateway, StripeGateway>("stripe");
// ctor: public Checkout([FromKeyedServices("stripe")] IPaymentGateway gateway)
```

Useful for strategy selection, and a reasonable alternative to a factory when the
set of keys is known at startup.

### Options pattern

```csharp
builder.Services.Configure<BookingOptions>(builder.Configuration.GetSection("Booking"));
```

| Interface | Lifetime | Reloads on config change? |
|---|---|---|
| `IOptions<T>` | Singleton | No |
| `IOptionsSnapshot<T>` | **Scoped** | Yes, per request |
| `IOptionsMonitor<T>` | Singleton | Yes, with change notifications |

`IOptionsSnapshot<T>` being scoped is a captive-dependency trap: injecting it into
a singleton is the same bug as §2. Singletons need `IOptionsMonitor<T>`.

Validate options at startup rather than discovering a bad value at first use:

```csharp
builder.Services.AddOptions<BookingOptions>()
    .Bind(builder.Configuration.GetSection("Booking"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

---

## 4. Scopes outside a request

No HTTP request means no ambient scope. A `BackgroundService` is a **singleton**, so
it cannot inject scoped services:

```csharp
public sealed class SlotPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;   // singleton-safe

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();      // one unit of work
            var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            // …
        }
    }
}
```

**Create the scope inside the loop, not outside.** A scope that wraps the whole loop
is a `DbContext` living for the process lifetime — accumulating tracked entities
until memory is the problem.

> **Where this becomes a tenant-isolation bug.** Outside HTTP there is no tenant
> context to resolve from, so the scope you create has *no* tenant — and a global
> query filter that reads from an empty tenant context may return everything. That
> is Block E8, and it is one of the classic multi-tenant leak sites.

---

## 5. `IDisposable` and who owns disposal

The container disposes instances **it created**, at the end of the owning scope.
Two consequences:

- **Transient `IDisposable` resolved from the root provider is never disposed until
  shutdown** — they accumulate for the application's lifetime. This is a real leak
  source.
- An instance you construct and register yourself
  (`services.AddSingleton(new Thing())`) is **not** disposed by the container. You
  own it.

Prefer `IAsyncDisposable` where the resource is I/O-backed; the container honours it.

---

## 6. Where DI meets Clean Architecture

The dependency rule says inner layers do not depend on outer ones. DI is the
mechanism that makes that physically true: the Application layer declares an
interface (`ISlotRepository`), Infrastructure implements it, and **composition
happens at the host**, which is the only place allowed to know both.

```
Domain          ← no dependencies at all
Application     ← defines ports (interfaces). Depends on Domain
Infrastructure  ← implements the ports. Depends on Application
Api (host)      ← references Infrastructure only to call AddBookingModule()
```

The practical test: if `Api.csproj` must reference a concrete repository type to
compile, the composition has leaked out of the module. `AddBookingModule()` exists
precisely so the host registers a module's internals without naming them — which is
why its body belongs in Infrastructure, not the host.

**Service locator is the anti-pattern here.** Injecting `IServiceProvider` into a
handler and calling `GetService<T>()` hides dependencies from the constructor, so
the type lies about what it needs and the failure moves from startup to runtime.
Legitimate uses are narrow: composition root, factories, and scope creation in
background work.

**Constructor over-injection is a design smell, not a container problem.** Eight
dependencies means the class has too many reasons to change. The fix is to split it
or introduce a facade — not to switch to property injection.

---

## 7. Built-in container vs the alternatives

| | Built-in | Autofac / Lamar |
|---|---|---|
| Lifetimes | 3 | More (per-matching-scope, instance-per-owned) |
| Assembly scanning | Manual | Convention-based registration |
| Decorators | Manual wiring | First-class |
| Property injection | No | Yes |
| Interception / AOP | No | Yes |
| Startup cost | Lowest | Slightly higher |

**The honest position for an interview:** the built-in container covers the great
majority of applications, and its deliberate limitations push you toward explicit
composition, which is usually a good thing. You reach for a third-party container
when you genuinely need **decorators at scale** (cross-cutting behaviour wrapped
around many handlers) or **convention registration** because manual registration has
become a maintenance burden. "We used Autofac because it's more powerful" is a weak
answer; naming the specific feature is a strong one.

Decorators without a third-party container, for completeness:

```csharp
services.AddScoped<IBookingService, BookingService>();
services.Decorate<IBookingService, LoggingBookingService>();   // Scrutor
```

---

## 8. Interview question bank

**Frequency:** 🔴 near-certain · 🟠 common · 🟡 senior rounds.

> Grouped by how often the *shape* recurs in .NET hiring loops, not attributed to
> named employers — anyone claiming a specific question belongs to a specific
> company is guessing.

---

### 🔴 Q1. Explain the three service lifetimes.

Asked in essentially every .NET interview. The differentiator is not the
definitions — it is naming what each one *breaks*.

- **Weak:** "Transient is new every time, scoped is per request, singleton is one."
- **Strong:** The definitions, then the failure modes: transient resolved twice in
  one request gives two instances, so shared state is silently lost; scoped is per
  *scope*, which outside HTTP you must create yourself; singleton requires you to
  make the type thread-safe, because the container synchronises nothing.
- **Follow-up:** *"Which would you use for `DbContext`, and why?"* — Scoped. It is
  a unit of work: one per request gives a natural transaction boundary and shared
  change tracking across the request. Singleton corrupts change tracking; transient
  means two services in the same request cannot see each other's pending changes.

---

### 🔴 Q2. What is a captive dependency?

- **Strong:** A longer-lived service capturing a shorter-lived one, pinning it to
  the longer lifetime. The classic is a singleton injecting a scoped `DbContext`:
  every request then shares one context, producing cross-request entity state and
  concurrency exceptions.
- **Follow-up:** *"How would you catch it?"* — `ValidateScopes`, which is on by
  default **only in Development**. Say that asymmetry out loud — it is exactly why
  the bug survives to production. Pair it with `ValidateOnBuild` so missing
  registrations fail at startup rather than on first request.
- **Second follow-up (multi-tenant roles):** *"Why is this worse in a multi-tenant
  app?"* — A captured tenant context means one tenant's identity serving another
  tenant's request: a data-isolation breach, not merely a bug.

---

### 🟠 Q3. How do you use a scoped service inside a `BackgroundService`?

- **Strong:** You cannot inject it — a `BackgroundService` is a singleton. Inject
  `IServiceScopeFactory`, call `CreateScope()` per unit of work, and resolve from
  the scope's provider.
- **Follow-up:** *"Where exactly do you create the scope?"* — Inside the loop, one
  per iteration. Wrapping the whole loop in one scope means a `DbContext` that lives
  as long as the process and accumulates tracked entities until memory fails.

---

### 🟠 Q4. Two implementations of the same interface are registered. What resolves?

- **Strong:** The **last** registration wins for a single `T`; resolving
  `IEnumerable<T>` returns all of them in registration order. If you need to choose
  deliberately, use keyed services or a factory rather than relying on order.
- **Follow-up:** *"What is `TryAdd` for?"* — Registering only if absent, so a
  library's `AddXyz()` does not silently overwrite the host's own choice. Relevant
  to any module-registration entry point.

---

### 🟠 Q5. Is service locator an anti-pattern?

- **Strong:** Mostly yes, and the reason is honesty of contract. Injecting
  `IServiceProvider` and resolving inside a method hides what the class depends on,
  so the constructor no longer tells the truth, tests cannot see what to substitute,
  and a missing registration fails at runtime instead of startup. The legitimate
  exceptions are narrow: the composition root, factories, and scope creation in
  background work.

---

### 🟡 Q6. Why would you use a third-party container?

- **Strong:** Name the feature, not the brand. Specific triggers: decorators applied
  across many handlers for cross-cutting behaviour; convention-based assembly
  scanning when manual registration is unmaintainable; interception. Absent one of
  those, the built-in container's limits are a feature — they keep composition
  explicit.
- **Follow-up:** *"Can you do decorators without one?"* — Yes, manually or with
  Scrutor's `Decorate<T>()`. Worth knowing before adding a whole container.

---

### 🟡 Q7. How does DI support the dependency inversion principle in Clean Architecture?

- **Strong:** The Application layer declares ports as interfaces and depends on
  nothing concrete. Infrastructure implements them. The host wires the two at the
  composition root, which is the only place that knows both sides — so dependencies
  point inward at compile time even though control flows outward at runtime.
- **Follow-up:** *"How do you keep the host from knowing the module's internals?"* —
  A single registration entry point per module, with the registration body living
  inside the module. The host calls `AddBookingModule()` and references nothing else.

---

### 🟡 Q8. `IOptions`, `IOptionsSnapshot`, `IOptionsMonitor` — differences?

- **Strong:** `IOptions<T>` is a singleton resolved once, so it never sees config
  changes. `IOptionsSnapshot<T>` is **scoped** and recomputed per request.
  `IOptionsMonitor<T>` is a singleton that supports change notification.
- **Follow-up:** *"Which can a singleton inject?"* — `IOptionsMonitor<T>` only.
  Injecting `IOptionsSnapshot<T>` into a singleton is a captive dependency — the
  same bug as Q2, which is why this follows on so often.

---

### Questions to ask back

- "Do you run `ValidateScopes` and `ValidateOnBuild` in all environments?"
- "How do background jobs get a scope — and a tenant context?"
- "Is composition centralised, or does each module own its registrations?"

---

## 9. Exercises

1. A singleton injects `IOptionsSnapshot<T>`. What is the bug, and what should it
   inject instead?
2. A transient `IDisposable` is resolved from the root provider on every request.
   When is it disposed? Why is that a leak?
3. Your `BackgroundService` processes a queue. Where do you create the scope, and
   what happens to the `DbContext` if you get it wrong?
4. `AddBookingModule()` registers `IClock`. The host also registers `IClock`. Which
   wins, and how should the module have registered it?
5. A message consumer must enforce tenant isolation but has no HTTP context. What
   must the scope carry, and what does a global query filter do if it is missing?
6. You have eight constructor parameters. Is this a DI problem? What is the fix?
7. At what point would you add a third-party container to *this* project?

---

## 10. What this lesson does not settle

| Question | Decided in |
|---|---|
| Whether a repository abstraction earns its place | A7 / Phase 2 |
| How tenant context is populated | **B1** |
| Tenant context across async boundaries | **E8** |
| Whether modules register via scanning or explicitly | A4 / A5 |

---

## References

- [Dependency injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Service lifetimes](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-lifetimes)
- [DI guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines)
- [Keyed services](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#keyed-services)
- [Options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)
- [Scrutor (decorators)](https://github.com/khellang/Scrutor)
