# Curriculum — session-by-session teaching order

Built around the developer's six focus areas, **not** the original roadmap's phase
order. The original 15-phase plan is preserved in `ROADMAP.md` as reference.

**Audience assumption:** senior .NET developer. No fundamentals. Every session
starts at the design-decision level and ends with a defensible written decision.

Status: `[ ]` not started · `[~]` in progress · `[x]` ADR/doc written + implemented

**Focus tags:** 🏛 Code architecture · 🏢 Multi-tenancy · 📐 System design
📨 Messaging · 🔒 Security · ☁️ Cloud · ⚡ Concurrency (signature problem)
🧰 Supporting (minimum needed, no dwelling)

---

## Session format

1. **Frame** — the real problem, and why naive approaches fail
2. **Options** — 2–4 genuine alternatives, honest trade-offs
3. **Pressure test** — AI argues against your preference; you defend it
4. **Decide** — you write the ADR (not the AI)
5. **Design** — diagram / model / flow together
6. **You implement** — AI does not touch `src/`
7. **Review + quiz** — AI reviews your code, then tests whether you can defend it

Standing question every session: **"at what scale is this choice wrong?"**

---

## Block A — Foundations you cannot cheaply reverse

These decisions constrain everything downstream. Do them before any schema exists.

- [~] **A1. Process, tooling, and framing** 📐
  OpenSpec vs BMAD · ADRs vs specs · how docs serve both the build and interviews
  → ADR-0001 ✅, `PROJECT-CONTEXT.md` ✅, this file ✅ · **Session 1**

- [ ] **A2. Domain modeling & bounded contexts** 🏛📐
  The actual nouns. Why Scheduling and Bookings are separate contexts. Where
  Practitioner means two different things. Context map, relationship patterns
  (shared kernel / customer-supplier / anticorruption layer).
  → `docs/design/domain-model.md`, context map Artifact
  **This is the highest-leverage session in the whole plan.** Get the boundaries
  wrong and every later module decision inherits the error.

- [ ] **A3. Aggregates, invariants & consistency boundaries** 🏛⚡
  What an aggregate actually is (a transaction boundary, not a data cluster).
  **Is `Slot` inside the `Booking` aggregate, or its own?** That single choice
  determines how you solve double-booking. Eventual consistency between aggregates.
  → ADR, `docs/design/aggregates.md`
  Directly sets up Block D. Most "DDD in .NET" material gets this wrong.

- [ ] **A4. Clean Architecture, honestly** 🏛
  Dependency rule, ports & adapters, use cases. **Where it is overkill** — and
  having the nerve to say so. Domain / Application / Infrastructure / Contracts:
  what belongs in each, which direction dependencies point, why Contracts is public.
  → ADR, `docs/design/module-anatomy.md`

- [ ] **A5. Modular monolith vs microservices** 🏛📐
  Why monolith-first is right here. What "modular" buys that layering doesn't.
  The actual triggers for splitting (team topology, deploy cadence, scaling
  asymmetry — not "it feels big"). Distributed monolith as the failure mode.
  → ADR

- [ ] **A6. Multi-tenancy model** 🏢📐🔒
  Shared schema + TenantId · schema-per-tenant · database-per-tenant · hybrid.
  Axes: isolation strength, blast radius, cost per tenant, migration pain, noisy
  neighbours, data residency, per-tenant restore, onboarding time.
  **Decided before the first table exists** — see PROJECT-CONTEXT §7.
  → ADR, `docs/design/multi-tenancy.md`, comparison Artifact

- [ ] **A7. Persistence & data access** 🧰
  EF Core vs NHibernate vs Dapper — and why NHibernate was demoted here. Global
  query filters, interceptors, migrations strategy. Repository: when it earns its
  place and when it's a pointless wrapper over `DbSet`.
  → ADR

- [ ] **A8. Database engine + the SQL gap** 🧰⚡
  PostgreSQL vs SQL Server (Azure hosting, cost, locking semantics, JSONB).
  **Then close the SQL weak spot deliberately**: isolation levels and the anomalies
  each permits, index design and covering indexes, reading a query plan, lock types
  and deadlocks. Non-optional — Block D is unlearnable without it.
  → ADR, `docs/learning/sql-depth.md`

---

## Block B — Multi-tenancy in depth 🏢🔒

- [ ] **B1. Tenant resolution** — subdomain vs JWT claim vs header vs path.
  Precedence rules, the unauthenticated case, **spoofing** (why a header alone is a
  vulnerability), and what happens when resolution fails.
- [ ] **B2. Enforcing isolation in the data layer** — base entity, global query
  filters, save interceptors. Why "remember to add `WHERE TenantId`" always fails
  eventually. The dangerous gaps: raw SQL, bulk operations, cross-tenant admin
  queries, background jobs with no HTTP context.
- [ ] **B3. Proving isolation** — the test suite that makes a leak unmergeable.
  Negative-path tests, IDOR across tenants, and testing the *absence* of a filter.
- [ ] **B4. Tenant lifecycle** — onboarding, plans, feature flags per tenant,
  branding, per-tenant config, suspension, export, deletion (and GDPR-style erasure).
- [ ] **B5. Noisy neighbours** — per-tenant rate limiting and quotas so one tenant
  cannot degrade the rest. Fair queuing for background work.

---

## Block C — Security 🔒

- [ ] **C1. Threat modeling the booking domain** — STRIDE over the real flows.
  Who attacks a booking system, and for what: slot hoarding, scraping competitors'
  availability, impersonating a provider, forging tickets.
- [ ] **C2. Identity: phone/OTP as primary** — not email. OTP generation, rate
  limiting, replay, enumeration, SMS cost as an attack surface, account recovery.
- [ ] **C3. Tokens & sessions** — JWT vs reference tokens, refresh rotation,
  revocation (the hard part), tenant and role claims, token lifetime trade-offs.
- [ ] **C4. Authorization** — RBAC vs ABAC, resource-based auth, the user-in-many-
  tenants case, admin impersonation done safely. IDOR as the default bug.
- [ ] **C5. Verifiable tickets** — QR payload design, signing, offline verification,
  short human-readable codes, replay prevention. What actually stops a forgery.
- [ ] **C6. Secrets & config** — Key Vault + Managed Identity (and AWS Secrets
  Manager for comparison). Nothing secret in config or git, ever. Rotation.
- [ ] **C7. OWASP pass + API hardening** — injection, broken access control, CORS,
  rate limiting, input validation, mass assignment, error leakage.

---

## Block D — Scheduling & booking correctness ⚡

The signature technical problem. If this is wrong, the product is worthless.

- [ ] **D1. Modeling time** — availability *rules* vs generated *slots* vs
  *bookings*. Time zones stored properly, DST transitions (the 2am booking that
  happens twice), recurring weekly patterns, exceptions and holidays.
  Time modeling is where booking systems rot.
- [ ] **D2. Slot generation** — materialize ahead vs compute on read vs hybrid.
  The weekly publication cycle. Idempotent regeneration when availability changes
  *after* bookings exist — the genuinely hard case.
- [ ] **D3. Double-booking: the concurrency core** ⚡ — optimistic versioning,
  pessimistic locking (`SELECT … FOR UPDATE`), **unique constraints as the real
  guard**, Redis holds as UX not correctness. Why you layer all three and which one
  is load-bearing. Then **prove it** with a load test.
- [ ] **D4. Booking state machine** — lifecycle, legal vs illegal transitions,
  the deliberately-unused payment state, cancellation windows, no-show marking.
  Modeling a state machine so illegal states are unrepresentable.
- [ ] **D5. Fairness & abuse** — one active booking per verified identity, no-show
  tracking, slot hoarding, designing a limit that survives a determined abuser
  rather than a careless one.

---

## Block E — Messaging & distributed concerns 📨📐

- [ ] **E1. Why messaging at all** — coupling, latency, failure isolation. The
  honest cost: eventual consistency, debugging, operational burden. When an
  in-process call is simply better.
- [ ] **E2. RabbitMQ deeply** — exchanges, routing, acks, prefetch, DLQ, retry with
  backoff, poison messages, queue-depth monitoring. Commands and work queues.
- [ ] **E3. Kafka deeply** — topics, partitions, keys and ordering guarantees,
  consumer groups, offsets, rebalancing, retention and replay, compaction.
  Event streams and log semantics.
- [ ] **E4. RabbitMQ vs Kafka** 📨📐 — the comparison you will be grilled on.
  Where each belongs *in this system* and why. Delivery semantics, ordering,
  scaling model, replay, operational cost. Also: when neither is right.
  → ADR. **Then the Azure mapping:** Service Bus and Event Hubs — what changes.
- [ ] **E5. Transactional Outbox** — why dual-write is a *correctness bug*, not a
  reliability nicety. Outbox mechanics, publisher, ordering, cleanup.
- [ ] **E6. Idempotency & exactly-once** — at-least-once reality, dedup strategies,
  idempotency keys, why "exactly-once delivery" is mostly a marketing claim and what
  exactly-once *processing* actually requires.
- [ ] **E7. Sagas & long-running workflows** — orchestration vs choreography,
  compensation over rollback, timeouts. The booking-with-payment flow as the example.
- [ ] **E8. Tenant context across async boundaries** 🏢📨 — propagating TenantId
  through message headers into consumers with no HTTP context. A classic leak site.
- [ ] **E9. Background work** — worker host, scheduled publication, graceful
  shutdown, at-most-once scheduling across multiple instances (leader election).
- [ ] **E10. Real-time** — SignalR for queue position; single-server vs backplane,
  and precisely what breaks when you scale out.

---

## Block F — Cloud: Azure primary, AWS secondary ☁️

- [ ] **F1. Containerization & local parity** 🧰 — Dockerfiles, compose with the
  full dependency set, dev/prod parity, Testcontainers for integration tests.
- [ ] **F2. Azure fundamentals for architects** — subscriptions, resource groups,
  Entra ID, Managed Identity, RBAC, networking basics, Private Endpoints.
  The identity model is the part people skip and then get wrong.
- [ ] **F3. Azure compute choice** — App Service vs Container Apps vs AKS vs
  Functions. Decide for this workload and justify it. Scaling, cold starts, cost.
  → ADR
- [ ] **F4. Azure data & state** — Azure SQL vs Database for PostgreSQL (tiers,
  backup/restore, read replicas, per-tenant restore), Azure Cache for Redis,
  Blob Storage, Cosmos DB where it fits.
- [ ] **F5. Azure messaging** 📨 — Service Bus vs Event Hubs vs Storage Queues;
  mapping your RabbitMQ/Kafka design onto managed services. What you gain, what you
  lose, what gets locked in.
- [ ] **F6. Infrastructure as code** — Bicep (or Terraform for portability).
  Environments, parameterization, drift, what should never be in code.
- [ ] **F7. CI/CD & deployment safety** — GitHub Actions pipeline, migrations with
  live tenants, zero-downtime deploys, blue/green vs rolling, rollback, feature flags.
- [ ] **F8. Observability** — OpenTelemetry → Application Insights. Structured
  logs, correlation IDs, **TenantId on every log line and span**, distributed
  tracing, the four golden signals, alerts that mean something.
- [ ] **F9. Cost modeling** — sizing from the capacity model, budget alerts, the
  per-tenant unit economics of each isolation model.
- [ ] **F10. AWS comparison pass** ☁️ — map every Azure service to its AWS
  counterpart, with the real semantic differences, not renamed boxes. Then deploy
  one component on AWS for real so the comparison is earned.
  → `docs/design/cloud-comparison.md`

---

## Block G — System design as a practised skill 📐

Run these *alongside* the blocks above, not after. One per month.

- [ ] **G1. Capacity & bottleneck model** — traffic assumptions → component sizing
  → where it breaks first. Written down with arithmetic, not adjectives.
- [ ] **G2. Scale-up drills** — what breaks at 10× and 100×, in order, and what you
  change at each step. Done as whiteboard sessions with push-back.
- [ ] **G3. Failure-mode analysis** — every dependency down in turn: DB, Redis,
  broker, SMS provider. Degradation strategy, circuit breakers, timeouts, retries
  with jitter. What the user sees in each case.
- [ ] **G4. Resilience & data safety** — backup/restore actually tested, RPO/RTO,
  per-tenant point-in-time restore, DR posture, and the cost of each guarantee.
- [ ] **G5. Designing adjacent systems** — practise on problems you haven't built:
  ride-hailing dispatch, ticketing with a flash sale, notification fan-out.
  Transfers the skill off this codebase so it generalizes in an interview.

---

## Block H — Quality & delivery

- [ ] **H1. Testing strategy** — the shape of the pyramid for *this* system.
  Unit on domain logic, integration on concurrency with real infrastructure,
  what deserves E2E and what doesn't.
- [ ] **H2. Architecture tests** 🏛 — NetArchTest enforcing module boundaries and
  the dependency rule, so a violation fails the build rather than a review.
- [ ] **H3. Load & chaos testing** ⚡ — k6 scenarios, the concurrency proof,
  measured numbers recorded. Kill dependencies under load and observe.
- [ ] **H4. Thin UI pass** 🧰 — Angular, deliberately minimal. 6+ years already;
  build only enough to exercise the backend. Localization + RTL if time allows.
- [ ] **H5. Service extraction** 🏛📨 — extract Notifications, then Reviews.
  What changes when the boundary becomes a network hop. Gateway, tracing, the
  distributed-monolith trap.
- [ ] **H6. Documentation & presentation** — README a stranger can run, diagrams,
  the decision log as a narrative, the deliverables in PROJECT-CONTEXT §9.

---

## Deliberately out of scope

| Item | Why |
|---|---|
| Legacy .NET Framework 4.8 migration | No value against the focus list |
| Lamar | Built-in DI suffices; convention scanning is not a gap |
| Deep Angular work | 6+ years already — zero learning value |
| Deep MongoDB work | 6+ years already — used only as a read-model exercise |
| NHibernate as the main ORM | Demoted to an optional single-session detour |

---

## Progress & exercise log

| # | Topic | Status | ADR | Exercise | Done |
|---|---|---|---|---|---|
| A1 | Process & framing | [~] | 0001 | Write ADR-0002 unaided, then defend it | |
