# Health checks

*Liveness, readiness, and how a wrong probe turns a blip into an outage.*

**Phase:** ROADMAP Phase 1 — ASP.NET Core Web API & Architecture
**Target framework:** .NET 10 (ADR-0003)
**Related:** [01 — Controllers §9](01-web-api-controllers-routing-model-binding-validation.md#9-health-endpoints--what-health-actually-asserts)
introduced this; here it is in full.

**Contents**

0. [Ten-second recall](#0-ten-second-recall)
1. [Three probes, three questions](#1-three-probes-three-questions)
2. [The crash-loop failure](#2-the-crash-loop-failure)
3. [Registering and exposing checks](#3-registering-and-exposing-checks)
4. [Writing a good check](#4-writing-a-good-check)
5. [Degraded, and what to do with it](#5-degraded-and-what-to-do-with-it)
6. [Security and multi-tenancy](#6-security-and-multi-tenancy)
7. [**Interview question bank**](#7-interview-question-bank)
8. [Exercises](#8-exercises)
9. [What this lesson does not settle](#9-what-this-lesson-does-not-settle)

---

## 0. Ten-second recall

| Thing | The one sentence that matters |
|---|---|
| Liveness | "Should I be killed?" — must **not** check dependencies |
| Readiness | "Should I get traffic?" — checks dependencies, no restart on failure |
| Startup | "Have I finished booting?" — holds liveness off during slow init |
| Liveness checking the DB | Converts a dependency blip into a cluster-wide crash loop |
| Tags + predicates | One registration set, several endpoints filtering differently |
| `Degraded` | Healthy enough for traffic; alert, do not remove from the pool |
| Readiness on a hard dependency | Correct |
| Readiness on a soft dependency | Wrong — you remove yourself over a non-essential service |
| Timeouts | A check without one can hang and make the probe itself the outage |
| Probe cost | Runs every few seconds per instance. Expensive checks are self-inflicted load |
| Detailed output | Reconnaissance if public. Terse liveness, restricted readiness |
| Bypass tenant resolution | A probe has no tenant |

---

## 1. Three probes, three questions

| Probe | Question | Checks dependencies? | On failure |
|---|---|---|---|
| **Liveness** | Is the process broken beyond recovery? | **No** | Container killed and restarted |
| **Readiness** | Can I serve traffic right now? | **Yes** | Removed from the load balancer, **no restart** |
| **Startup** | Has initialisation finished? | Sometimes | Liveness held off until it passes |

The distinction is **what the orchestrator does with the answer**. Liveness means
"restart me"; readiness means "stop sending me traffic". Conflating them means
asking for a restart when you meant "wait".

**Liveness should check almost nothing.** Deadlock, a corrupted process, an
exhausted thread pool — conditions a restart genuinely fixes. For most services
returning `Healthy` unconditionally is correct and honest: if the process can answer
the HTTP request, the process is alive.

**Readiness checks hard dependencies** — the ones without which the service cannot
do its job. The database, usually. Not every dependency (§4).

**Startup probes** exist because a slow boot otherwise trips liveness and restarts
the app mid-initialisation, forever. Relevant with migrations or cache warming at
startup.

---

## 2. The crash-loop failure

The story worth being able to tell, because it is the whole reason the distinction
matters:

1. Liveness is wired to check the database — it seems thorough
2. The database fails over, unavailable for 30 seconds
3. **Every** instance fails liveness simultaneously
4. The orchestrator kills and restarts all of them
5. Restarting does not fix a database
6. New instances start, fail liveness, get killed → `CrashLoopBackOff`
7. Meanwhile caches are cold and in-flight requests were dropped

A 30-second recoverable blip became a total outage with a slow recovery, caused
entirely by the probe. With liveness correct, those instances would have stayed up,
failed readiness, left the load balancer, and returned automatically when the
database came back.

> **The rule:** liveness answers a question only a restart can fix. If a restart
> would not help, it does not belong in liveness.

---

## 3. Registering and exposing checks

Tags plus predicates — one set of registrations, several endpoints:

```csharp
builder.Services.AddHealthChecks()
    // Liveness: no dependencies. If we can answer, we are alive.
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])

    // Readiness: the hard dependencies only.
    .AddNpgSql(connectionString, name: "database", tags: ["ready"])
    .AddRedis(redisConnection, name: "cache", tags: ["ready"]);

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

`Predicate = _ => false` runs **no** checks — the cheapest possible liveness
endpoint, useful when you want the probe to assert only that the process responds.

### Status to HTTP mapping

| `HealthStatus` | Default HTTP |
|---|---|
| `Healthy` | 200 |
| `Degraded` | **200** |
| `Unhealthy` | 503 |

`Degraded` returning 200 is deliberate — degraded still serves traffic. Override
with `ResultStatusCodes` if you need different semantics.

### Why not a controller?

`MapHealthChecks` already aggregates results, applies timeouts and handles status
mapping. A `HealthController` re-implements that and drifts. Write one only when
you need a response shape the middleware cannot produce — and know that is what you
are trading for.

---

## 4. Writing a good check

```csharp
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly BookingDbContext _db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database unreachable", ex);
        }
    }
}
```

**Rules that matter in production:**

**Cheap.** It runs every few seconds on every instance. A check doing real queries
across ten instances at 5-second intervals is a self-inflicted load problem — and
the load arrives exactly when the system is already struggling.

**Timeout-bounded.** A check that hangs makes the probe hang, which the orchestrator
reads as failure. `AddCheck(...).WithTimeout()` or honour the token.

**Never throw.** Catch and return `Unhealthy`. An unhandled exception gives a 500
rather than a structured result.

**Hard dependencies only, for readiness.** If the service can still do its primary
job without something, it is not a readiness dependency. An email provider being
down should not remove your booking API from the load balancer — bookings still
work, emails queue. Put soft dependencies in a separate monitoring-only endpoint or
report them as `Degraded`.

> **The question that settles it:** *"If this dependency is down, should we refuse
> all traffic?"* If no, it does not belong in readiness.

### Check the right thing

`SELECT 1` proves connectivity and nothing else. It does not prove the connection
pool has capacity, that migrations ran, or that the schema matches. Deeper checks
catch more but cost more and risk false alarms. Connectivity is usually the right
default; add a migration-state check at startup rather than per probe.

---

## 5. `Degraded`, and what to do with it

The underused third state: working, but not fully.

Good uses: a read replica is down so reads hit the primary; a cache is unavailable
so you are serving from the database more slowly; a non-critical integration is
failing.

By default `Degraded` returns 200, so the instance keeps traffic — which is right.
Its value is **alerting**: it distinguishes "page someone now" from "look at this
tomorrow". Without it every problem is binary and you either over-page or miss
real degradation.

---

## 6. Security and multi-tenancy

### Detailed output is reconnaissance

The default `/health` response is terse. A detailed writer that enumerates every
dependency and its error message tells an attacker your database engine, your cache,
your internal hostnames, sometimes versions.

Practical split: liveness public and terse; readiness detail restricted — separate
port, internal network only, or authenticated.

```csharp
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = c => c.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse   // detailed: restrict it
});
```

### Probes must bypass tenant resolution

A probe carries no tenant — no subdomain, no JWT, no header. Tenant-resolution
middleware that rejects unresolvable requests will 400 your health endpoint, and the
orchestrator will read that as an unhealthy instance.

Two mechanisms: path check inside the middleware, or `UseWhen` to branch around it
([lesson 02 §2](02-middleware-pipeline-and-error-handling.md#2-run-use-map--and-the-terminal-distinction)).
The branch is cleaner — the exclusion is visible in `Program.cs` rather than buried
in middleware.

**Do not add per-tenant health checks to the main endpoints.** With a thousand
tenants a probe would do a thousand checks every few seconds. Tenant-level health
is a monitoring concern, not a probe concern.

---

## 7. Interview question bank

**Frequency:** 🔴 near-certain in container-shop interviews · 🟠 common · 🟡 senior.

> Grouped by how often the *shape* recurs, not attributed to named employers.
> This topic is far more common now that most teams deploy containers.

---

### 🔴 Q1. Liveness vs readiness — what is the difference?

- **Weak:** "Liveness checks if it's running, readiness checks if it's ready."
- **Strong:** The difference is what the orchestrator *does*. Liveness failing
  means kill and restart, so it must only check conditions a restart fixes, which
  means no external dependencies. Readiness failing means stop routing traffic here
  with no restart, so it checks hard dependencies. One says "restart me", the other
  says "wait".
- **Follow-up:** *"What happens if liveness checks the database?"* — Tell the crash
  loop story (§2): DB fails over, all instances fail liveness at once, all restart,
  restarting does not fix a database, `CrashLoopBackOff`, cold caches. You converted
  a 30-second blip into a full outage. The narrative lands far better than the rule.

---

### 🟠 Q2. What would you put in a readiness check?

- **Strong:** Hard dependencies only — the ones without which the service cannot do
  its primary job, typically the database. The test question: *"if this is down,
  should we refuse all traffic?"* If no, it does not belong. An email provider
  failing should not remove a booking API from the load balancer; bookings still
  work and emails queue.
- **Follow-up:** *"How do you surface soft dependencies then?"* — `Degraded`, which
  returns 200 by default so traffic continues, plus a monitoring-only endpoint. It
  separates "page now" from "look tomorrow".

---

### 🟠 Q3. How do you implement multiple health endpoints in ASP.NET Core?

- **Strong:** Register checks once with **tags**, then map several endpoints with
  **predicates** filtering by tag. `Predicate = _ => false` runs no checks at all,
  which is the cheapest liveness. Mention status mapping: `Healthy` and `Degraded`
  both 200, `Unhealthy` 503.

---

### 🟡 Q4. What are the risks of a health endpoint?

- **Strong:** Three. Information disclosure — detailed output enumerates your
  dependencies, engines and internal hostnames, which is reconnaissance. Cost — it
  runs every few seconds per instance, so expensive checks add load exactly when
  the system is struggling. Hangs — a check without a timeout makes the probe hang,
  which reads as failure, so the check causes the outage it was meant to detect.
- **Follow-up:** *"So how do you expose it safely?"* — Terse public liveness;
  detailed readiness on an internal port or behind auth.

---

### 🟡 Q5. In a multi-tenant system, do you health-check per tenant?

A good discriminator — the naive answer scales terribly.

- **Strong:** Not in the probe. With a thousand tenants, a probe running a check per
  tenant every few seconds is a self-inflicted load problem, and one broken tenant
  would mark the whole instance unhealthy and pull it from the pool — turning a
  single-tenant issue into an outage for everyone. Tenant-level health is a
  monitoring and alerting concern, not an orchestrator probe.
- **Follow-up:** *"What about tenant resolution middleware?"* — Health endpoints
  must bypass it: a probe has no tenant, so middleware that rejects unresolvable
  requests would fail the probe and get the instance killed.

---

### 🟡 Q6. Where do health checks fit in the middleware pipeline?

- **Strong:** Early, and before tenant resolution and authentication, so probes stay
  cheap and do not depend on subsystems they are not testing. The trade-off is that
  an early endpoint is unauthenticated by default, which is fine for terse liveness
  and not for detailed readiness.

---

### Questions to ask back

- "Does your liveness probe check any external dependency?"
- "What actually happens when readiness fails — restart, or deregister?"
- "Is detailed health output reachable from outside the cluster?"

---

## 8. Exercises

1. Liveness runs `SELECT 1`. Walk through exactly what happens during a 30-second
   database failover across six instances.
2. Which of these belong in readiness: primary database · Redis cache · SMS
   provider · payment gateway · message broker? Justify each.
3. Write the tag/predicate registration for separate liveness and readiness
   endpoints.
4. Your tenant-resolution middleware 400s requests with no resolvable tenant. What
   happens to the probe, and give two fixes.
5. A health check occasionally takes 30 seconds. What goes wrong, and what do you
   add?
6. When is `Degraded` the right status rather than `Unhealthy`? Give an example
   from this system.
7. At what scale does per-tenant health checking become actively harmful?

---

## 9. What this lesson does not settle

| Question | Decided in |
|---|---|
| Which dependencies this system actually has | Phase 2+ |
| Where tenant resolution sits, hence what probes must bypass | **B1** |
| Orchestrator (App Service / Container Apps / AKS) and its probe semantics | **F3** |
| Alerting thresholds and on-call policy | Observability track |

---

## References

- [Health checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [AspNetCore.Diagnostics.HealthChecks](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks) — the community check packages
- [Kubernetes: configure liveness, readiness and startup probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)
- [Azure Container Apps health probes](https://learn.microsoft.com/en-us/azure/container-apps/health-probes)
