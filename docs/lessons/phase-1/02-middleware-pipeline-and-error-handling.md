# Middleware pipeline and error handling

*How a request actually travels through ASP.NET Core, and where failures are caught.*

**Phase:** ROADMAP Phase 1 — ASP.NET Core Web API & Architecture
**Target framework:** .NET 10 (ADR-0003)
**Audience assumption:** senior .NET developer. No fundamentals.
**Prerequisite:** [01 — Controllers](01-web-api-controllers-routing-model-binding-validation.md),
especially §6 on filters.

**Contents**

0. [Ten-second recall](#0-ten-second-recall)
1. [What middleware actually is](#1-what-middleware-actually-is)
2. [Run, Use, Map — and the terminal distinction](#2-run-use-map--and-the-terminal-distinction)
3. [The canonical order, and why each position is forced](#3-the-canonical-order-and-why-each-position-is-forced)
4. [Writing middleware: three forms](#4-writing-middleware-three-forms)
5. [Error handling](#5-error-handling)
6. [`ProblemDetails` and a consistent error contract](#6-problemdetails-and-a-consistent-error-contract)
7. [Middleware in a multi-tenant system](#7-middleware-in-a-multi-tenant-system)
8. [**Interview question bank**](#8-interview-question-bank)
9. [Exercises](#9-exercises)
10. [What this lesson does not settle](#10-what-this-lesson-does-not-settle)

---

## 0. Ten-second recall

| Thing | The one sentence that matters |
|---|---|
| Middleware | A chain of delegates; each decides whether to call `next` |
| Order | Registration order **is** execution order. It is semantics, not style |
| `Use` vs `Run` | `Run` is terminal — nothing after it executes |
| `Map` | Branches the pipeline on a path prefix; the branch never rejoins |
| Not calling `next` | Short-circuits — everything downstream is skipped |
| Exception handler | Must be **outermost**; it only catches what is downstream |
| `UseRouting` | Selects the endpoint and attaches its metadata |
| `UseAuthorization` after `UseRouting` | It reads `[Authorize]` off the matched endpoint |
| CORS before auth | Otherwise preflight gets a 401 the browser reports as a CORS error |
| Writing after the response started | `InvalidOperationException` — check `HasStarted` |
| Middleware is a **singleton** | Constructor-injected scoped services are captive; inject into `Invoke` |
| `IMiddleware` | Resolved per request from DI — the escape hatch from the above |
| Exception filter vs middleware | Filter misses result-execution and non-MVC failures |
| `ProblemDetails` | RFC 9457. One error shape for the whole API |
| Leaking stack traces | Information disclosure. Detail in Development only |

---

## 1. What middleware actually is

A middleware is a function that takes a `HttpContext` and a `RequestDelegate`
("the rest of the pipeline") and decides what to do:

```csharp
app.Use(async (context, next) =>
{
    // before: everything downstream has not run yet
    await next(context);
    // after: everything downstream has completed
});
```

The pipeline is built by composing these into a single nested delegate at startup.
That composition detail explains the three properties people find surprising:

1. **It is a stack, not a list.** Code before `next` runs on the way in; code after
   it runs on the way out, in reverse order.
2. **Each middleware can short-circuit** by not calling `next`. Everything
   downstream — including endpoint execution — simply does not happen.
3. **It is built once at startup**, not per request. The delegate chain is fixed, so
   you cannot conditionally register middleware per request; you branch instead.

> **The mental model that makes ordering obvious:** middleware wraps. Whatever you
> register first is the outermost wrapper, so it sees the request earliest and the
> response latest. Ask "what must wrap what?" and the order answers itself.

---

## 2. Run, Use, Map — and the terminal distinction

| Method | Behaviour |
|---|---|
| `Use` | Non-terminal. May call `next`, may short-circuit |
| `Run` | **Terminal.** Never calls `next`. Anything registered after it is dead code |
| `Map` | Branches on a path prefix. The branch is its own pipeline and **never rejoins** |
| `MapWhen` | Branches on an arbitrary predicate over `HttpContext` |
| `UseWhen` | Branches on a predicate, but the branch **rejoins** the main pipeline |

`UseWhen` vs `MapWhen` is a genuine gotcha: `MapWhen` creates a dead-end branch, so
if the branch does not produce a response you get a 404. `UseWhen` re-enters the
main pipeline afterwards.

```csharp
// Only add expensive diagnostics for API routes, then continue normally
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseMiddleware<RequestTimingMiddleware>());
```

---

## 3. The canonical order, and why each position is forced

This is the part that is actually asked. Do not memorise the list — learn the
*constraint* behind each position, because that is the follow-up question.

```csharp
app.UseExceptionHandler("/error");   // 1  outermost: catches everything below
app.UseHsts();                       // 2  response header, non-dev only
app.UseHttpsRedirection();           // 3  bail out before doing any real work
app.UseStaticFiles();                // 4  short-circuit; no auth cost for assets
app.UseRouting();                    // 5  SELECTS the endpoint + its metadata
app.UseCors();                       // 6  after routing, before auth
app.UseAuthentication();             // 7  establishes HttpContext.User
app.UseAuthorization();              // 8  reads [Authorize] off the endpoint
app.UseRateLimiter();                // 9  needs identity to limit per-user
app.MapControllers();                // 10 executes the endpoint
```

| Position | The constraint that forces it |
|---|---|
| **Exception handler first** | It can only catch exceptions thrown *downstream* of itself. Register it second and anything in the first middleware is uncaught |
| **HTTPS redirection early** | A request you are about to answer with a 307 should not first hit the database |
| **Static files before auth** | Otherwise every CSS file pays the cost of authentication; also lets assets serve while auth is misconfigured |
| **Routing before authorization** | Authorization needs the matched endpoint's metadata to know which policy applies. No matched endpoint → no `[Authorize]` to read |
| **CORS after routing, before auth** | Preflight `OPTIONS` requests carry no credentials. Behind authentication they get a 401, which the browser surfaces as an opaque CORS error — one of the most time-wasting bugs in web development |
| **Authentication before authorization** | You cannot evaluate a policy about a user before you know who the user is |
| **Rate limiting after auth** (usually) | Per-user or per-tenant limits need identity. But see §7 — there is a real argument for a cheap pre-auth limiter as well |

**The two orderings that cause the most production incidents:**

1. Exception handling not outermost → unhandled exceptions escape as raw 500s with
   stack traces.
2. CORS after authentication → preflight 401s that present as inexplicable browser
   CORS failures.

---

## 4. Writing middleware: three forms

### Inline

Fine for two or three lines. Beyond that it clutters `Program.cs`.

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    await next(context);
});
```

### Convention-based class

The common form. **No interface** — the framework finds `Invoke`/`InvokeAsync` by
convention.

```csharp
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;   // singleton: OK

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // Scoped dependencies are injected HERE, per request — not in the constructor
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        await _next(context);
    }
}
```

> **The captive-dependency trap, and the single most-asked middleware gotcha:**
> convention-based middleware is instantiated **once** — it is effectively a
> singleton. A scoped service injected into its *constructor* is captured for the
> application's lifetime, so every request shares one instance. For a `DbContext`
> that is corrupted change-tracking; for anything tenant-scoped it is **one tenant's
> data leaking into another tenant's request**. Inject scoped services as
> **method parameters on `InvokeAsync`** instead.

### `IMiddleware`

Implements an interface and is resolved from DI **per request**, which removes the
captive-dependency problem entirely. Costs a DI registration:

```csharp
public sealed class TenantResolutionMiddleware : IMiddleware
{
    private readonly ITenantStore _store;   // scoped is now safe
    public TenantResolutionMiddleware(ITenantStore store) => _store = store;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) { … }
}

builder.Services.AddScoped<TenantResolutionMiddleware>();
```

**When to prefer it:** middleware with scoped dependencies, or middleware you want
to unit-test by construction. Otherwise the convention form is lighter.

---

## 5. Error handling

Four mechanisms, and they are not interchangeable.

### `UseExceptionHandler` — the one you want

Catches unhandled exceptions, clears the response, and re-executes against a
handler path or an inline handler. In .NET 8+ the modern form is `IExceptionHandler`:

```csharp
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var problem = exception switch
        {
            NotFoundException   => Problem(StatusCodes.Status404NotFound, "Not found"),
            ValidationException => Problem(StatusCodes.Status400BadRequest, "Validation failed"),
            ConflictException   => Problem(StatusCodes.Status409Conflict, "Conflict"),
            _                   => Problem(StatusCodes.Status500InternalServerError, "Server error")
        };

        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;   // false → fall through to the next handler
    }
}

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
app.UseExceptionHandler();
```

Returning `false` lets you chain handlers — each gets a chance, and the first to
return `true` wins.

### `UseDeveloperExceptionPage` — Development only

Renders the stack trace, the query string, headers, cookies. **Never** in
Production: it is textbook information disclosure, exposing file paths, package
versions and sometimes connection strings.

### Exception filters — MVC only, deliberately narrower

Covered in [lesson 01 §6](01-web-api-controllers-routing-model-binding-validation.md#6-filters--the-pipeline-inside-mvc).
They catch exceptions from actions, but **not** from result execution
(serialisation), **not** from other middleware, and **not** from requests that never
matched an action. Use them for action-specific handling; keep the middleware as the
backstop.

### `UseStatusCodePages` — for responses with no body

A 404 from routing produces an empty body, not an exception, so no exception
handler fires. `UseStatusCodePages` gives those a consistent body.

### The rule that ties them together

> Exception-handling middleware is the **outermost** layer and the last line of
> defence. Everything else is a refinement inside it.

### `Response.HasStarted`

Once any byte of the response has been written, headers are flushed and the status
code is fixed. Attempting to rewrite then throws `InvalidOperationException`. This
is why exceptions thrown *during* streaming or serialisation cannot be converted to
a clean 500 — the 200 has already gone. Defensive handlers check:

```csharp
if (context.Response.HasStarted) { _logger.LogError(ex, "…"); throw; }
```

---

## 6. `ProblemDetails` and a consistent error contract

RFC 9457 (which obsoleted 7807) defines a standard error body:

```json
{
  "type": "https://example.com/errors/slot-already-booked",
  "title": "The slot is no longer available",
  "status": 409,
  "detail": "Slot 3f2a… was booked by another client.",
  "instance": "/api/v1/bookings",
  "traceId": "00-8a3c…-01"
}
```

Why it is worth adopting rather than inventing a house format: clients can parse one
shape across every endpoint, `[ApiController]` already produces it for validation
failures, and `AddProblemDetails()` makes the framework use it for status-code-only
responses too.

**Always include a correlation/trace id.** It is what turns "the site broke" into a
single log query. With OpenTelemetry in scope later, use `Activity.Current?.Id` so
the id ties the HTTP response to the distributed trace.

**Two security rules, both commonly violated:**

- `detail` must never contain exception messages in Production — they leak SQL
  fragments, file paths, and internal service names.
- Error responses must not distinguish "user not found" from "wrong password".
  That is a user-enumeration oracle, and it matters even more for the phone/OTP
  identity model this project uses.

---

## 7. Middleware in a multi-tenant system

This is where the generic ordering advice meets this project.

**Tenant resolution is middleware, not a filter** — it must run before anything
that touches tenant-scoped data, including non-MVC endpoints and background-ish
paths. A filter would miss those.

**But its position depends on an undecided question:**

| Tenant source | Must run | Because |
|---|---|---|
| Subdomain / host header | **Before** authentication | The value is on the raw request |
| JWT claim | **After** authentication | The claim does not exist until the principal is built |
| Request header | Before auth — **but** a header alone is spoofable and must never be trusted from an external client |
| Route segment | After routing, since route values are not populated before it |

That decision is **B1** in CURRICULUM and is deliberately open. Do not hardcode a
position until it is made.

**Health endpoints must bypass tenant resolution** — a probe has no tenant.
Short-circuit on the path before resolution runs, or branch with `UseWhen`.

**Rate limiting is genuinely contested.** After authentication you can limit per
tenant and per user, which is what you want for fairness. But an unauthenticated
flood then reaches your authentication and tenant-resolution logic before any limit
applies — and OTP endpoints cost real money per request. The mature answer is two
limiters: a cheap IP-based one early, and an identity-aware one after auth. That is
**B5**.

---

## 8. Interview question bank

**Frequency** reflects how often this comes up in .NET interviews generally:
🔴 near-certain · 🟠 common · 🟡 occasional, usually senior rounds.

> **An honest note on sourcing.** These are grouped by how commonly they appear in
> .NET hiring loops and by interview *type*, not attributed to named employers.
> Anyone claiming "this exact question is asked at company X" is guessing; what is
> real is that the *shape* below recurs constantly. Prepare the shape.

---

### 🔴 Q1. What is middleware and how does the pipeline work?

- **Weak:** "Components that handle requests in order."
- **Strong:** A composed chain of delegates, each receiving `HttpContext` and a
  reference to the rest of the pipeline. It behaves like a stack: code before
  `await next()` runs inbound, code after runs outbound in reverse. Any middleware
  can short-circuit by not calling `next`. The chain is composed once at startup,
  not per request.
- **Follow-up:** *"What happens if you don't call `next`?"* — Everything downstream
  is skipped, including endpoint execution, and the response unwinds from that
  point. That is the intended mechanism for caching, rate limiting and auth
  rejection — not an error.

---

### 🔴 Q2. Does middleware order matter? Give an example where it breaks.

Near-universal. Interviewers want a *specific* failure, not "yes, order matters."

- **Strong:** Order is semantics. Two concrete breakages: (1) exception-handling
  middleware registered after another middleware cannot catch that middleware's
  exceptions, since it only wraps what is downstream; (2) CORS after authentication
  means preflight `OPTIONS` requests — which carry no credentials — get a 401, and
  the browser reports it as an opaque CORS failure, sending you hunting in the
  wrong place entirely.
- **Follow-up:** *"Why must `UseRouting` precede `UseAuthorization`?"* — Because
  authorization reads policy metadata (`[Authorize]`, required roles) off the
  **matched endpoint**. Before routing runs there is no matched endpoint, so there
  is nothing to enforce.

---

### 🔴 Q3. Middleware vs filters — when do you use each?

- **Strong:** The discriminator is **context**. Middleware runs for every request,
  including static files and requests that match no endpoint, but it does not know
  the action, the bound arguments, or the action result. Filters know all of that
  but run only for MVC-routed requests. Needs action context → filter. Must cover
  everything including 404s → middleware.
- **Follow-up:** *"So where does global exception handling go?"* — Middleware, and
  outermost. An exception filter misses serialisation failures during result
  execution, exceptions from other middleware, and anything outside MVC. Filters
  are a refinement, never the backstop.

---

### 🟠 Q4. `Use` vs `Run` vs `Map`?

- **Strong:** `Use` is non-terminal and may call `next`. `Run` is terminal and never
  does, so anything registered after it is unreachable. `Map` branches the pipeline
  on a path prefix, and the branch does not rejoin.
- **Follow-up:** *"What is the difference between `MapWhen` and `UseWhen`?"* —
  `MapWhen` creates a dead-end branch; if it produces no response you get a 404.
  `UseWhen` rejoins the main pipeline after the branch. Knowing `UseWhen` exists is
  a small but reliable signal.

---

### 🟠 Q5. How do you write custom middleware, and what is the DI gotcha?

Asked specifically to see whether you have hit the captive-dependency bug.

- **Strong:** Convention-based class with `InvokeAsync(HttpContext, …)`, registered
  with `UseMiddleware<T>()`. The gotcha: convention-based middleware is constructed
  **once**, so it is effectively a singleton. A scoped service injected into its
  constructor is captured for the application lifetime and shared across all
  requests. Inject scoped services as **parameters of `InvokeAsync`**, which are
  resolved per request — or implement `IMiddleware`, which DI resolves per request.
- **Follow-up:** *"What breaks concretely?"* — A captured `DbContext` gives shared
  change tracking and concurrency exceptions. In a multi-tenant app a captured
  tenant context is worse: one tenant's context serving another tenant's request.
  `ValidateScopes` catches it in Development and is **off in Production**, which is
  precisely why it escapes.

---

### 🟠 Q6. How do you implement global error handling?

- **Strong:** `IExceptionHandler` (.NET 8+) registered with
  `AddExceptionHandler<T>()` and `UseExceptionHandler()`, mapping domain exception
  types to status codes and returning `ProblemDetails`. Developer exception page in
  Development only. `UseStatusCodePages` for body-less status responses like a
  routing 404.
- **Follow-up:** *"What do you put in the response?"* — A stable `type`, a safe
  `title`, the status, and a correlation id. Never the exception message in
  Production: it leaks SQL fragments, paths and internal hostnames. Log the detail
  server-side keyed by the same correlation id.

---

### 🟡 Q7. Should exceptions be used for control flow — e.g. a booking conflict?

A senior design question with no single right answer; they want the trade-off.

- **Strong:** Exceptions are costly and semantically mean "unexpected". A slot
  already being booked is an *expected* outcome under concurrency, so a result
  type (`Result<T>`) models it more honestly and keeps the handler's contract
  visible. The counter-argument is real: exceptions propagate automatically, so
  deep call chains do not need every layer to thread a result through. A common
  split is result types inside the domain, exceptions only at genuine boundary
  failures.
- **Follow-up:** *"What does that mean for your middleware?"* — With result types,
  the controller maps results to status codes and the exception handler only sees
  genuine faults. That makes a 500 meaningful again, since it no longer fires for
  routine business outcomes.

---

### 🟡 Q8. Where would you put rate limiting, and why?

- **Strong:** Both sides matter. After authentication you can limit per user and per
  tenant, which is what fairness requires. But then an unauthenticated flood still
  reaches authentication and tenant resolution before any limit applies. For an API
  with SMS/OTP endpoints that is a direct cost-amplification attack. The mature
  answer is layered: a cheap IP-based limiter early, an identity-aware one after
  auth.
- **Follow-up:** *"What breaks when you scale to several instances?"* — In-memory
  limiters count per instance, so N instances means N times the intended limit. You
  need a distributed store (Redis) for a real global limit, and then you have to
  decide what happens when that store is unavailable — fail open and lose the limit,
  or fail closed and lose availability.

---

### 🟡 Q9. Walk me through a request end to end.

See [lesson 01 §10 Q16](01-web-api-controllers-routing-model-binding-validation.md#10-interview-question-bank)
for the full chain. The middleware-specific point to land: routing **selects** the
endpoint, and endpoint **execution** happens at the very end of the pipeline — which
is why everything between `UseRouting` and `MapControllers` can still inspect the
matched endpoint's metadata before the action runs.

---

### 🟡 Q10. How do you unit-test middleware?

- **Strong:** Construct it with a `RequestDelegate` stub, invoke it with a
  `DefaultHttpContext`, and assert on the context afterwards — status code, headers,
  whether `next` was called. For ordering and interaction, `WebApplicationFactory`
  integration tests are the honest level, because ordering bugs only exist in a
  real composed pipeline.
- **Follow-up:** *"What would a unit test miss?"* — Precisely the ordering, which is
  where the real bugs are. A middleware that is individually correct and registered
  in the wrong position passes every unit test.

---

### Questions to ask back

- "Is your exception handling middleware or filters — and does anything escape it?"
- "Do your error responses carry a correlation id that reaches your logs?"
- "Where does tenant resolution sit relative to authentication?"
- "Is rate limiting per instance or distributed?"

---

## 9. Exercises

1. Exception middleware is registered **after** a logging middleware. The logging
   middleware throws. What does the client receive, and why?
2. Your Angular app gets a CORS error in the browser, but calling the same endpoint
   with curl works. What is the most likely pipeline misconfiguration?
3. A middleware injects `ITenantContext` (scoped) in its constructor. Describe the
   failure and the exact data-leak scenario it creates in a multi-tenant system.
4. You must exclude `/health` from tenant resolution. Give two mechanisms and say
   which you prefer.
5. An exception is thrown while serialising a large response that has already begun
   streaming. Can your exception handler turn it into a clean 500? What actually
   happens?
6. Where would you put a correlation-id middleware so the id appears in the logs of
   *every* request, including 404s — and what does that imply about its position
   relative to exception handling?
7. At what scale is "one global exception handler" the wrong design?

---

## 10. What this lesson does not settle

| Question | Decided in |
|---|---|
| Where tenant resolution sits | **B1** |
| Rate limiting position and store | **B5** |
| Authentication scheme | **C2 / C3** |
| Result types vs exceptions in the domain | A3 / Phase 2 |
| Correlation id format and propagation | Observability track |

---

## References

- [Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)
- [Middleware order](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/#middleware-order)
- [Write custom middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write)
- [Handle errors](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- [`IExceptionHandler`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.diagnostics.iexceptionhandler)
- [RFC 9457 — Problem Details](https://datatracker.ietf.org/doc/html/rfc9457)
- [Rate limiting middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
