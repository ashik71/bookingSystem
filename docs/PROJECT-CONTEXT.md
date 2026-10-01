# SlotBook — Project Context

> **Read this first.** Single source of truth for what this project is, why it
> exists, and the rules of engagement. Any new session (human or AI) starts here.

Last updated: 2026-10-01 (Session 1)

---

## 1. What this is and why it exists

**SlotBook** — a multi-tenant appointment booking platform, built as a modular
monolith on .NET, later partially split into services.

**This is deliberate skill development for an already-senior engineer.** The
developer is a senior .NET developer closing specific gaps, not learning to code.

**The focus list — set by the developer, this drives everything:**

1. **Azure** — cloud platform depth (**primary**; AWS as a secondary comparison)
2. **System design** — designing distributed systems and defending the design
3. **RabbitMQ & Kafka** — messaging, and knowing which one and why
4. **Multi-tenancy** — isolation, onboarding, per-tenant everything
5. **Security** — authn/authz, OWASP, secrets, tenant-boundary enforcement
6. **Code architecture** — Clean Architecture, DDD, modular monolith

Everything else in this project is **scaffolding to exercise those six**. If a task
does not serve one of them, it is a candidate for cutting.

Estimated **7–9 months at 10–12 hrs/week**.

### This changes what "good architecture" means here

Normally you'd challenge RabbitMQ *and* Kafka, or MongoDB for a single module, as
over-engineering. Here they are **deliberately in scope** — they are on the focus
list, and the point is to build real fluency with both.

So the design question is never *"do we need Kafka?"* It is:

> *"Both are in scope. Where does each genuinely belong, and can the developer
> defend that split against someone who pushes back hard?"*

At senior level, the valuable skill is **knowing when NOT to use each one**. That is
more convincing than having used everything everywhere. Every ADR therefore records
the honest trade-off, including **the scale at which this choice would be wrong** —
that sentence is the senior signal.

### Scope deliberately trimmed from the original roadmap

The developer's original 15-phase roadmap was built around a specific job spec
(Ding: AWS, NHibernate, Lamar, Angular). The focus list supersedes it:

| Original roadmap item | Decision |
|---|---|
| **AWS** (ECS, RDS, S3, CloudWatch) | **Azure becomes primary**; AWS kept as a secondary comparison pass. Deploy for real on Azure, then map every service to its AWS equivalent and deploy one component there. Cross-cloud fluency is a stronger senior signal than single-cloud depth. |
| **NHibernate** | **Demoted.** Was a job-spec requirement, not a skill goal. EF Core unless an ADR says otherwise. NHibernate remains an optional single-session detour for the mapping/interceptor concepts. |
| **Lamar** | **Dropped.** Built-in DI unless convention scanning earns its place. |
| **Angular** | **Minimised.** 6+ years of it already — no learning value. Build the thinnest UI that exercises the backend; spend the hours on the focus list instead. |
| **Legacy .NET 4.8 migration** (Phase 15) | **Dropped.** No value against the focus list. |
| **MongoDB** | **Kept but deprioritised.** Already strong (6+ years). Useful only as a polyglot-persistence and read-model exercise. |
| **Clean Architecture / DDD** | **Promoted to first-class.** Barely present in the original roadmap; now a core focus area. |
| **System design** | **Promoted to first-class.** Was an "interview prep" afterthought; now a recurring deliverable — every major topic produces a design document and diagram. |
| **Security** | **Promoted to first-class.** Was scattered 🆕⭐ items; now its own phase plus a standing requirement in every change. |

---

## 2. Separation from client work — IMPORTANT

The developer has a **separate, real, paying client**: an appointment system for a
spiritual-treatment center in Bangladesh (~478 hrs, 5–6 months, billed hourly).

**These are two different projects and this repo must stay clean of the client's
intellectual property.** This repo may become public as a portfolio piece.

**Rules:**
- ❌ No client name, branding, logos, colour palette, or screenshots in this repo
- ❌ No client code, pricing, cost breakdowns, margins, or negotiation strategy
- ❌ No client domain specifics (religious content, scholar approvals, local payment
  vendor contracts)
- ✅ Generic domain language only: *tenant, practitioner, slot, booking, patient/client*
- ✅ Patterns learned here may inform the client build — **ideas travel, artifacts
  do not**

Client-specific documents (PRD, pricing, negotiation floor, contract terms) live
**outside this repo**, in the developer's private client folder.

### What legitimately crosses over

Generic *requirements shapes* the client work revealed, which make this project
more realistic than a tutorial — safe to keep because they are not client-specific:

| Pattern | Why it is interesting |
|---|---|
| Weekly publication cycle | Slots for the next week publish on a fixed weekday; booking stays open all week. More interesting than "book any future date". |
| Verifiable booking tickets | QR + verification code so a booking can be proven genuine at the door. Anti-fraud by verification rather than by payment. |
| Phone/OTP as primary identity | Not email. Changes the whole auth design; SMS costs money, so OTP sending must be smart. |
| Free booking → abuse risk | No payment means nothing stops one person taking every slot. Needs per-identity limits and no-show tracking. Good concurrency + fairness problem. |
| Bangla/English/Arabic + RTL | Real localization pressure, including right-to-left layout. |
| Payment as a later seam | Booking state machine designed with a payment state that is unused at first. Teaches designing for optional future capability. |

---

## 3. Who is building it

Solo developer, **MD Ashik Ashrafe**.

| Skill | Level |
|---|---|
| .NET / C# | 6+ years — senior. Assume fluency. |
| Angular | 6+ years — strong. **No learning value; keep the UI thin.** |
| MongoDB | 6+ years — strong |
| SQL | 2 years — **weak spot.** Indexes, isolation levels, locking, query plans need real work. Directly blocks the concurrency topics. |
| **Azure** | 🎯 Focus area (primary cloud) |
| AWS | Secondary — comparison + one real deployment |
| **System design** | 🎯 Focus area |
| **RabbitMQ / Kafka** | 🎯 Focus area — new |
| **Multi-tenancy** | 🎯 Focus area — new |
| **Security** | 🎯 Focus area |
| **Clean Architecture / DDD** | 🎯 Focus area |
| Redis | New — supporting |
| Docker / Kubernetes | Limited — supporting |
| Flutter, app store publishing | Zero (client work, not this repo) |

**The developer is already a senior .NET developer.** This is not a juniors-to-mid
journey. Teaching should assume: C#, async, LINQ, DI, REST, ORMs, and general web
app construction are known. Do not explain them.

**The actual gaps, in the developer's own words:** Azure · system design ·
RabbitMQ · Kafka · multi-tenancy · security · code architecture (Clean Arch, DDD).

Teaching style that follows from this:
- Skip fundamentals. Start at the design-decision level.
- Lead with trade-offs, failure modes, and operational reality — not syntax.
- Push back. A senior engineer needs their reasoning stress-tested, not validated.
- "Why is this wrong at 100× the scale?" is the standing question.

---

## 4. Learning contract (IMPORTANT for AI sessions)

**The developer writes all production code. The AI does not.**

The AI's role is teacher, architect and reviewer:
- Explain concepts; compare options with honest trade-offs; recommend and justify
- Co-design system diagrams, data models, business flows
- Write and review documentation (PRD, ADR, OpenSpec changes, design docs)
- Review the developer's code and explain what is wrong and *why*
- Set exercises and quiz the developer to find the gaps between "read" and "knows"
- **Interview-drill**: challenge decisions the way a skeptical interviewer would

The AI must **not** write implementation code into `src/`, even when asked to "just
show an example" of the thing currently being built. Illustrative snippets in docs
or chat that teach a pattern are fine. Finished features are not.

If the developer asks the AI to write production code, the AI should remind them of
this contract once and offer to teach the topic instead. Explicit, deliberate
override is the developer's call.

**Session shape:** one topic per session, taught deeply.
Theory → options → trade-offs → design → ADR → developer implements → review.
Each topic gets a note in `docs/learning/`.

**Delivery formats:** markdown in repo (durable record) · published Artifacts for
diagrams and visual comparisons · conversational teaching · exercises and quizzes.

---

## 5. Process & tooling

- **Spec workflow: OpenSpec** (see ADR-0001). Each change is a folder under
  `openspec/changes/` with `proposal.md`, `design.md`, `tasks.md`. Archived when
  shipped. Stable capability specs live in `openspec/specs/`.
- **Decisions: ADRs** in `docs/adr/NNNN-title.md`. Permanent; outlive any change.
  *Rule of thumb: if it will still be true after this feature ships, it's an ADR.*
  **ADRs are the single most valuable artifact here** — they are the interview prep.
- **PRD** in `docs/prd/` — platform requirements only, no client material.
- **System design** in `docs/design/`.
- **Learning notes** in `docs/learning/`, including `LEARNINGS.md` (2–3 lines per
  phase on what was genuinely hard — raw material for interview stories).

**BMAD was evaluated and rejected** (ADR-0001). Its engine is AI agents writing the
code — precisely what is switched off here. Its structured-elicitation idea is
borrowed: the AI interrogates with specific questions rather than inventing
requirements to fill gaps.

---

## 6. Current state of the code

- `src/BookingSystem.Api/` — default web template, still has the WeatherForecast
  endpoint. Nothing real yet.
- `src/cases/` — empty.
- Two commits. Clean slate.
- **.NET SDK 10.0.401 installed**; the project targets `net10.0`.
  ⚠️ The roadmap says .NET 8 (the job spec's version). Needs a decision — see ADR-0002.
- No Angular project yet. No docker-compose yet. No tests yet.

---

## 7. Target architecture (aspirational)

Modular monolith first, selectively split later. Single API host plus a worker host.
Modules isolated as Domain / Application / Infrastructure / Contracts, communicating
only through `Contracts`, enforced by NetArchTest.

**Planned modules:** Tenants, Users, Practitioners, Scheduling, Bookings, Payments,
Reviews, Notifications.

**Planned building blocks:** MultiTenancy, Messaging, Caching, Outbox, Persistence.

End state after Phase 13:

```
Angular SPA — English / বাংলা / العربية (RTL)        [thin: no learning value]
      │
      ▼
API Gateway / Azure Front Door  (rate limiting, per-tenant throttling)
      │
      ├──► Booking Monolith — modular, Clean Arch + DDD
      │      Tenants · Users · Practitioners · Scheduling · Bookings · Payments
      │        ├── Azure SQL or Azure Database for PostgreSQL
      │        ├── Redis (Azure Cache) — slot holds + read caching
      │        └── Outbox ──► RabbitMQ (commands) / Kafka (event stream)
      │
      ├──► Notification Service  ◄── RabbitMQ      [first extraction]
      ├──► Reviews Service ──► MongoDB (Cosmos DB Mongo API)
      └──► Analytics Consumer ◄── Kafka (Event Hubs Kafka endpoint)

Observability: OpenTelemetry → Azure Application Insights (+ Prometheus/Grafana locally)
Hosting:       Azure Container Apps or App Service
Secrets:       Azure Key Vault + Managed Identity
CI/CD:         GitHub Actions (or Azure DevOps) → tests, Playwright E2E, scan
```

**Azure-vs-self-hosted is a deliberate teaching tension.** Run RabbitMQ, Kafka,
Redis and Mongo in Docker locally to learn the *protocols and failure modes*; then
map each to its managed Azure equivalent (Service Bus, Event Hubs, Azure Cache,
Cosmos) and write up what the managed service takes away and what it costs. That
comparison is a strong system-design interview answer in itself.

Every technology gets an ADR recording the honest trade-off **and the scale at
which the choice would be wrong** — never a justification pretending it was
necessary.

### Known sequencing problem — resolve early

The original roadmap puts **multi-tenancy at Phase 14, last**. `TenantId` touches
every table, every cache key, every message header, and every blob path.
Retrofitting it is a large, error-prone migration across the whole codebase.

Given multi-tenancy is a **named focus area**, deferring it to last is wrong here:
it would be rushed at the end, after the interesting design space has already been
closed off by decisions made without it in mind.

**Recommendation: design multi-tenancy in from the start** (Phase 1–2), which is
also what you would do professionally on a SaaS product. The "retrofit pain" lesson
can be had far more cheaply by writing up *why* retrofitting is expensive.

Needs an explicit ADR before any schema exists.

---

## 8. Roadmap

Full phase list with resources: `docs/learning/ROADMAP.md` (the developer's own
15-phase plan, preserved).

Session-by-session teaching order: `docs/learning/CURRICULUM.md`, which maps onto
the roadmap phases but front-loads the decisions that are expensive to reverse.

**Milestones:**
- Phase 5 — working booking MVP
- Phase 10 — portfolio-ready, presentable to employers
- Phase 14 — multi-tenancy complete

---

## 9. Deliverables to accumulate

Not an afterthought — the actual output of this project. Each maps to a focus area.

**System design**
- [ ] Architecture diagram + README a stranger can follow
- [ ] A written design doc per major topic, with the alternatives rejected
- [ ] Capacity model: traffic assumptions → component sizing → bottleneck analysis
- [ ] The scale-up story: what breaks first at 10×, at 100×, and what you'd change

**Code architecture (Clean Arch / DDD)**
- [ ] Context map with bounded contexts and their relationships
- [ ] Aggregate design write-up: boundaries, invariants, why Booking and Slot are
      (or are not) one aggregate — the crux of the concurrency problem
- [ ] NetArchTest suite making boundary violations fail the build
- [ ] Honest note on where pure Clean Architecture was *not* worth it

**Multi-tenancy**
- [ ] Isolation-model ADR with the cost/isolation/migration trade-off
- [ ] Tests proving tenant A cannot read, write or book tenant B's data
- [ ] Per-tenant rate limiting so one tenant cannot degrade others
- [ ] Tenant onboarding walkthrough: zero to serving traffic

**Messaging**
- [ ] **RabbitMQ vs Kafka**: when each is right — expect hard push-back
- [ ] Outbox implementation + why dual-write is a correctness bug
- [ ] Idempotent consumer + what happens on redelivery
- [ ] Poison-message handling: retries, backoff, dead-letter, and the human recovery path

**Security**
- [ ] Threat model for the booking domain
- [ ] OWASP pass with findings and fixes
- [ ] Secrets: Key Vault + Managed Identity, nothing in config or git
- [ ] Verifiable-ticket design: what actually stops a forged booking
- [ ] Authorization tests: IDOR attempts across users *and* across tenants

**Cloud — Azure primary, AWS secondary**
- [ ] Deployed, publicly reachable environment on Azure
- [ ] Infrastructure as code (Bicep for Azure; Terraform if you want it portable)
- [ ] Managed-vs-self-hosted comparison for each piece of infrastructure
- [ ] Cost model with budget alerts
- [ ] **Azure↔AWS service mapping table** with the real differences, not just
      renamed boxes (Service Bus vs SQS/SNS, Event Hubs vs Kinesis/MSK, Container
      Apps vs ECS/Fargate, Key Vault vs Secrets Manager, Entra managed identity vs
      IAM roles) — plus where the models genuinely diverge
- [ ] One component actually deployed on AWS, so the comparison is earned not read

**Concurrency (the signature problem)**
- [ ] **Double-booking prevention**: optimistic version + unique constraint + Redis
      hold — *why all three*, and which one is the actual guard
- [ ] **Load-test numbers**: "200 concurrent users, 10 slots, zero double bookings,
      N req/sec with Redis vs without" — real measured figures, written down

**Throughout**
- [ ] `LEARNINGS.md` — 2–3 lines per topic on what was genuinely hard
