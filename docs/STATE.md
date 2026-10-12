# STATE — where the project actually is

> **Every interactive session reads this file first and updates it last.** It is the
> handoff between sessions. Keep it short; detail belongs in session logs and ADRs.

**Last session:** 013 · 2026-10-12
**Next session:** 014: story 9 #30 (plan) → 11 #32 (plan) → 10 #31 (hitl) → 12 #33; Claude drives the loop and stops at `ai:pr-ready`, developer merges

## Handoff to session 014

**Where we stopped:** epic 1's backend lane is built except deploy, observability and the
proofs. The demo is live on Render (`slotbook-api-sc87`, `slotbook-web`). **Merge rule:**
in 013 the developer let Claude approve and merge as them **for that session only**; from
014 Claude stops at `ai:pr-ready` again and the developer merges.

| Story | Issue | State |
|---|---|---|
| 1–5, 13, 14, 3 arch rules, 6a outbox | #21, #12–#15, #18, #13, #24–#27 | ✅ merged |
| 6b inbox + consumer retries | #37 | ✅ merged (PR #39) |
| 7a lease + job runner · 7b heartbeat + `/internal/jobs` seam | #28 · #40 | ✅ merged (PRs #41, #42) |
| 8 OpenAPI in CI + security headers · 8a XSRF survives restart · 8b one scheme per endpoint | #29 · #43 · #44 | ✅ merged (PRs #45, #46, #47) |
| 9 deploy on merge | #30 | ⬜ next; needs a plan run. Secrets `RENDER_DEPLOY_HOOK_API/WEB` and variable `DEMO_API_URL` are set |
| 11 Grafana Cloud | #32 | ⬜ needs a plan run. OTLP env vars set on `slotbook-api` in Render; Synthetics check not created yet |
| 10 free-tier proofs | #31 | ⬜ hitl; waits on 9 (and 6b–8b, done); switch the heartbeat on in Render |
| 12 refactor sweep | #33 | ⬜ last; candidate noted on the issue (slow ArchitectureTests scan host) |

**Check first in 014:** no open PRs, nothing running. Paste each split child's plan into
its own issue before its first run (the sandbox reads only that issue).

**Accounts:** Render ✅ · Atlas M0 ✅ · CloudAMQP Little Lemur ✅ · Grafana Cloud ✅ (2026-10-12).

**Parked until UX starts:** correct-course for epics 2–12 (envelope edits,
three PRD changes, single-tenant) and epic 2 inception; the team slide deck.

---

## Goal

Build SlotBook (frontend + backend) through a **sandbox agentic ecosystem**. The
developer plans with BMAD (epic → feature → story → task). Claude Code builds each
story in a Docker sandbox and opens a PR. The developer comments, the agent replies
and fixes, and the developer merges. Process: `docs/process/SANDBOX-WORKFLOW.md`.

## Current position

| Track | Status |
|---|---|
| **BMAD planning** | Brief ✅ · Epic 0 ✅ (epic #3 closed) · PRD ✅ **final** in `prd-slotbook/` (FR-1–45, NFR-1–19, SM-1–12) · **Epics ✅** (009): 12 epic envelopes + initiative envelope, tree-checked; 1–10 go-live, 11–12 fast-follow; **not yet published** to GitHub · UX ⬜ · **Architecture ✅ final** (011) in `architecture-slotbook/`: AD-1–AD-22 + threat model; 6 reviews applied; single tenant, free hosting · `bmad-ticket` GitHub store configured ✅ |
| **Pipeline** | 🟡 Host ready ✅ · sandbox image + `run-job.sh` ✅ (plan, implement, fix in daily use; 013: sidecar oplog cap, CI mongosh race fix, push retry) · models per mode ✅ · CI ✅ · ⬜ `review` mode, worker, dashboard |
| **GitHub** | Bot `ashik71-slotbot` (Write, classic PAT with `workflow` scope) ✅ · ruleset `protect-main` ✅ (1 approval + required `ci-ok`, bound to GitHub Actions) · CI `ci.yml` ✅ · `ai:*` + BMAD store labels ✅ · milestone `initiative-booking-management` ✅ · Project board [SlotBook delivery](https://github.com/users/ashik71/projects/2) ✅ with pipeline columns; `board-sync` Action derives Status from labels |
| **Code** | Epic 1: stories 1–8 merged incl. splits 6b, 7a/7b, 8/8a/8b (PRs #39, #41, #42, #45–#47, session 013); 6a merged (PR #38); earlier: #12 (PR #16), #15 (PR #17), #14 (PR #19), #18 (PR #20) merged; sandbox gets MongoDB + RabbitMQ sidecars per run (`fae9037`). Epic 0 stories all merged: #4 `/health` + Angular page (PR #6, ~$1.50, 3 fix rounds), #7 backend style (PR #8, ~$1.10), #5 CI with required `ci-ok` (PR #9, ~$0.83). Red-check proof: PR #10 (closed). Epic #3 closed |
| **Blocked on** | Nothing |
| **Goal (010, 011)** | **Near-term target is a team demo**: one tenant, free hosting only, no paid fallback. Clinic launch parked, not dropped. Epic order and done line need re-sequencing |
| **Sequencing** | Finish architecture. Then publish the epics and incept epic 1 (deploy platform, no screens). `bmad-ux` before epic 2 is incepted. Build order: `_bmad-output/initiative-booking-management/tickets.toml` |

## Decisions made

| ADR | Decision | Status |
|---|---|---|
| 0003 | Target .NET 10 (all projects `net10.0`) | Accepted |
| 0004 | Sandbox agentic delivery: BMAD planning; agent codes FE + BE; PR comment loop; developer merges. OpenSpec dropped (amended 2026-10-09) | Accepted |
| 0005 | Angular for the frontend (deciding factor: the developer's review fluency) | Accepted |
| 0006 | Monorepo, deploy-independent `src/backend` (tests in `src/backend/tests`) + `src/frontend`; xUnit v3 + Shouldly on MTP; Angular 22 + Vitest | Accepted |
| 0007 | Backend code style: the developer's reference conventions, enforced as build errors (StyleCop, Meziantou, VS Threading) | Accepted |

| 0002 | Modular monolith, one deployable on one free Render web service | Accepted |
| 0008 | MongoDB on Atlas Free | Accepted |
| 0009 | Hosting on Render free tier; Azure later | Accepted |
| 0010 | RabbitMQ via MassTransit v8 (Apache 2.0); Kafka parked | Accepted |
| 0011 | Shared collections, `tenantId` on every document; **one configured tenant per deployment** (amended 011); no tenant in routes or headers | Accepted |
| 0012 | Concurrency: one invariant, one guard document | Accepted |
| 0013 | Backend module structure: `Modules/{Module}` + Contracts, 7 modules | Accepted |
| 0014 | Staff password + TOTP; server-side sessions for both audiences | Accepted |
| — | **Go-live line:** epics 1–10 before the first real booking; 11 announcements + 12 chat fast-follow; every epic ships to a pre-launch environment from epic 1 (developer, 2026-10-10) | Initiative Notes |

## Decisions deliberately still open

Don't let a session assume an answer to any of these.

| Question | Blocks | Target |
|---|---|---|
| Data retention period (Q3), uptime expectation (Q5), privacy notice text | Privacy policy, ops | Before launch |
| Free SMS quota for local numbers (Q1)? Live OTP is SMS only either way; it sets pre-sale cost and the default daily OTP budget | SMS provider | Clinic launch (spine Deferred; demo uses the dev channel) |
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
- **Azure (focus area 1) and Kafka (half of focus area 3) are parked** while hosting is
  Render and messaging is RabbitMQ only
- **Architecture blueprint (009):** the architecture follows the developer's reference
  architecture, an external client repo: Clean Architecture (Domain, Application,
  Infrastructure, Api, event contracts; aggregates; feature folders aligned across
  layers) and event-driven (integration events, MongoDB, inbox and outbox patterns).
  Firewall: extract generic patterns into a SlotBook input file; never copy its code,
  names or packages. Its company-private shared libraries can't be used, so SlotBook
  builds its own equivalents. ADR-0006 (test stack) and ADR-0007 (code style) win where
  they differ
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
| 009 | Note the deciding reason for MongoDB and for Render, for their ADRs in 010 | Done (010) |
| 010 | Read the architecture document (AD-1–AD-22, Epic 1 proofs) and approve or correct it | Done (011, reviewed by Claude) |
| 010 | Custom-domain fallback: own a domain, or accept ~$10/year as an NFR-16 exception? | Closed (011): free only; `wwwroot` fallback |
| 012 | Before story 9 (#30): add the Render deploy hooks as GitHub secrets (Claude guides) | Done (013) |
| 012 | Before story 11 (#32): create a Grafana Cloud free account (Claude guides) | Done (013); OTLP vars set in Render |
| 013 | Optional before story 11: Grafana Synthetics HTTP check on `/health/ready` (Claude guides) | Open |

---

## How to resume (for an AI session)

1. Read `CLAUDE.md`, then this file
2. Read the newest `docs/sessions/` log if more context is needed
3. Check `_bmad-output/initiative-booking-management/` for in-progress BMAD drafts
   and offer to resume them
4. **Don't re-derive settled decisions.** If an ADR exists, the question is decided
