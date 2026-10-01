# Modular monolith

*What "modular" buys that layering does not, and the real triggers for splitting.*

**Phase:** ROADMAP Phase 1 — ASP.NET Core Web API & Architecture
**Target framework:** .NET 10 (ADR-0003)
**Also covers:** CURRICULUM **A5** — modular monolith vs microservices
**Prerequisite:** [04 — Clean / layered architecture](04-clean-and-layered-architecture.md)

> **Study the structure, don't copy it all.** The reference implementation
> ([kgrzybek/modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd))
> is deliberately maximal. Take the boundary enforcement and the integration-event
> pattern; leave anything you cannot justify.
>
> **ADR-0002 is reserved for this decision.** This lesson is the input; the ADR is
> yours to write.

**Contents**

0. [Ten-second recall](#0-ten-second-recall)
1. [Layering is not modularity](#1-layering-is-not-modularity)
2. [What a module is](#2-what-a-module-is)
3. [How modules communicate](#3-how-modules-communicate)
4. [Enforcing boundaries](#4-enforcing-boundaries)
5. [Data ownership](#5-data-ownership)
6. [The real triggers for splitting](#6-the-real-triggers-for-splitting)
7. [The distributed monolith](#7-the-distributed-monolith-the-failure-mode)
8. [**Interview question bank**](#8-interview-question-bank)
9. [Exercises](#9-exercises)
10. [What this lesson does not settle](#10-what-this-lesson-does-not-settle)

---

## 0. Ten-second recall

| Thing | The one sentence that matters |
|---|---|
| Layering | Horizontal slices. Every module sees every other module's layers |
| Modularity | **Vertical** slices with enforced boundaries |
| Module | Owns its data, exposes a contract, hides everything else |
| `Contracts` project | The only thing another module may reference |
| Cross-module calls | Through contracts or integration events — never a direct entity reference |
| Data ownership | One module owns a table. No cross-module joins, no cross-module FKs |
| In-process events | Same transaction, synchronous. Simple, and coupling is still real |
| Integration events | Across the boundary. Eventually consistent by design |
| Split trigger | Team topology, deploy cadence, scaling asymmetry — **not** size |
| Distributed monolith | Services that must deploy together. Worst of both worlds |
| Monolith-first | Correct default: boundaries are cheap to move before a network sits between them |
| The real cost of microservices | Not the code — the operations, the debugging and the data consistency |

---

## 1. Layering is not modularity

A layered monolith slices **horizontally**:

```
┌──────────────── Api ────────────────┐
├──────────── Application ────────────┤
├────────────── Domain ───────────────┤
└─────────── Infrastructure ──────────┘
```

Every feature's code is spread across all four, and — critically — any class in
`Application` can call any other class in `Application`. Nothing stops Billing
reaching into Booking's internals. It will not happen on day one. It happens on
month nine, under deadline, and it is invisible in review because it compiles.

A modular monolith slices **vertically first**, then layers *within* each slice:

```
┌─ Booking ──┐ ┌─ Scheduling ─┐ ┌─ Notifications ─┐
│ Api        │ │ Api          │ │ Api             │
│ Application│ │ Application  │ │ Application     │
│ Domain     │ │ Domain       │ │ Domain          │
│ Infra      │ │ Infra        │ │ Infra           │
│ Contracts  │ │ Contracts    │ │ Contracts       │ ← the only public part
└────────────┘ └──────────────┘ └─────────────────┘
```

> **The one-sentence difference:** layering organises *technical* concerns;
> modularity organises *business* concerns and makes the boundary between them
> enforceable.

Both matter — modules are layered internally. But layering alone does not stop
coupling, and coupling is what makes a codebase expensive to change.

---

## 2. What a module is

A module is a **bounded context made physical**. Three properties:

1. **Owns its data.** Its tables, its schema. No other module reads them directly.
2. **Exposes a contract.** A deliberately small public surface.
3. **Hides everything else.** Entities, repositories, handlers are `internal`.

This project's skeleton:

```
src/Modules/Booking/
  Booking.Domain          internal — entities and rules
  Booking.Application     internal — use cases and ports
  Booking.Infrastructure  internal — EF Core, AddBookingModule()
  Booking.Contracts       PUBLIC   — the only referenced project
```

### Use `internal` deliberately

C#'s `internal` is the enforcement mechanism and it is under-used. If
`Booking.Domain` types are `public`, any project referencing that assembly can
construct a `Booking` bypassing every invariant. Making them `internal` means the
compiler stops it.

`InternalsVisibleTo` for the module's own test project is the standard exception.

---

## 3. How modules communicate

Three mechanisms, in increasing order of decoupling.

### Direct call through a contract interface

```csharp
// Scheduling.Contracts — the port Scheduling publishes
public interface ISlotAvailabilityQuery
{
    Task<bool> IsAvailableAsync(Guid slotId, CancellationToken ct);
}
```

Booking references `Scheduling.Contracts`, injects the interface, calls it.
Synchronous, same transaction, easy to debug. Still real coupling: Booking cannot
function if Scheduling is broken.

**Use when** you need an immediate answer and consistency within one transaction.

### In-process domain/integration events

```csharp
public sealed record BookingConfirmed(Guid BookingId, Guid SlotId, DateTimeOffset OccurredAt);
```

Publisher does not know its subscribers. Still in-process, so it can run in the
same transaction — which is simple but means a subscriber's failure rolls back the
publisher.

**Use when** several modules react to something and the publisher should not care
who.

### Out-of-process messaging (RabbitMQ / Kafka)

Full decoupling and genuine failure isolation, at the cost of eventual consistency,
harder debugging and real operational burden. This arrives in Block E.

**The honest sequencing:** start with contract calls, move to in-process events
where the coupling hurts, and only go out-of-process when you need failure
isolation or independent scaling. Jumping straight to a broker buys complexity
before you have the problem it solves.

---

## 4. Enforcing boundaries

Conventions decay. Pick mechanisms the compiler or CI enforces.

| Mechanism | Strength | Cost |
|---|---|---|
| Separate projects + `internal` | **Compiler-enforced** | Project sprawl |
| Only `*.Contracts` referenced | **Compiler-enforced** | Discipline in `.csproj` review |
| Architecture tests (NetArchTest) | CI-enforced | A test project to maintain |
| Folder conventions | None — documentation | Free, and worthless under deadline |

Architecture tests are worth the hour:

```csharp
[Fact]
public void Booking_internals_are_not_referenced_by_other_modules()
{
    var result = Types.InAssembly(typeof(SchedulingModule).Assembly)
        .Should().NotHaveDependencyOn("Booking.Domain")
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}
```

### The controller-discovery problem

MVC discovers controllers by assembly scanning, so a `public` controller inside a
module joins the API surface whether the module intended it or not
([lesson 01 §1](01-web-api-controllers-routing-model-binding-validation.md#1-what-a-controller-actually-is)).
Two coherent resolutions:

1. **Controllers live in the host**, calling module contracts. API surface is
   reviewable in one place; HTTP concerns stay out of modules.
2. **Controllers live in modules**, registered explicitly via `AddApplicationPart()`.
   Feature code stays together, and the module is closer to extractable.

Option 2 is stronger if you expect to extract services later, which is the usual
direction of travel. Either way, make it a decision rather than a side effect of
scanning.

---

## 5. Data ownership

The rule that makes extraction possible later:

> **One module owns a table. No other module reads or writes it directly.**

That means **no cross-module joins** and **no cross-module foreign keys**. Both are
what quietly make a monolith unsplittable: a foreign key across a boundary is a
database-enforced coupling you cannot remove without a migration.

Options, strongest isolation first: schema per module (`booking.*`,
`scheduling.*`), table prefixes, or separate databases. Schema-per-module in one
database is usually the right balance — enforceable with database permissions,
while still allowing one transaction across modules when you genuinely need it.

**"But I need a join."** That pressure is the design signal. Either the boundary is
wrong and the two things belong together, or the consuming module needs its own
**read model** fed by events. Which answer applies is exactly what A2 decides.

> **Interaction with multi-tenancy:** if the isolation model is schema-per-tenant,
> schema-per-module multiplies schema count (tenants × modules). Decide A6 before
> committing to a schema strategy — these two choices are not independent.

---

## 6. The real triggers for splitting

**Not** triggers: the codebase feels big · microservices are modern · we want
Kubernetes · the team read a blog post.

**Actual triggers:**

| Trigger | Why it genuinely forces a split |
|---|---|
| **Team topology** | Separate teams blocking each other on one deploy pipeline. Conway's law — the split is organisational |
| **Deploy cadence** | One part needs hourly deploys, another is regulated and ships monthly |
| **Scaling asymmetry** | One component needs 20× the resources. Scaling the whole monolith wastes money |
| **Failure isolation** | A non-critical component's failure must not take down booking |
| **Technology need** | A component genuinely needs a different runtime |
| **Compliance** | Data residency or audit scope that must be physically separated |

Each is about **organisation or operations**, never about code aesthetics.

### Monolith-first is the defensible default

Boundaries are guesses at the start. Inside a monolith a wrong boundary costs a
refactor; across services it costs a migration, a versioned API and a data move.
Get the boundaries right first where they are cheap to change — then extract.

A well-built modular monolith makes extraction mechanical: the module already owns
its data, already communicates through contracts, already has no cross-boundary
joins. That is the argument for doing this work now even if you never split.

---

## 7. The distributed monolith — the failure mode

Services that must be deployed together. You pay every microservices cost and
receive no benefit:

**Symptoms:** services deployed in lockstep · a change requiring edits in three
repos · synchronous call chains where one slow service times out all of them ·
shared database between services · a schema change breaking several services.

**Root causes:** splitting on technical lines (`OrderApi`, `OrderDb`, `OrderWorker`)
rather than business capability; extracting before boundaries were understood;
keeping a shared database after the split.

This is why boundary work precedes distribution. The monolith is not the risk — the
*wrong boundaries* are, and distribution makes them permanent.

---

## 8. Interview question bank

**Frequency:** 🔴 near-certain · 🟠 common · 🟡 senior/architecture rounds.

> Grouped by how often the *shape* recurs in .NET hiring loops, not attributed to
> named employers.

---

### 🔴 Q1. Monolith, modular monolith, or microservices — which and why?

Nearly guaranteed in any senior/architect loop. They are testing judgement, not
preference.

- **Weak:** "Microservices scale better."
- **Strong:** Default to a modular monolith and justify by cost of being wrong.
  Boundaries at the start are guesses; inside a monolith a wrong boundary is a
  refactor, across services it is a migration plus a versioned API plus a data
  move. A modular monolith gets the boundary discipline — owned data, contract-only
  communication, no cross-boundary joins — while keeping one deploy, one
  transaction and one debugger. Extract when a *specific* trigger appears.
- **Follow-up:** *"What are those triggers?"* — Team topology, deploy cadence,
  scaling asymmetry, failure isolation, compliance. All organisational or
  operational. "It feels big" is not one.

---

### 🔴 Q2. What makes a monolith "modular"? Isn't that just layering?

- **Strong:** Layering is horizontal and does not prevent coupling — any class in
  the application layer can call any other, so Billing reaches into Booking's
  internals eventually, and it compiles so review misses it. Modularity is vertical
  with **enforced** boundaries: each module owns its data, exposes a small contract,
  and keeps the rest `internal` so the compiler rejects violations.
- **Follow-up:** *"How do you actually enforce it?"* — Separate projects plus
  `internal`, so only `*.Contracts` is referenceable; plus architecture tests in CI
  asserting no module references another's internals. Folder conventions enforce
  nothing.

---

### 🟠 Q3. How do modules communicate without coupling?

- **Strong:** Three mechanisms with increasing decoupling — contract interfaces
  (synchronous, same transaction, easy to debug, still coupled in availability);
  in-process events (publisher ignorant of subscribers, still one transaction);
  out-of-process messaging (true failure isolation, at the cost of eventual
  consistency and operational burden). Start with the first, escalate when the
  coupling actually hurts.
- **Follow-up:** *"Why not use a message broker everywhere from day one?"* —
  Because you pay eventual consistency, harder debugging and operational cost
  before you have the problem that justifies them. In-process is simpler and the
  migration path is open when a real need appears.

---

### 🟠 Q4. Can modules share a database?

- **Strong:** Share an *instance*, not *tables*. One module owns a table and no
  other module reads it directly. Schema-per-module is usually the right balance —
  enforceable via permissions while still permitting a single transaction when you
  genuinely need one.
- **Follow-up:** *"What about joins across modules?"* — Do not. A cross-module join
  or foreign key is database-enforced coupling that makes extraction impossible
  without a migration. If you need the data, either the boundary is wrong, or the
  consumer needs a read model fed by events. Needing a join is a *signal about the
  boundary*, not a reason to break it.

---

### 🟡 Q5. What is a distributed monolith and how do you avoid one?

- **Strong:** Services that must deploy together — every microservices cost, none
  of the benefits. Symptoms: lockstep deploys, one change spanning three repos,
  synchronous chains that fail together, a shared database. Causes: splitting on
  technical rather than business lines, extracting before boundaries are understood,
  keeping the shared database. Avoided by getting boundaries right in a monolith
  first, where they are cheap to move.

---

### 🟡 Q6. How would you extract a module into a service?

- **Strong:** The work is mostly already done if the module was built properly.
  Sequence: confirm it owns its data with no cross-boundary joins or FKs; replace
  in-process contract calls with a network API or messaging; move its tables;
  handle the consistency change, since what was one transaction is now two with
  eventual consistency between them; add the operational surface — deployment,
  monitoring, tracing. The hardest part is **not** the code, it is the consistency
  model, because you lose the single transaction.
- **Follow-up:** *"What breaks first?"* — Anything that relied on one transaction
  across the boundary. That is where the outbox pattern and sagas enter, which is
  Block E.

---

### 🟡 Q7. Conway's law — how does it affect this?

- **Strong:** Systems mirror the communication structure of the organisation that
  builds them. So a service split that does not match team boundaries produces
  services needing constant cross-team coordination — the distributed monolith
  again, arrived at organisationally. The inverse manoeuvre: decide the
  architecture you want, then shape teams to match. For a solo developer, team
  topology provides **no** split trigger at all, which is itself a strong argument
  for the monolith here.

---

### Questions to ask back

- "Are module boundaries compiler-enforced, or convention?"
- "Do any foreign keys cross a module boundary?"
- "If you have services — can any be deployed independently today?"

---

## 9. Exercises

1. Billing needs a practitioner's name, owned by Scheduling. Give three ways to get
   it and the trade-off of each.
2. A foreign key exists from `booking.Bookings` to `scheduling.Slots`. What does
   this prevent, and what would you do instead?
3. All module entities are `public`. What specifically can go wrong, and what is
   the fix?
4. Your module exposes a controller. Does the host want it discovered by scanning?
   Argue both resolutions.
5. You are solo. Which split triggers apply to you today? What would have to change?
6. A reviewer says "just use microservices, it's a booking system for multiple
   tenants." Rebut it in three sentences.
7. At what point does this project genuinely justify extracting a service — name
   the component and the trigger.

---

## 10. What this lesson does not settle

| Question | Decided in |
|---|---|
| **What the modules actually are** — bounded contexts | **A2** |
| Is Scheduling separate from Booking? | **A2** |
| Multi-tenancy isolation model (interacts with schema-per-module) | **A6** |
| Which messaging technology, and where | **E4** |
| The monolith-vs-microservices decision for this project | **ADR-0002** — yours to write |

---

## References

- [kgrzybek/modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd) — the reference implementation
- [MonolithFirst — Martin Fowler](https://martinfowler.com/bliki/MonolithFirst.html)
- [Microservice trade-offs — Martin Fowler](https://martinfowler.com/articles/microservice-trade-offs.html)
- [.NET microservices architecture](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/)
- [NetArchTest](https://github.com/BenMorris/NetArchTest)
