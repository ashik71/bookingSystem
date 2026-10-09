# What is a controller?

*ASP.NET Core controllers, in depth.*

**Phase:** ROADMAP Phase 1 — ASP.NET Core Web API & Architecture
**Target framework:** .NET 10 (ADR-0003)
**Audience assumption:** senior .NET developer. No C#, DI or REST fundamentals here.
**Decision on record:** this project uses **controllers**, not Minimal APIs
(developer's call, session 003, following ROADMAP Phase 1).

> ROADMAP.md says ".NET 8" because it was written before the move to .NET 10.
> ADR-0003 supersedes it. Everything below is .NET 10.

**Contents**

1. [What a controller actually is](#1-what-a-controller-actually-is)
2. [`[ApiController]` — what it silently changes](#2-apicontroller--what-it-silently-changes)
3. [Routing](#3-routing)
4. [Model binding and validation](#4-model-binding-and-validation)
5. [Action results](#5-action-results)
6. [Filters — the pipeline inside MVC](#6-filters--the-pipeline-inside-mvc)
7. [Async, cancellation and dependency injection](#7-async-cancellation-and-dependency-injection)
8. [Where controllers sit in Clean Architecture](#8-where-controllers-sit-in-clean-architecture)
9. [Health endpoints](#9-health-endpoints--what-health-actually-asserts)
10. [**Interview question bank**](#10-interview-question-bank) ← 16 Q&A, three tiers
11. [Exercises](#11-exercises)
12. [What this lesson does not settle](#12-what-this-lesson-deliberately-does-not-settle)

---

## 0. Ten-second recall

The night-before-the-interview version. If you can expand each line into a
paragraph, you know the topic.

| Thing | The one sentence that matters |
|---|---|
| `ControllerBase` vs `Controller` | `Controller` adds views; APIs use `ControllerBase` |
| `[ApiController]` | Auto-400, binding inference, `ProblemDetails`, attribute routing required |
| Lifetime | Per request, via `IControllerActivator`, **not** the DI container by default |
| Static field in a controller | Shared across concurrent requests — data race and data leak |
| Route constraints | Participate in **selection**, not just validation |
| Ambiguous routes | Throw at **request** time, not startup |
| Non-nullable `string` property | Implicitly `[Required]` when NRT is on |
| Binding to an entity | Mass assignment — use a request DTO, always |
| Tenant id from the body | Cross-tenant write primitive. Tenant comes from resolution only |
| `ActionResult<T>` | Status flexibility **and** an accurate OpenAPI schema |
| Filter stages | Authorization → Resource → Binding → Action → Exception → Result |
| Filters vs middleware | Filter knows the action; middleware sees every request |
| Exception filter | Does **not** catch result-execution or middleware exceptions |
| `UseRouting` before `UseAuthorization` | Authorization reads policy metadata off the matched endpoint |
| `CancellationToken` | Bound from `RequestAborted`; omitting it leaks connections under load |
| Scoped into singleton | Captive dependency — `ValidateScopes` is Development-only |
| Liveness vs readiness | Liveness must not check dependencies, or a DB blip becomes a crash loop |
| Fat controller | Rules become untestable without HTTP and unreachable from workers |

---

## 1. What a controller actually is

Nothing about `ControllerBase` is magic to the framework. Four mechanics matter.

### Discovery

At startup `AddControllers()` builds an `ApplicationModel` by scanning assemblies
for types that are:

- `public`, concrete, non-abstract, non-generic
- **and** either have a name ending in `Controller`, **or** carry `[Controller]`

`[NonController]` opts a type out. Scanning covers the entry assembly plus
`ApplicationPartManager` parts — which is how a controller in a *referenced* project
gets picked up without the host naming it.

> **Modular-monolith consequence.** Assembly scanning means the host can discover
> endpoints inside any referenced assembly, whether the module intended to expose
> them or not. That is in tension with `BookingModule.AddBookingModule()` being the
> module's single entry point. Options: keep controllers in the API project only; or
> register module parts explicitly via `AddApplicationPart()`. Decide deliberately —
> do not let scanning quietly define your public surface.

### Activation and lifetime

Controllers are instantiated **per request** by `IControllerActivator`. Constructor
dependencies resolve from the **request scope**.

They are *not* registered in the DI container by default. `AddControllersAsServices()`
changes that — the container then owns construction and lifetime. Relevant when you
need container features (decorators, interception) applied to the controller itself.

### Statelessness

Because the instance is per-request:

- an **instance** field lives exactly one request — safe, occasionally useful
- a **static** field is shared across all concurrent requests — a data race, and a
  classic way to leak one user's data into another user's response

### `ControllerBase` vs `Controller`

`Controller` adds View/Razor support. For an API inherit **`ControllerBase`** —
`Controller` drags in view machinery you will never call.

---

## 2. `[ApiController]` — what it silently changes

Not cosmetic. It applies four conventions:

| # | Convention | Why you care |
|---|---|---|
| 1 | **Automatic 400 on invalid `ModelState`** | A filter short-circuits *before* your action body. Your code never sees invalid input |
| 2 | **Inferred binding sources** | Complex types ← body, simple types ← route/query, no attributes needed |
| 3 | **`ProblemDetails` error shape** | RFC 7807 / 9457 responses by default |
| 4 | **Attribute routing required** | Conventional routing throws. Good — explicit routes are correct for an API |

### The automatic-400 gotcha

It is implemented by `ModelStateInvalidFilter`, an **action filter** that runs before
the action body. Consequences people trip on:

- your validation logging inside the action never fires
- you cannot reshape the response without configuration

To take control:

```csharp
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Turn the filter off entirely — you now own ModelState checks
        options.SuppressModelStateInvalidFilter = true;

        // Or keep it and customise the response
        options.InvalidModelStateResponseFactory = ctx => /* your IActionResult */;
    });
```

Legitimate reasons to suppress: complex cross-field validation, or a house error
envelope that is not `ProblemDetails`.

---

## 3. Routing

Attribute routing with token replacement:

```csharp
[ApiController]
[Route("api/v1/[controller]")]   // [controller] → class name minus "Controller"
public sealed class PractitionersController : ControllerBase
{
    [HttpGet("{id:guid}")]       // GET api/v1/practitioners/{id}
    public async Task<ActionResult<PractitionerResponse>> GetById(Guid id) { }
}
```

`[controller]` couples the URL to the class name — rename the class and the API
breaks. Many teams write the literal segment for exactly that reason.

### Constraints participate in selection, not just validation

This is the part that is easy to get wrong. `{id:int}` and `{slug}` can coexist as
separate actions **because** the constraint disambiguates them. Remove the
constraint and you get an ambiguous match.

Common constraints: `:int` `:guid` `:bool` `:datetime` `:min(n)` `:max(n)`
`:length(n)` `:alpha` `:regex(...)` `:required`

### Precedence

1. Literal segments beat route parameters
2. Constrained parameters beat unconstrained
3. Catch-all (`{*rest}`) loses to everything

**Failure mode worth knowing:** a genuine tie throws `AmbiguousMatchException` at
**request** time, not startup. It will pass your build and fail in production on one
URL shape. Avoid by making routes genuinely distinct — `[HttpGet("")]` vs
`[HttpGet("{id:guid}")]`.

---

## 4. Model binding and validation

### Source order

Route values → query string → body → headers → form.

Only **one** body parameter is allowed; two `[FromBody]` parameters is a runtime
error. Explicit attributes: `[FromRoute]` `[FromQuery]` `[FromBody]` `[FromHeader]`
`[FromForm]` `[FromServices]`.

### Nullable reference types change validation

This project has `<Nullable>enable</Nullable>`. A **non-nullable** reference-type
property is therefore implicitly `[Required]`:

```csharp
public sealed record CreateBookingRequest(
    Guid SlotId,        // value type — 0/default binds fine, needs its own rule
    string ClientName,  // implicitly [Required] — null fails validation
    string? Notes);     // genuinely optional
```

People add `[Required]` and are puzzled that it was already enforced. Conversely,
making something nullable silently makes it optional.

### Mass assignment is a vulnerability, not a style preference

Binding directly to a domain entity lets a caller set any bindable property by
adding a JSON field:

```jsonc
// POST with a bonus field the client was never meant to control
{ "slotId": "…", "clientName": "…", "tenantId": "<another tenant>", "isAdmin": true }
```

**Defence: bind to a request DTO containing exactly the properties a client may
set. Never bind to an entity.** This is OWASP broken-access-control, and in a
multi-tenant system a bindable `TenantId` is a cross-tenant write primitive.

Related: never trust a client-supplied tenant identifier. Tenant comes from
resolution (B1), never from the request body.

---

## 5. Action results

| Return type | Status flexibility | Type visible to OpenAPI |
|---|---|---|
| `T` | No — always 200 | Yes |
| `IActionResult` | Yes | **No** |
| `ActionResult<T>` | Yes | Yes |

Prefer **`ActionResult<T>`** — it gives both, so Swagger generates an accurate
schema and analyzers can check your returns.

Declare the non-200 outcomes or your published contract is a lie:

```csharp
[HttpGet("{id:guid}")]
[ProducesResponseType<PractitionerResponse>(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<PractitionerResponse>> GetById(Guid id) { }
```

---

## 6. Filters — the pipeline inside MVC

The stage order:

```
Authorization → Resource → Model binding → Action → Exception → Result
```

| Stage | Runs | Use it for |
|---|---|---|
| **Authorization** | First | Cheapest possible rejection |
| **Resource** | Wraps everything after authz, *including model binding* | Caching short-circuit |
| **Action** | Around the method, with bound arguments available | Audit, arg-level concerns |
| **Exception** | Unhandled exceptions from the action — **not** from result execution | Mapping domain exceptions |
| **Result** | Around response formatting | Response envelopes, headers |

### Ordering rules

- Scope: **global → controller → action**
- *Before* stages: outer runs first. *After* stages: the order reverses
- `IOrderedFilter.Order` overrides scope ordering
- If one class implements both `IActionFilter` and `IAsyncActionFilter`, **only the
  async one runs**

### Filters vs middleware — the distinction that matters

| | Sees | Does not see |
|---|---|---|
| **Middleware** | Every request, including static files and 404s | Actions, model binding, the matched endpoint |
| **Filter** | The action, its bound arguments, its result | Non-MVC requests |

Needs action context → **filter**. Must cover everything including 404s →
**middleware**.

---

## 7. Async, cancellation and dependency injection

Two topics that get skipped in tutorials and asked about constantly.

### `CancellationToken` is not optional in an API

Accept one in every async action and pass it all the way down:

```csharp
[HttpGet]
public async Task<ActionResult<IReadOnlyList<SlotResponse>>> Search(
    [FromQuery] SlotSearchRequest request,
    CancellationToken cancellationToken)
```

Model binding supplies it automatically — it is bound from
`HttpContext.RequestAborted`, no attribute needed. When the client disconnects the
token is cancelled and the work unwinds.

**Why it matters beyond tidiness:** without it, a user who hammers refresh on a slow
search leaves every abandoned query running to completion, each holding a connection
from the pool. Under load this is how a slow endpoint becomes a site-wide outage —
the pool exhausts and *healthy* endpoints start timing out. An interviewer asking
about cancellation tokens is usually probing whether you have seen that happen.

Caveat worth stating: do not pass the request token into work that must complete
regardless of the client, such as writing an outbox record after a commit.
Cancelling that mid-flight is how you get partial writes.

### `async void` and sync-over-async

`async void` in a controller is unobservable — the exception cannot be caught by the
framework and takes the process down. Always `Task` or `Task<T>`.

`.Result` or `.Wait()` on an async call risks deadlock in contexts with a
synchronisation context and, more commonly in ASP.NET Core, simply burns a thread-pool
thread while blocking. Under load that causes thread-pool starvation, which presents
as latency climbing across *every* endpoint for no obvious reason.

### Constructor injection vs `[FromServices]`

Constructor injection is the default: dependencies are visible in the signature and
the type is honest about what it needs.

`[FromServices]` injects per-action, which is worth it when one action of twelve
needs an expensive dependency and you do not want every request constructing it:

```csharp
public async Task<IActionResult> Export([FromServices] IReportGenerator generator)
```

In .NET 10 this inference is largely automatic for registered services, but being
explicit still reads better.

**The captive dependency trap** — asked often: injecting a **scoped** service into a
**singleton** captures it for the singleton's lifetime, so every request shares one
instance of something meant to be per-request. For a `DbContext` that means shared
change-tracking state and concurrency bugs; for anything tenant-scoped it means
**one tenant's context leaking into another's request**. `ValidateScopes` catches
it in Development and is off by default in Production — know that asymmetry, since
it is why the bug reaches production.

---

## 8. Where controllers sit in Clean Architecture

A controller is an **adapter**. Its whole job:

1. deserialise the request
2. hand a command or query to the application layer
3. map the result to a status code

**Practical rule:** no `if` statement about *domain meaning* belongs in a controller.
Validating request *shape* is fine; validating *rules* belongs to the domain.

The test for whether you got this right: if a business rule lives in a controller it
is now untestable without HTTP, and unreachable from a background worker or a message
consumer. In this system that second point is not hypothetical — Block E adds workers
that must enforce the same rules.

---

## 9. Health endpoints — what `/health` actually asserts

Three different questions, three different endpoints. Conflating them causes real
production damage.

| Probe | Question | Checks dependencies? | On failure |
|---|---|---|---|
| **Liveness** | Is the process alive, or should it be killed? | **No** | Orchestrator restarts the container |
| **Readiness** | Should traffic route here right now? | **Yes** | Removed from load balancer, **no restart** |
| **Startup** | Has slow initialisation finished? | Sometimes | Holds liveness off during boot |

**The failure mode:** if liveness checks the database, a DB blip fails liveness,
Kubernetes restarts a healthy pod, and restarting does not fix a database — so you
get a crash loop during an outage you would otherwise have ridden out.

ASP.NET Core models this with **tags and predicates** — one set of registrations,
several endpoints each filtering differently:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddNpgSql(connectionString, tags: ["ready"]);

app.MapHealthChecks("/health/live",  new() { Predicate = c => c.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });
```

### Two design questions to settle

- **Auth?** Normally no — the probe has no credentials. But an unauthenticated
  endpoint that enumerates dependency topology and failure detail is reconnaissance.
  Usual split: terse public liveness, detailed readiness restricted or on a separate
  port.
- **Tenant resolution?** **Bypass it.** A probe has no tenant. This becomes a
  pipeline-ordering constraint at the middleware step.

### Controller or `MapHealthChecks`?

`HealthCheckService` already aggregates checks and `MapHealthChecks` already exposes
them. A `HealthController` re-implements that aggregation by hand and tends to drift.
Use a controller only if you need a response shape the middleware cannot produce.
Decide this consciously — it is the first "do I need a controller at all?" moment in
the project.

---

## 10. Interview question bank

How to read this section: **Asked** is the question as it actually gets phrased.
**Weak** is the answer that ends the topic politely and moves on. **Strong** is the
answer that makes the interviewer follow up. **Follow-up** is what they ask next —
the real test is whether you survive the second question, not the first.

A note on all of these: senior interviews are rarely testing recall. They are
testing whether you know the **failure mode** and the **trade-off**. "It depends"
is a strong answer *only* when you immediately say what it depends on.

**Frequency:** 🔴 near-certain in a .NET interview · 🟠 common · 🟡 occasional,
usually senior rounds.

> **On sourcing.** These are grouped by how often the *shape* recurs in .NET hiring
> loops and by interview type, **not** attributed to named employers. Anyone
> claiming a specific question belongs to a specific company is guessing; what is
> real is that these shapes recur constantly. Prepare the shape.

---

### Tier 1 — screening (phone / first round)

🟠 **Q1. What is the difference between `Controller` and `ControllerBase`?**

- **Weak:** "`Controller` is for MVC, `ControllerBase` is for APIs."
- **Strong:** `Controller` derives from `ControllerBase` and adds view support —
  `View()`, `PartialView()`, `ViewBag`, `ViewData`, `TempData`. For an API none of
  that is reachable, so you inherit `ControllerBase` and avoid pulling view
  machinery and its `ITempDataDictionaryFactory` dependency into a type that will
  never render HTML.
- **Follow-up:** *"Does it actually cost anything at runtime?"* — Honest answer:
  very little. The real argument is API surface and intent, not performance. Saying
  so demonstrates you are not cargo-culting.

---

🔴 **Q2. What does `[ApiController]` do?**

This is the single most common ASP.NET Core interview question. Most candidates name
one convention. Name all four and you are immediately above the median.

- **Strong:** Four conventions — (1) automatic 400 on invalid `ModelState` via
  `ModelStateInvalidFilter`; (2) binding-source inference, complex from body,
  simple from route/query; (3) `ProblemDetails` error responses per RFC 7807/9457;
  (4) attribute routing becomes mandatory.
- **Follow-up:** *"The automatic 400 — where exactly does it run, and how would you
  customise it?"* — It is an action filter, so it short-circuits **before** your
  action body executes. Customise via `ConfigureApiBehaviorOptions`:
  `SuppressModelStateInvalidFilter = true` to own it entirely, or
  `InvalidModelStateResponseFactory` to reshape the response. Mention that this is
  why validation logging inside the action never fires — that detail signals you
  have actually debugged it.

---

🟠 **Q3. What is the controller lifetime?**

- **Weak:** "Scoped."
- **Strong:** Per request, but **not** via the DI container by default — the
  `IControllerActivator` constructs them, resolving constructor dependencies from
  the request scope. `AddControllersAsServices()` moves construction into the
  container, which matters if you want decorators or interception applied to the
  controller itself.
- **Follow-up:** *"So can I hold state in a field?"* — An instance field lives
  exactly one request, which is safe. A **static** field is shared across all
  concurrent requests: a data race, and a classic cross-user data leak. Say
  explicitly that you would never cache per-user data in a static.

---

🟠 **Q4. How do you return different status codes from an action?**

- **Strong:** `ActionResult<T>` over `IActionResult`, because it preserves both
  status flexibility *and* the response type for OpenAPI generation and analyzers.
  Pair it with `[ProducesResponseType]` for the non-200 outcomes, otherwise your
  published contract is a lie and clients generate wrong SDKs.

---

### Tier 2 — the real technical round

🔴 **Q5. Filters vs middleware — when would you use each?**

The discriminator question. Interviewers use it to separate people who have read
docs from people who have shipped.

- **Weak:** "Middleware is global, filters are for controllers."
- **Strong:** The distinction is **what context each has access to.** Middleware
  runs for every request including static files and unmatched 404s, but it does not
  know the action, the bound arguments, or the result object. Filters know all of
  that but only run for MVC-routed requests. So: needs the action context (audit
  logging that records which use case ran, model-aware validation) → filter. Must
  cover everything including requests that never match an endpoint (correlation IDs,
  global exception handling, security headers) → middleware.
- **Follow-up:** *"Where would you put authentication?"* — Middleware, because the
  principal must be established before endpoint authorization evaluates policies,
  and because non-MVC endpoints need it too. Authorization then runs as both
  middleware (`UseAuthorization`) and filter metadata — `[Authorize]` is metadata
  the middleware reads off the matched endpoint, which is *why* `UseRouting` must
  come before `UseAuthorization`.

---

🟠 **Q6. Explain filter execution order.**

- **Strong:** Stage order is Authorization → Resource → Model binding → Action →
  Exception → Result. Within a stage, scope order is global → controller → action
  for the *before* half, and reverses for the *after* half — it nests like a stack.
  `IOrderedFilter.Order` overrides scope entirely. And if one class implements both
  `IActionFilter` and `IAsyncActionFilter`, only the async one is invoked.
- **Follow-up:** *"An exception filter — does it catch everything?"* — No. It
  catches unhandled exceptions from the action and from earlier filters, but **not**
  from result execution (serialisation failures) and not from middleware. This is
  precisely why you still need exception-handling middleware at the outermost layer
  even if you have exception filters.

---

🟠 **Q7. Two routes: `/practitioners/{id}` and `/practitioners/featured`. Which wins?**

- **Strong:** `featured` wins, and the reason is deterministic, not luck: route
  matching scores literal segments above parameter segments. Add `{id:guid}` and it
  is doubly safe, because `featured` fails the constraint so that candidate is
  eliminated during selection, not merely outranked.
- **Follow-up:** *"What if both were unconstrained parameters?"* —
  `AmbiguousMatchException`, thrown at **request** time rather than startup. That is
  the dangerous part: it compiles, it deploys, and it fails on one URL shape in
  production. Constraints are a selection mechanism, not just validation.

---

🔴 **Q8. What is mass assignment and how do you prevent it?**

In a multi-tenant system this question is really about tenant isolation.

- **Strong:** Binding a request directly to a domain entity lets a caller set any
  bindable property by adding a JSON field — `isAdmin`, `tenantId`, `price`,
  `status`. The defence is a request DTO containing exactly the properties a client
  is permitted to set, mapped explicitly to the domain. Never bind to an entity.
- **Follow-up:** *"Isn't `[Bind]` enough?"* — `[Bind]`/`[BindNever]` is an
  allow-list bolted onto the wrong type, and it fails open: add a property to the
  entity and forget the attribute, and it becomes bindable silently. A DTO fails
  closed — new entity properties are simply not reachable. Then the killer point for
  this domain: **tenant identity must never be bindable at all.** It comes from
  tenant resolution, never from the payload, or you have handed an attacker a
  cross-tenant write primitive.

---

🟠 **Q9. Your controller is 400 lines. What is wrong and how do you fix it?**

- **Strong:** The controller has stopped being an adapter. Its job is exactly three
  things — deserialise, delegate to the application layer, map the result to a
  status code. Business rules in a controller are untestable without HTTP and
  unreachable from a background worker or message consumer, which matters the moment
  you add async processing. The fix is to push rules into the domain and use cases
  into the application layer, leaving a thin adapter.
- **Follow-up:** *"Isn't that over-engineering for a CRUD endpoint?"* — Have a real
  answer. Honest position: for genuine CRUD with no invariants, a thin controller
  calling a repository directly is defensible and the indirection earns nothing. The
  trigger for the full use-case layer is **invariants that must hold regardless of
  entry point**. Being willing to say "this layer would be overkill here" reads as
  more senior than defending ceremony everywhere.

---

🟠 **Q10. How do you version an API?**

- **Strong:** Four options with honest trade-offs — URL path (`/api/v1/…`): most
  visible, easiest to route and cache, pollutes every URL. Query string: easy to
  default, easy to miss. Custom header: clean URLs, invisible to a browser and
  harder to test by hand. Media type / content negotiation
  (`Accept: application/vnd.x.v2+json`): most RESTful, least used in practice.
  Recommend URL path for a public API because discoverability and cacheability beat
  purity.
- **Follow-up:** *"When do you bump the major version?"* — Only on a **breaking**
  change: removing a field, changing a type, tightening validation, changing
  semantics of an existing field. Adding an optional field is not breaking.
  Mention Postel's law and tolerant readers.

---

🔴 **Q11. Health checks — what is the difference between liveness and readiness?**

Increasingly common because everyone runs containers now.

- **Strong:** Liveness answers "should this process be killed and restarted"; it
  must **not** check external dependencies. Readiness answers "should traffic route
  here"; it does check dependencies, and failing it removes the instance from the
  load balancer without restarting it.
- **Follow-up:** *"What breaks if liveness checks the database?"* — The exact
  failure story: the database has a brief outage, every instance fails liveness
  simultaneously, the orchestrator restarts all of them, restarting does not fix the
  database, and now you have a crash loop plus cold caches on top of the original
  incident. You converted a recoverable dependency blip into a full outage. Telling
  it as a failure story lands far better than reciting definitions.

---

### Tier 3 — senior / architecture round

🟡 **Q12. How do controllers fit a modular monolith?**

- **Strong:** The tension is that MVC discovers controllers by assembly scanning via
  `ApplicationPartManager`, so any public controller in a referenced assembly
  becomes part of your public surface whether the module intended it or not. That
  directly undercuts a module exposing one deliberate entry point. Two coherent
  resolutions: keep all controllers in the host and have them call module contracts;
  or keep controllers inside modules but register parts explicitly with
  `AddApplicationPart()` so exposure is a decision rather than a side effect.
- **Follow-up:** *"Which do you prefer?"* — Pick one and defend it. Controllers in
  the host keeps HTTP concerns out of modules and makes the API surface reviewable
  in one place; controllers in modules keeps a feature's code together and is
  better if modules might later become services. The second is stronger if you
  expect to extract services, which is the usual direction of travel.

---

🟡 **Q13. Should the repository be called from the controller?**

- **Strong:** It is a question about where invariants live. Controller → repository
  directly is fine for genuine CRUD with no rules. The moment there is an invariant
  — "a slot cannot be double-booked", "a booking cannot be cancelled after the
  window" — that rule must be enforced somewhere reachable by every entry point,
  not just HTTP. Say clearly that the trigger is the invariant, not the layer count.

---

🟡 **Q14. How would you test a controller?**

- **Strong:** Mostly, do not unit-test it. If the controller is a correct adapter
  there is almost nothing in it to test — unit tests that mock a mediator and assert
  `Ok()` was returned verify the framework, not your logic. Test the application
  layer directly for behaviour, and use `WebApplicationFactory` integration tests for
  the things that only exist in the HTTP layer: routing, model binding, filters,
  status-code mapping, auth policies, serialisation.
- **Follow-up:** *"What do integration tests catch that unit tests do not?"* — Route
  ambiguity, binding-source inference surprises, filter ordering, the automatic 400,
  serialisation of edge types (`DateOnly`, `decimal`, enums), and auth policy
  evaluation. Every one of those is invisible to a unit test that calls the method
  directly.

---

🟡 **Q15. Minimal APIs or controllers — which, and why?**

Expect this to be a preference-and-defence question, not a right-answer question.

- **Strong:** Minimal APIs have lower per-request overhead (no action invoker, no
  model-state machinery) and make the endpoint's dependencies explicit. Controllers
  give you convention: filters, `[ApiController]` behaviours, attribute-driven
  versioning, and a familiar organisation that scales across a team without
  everyone inventing their own structure. For a long-lived multi-module API with
  many cross-cutting concerns, convention is worth more than the microseconds.
- **Follow-up:** *"At what scale is your choice wrong?"* — Controllers are wrong for
  a small number of hot endpoints where per-request overhead is measurable, or a
  tiny service where MVC ceremony dwarfs the logic. Minimal APIs are wrong when
  cross-cutting policy needs to apply by convention rather than per-endpoint wiring,
  because explicit wiring is exactly what gets forgotten on endpoint number 80.

---

🟡 **Q16. Walk me through what happens between the request arriving and your action
running.**

The question that most reveals depth. The ordered chain:

1. Kestrel accepts the connection and parses the HTTP request
2. Middleware pipeline executes in registration order
3. `UseRouting` matches the request to an **endpoint** and attaches its metadata
4. `UseAuthentication` establishes `HttpContext.User`
5. `UseAuthorization` reads `[Authorize]` metadata off the matched endpoint and
   evaluates policies
6. The endpoint middleware invokes the MVC pipeline
7. Authorization filters → resource filters → **model binding and validation** →
   action filters → the action method
8. The result executes through result filters, then formatters serialise it
9. The response unwinds back out through the middleware stack

- **Follow-up:** *"Why must `UseRouting` come before `UseAuthorization`?"* — Because
  authorization needs the matched endpoint's metadata to know which policy applies.
  Without a matched endpoint there is no `[Authorize]` attribute to read, so the
  middleware has nothing to enforce.

---

### Questions *you* should ask back

Asking one of these at the end signals seniority more reliably than any answer:

- "Where do your business rules live today — and is that where you want them?"
- "How do you enforce tenant isolation at the data layer, not just at the edge?"
- "What does a failing readiness check actually do in your deployment?"
- "Do you integration-test routing and filters, or only unit-test handlers?"

---

## 11. Exercises

Answer these before writing code; they are the ones that get probed in interviews.

1. Why does `[ApiController]`'s automatic 400 happen before your action body, and
   **which filter stage** implements it?
2. You have `GET api/v1/practitioners/{id}` and `GET api/v1/practitioners/featured`.
   Does `featured` ever reach the `{id}` action? What makes that deterministic?
3. A controller has `private List<string> _items;`. Is that a bug? What if it were
   `static`?
4. Your health endpoint must not be slowed by tenant resolution. Filter or
   middleware — and why?
5. Where should `/health` live, and what does your answer say about module
   boundaries?
6. A `PUT` binds straight to your `Practitioner` entity. Name two concrete attacks.
7. At what scale is "controllers everywhere" the wrong choice?

---

## 12. What this lesson deliberately does not settle

| Question | Decided in |
|---|---|
| Where tenant resolution sits in the pipeline | B1 |
| Rate limiting position | B5 |
| Whether a repository earns its place | A7 / ROADMAP Phase 2 |
| Authn / authz scheme | C2–C4 |
| Whether Scheduling and Booking are one context | A2, with the first domain slice |

---

## References

- [Controllers & actions](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions)
- [Routing](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing)
- [Model binding](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding)
- [Validation](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation)
- [Filters](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters)
- [Health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [`ProblemDetails` / RFC 9457](https://datatracker.ietf.org/doc/html/rfc9457)
- [OWASP Mass Assignment](https://cheatsheetseries.owasp.org/cheatsheets/Mass_Assignment_Cheat_Sheet.html)
