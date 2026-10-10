# STATE — where the project actually is

> **Every interactive session reads this file first and updates it last.** It is the
> handoff between sessions. Keep it short; detail belongs in session logs and ADRs.

**Last session:** 009 · 2026-10-10
**Next session:** 010: `bmad-architecture` (Render + MongoDB are inputs; write their ADRs; settle the cross-epic decisions in the initiative Notes)

---

## Goal

Build SlotBook (frontend + backend) through a **sandbox agentic ecosystem**. The
developer plans with BMAD (epic → feature → story → task). Claude Code builds each
story in a Docker sandbox and opens a PR. The developer comments, the agent replies
and fixes, and the developer merges. Process: `docs/process/SANDBOX-WORKFLOW.md`.

## Current position

| Track | Status |
|---|---|
| **BMAD planning** | Brief ✅ · Epic 0 ✅ (epic #3 closed) · PRD ✅ **final** in `prd-slotbook/` (FR-1–45, NFR-1–19, SM-1–12) · **Epics ✅** (009): 12 epic envelopes + initiative envelope, tree-checked; 1–10 go-live, 11–12 fast-follow; **not yet published** to GitHub · UX ⬜ · Architecture ⬜ · `bmad-ticket` GitHub store configured ✅ |
| **Pipeline** | 🟡 Host ready ✅ · sandbox image + `run-job.sh` ✅ (plan, implement, fix used on 3 real stories) · models per mode ✅ · CI ✅ · ⬜ `review` mode, worker, dashboard |
| **GitHub** | Bot `ashik71-slotbot` (Write, classic PAT with `workflow` scope) ✅ · ruleset `protect-main` ✅ (1 approval + required `ci-ok`, bound to GitHub Actions) · CI `ci.yml` ✅ · `ai:*` + BMAD store labels ✅ · milestone `initiative-booking-management` ✅ · Project board [SlotBook delivery](https://github.com/users/ashik71/projects/2) ✅ with pipeline columns; `board-sync` Action derives Status from labels |
| **Code** | Epic 0 stories all merged: #4 `/health` + Angular page (PR #6, ~$1.50, 3 fix rounds), #7 backend style (PR #8, ~$1.10), #5 CI with required `ci-ok` (PR #9, ~$0.83). Red-check proof: PR #10 (closed). Epic #3 closed |
| **Blocked on** | Nothing |
| **Sequencing** | Architecture next. Then publish the epics and incept epic 1 (deploy platform, no screens). `bmad-ux` before epic 2 is incepted. Build order: `_bmad-output/initiative-booking-management/tickets.toml` |

## Decisions made

| ADR | Decision | Status |
|---|---|---|
| 0003 | Target .NET 10 (all projects `net10.0`) | Accepted |
| 0004 | Sandbox agentic delivery: BMAD planning; agent codes FE + BE; PR comment loop; developer merges. OpenSpec dropped (amended 2026-10-09) | Accepted |
| 0005 | Angular for the frontend (deciding factor: the developer's review fluency) | Accepted |
| 0006 | Monorepo, deploy-independent `src/backend` (tests in `src/backend/tests`) + `src/frontend`; xUnit v3 + Shouldly on MTP; Angular 22 + Vitest | Accepted |
| 0007 | Backend code style: the developer's reference conventions, enforced as build errors (StyleCop, Meziantou, VS Threading) | Accepted |

| — | **Database: MongoDB** (developer, 2026-10-10) | ADR to write in 010 |
| — | **Hosting: Render, for now**; Azure later as its own epic (developer, 2026-10-10) | ADR to write in 010 |
| — | **Go-live line:** epics 1–10 before the first real booking; 11 announcements + 12 chat fast-follow; every epic ships to a pre-launch environment from epic 1 (developer, 2026-10-10) | Initiative Notes |

*ADR-0002 is reserved for modular monolith vs microservices (architecture step).*

## Decisions deliberately still open

Don't let a session assume an answer to any of these.

| Question | Blocks | Target |
|---|---|---|
| Data retention period (Q3), uptime expectation (Q5), privacy notice text | Privacy policy, ops | Before launch |
| Staff sign-in method (Q4; MFA for all staff is decided) | Staff auth epic | Architecture |
| Threat model (NFR-8) | Security-sensitive epics | Architecture |
| Bounded contexts; is Slot inside the Booking aggregate? How is the cross-slot booking limit guarded? | Concurrency design | Architecture |
| Multi-tenancy isolation model in MongoDB (database per tenant, or shared collections with a tenant key) | First collection | Architecture, **before the first collection** |
| Data access (official driver or EF Core provider), schema changes; MongoDB host and tier (assumed Atlas free: no backups, throughput cap, custom roles unknown) | Epics 1, 2 | Architecture |
| Background jobs on Render's free tier, where services sleep when idle | Epics 3, 5, 6, 10 | Architecture; epic 1 proves a timer fires |
| Free SMS quota for local numbers (Q1)? Live OTP is SMS only either way; it sets pre-sale cost and the default daily OTP budget | SMS provider | Architecture |
| RabbitMQ vs Kafka split | Messaging epic | Architecture |
| Where the worker and dashboard live (default: `sandbox/` here, dashboard as its own epic) | Pipeline build | Architecture |

## Open threads

- **PRD inputs (007):** the developer's prior private plan for the first tenant is
  summarised generically in `prd-slotbook/input-prior-plan.md`. The originals stay
  outside the repo. Decisions C1–C10 are in the PRD memlog
- **PRD key rules for architecture (008, updated 009):** live OTP is SMS only and
  there is no email in v1; the capacity model (free / reserved / taken), the
  per-number limit, sequence numbers and ticket-code uniqueness are database
  invariants. With MongoDB, the architecture document must work each one through in
  MongoDB terms (conditional updates, multi-document transactions, write concern,
  retries). The addendum's SQL sketches are background only
- **Epic set (009):** initiative envelope + 12 epics in `_bmad-output/initiative-booking-management/`.
  Every epic's Done when ends with the **standard epic checks SC-1–SC-8** defined in
  the initiative file. The cross-epic decisions for architecture are listed in the
  initiative Notes
- **Azure (focus area 1) is parked** while hosting is on Render
- **Working style (008):** apply standard doctor–patient booking conventions by
  default; don't ask the developer about edge cases those conventions already settle
- **Git history is not scrubbed (developer's decision, 2026-10-09).** Early commits
  contain client details and stay in the public history. The firewall rule still
  applies to all **new** content
- `docs/AIAgenticGuideline/` holds the developer's original guideline files. They are
  gitignored because they name the client. The clean, current version is
  `docs/process/SANDBOX-WORKFLOW.md`
- The old hand-coding curriculum, roadmap and lessons are in `docs/archive/`, for
  reference only

## Homework outstanding

| Set in | Task | Status |
|---|---|---|
| 004 | Write the brief entry in `docs/learning/LEARNINGS.md` | Open |
| 005 | `colima stop` after each sandbox session | Habit |
| 006 | Upgrade Node on the Mac to ≥ 24.15 (Angular 22 requirement) | Open |
| 009 | Note the deciding reason for MongoDB and for Render, for their ADRs in 010 | Open |

---

## How to resume (for an AI session)

1. Read `CLAUDE.md`, then this file
2. Read the newest `docs/sessions/` log if more context is needed
3. Check `_bmad-output/initiative-booking-management/` for in-progress BMAD drafts
   and offer to resume them
4. **Don't re-derive settled decisions.** If an ADR exists, the question is decided
