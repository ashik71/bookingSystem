# SlotBook — Project Context

> **Read this first.** The single source of truth for what this project is, why it
> exists, and how it is built. Any new session, human or AI, starts here.

Last updated: 2026-10-09 (Session 004)

---

## 1. What this is and why it exists

**SlotBook** is a multi-tenant appointment booking platform for clinics: a web
frontend and a .NET backend, built as a modular monolith and partly split into
services later.

It has two goals, and both are real:

1. **The product.** A booking system good enough to run a real clinic. The first
   clinic is a real prospective client, so this is not a toy. See the product
   brief in `_bmad-output/initiative-booking-management/brief-slotbook/`.
2. **The ecosystem.** Building that product through a **sandbox agentic workflow**.
   The developer plans with BMAD (epic → feature → story → task). Claude Code builds
   each story in a disposable Docker sandbox and opens a pull request. The developer
   reviews and comments, the agent replies and fixes, and the developer merges.
   Frontend and backend are both built this way. See ADR-0004 and
   `docs/process/SANDBOX-WORKFLOW.md`.

**The skills it builds**, set by the developer. The architecture should give each a
genuine job:

1. **Azure**: primary cloud; AWS as a secondary comparison
2. **System design**: designing distributed systems and defending the design
3. **RabbitMQ & Kafka**: messaging, and knowing which one and why
4. **Multi-tenancy**: isolation, onboarding, per-tenant everything
5. **Security**: authn/authz, OWASP, secrets, tenant-boundary enforcement
6. **Code architecture**: Clean Architecture, DDD, modular monolith
7. **Agentic delivery**: running an AI build pipeline safely and getting quality
   out of it

**Where the developer's depth comes from now.** Not from typing the code, but from
writing requirements the agent can't misread, designing the architecture, and
reviewing PRs hard enough to explain every line of the focus-area code (the
double-booking guard, tenant isolation, the outbox).

### What "good architecture" means here

RabbitMQ *and* Kafka, or MongoDB for one module, would normally be challenged as
over-engineering. Here they are **deliberately in scope**, because they are on the
skills list. So the design question is never *"do we need Kafka?"* but:

> *"Both are in scope. Where does each genuinely belong, and can the developer
> defend that split against hard push-back?"*

Every ADR records the honest trade-off, including **the scale at which the choice
would be wrong**.

### Hard constraint: zero running cost before a sale

Until the product is sold, nothing may cost money to run. Hosting, database, SMS
and email all stay within free tiers. Anything paid needs a free alternative.
This is itself a design input (see the brief).

---

## 2. Separation from client work — IMPORTANT

The developer has a **separate, real, paying client**. **This repo must stay clean
of that client's intellectual property.** It may become public as a portfolio piece.

**Rules:**
- ❌ No client name, branding, logos, colour palette or screenshots
- ❌ No client code, pricing, cost breakdowns, margins or negotiation strategy
- ❌ No client domain specifics: religious or treatment content, local vendor contracts
- ✅ Generic domain language only: *tenant, clinic, practitioner, patient, slot, booking*
- ✅ Patterns may inform both projects. **Ideas travel, artifacts do not.**

Client-specific documents live **outside this repo**, in the developer's private
client folder. The first real clinic is configured privately, never in source.

### Generic requirement shapes (safe to use)

| Pattern | Why it is interesting |
|---|---|
| Weekly publication cycle | Next week's slots publish on a fixed weekday; booking stays open all week |
| Reserved slots | Practitioners hold slots back for patients who missed theirs, then release the unused ones |
| Verifiable booking tickets | QR + verification code, so a booking can be proven genuine at the door |
| Phone/OTP as primary identity | Not email. Changes the whole auth design; SMS costs money, so OTP must be careful |
| Free booking means abuse risk | No payment, so nothing stops hoarding. Needs per-identity limits: a concurrency and fairness problem |
| Bangla/English/Arabic + RTL | Real localization pressure, including right-to-left layout |
| Payment as a later seam | Booking states leave room for a payment step that v1 doesn't use |

---

## 3. Who is building it

Solo developer, **MD Ashik Ashrafe**, acting as **product owner, architect and
reviewer**. The agent writes the code.

| Skill | Level |
|---|---|
| .NET / C# | 6+ years, senior. Assume fluency |
| Angular | 6+ years, strong. Can review frontend PRs in depth |
| MongoDB | 6+ years, strong |
| SQL | 2 years, **the weak spot**: indexes, isolation levels, locking, query plans. Matters for reviewing the concurrency code |
| Azure · System design · Messaging · Multi-tenancy · Security · Clean Arch/DDD · Agentic delivery | 🎯 Focus areas |
| Redis · Docker/Kubernetes | Supporting |

**Working style for AI sessions:** skip fundamentals and start at the decision level.
Lead with trade-offs and failure modes. Push back on design and requirements, then
record the decision and move on. Interrogate instead of inventing requirements.

---

## 4. How it is built

Full detail: `docs/process/SANDBOX-WORKFLOW.md`. In short:

| Stage | Where | Who |
|---|---|---|
| BMAD planning: brief → PRD → UX → architecture → epic specs → tickets | Interactive Claude Code session on the Mac | Developer answers; agent facilitates |
| Tickets → GitHub (Initiative = Milestone, Epic = issue, Story = sub-issue, Feature = label, Task = checklist) | `bmad-ticket` publish | Developer |
| Plan run → plan review → implement run → PR | Docker sandbox, driven by `ai:*` labels | Agent builds; developer gates every step |
| PR review → agent replies and fixes → merge | GitHub | Developer comments and merges; agent fixes |
| Epic retrospective; lessons into `CLAUDE.md` | Interactive | Developer |

**Decisions:** ADRs in `docs/adr/`, permanent. **Planning artifacts:** BMAD output in
`_bmad-output/initiative-booking-management/`, committed so every sandbox run reads it.
Whether OpenSpec change folders are still used alongside BMAD specs is an open question.

---

## 5. Target architecture (aspirational — the BMAD architecture step decides)

A modular monolith first, split selectively later: a single API host plus a worker
host. Modules are isolated as Domain / Application / Infrastructure / Contracts,
talk to each other only through `Contracts`, and the boundaries are enforced by
architecture tests.

**Candidate modules:** Tenants, Users, Practitioners, Scheduling, Bookings,
Notifications (Payments and Reviews later).

```
Web frontend — English / বাংলা / العربية (RTL)   [framework: architecture step]
      │
      ▼
Azure Front Door / gateway (rate limiting, per-tenant throttling)
      │
      ├──► Booking Monolith — modular, Clean Arch + DDD
      │      Tenants · Users · Practitioners · Scheduling · Bookings
      │        ├── Azure SQL or PostgreSQL
      │        ├── Redis — slot holds + read caching
      │        └── Outbox ──► RabbitMQ (commands) / Kafka (event stream)
      │
      ├──► Notification Service ◄── RabbitMQ      [first extraction]
      └──► Analytics Consumer  ◄── Kafka

Observability: OpenTelemetry → Prometheus/Grafana + Azure Application Insights
Hosting:       Azure Container Apps or App Service (within free tiers before a sale)
Secrets:       Azure Key Vault + Managed Identity
CI/CD:         GitHub Actions: build, tests, E2E, scan on every PR
```

**Multi-tenancy:** one tenant in v1, but data is tenant-aware from the first table.
Retrofitting `TenantId` later is expensive, so the isolation model has to be decided
in the architecture step, before any schema exists.

**Self-hosted vs managed** is a deliberate tension. Run RabbitMQ, Kafka, Redis and
Mongo in Docker locally to learn the protocols and failure modes. Then map each to
its managed Azure equivalent and write up what it takes away and what it costs.

---

## 6. Deliverables to accumulate

These are the portfolio. Each maps to a focus area.

**Product**
- [ ] v1 running for one clinic: publish, reserve, book, ticket, door check
- [ ] Demo in all three languages, including RTL

**Agentic delivery**
- [ ] Sandbox image + job script + worker; the label state machine working
- [ ] Dashboard (built through the pipeline itself)
- [ ] Measured throughput: stories per day, PR iterations per story, limit hits
- [ ] `CLAUDE.md` lessons log showing the pipeline improving over time

**System design**
- [ ] Architecture diagram + README a stranger can follow
- [ ] Capacity model and the scale-up story: what breaks first at 10×, at 100×

**Code architecture (Clean Arch / DDD)**
- [ ] Context map; aggregate design (are Booking and Slot one aggregate, and why)
- [ ] Architecture tests that make boundary violations fail the build

**Multi-tenancy**
- [ ] Isolation-model ADR; tests proving tenant A can't touch tenant B
- [ ] Per-tenant rate limiting

**Messaging**
- [ ] RabbitMQ vs Kafka split, defended
- [ ] Outbox, idempotent consumers, poison-message handling

**Security**
- [ ] Threat model; OWASP pass; Key Vault + Managed Identity
- [ ] Ticket forgery model; IDOR tests across users and tenants

**Cloud**
- [ ] Deployed on Azure with infrastructure as code (Bicep), within free tiers
- [ ] Azure↔AWS mapping table, with one component actually deployed on AWS

**Concurrency (the signature problem)**
- [ ] Double-booking prevention, with measured load-test numbers
      (200 concurrent users, 10 slots, exactly 10 bookings)

**Observability**
- [ ] OpenTelemetry exported two ways; a Grafana dashboard as code
- [ ] Three SLOs with error budgets and burn-rate alerts
