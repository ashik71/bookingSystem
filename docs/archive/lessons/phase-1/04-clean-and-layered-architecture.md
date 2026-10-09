# Clean / layered architecture

*The dependency rule, what each layer owns, and where the whole thing is overkill.*

**Phase:** ROADMAP Phase 1 — ASP.NET Core Web API & Architecture
**Target framework:** .NET 10 (ADR-0003)
**Also covers:** CURRICULUM **A4** — "Clean Architecture, honestly"
**Prerequisite:** [03 — Dependency injection](03-dependency-injection.md) §6

**Contents**

0. [Ten-second recall](#0-ten-second-recall)
1. [The dependency rule](#1-the-dependency-rule)
2. [The four layers in this project](#2-the-four-layers-in-this-project)
3. [Ports and adapters](#3-ports-and-adapters)
4. [Use cases](#4-use-cases-the-application-layer)
5. [Where it is overkill](#5-where-it-is-overkill--and-having-the-nerve-to-say-so)
6. [Common mistakes](#6-common-mistakes)
7. [**Interview question bank**](#7-interview-question-bank)
8. [Exercises](#8-exercises)
9. [What this lesson does not settle](#9-what-this-lesson-does-not-settle)

---

## 0. Ten-second recall

| Thing | The one sentence that matters |
|---|---|
| The dependency rule | Source-code dependencies point **inward**, always |
| Domain | No dependencies. Not even on the framework |
| Application | Defines ports, orchestrates use cases, depends on Domain |
| Infrastructure | Implements ports. Depends inward, never referenced by Application |
| Contracts | The module's **public** surface, for other modules |
| Dependency inversion | The interface lives with the **caller**, not the implementer |
| Control flow vs dependency | Flow goes outward at runtime; dependencies point inward at compile time |
| Entities vs DTOs | Separate. Crossing a boundary means mapping |
| The real test | Can you swap the database without touching Application? |
| Overkill signal | CRUD with no invariants — the layers add indirection, not safety |
| Anemic domain | Entities with only getters/setters; rules leaked into services |
| Clean ≠ folders | Four folders with EF Core types in Domain is layering theatre |

---

## 1. The dependency rule

> **Source-code dependencies point only inward.** Nothing in an inner circle knows
> anything about an outer one.

Everything else in Clean Architecture is a consequence of this one rule. The reason
it matters is not elegance — it is that **the inner layers become testable and
reusable without the outer ones**. Domain logic that depends on `DbContext` cannot
be tested without a database, and cannot be invoked from a message consumer without
dragging HTTP concerns along.

### Control flow is not dependency direction

The distinction interviewers probe:

```
Runtime control flow:   HTTP → Controller → UseCase → Repository → Database
Compile-time deps:      Api  → Application ← Infrastructure
                                    ↑
                              (Application defines ISlotRepository;
                               Infrastructure implements it)
```

Control flows **outward** to the database. Source dependencies point **inward**.
The inversion is what makes both true at once — and DI is the mechanism
([lesson 03 §6](03-dependency-injection.md#6-where-di-meets-clean-architecture)).

---

## 2. The four layers in this project

The module skeleton already has these as separate **projects**, which is stronger
than folders: the compiler enforces the rule rather than a code review.

```
Booking.Domain          ← entities, value objects, domain events, invariants
Booking.Application     ← use cases, ports (interfaces), orchestration
Booking.Infrastructure  ← EF Core, repositories, external services, AddBookingModule()
Booking.Contracts       ← what OTHER modules may reference
```

| Layer | Contains | Must **not** contain |
|---|---|---|
| **Domain** | Entities, value objects, aggregates, domain events, invariants, domain exceptions | EF Core attributes, `DbContext`, HTTP types, any NuGet framework reference |
| **Application** | Use-case handlers, port interfaces, DTOs, validation of *intent* | SQL, `HttpContext`, concrete infrastructure |
| **Infrastructure** | EF Core mappings, repository implementations, message publishers, email/SMS clients, DI registration | Business rules |
| **Contracts** | Integration events, cross-module interfaces, shared read DTOs | Entities, internals |

### Why `Contracts` is a separate project

It is the module's **published surface**. Other modules reference `Booking.Contracts`
and nothing else — so a cross-module dependency cannot reach an entity or a
repository even by accident. The compiler enforces the module boundary.

This is the structural difference between a modular monolith and a layered monolith:
in a layered monolith every module can see every other module's internals, and over
time it does.

### The test that proves Domain is clean

Open `Booking.Domain.csproj`. If it has **any** `PackageReference`, justify each
one. A domain project typically needs none. The moment EF Core appears there,
persistence concerns have leaked into your model and `[Key]`/`[Required]` attributes
start dictating domain design.

---

## 3. Ports and adapters

Clean Architecture and Hexagonal (Ports & Adapters) are the same idea with different
vocabulary.

- A **port** is an interface owned by the inner layer, expressed in *its* language
- An **adapter** is an outer-layer implementation of that port

```csharp
// Application layer — the PORT. Note the vocabulary is domain, not database.
public interface ISlotRepository
{
    Task<Slot?> FindAsync(SlotId id, CancellationToken ct);
    Task<IReadOnlyList<Slot>> FindAvailableAsync(PractitionerId id, DateRange range, CancellationToken ct);
}

// Infrastructure layer — the ADAPTER
internal sealed class EfSlotRepository : ISlotRepository { … }
```

**Driving vs driven:**

| Kind | Who initiates | Examples |
|---|---|---|
| **Driving** (primary) | The outside calls in | Controller, message consumer, CLI, scheduled job |
| **Driven** (secondary) | The app calls out | Repository, email sender, payment gateway, clock |

The payoff is concrete: a use case with driving adapters for both HTTP **and** a
RabbitMQ consumer is the same code invoked two ways. That only works if the use
case knows about neither — which is the whole point when Block E adds workers.

### The leaky-port smell

```csharp
IQueryable<Slot> Query();                       // leaks EF Core semantics
Task<Slot> FindAsync(string sql);               // leaks SQL
Task SaveAsync(Slot slot, DbTransaction tx);    // leaks the provider
```

If the port's signature changes when you change database, it is not a port. An
`IQueryable` return type is the most common version: the caller now composes
provider-specific expression trees, and swapping providers breaks callers.

---

## 4. Use cases (the Application layer)

A use case is **one** thing the system does: `BookSlot`, `CancelBooking`,
`PublishWeeklySlots`. It orchestrates; it does not contain business rules.

```csharp
public sealed class BookSlotHandler
{
    public async Task<Result<BookingId>> HandleAsync(BookSlotCommand cmd, CancellationToken ct)
    {
        var slot = await _slots.FindAsync(cmd.SlotId, ct);
        if (slot is null) return Result.NotFound();

        var booking = slot.Book(cmd.ClientId, _clock.UtcNow);   // ← the RULE lives in the domain
        if (booking.IsFailure) return booking.Error;

        await _bookings.AddAsync(booking.Value, ct);
        await _unitOfWork.CommitAsync(ct);
        return booking.Value.Id;
    }
}
```

**The line to hold:** "can this slot be booked?" belongs to `Slot`. "Load it, call
it, save it, publish the event" belongs to the handler. When that rule migrates into
the handler, you get an **anemic domain model** — entities reduced to property bags
with all behaviour in services. Technically functional, and it throws away the main
benefit of DDD: rules enforced in one place, impossible to bypass.

### Does this need MediatR?

Not necessarily, and it is worth being able to say so. MediatR gives you a uniform
handler shape and a behaviour pipeline (validation, logging, transactions) — genuine
value when there are many handlers. The cost is indirection: `Send()` obscures which
handler runs, and navigation goes through a dispatcher. For a handful of use cases,
injecting handlers directly is simpler and more honest.

---

## 5. Where it is overkill — and having the nerve to say so

The curriculum titles this topic "Clean Architecture, **honestly**" for a reason.
Being able to argue *against* the pattern is a stronger senior signal than defending
it everywhere.

**It is overkill when:**

- The operation is genuine CRUD with no invariants. A use case that loads an entity,
  sets two properties and saves is pure ceremony — four files to express `UPDATE`.
- The application is small and short-lived, where the layers cost more than they
  return.
- You are mapping the same shape four times (entity → domain → DTO → response) with
  no transformation. Each mapping is a place to introduce a bug and nothing else.
- There is exactly one adapter and there will never be another. The abstraction's
  value is substitutability; with no substitute it is just indirection.

**It earns its place when:**

- There are **invariants that must hold regardless of entry point** — HTTP, message
  consumer, scheduled job, admin tool. This project's double-booking rule is exactly
  that, which is why the layering is justified here.
- Business rules are complex enough to be worth testing without infrastructure.
- Multiple adapters genuinely exist: this system will have HTTP and at least one
  messaging consumer.

**The honest compromise most teams land on:** full layering for modules with real
invariants (Booking, Scheduling), thin or no layering for reference-data CRUD
(lookup tables, tenant settings). Mixing deliberately is a judgement call, not
inconsistency — but document it, or the next developer will "fix" it.

> **The scale question:** at what scale is Clean Architecture wrong? Below roughly
> one developer and a few weeks of lifetime, the indirection costs more than it
> saves. Above a few teams, layering alone is insufficient — you need module
> boundaries too, which is the next lesson.

---

## 6. Common mistakes

| Mistake | Why it breaks the rule |
|---|---|
| EF Core attributes on domain entities | Domain now depends on a persistence library; `[Key]` shapes your model |
| Returning entities from controllers | Couples your API contract to your schema; a column rename is a breaking API change. Also mass assignment |
| `IQueryable<T>` from a repository | Leaks provider semantics; the caller builds provider-specific queries |
| Application referencing Infrastructure | Dependency now points outward — the rule is broken outright |
| A `Common`/`Shared` project everything references | Becomes a dumping ground and a hidden coupling hub |
| One repository per table | Repositories belong to **aggregates**, not tables |
| Mapping with no transformation | Four identical shapes; delete the layers that add nothing |
| Folders named for layers, no enforcement | Nothing stops a reference; the compiler must enforce it |

On the last: this project already uses separate **projects**, so a wrong reference
fails the build. That is the main reason to prefer projects over folders.

---

## 7. Interview question bank

**Frequency:** 🔴 near-certain · 🟠 common · 🟡 senior rounds.

> Grouped by how often the *shape* recurs in .NET hiring loops, not attributed to
> named employers.

---

### 🔴 Q1. Explain Clean Architecture / the dependency rule.

- **Weak:** Reciting the four circles from the diagram.
- **Strong:** One rule — source-code dependencies point inward — and the reason:
  inner layers become testable and reusable without outer ones. Then the inversion:
  the Application layer *defines* the repository interface and Infrastructure
  implements it, so control flows outward at runtime while dependencies point inward
  at compile time.
- **Follow-up:** *"How do you enforce it?"* — Separate projects, so a wrong
  reference fails compilation. Optionally architecture tests (NetArchTest,
  ArchUnitNET) asserting that Domain references nothing. Folders enforce nothing.

---

### 🔴 Q2. Where does business logic go?

- **Strong:** Rules that are always true about an entity belong **in** the entity or
  its value objects. Orchestration — load, invoke, persist, publish — belongs in the
  use-case handler. The test is whether a rule can be bypassed: if a caller can set
  properties into an invalid state, the rule is in the wrong place.
- **Follow-up:** *"What is an anemic domain model and is it always wrong?"* —
  Entities with only getters and setters, all behaviour in services. Not always
  wrong: for genuine CRUD it is fine and simpler. It is wrong when there are
  invariants, because now they live in several services and the next caller forgets
  one.

---

### 🟠 Q3. Why does the Application layer define the repository interface?

- **Strong:** Dependency inversion — the interface belongs with the **consumer**, in
  the consumer's vocabulary. If Infrastructure owned it, Application would have to
  reference Infrastructure and the rule inverts. Placing it in Application also
  means the signature is expressed in domain terms, not storage terms.
- **Follow-up:** *"Isn't that an unnecessary abstraction over `DbSet`?"* — Often,
  yes, and say so. A repository that forwards to `DbSet` with no added semantics is
  a pointless wrapper; `DbContext` is already a unit of work with `IQueryable`. It
  earns its place when it expresses aggregate-level operations and keeps query
  construction out of the application layer. Being able to argue both sides is the
  actual test here.

---

### 🟠 Q4. How do you keep entities out of your API contract?

- **Strong:** Separate request and response DTOs at the boundary, mapped explicitly.
  Two reasons, one design and one security: your schema stops being your public
  contract, so a column rename is not a breaking change; and binding to entities is
  mass assignment, which in a multi-tenant system means a bindable `TenantId` is a
  cross-tenant write primitive.

---

### 🟡 Q5. When is Clean Architecture the wrong choice?

The question that separates people who have shipped from people who have read.

- **Strong:** When there are no invariants to protect. CRUD over reference data
  gains nothing from four layers and four mappings — it adds files and bug sites.
  Also when there is exactly one adapter and never will be another, since the value
  of a port is substitutability. The trigger for full layering is **invariants that
  must hold regardless of entry point**, which is precisely the case once a message
  consumer enforces the same rules as HTTP.
- **Follow-up:** *"So would you mix approaches in one codebase?"* — Yes,
  deliberately: full layering for modules with real rules, thin for reference data.
  Then document the rule, or the next person "fixes" the inconsistency.

---

### 🟡 Q6. Clean Architecture vs Hexagonal vs Onion?

- **Strong:** Substantially the same idea with different vocabulary and emphasis.
  Hexagonal (Cockburn) centres ports and adapters and is symmetric about driving
  versus driven sides. Onion (Palermo) emphasises concentric layers around a domain
  core. Clean (Martin) adds the use-case layer explicitly and names the dependency
  rule. All three state: dependencies point inward, infrastructure is a detail.
  Treating them as rival frameworks is a misread.

---

### 🟡 Q7. How do you test each layer?

- **Strong:** Domain — plain unit tests, no mocks needed, because it has no
  dependencies; that is the payoff of the rule. Application — unit tests with test
  doubles for ports, or in-memory fakes which are usually better than mocks.
  Infrastructure — integration tests against the real engine via Testcontainers,
  because an in-memory provider does not reproduce the SQL semantics you care about.
  Api — `WebApplicationFactory` for routing, binding, filters and auth.
- **Follow-up:** *"Why not the EF in-memory provider?"* — It does not enforce
  relational constraints, does not reproduce concurrency behaviour, and has
  different translation semantics. Tests pass that production would fail — and for
  this project, double-booking correctness depends entirely on real database
  behaviour.

---

### Questions to ask back

- "Where do your business rules live today — entities or services?"
- "Is the dependency rule compiler-enforced, or convention?"
- "Do your integration tests run against the real database engine?"

---

## 8. Exercises

1. `Booking.Domain.csproj` gains a `PackageReference` to EF Core. Name two concrete
   problems this creates.
2. A repository returns `IQueryable<Slot>`. Why is that a leaky port, and what
   breaks when you change provider?
3. Your `BookSlotHandler` contains `if (slot.IsBooked) return Error(...)`. Is that
   rule in the right place? What is the test?
4. The same use case must be callable from HTTP and from a RabbitMQ consumer. What
   must be true of the handler?
5. A module needs data owned by another module. Which project does it reference,
   and what stops it reaching an entity?
6. Give a concrete example *in this system* where Clean Architecture would be
   overkill, and defend cutting it.
7. At what scale does layering alone stop being enough?

---

## 9. What this lesson does not settle

| Question | Decided in |
|---|---|
| Whether a repository abstraction earns its place here | **A7** / Phase 2 |
| Aggregate boundaries — is `Slot` inside `Booking`? | **A3** |
| Bounded contexts — is Scheduling separate from Booking? | **A2** |
| Modular monolith vs microservices | **A5** / lesson 05 |
| MediatR or direct handler injection | Open; decide with the first real use case |

---

## References

- [Common web application architectures](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [DDD/CQRS patterns](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/)
- [The Clean Architecture — Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Hexagonal Architecture — Alistair Cockburn](https://alistair.cockburn.us/hexagonal-architecture/)
- [NetArchTest](https://github.com/BenMorris/NetArchTest) — architecture tests
