---
type: input
for: bmad-architecture (architecture-slotbook)
source: the developer's reference architecture (an external repo; patterns only)
created: 2026-10-10
---

# Input — patterns from the developer's reference architecture

The developer wants SlotBook's backend to follow their reference architecture. It is a
production .NET 10 service built with Clean Architecture and an event-driven design on
MongoDB and a message broker. This file lists its **generic patterns** so the
architecture run can adopt, adapt or reject each one.

**Firewall.** The reference is private code that belongs to someone else. Nothing here
copies its code, file names, type names, package names, product or company names. Its
shared libraries are company-private, so for every pattern that relies on one, SlotBook
must **build its own equivalent** (see *What SlotBook must build itself* at the end).

**Precedence.** ADR-0006 (monorepo layout, xUnit v3 + Shouldly on MTP, Angular +
Vitest) and ADR-0007 (code style and analyzers) win wherever the reference differs.

Each pattern is tagged with a verdict for the architecture run:

- **Adopt:** take it as is.
- **Adapt:** take the idea and reshape it for SlotBook.
- **Skip:** it doesn't fit SlotBook.
- **Decide:** the architecture run has to choose.

---

## P-1 Four layers, enforced twice — Adopt

- Projects: **Domain** has no project references, only its own base types.
  **Application** references Domain and the event-contracts project. **Infrastructure**
  references Application. **Api** references Infrastructure. A separate
  **event-contracts** project holds the events the service publishes.
- The **compiler** enforces the rule through project references.
- **Architecture fitness tests** (NetArchTest) also enforce it:
  - Domain depends on no database driver, no broker client, no Application and no
    Infrastructure;
  - Application doesn't depend on Infrastructure;
  - the broker client library is referenced only from Infrastructure's messaging
    namespaces.
- Merging Application and Infrastructure into one "Features" project was considered
  and rejected. It would drop compiler enforcement and leave only a naming convention,
  and naming conventions drift.

**SlotBook fit:** this matches the decided blueprint. It goes under `src/backend`, with
tests under `src/backend/tests` (ADR-0006). MassTransit types stay in Infrastructure
(initiative Notes), and a fitness test can check that.

## P-2 Aligned feature folders — Adopt

- Application and Infrastructure are each organised as **one folder per business
  capability, with the same folder name in both projects**. Cross-cutting code goes in a
  folder named `Common`.
- Inside a slice, each use case has a subfolder holding the command or query, its
  handler and its response types.
- Each slice owns its own DI registration extension (`Add<Slice>()`). The composition
  roots only delegate, one line per slice, and hold no slice logic.
- The cost they accepted: nothing forces the two folders of a slice to stay in step.
  That's a review discipline.

**SlotBook fit:** the slices follow the bounded contexts the architecture run settles,
for example tenancy and staff, schedule, booking, notifications, audit. These are also
the units the sandbox agent builds story by story.

## P-3 Aggregates own their state — Adopt

- Every mutable property has a `private set`. The only way to create an aggregate or
  move it to a new state is through a **named method on the aggregate**.
- A transition from the wrong state throws a **domain exception** from a single guard
  method.
- Each aggregate has its own folder under Domain. Plumbing that two aggregates share
  word for word goes in a small base class. Behaviour that differs stays in each
  aggregate, through abstract members.
- Nothing outside Domain sets aggregate state.
- **Persistence mapping lives in Infrastructure** as BSON class maps registered once at
  startup. Domain carries no MongoDB attributes. Collections keep mutable backing fields
  so deserialization works.

**SlotBook fit:** this is the natural home for the booking invariants, such as a slot's
free, reserved and taken counts and booking state transitions. The architecture run
decides which aggregates hold them (STATE open question: is Slot inside the Booking
aggregate?).

## P-4 CQRS-style handlers; handlers own aggregate mutation — Adopt (Decide the mechanism)

- Controllers send command and query objects through a dispatcher. A handler is the
  only place that **loads an aggregate, calls its methods and persists it**.
- Helper steps (pipeline stages, codecs) work on a **context projection**, never on the
  aggregate. Each step can then be tested on its own, and state changes have a single
  owner.
- Loading "the aggregate or a not-found error" happens in **exactly one store helper**,
  so the not-found handling is consistent.

**SlotBook fit:** adopt the rule. *Decide:* the dispatcher. The reference uses a
private library. The candidates are a small hand-written dispatcher, or a library whose
**current licence must be checked** (some popular mediator libraries have moved to
commercial licences).

## P-5 Unit of work and a transactional outbox on MongoDB — Adopt (build it)

- A handler **stages integration events on the unit of work** as it changes aggregates.
  `Commit` writes the aggregate changes **and** the outbox documents in **one
  multi-document transaction**.
- A background **outbox worker** reads pending outbox documents and publishes them
  through MassTransit to RabbitMQ. It watches a change stream and falls back to a
  polling backoff. Each document moves from pending to processing to done.
- Each event keeps **one message id**. Recovery republishes with the same id, so
  receivers can drop duplicates.
- MongoDB has to run as a **replica set** everywhere, including locally and in tests,
  because transactions and change streams need one.
- **Pitfalls they hit:**
  - The outbox serializes through the compile-time type. Staging a derived event
    through a base-typed reference silently drops the derived properties. Every event
    is therefore staged through its concrete type, and a test round-trips every event
    type through the outbox's serialize, name-registry and deserialize path.
  - A cancellation token can fire after a commit has already succeeded, and the abort
    that follows fails. They needed a workaround.
  - Hosts shutting down while the outbox worker is mid-commit. Tests wait for the
    outbox to drain before disposal.

**SlotBook fit:** this is what notifications (epic 6) and anything else downstream of
a booking need. *Decide:*
- whether the outbox worker fits Render's free tier, where services sleep when idle,
  and change-stream support on the MongoDB host (assumed Atlas free; must be verified);
- whether SlotBook needs events across process boundaries at all in v1, or only
  in-process (see P-10).

## P-6 Dedup by identity, not by an idempotency key — Adopt

- There is **no separate idempotency key**. The identity of the thing is the dedup
  token:
  - a natural id from upstream becomes the document `_id`, so MongoDB's primary-key
    uniqueness rejects a replay;
  - something with no natural id gets a **deterministic id computed from what makes it
    unique** (for example a hash of the source coordinates);
  - a compound unique index sits beside it as a safety net.
- An attempt counter is only for observability and a retry cap. It is never part of an
  identity.
- **Duplicate-key detection is precise.** It walks the full exception chain, matches
  the write-error category `DuplicateKey`, and is scoped to the expected collection, so
  a duplicate on a different write in the same transaction is never misread as "already
  done".
- **Their lesson:** when the meaning of an identity changes, the index and its
  documentation have to change together.

**SlotBook fit:** this matters a lot for booking. One booking per patient per slot,
ticket-code uniqueness and sequence numbers can all be expressed as identity or unique
index constraints, and consumers become idempotent by construction. The NFR-1 work in
the architecture run should start from this rule.

## P-7 An explicit retry policy per consumer — Adopt

- Each consumer that matters declares its own MassTransit retry policy instead of
  relying on defaults, which are zero retries:
  - **deterministic** failures (parse errors, domain-rule violations) are excluded from
    retry and go straight to the error queue;
  - **transient** failures are retried with a short, bounded, in-memory schedule (about
    one minute in total).
- The **error queue is the inspectable poison store.** An operator inspects messages
  there and redelivers them.
- They didn't use delayed redelivery, because it needs a broker plugin or a message
  scheduler they hadn't set up.
- **Hazard they guarded against:** the domain-exception type is excluded from retry, so
  a "not found" caused by replica read lag must never be thrown as that type in a
  retried path. A fitness test enforces this.

**SlotBook fit:** adopt. *Decide:* whether the hosted RabbitMQ free tier (CloudAMQP
assumed) offers delayed messages. Reminders and retries may need them (epic 6).

## P-8 Store first, process later (a durable inbox) — Adapt

- When input arrives from outside, the first step only makes it durable:
  1. persist a receipt with a deterministic id;
  2. stage an internal event;
  3. commit;
  4. only then acknowledge the source.
- Everything that can fail for a business reason (parsing, validation, remote checks)
  runs afterwards, asynchronously, behind the broker's retry and error queue.
- The two phases fail differently. An infrastructure failure gives bounded backoff and
  a health check that turns unhealthy, and the source is never acknowledged early. A
  business failure ends in the error queue.

**SlotBook fit:** SlotBook has no inbound event stream. The pattern applies only where
an outside system calls in, for example **SMS delivery-status webhooks**. Use it there
or skip it.

## P-9 Two commits around a long side effect — Adopt for notification sends

- Before a slow or external side effect, the handler marks the aggregate *processing*
  and commits. It then runs the side effect, records the outcome (sent or failed),
  stages the outcome event, and commits again.
- A **try-start guard** refuses to start something already processing or finished, so
  nothing is started twice.
- A crashed process leaves a visible *processing* record that a recovery job can find.
  Their cost: two round trips. Their lesson: the visible record is necessary, but it's
  only useful once a recovery job exists.

**SlotBook fit:** SMS and push sends (epic 6), and any call to an outside provider.
Recovery is a background job, which ties into the Render timer question.

## P-10 Internal vs external events — Adapt

- **Internal events** live in their Application slice and are never published as
  contracts. **External events** live in the event-contracts project, and the
  **publisher owns the contract**: a consumer depends only on the publisher's package.
- Event class names don't end in an "IntegrationEvent" suffix.
- Related events share an **abstract base for code reuse only**. Each event is
  published and consumed as exactly one concrete type, and the base carries no
  serializer polymorphism.
- Internal events split by *what this service does next*. External events split by
  *what the downstream consumer does next*. Lifecycle and audit events stay single,
  because they mean the same thing for every variant.

**SlotBook fit:** SlotBook is one product. *Decide (ADR-0002, modular monolith vs
microservices):* if it's a modular monolith, most events are internal between modules,
and an event-contracts project may be only the seam between modules, or a later
extraction point.

## P-11 Declarative endpoint authorization with a scope check — Adapt

- Every endpoint carries a **permission attribute or an explicit anonymous attribute**.
  There is **no global fallback policy**. Instead, a **fitness test enumerates every
  controller action** and fails the build if one declares neither, so new endpoints
  fail closed.
- Endpoints addressed by a scope id in the route (in SlotBook, the tenant) carry a
  **scope attribute**. Its authorization handler compares the caller's scope claim with
  the route value and parses both, so a missing or malformed claim never matches. It
  runs in the authorization middleware, **before model binding**, so a refused request
  never reaches a handler.
- If the caller names another scope's id, the answer is **403, not 404**: the caller
  supplied the id, so there's nothing to hide.
- Auth configuration exists only in the per-environment settings. A missing section
  fails startup.
- An OpenAPI transformer derives the security requirements and the "required
  permission" text from the same attributes that enforce them, so the API document
  can't drift from the checks.
- Integration tests replace **only authentication**, with a test handler that reads
  claims from headers. Authorization stays real. Permission strings are pinned in
  tests as literals, because they're a contract.

**SlotBook fit:** this lines up with SC-2 (tenant isolation) and SC-4 (roles), and
gives the cross-tenant suite a build-time guard. *Decide:* how the tenant is resolved
(route, host or claim), the role model (owner, practitioner, staff, patient), and the
separation between patient and staff audiences.

## P-12 One error format: RFC 9457 problem details — Adopt (build it)

- One exception-handling middleware maps every error to `application/problem+json`. A
  domain-rule violation becomes **422**.
- The authorization middleware runs **after** exception handling, so 401 and 403
  responses also come back as problem details.

**SlotBook fit:** build it on ASP.NET Core's built-in problem details and exception
handlers. *Decide:* business reason codes, and how they sit alongside 429
(initiative Notes).

## P-13 Configuration fails fast — Adopt

- Options are bound and **validated on start**. There is no silent fallback for
  required configuration.
- Secrets never live in the repo. Locally they're pulled from a vault into user
  secrets.

**SlotBook fit:** use Render environment variables in deployed environments, and
`*.local.json` or user secrets locally (CLAUDE.md).

## P-14 Indexes declared in code — Adopt

- Each aggregate has an index configuration class. A hosted service creates the
  indexes at startup.
- Index names are explicit, with a unique or non-unique prefix, and are configurable
  through validated options.
- Compound unique indexes are the safety net behind identity-based dedup (P-6).

**SlotBook fit:** unique indexes carry several of the NFR-1 invariants. *Decide:* how
index changes and data changes ("migrations") are managed once there is real data. The
reference treats all of its data as temporary, so it never had to solve this. SlotBook
does, after go-live.

## P-15 Observability — Adapt

- **OpenTelemetry** for traces, metrics and logs, exported over OTLP.
- Logs use **source-generated `[LoggerMessage]` methods**, kept in one logger-extensions
  file per folder. **EventId ranges per slice** prevent collisions, and a fitness test
  checks that every EventId is unique.
- Each slice has its own diagnostics class. On skip and idempotency paths, the trace
  event is written before the log entry.

**SlotBook fit:** adopt the logging discipline. It also makes SC-6, the scan for
personal data in logs, easier to enforce. *Decide:* which free observability backend to
use, if any (SC-7, NFR-16).

## P-16 Testing — Adapt (ADR-0006 wins on tools)

- Unit tests with snapshot verification (Verify) where the output shape is the
  contract.
- **Integration tests** use `WebApplicationFactory` against a **real MongoDB replica
  set and a real RabbitMQ** started in containers:
  - a unique database name per run;
  - HTTP fakes for outside APIs (WireMock);
  - the tests drive the full chain, from HTTP through the database, outbox and broker
    to the consumer.
- **Architecture fitness tests:** layer rules, endpoint access, EventId uniqueness, and
  the retry-hazard rule from P-7.
- One test-data factory builds domain objects.

**SlotBook fit:** adopt the shape, using xUnit v3 + Shouldly on MTP (ADR-0006). The
concurrency suite (Done when 1) and the cross-tenant suite need the real-database
tests. *Decide:* **how integration tests get MongoDB and RabbitMQ inside the sandbox
container and in CI.** The options are sidecar containers on a shared network, binaries
in the image, or Testcontainers with a mounted Docker socket. Mounting the Docker
socket would weaken the sandbox's isolation.

## P-17 Committed OpenAPI document — Decide

- The generated OpenAPI document is committed. CI fails if it's out of date, and a
  linter (Spectral) checks it.

**SlotBook fit:** useful if the Angular client is generated from it, and the sandbox
agent then sees API drift in its diff. It costs one CI step.

## P-18 The spine and ADRs as agent guardrails — Adopt

- The spine states each invariant and **points at the code or test that enforces it**.
  If the code and the spine disagree, the spine is fixed, because the code is the
  source of truth.
- The agent instructions tell the coding agent: **"if a change cannot fit a rule, stop
  and raise it instead of working around it."**
- ADRs follow a lean MADR-style template. Some ADRs record a rule that's already
  correct in the code, so it has a name and a home.

**SlotBook fit:** this suits the sandbox agent directly. The stop-and-raise rule is a
candidate for CLAUDE.md's build-run rules. Proposing that is out of scope here; the
developer decides.

## P-19 Local orchestration with .NET Aspire — Decide

- An Aspire AppHost runs the service with MongoDB, RabbitMQ and Redis locally.

**SlotBook fit:** it helps local development. It isn't needed on Render, and it
doesn't solve the sandbox question in P-16. Optional.

## P-20 Distributed cache — Skip for v1

- They use Redis behind a hybrid cache library, and auth reads session roles from
  Redis. Their accepted cost: when Redis is down, every protected endpoint fails.

**SlotBook fit:** no requirement needs it, and it adds a free-tier service and a single
point of failure. Bring it back only if NFR-13 measurements demand it.

## Not applicable

These belong to the reference's own domain: its event streaming platform (Kafka is
parked for SlotBook), its key vault cryptography, gRPC contracts, generated SDK
clients, private package feeds, and the template-sync constraints that come from its
company template.

---

## What SlotBook must build itself

The reference gets these from company-private libraries. SlotBook needs small, owned
equivalents, built early because every later story depends on them:

1. **Domain base types:** an aggregate-root base and one domain exception type, which
   maps to 422.
2. **Command and query dispatch:** command and query interfaces, handler bases and a
   dispatcher (see P-4 for the library choice).
3. **MongoDB persistence:**
   - a unit of work with a repository per aggregate, committed in one multi-document
     transaction;
   - the transactional outbox collection and the outbox worker;
   - class-map registration and index creation at startup.
4. **Messaging:**
   - an integration-event base with a message id;
   - an event-type registry for rehydrating events from the outbox;
   - a consumer base;
   - MassTransit and RabbitMQ wiring, with per-consumer retry definitions.
5. **Error handling:** problem-details mapping for domain, validation, not-found and
   authorization errors.
6. **Authorization:** permission and tenant-scope attributes, their handlers, and the
   endpoint-access fitness test.

**Note for the epic plan:** items 1–5 make a sizeable platform slice. The architecture
run should say which epic owns it. Epic 1, deploy platform, or epic 2, tenant and
staff, are the natural candidates. Otherwise the first story that needs a commit will
build it ad hoc.

---

# Second reference — the developer's modular-monolith structure

Added 2026-10-10. The developer asked SlotBook to follow the project structure of a
second private repo, which they describe as "a solid modular monolith". The same
firewall applies: shape only, no names, code or files.

## M-1 Modules as folders of projects — Adopt

- Each bounded context lives under `Modules/{Module}/`.
- A **simple module** is one project with `Domain/`, `Application/` and
  `Infrastructure/` folders inside it.
- A **complex module** is split into separate `Domain`, `Application` and
  `Infrastructure` projects, so the compiler keeps its domain pure.
- Every module has a **public contracts project** holding its integration events and
  the public queries other modules may dispatch.
- Unit tests are mirrored per module, with one test folder per use case.

## M-2 Module isolation by project references — Adopt

- A module project references only the shared kernel and **other modules' contracts
  projects**, never another module's internals. Most of the reference follows this.
  Its few exceptions, such as a migrations module and some tests, are the drift
  SlotBook should guard against with a fitness test.
- Cross-module reads dispatch a public query from the other module's contracts. The
  owning module handles it.

## M-3 A shared kernel in four parts — Adopt

`SharedKernel.Domain` (base types), `SharedKernel.Application` (command and query
abstractions, concurrency helpers), `SharedKernel.Infrastructure` (persistence and
messaging plumbing) and `SharedKernel.IntegrationEvents` (event and command base
types).

## M-4 Thin hosts compose modules — Adopt (one host for SlotBook)

- Each host project (API, background command host, job host) calls one
  `Add{Module}Module()` per module. A module's DI registration lives in its own
  `Infrastructure/ServiceCollectionExtensions.cs`.
- Controllers live in the API host under `Controllers/{Module}/` and only dispatch
  commands and queries.
- The same modules can be composed into several hosts. That's how the reference
  scales background work separately.
- SlotBook keeps one host (AD-1), but the shape lets a worker host be added later
  without moving code.

## M-5 Use-case folders with a validator per command — Adopt

- `Application/{UseCase}/` holds the command or query, its handler, and a
  FluentValidation validator.
- Handlers and validators are registered by assembly scanning.
- Repositories are split into read and write interfaces per aggregate, declared with
  the aggregate.

## M-6 Priority queues — Adopt for notifications

- User-waiting work and background work run on **separate queues and consumers**, so a
  burst of background work never delays a user.
- SlotBook equivalent: OTP and booking-confirmation sends on a high-priority endpoint,
  reminders and announcement fan-out on a low-priority one. The PRD addendum already
  asks that fan-out never starve OTPs.

## Not adopted

- The reference's older core split into projects **by CQRS role** (commands, command
  handlers, queries, query handlers, events, event handlers, services). The reference
  itself is moving away from it, toward modules.
- Its separate job and Kafka hosts, which SlotBook can't afford on the free tier
  (AD-1).
