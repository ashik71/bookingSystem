# STATE — where the project actually is

> **Every interactive session reads this file first and updates it last.** It is the
> handoff between sessions. Keep it short; detail belongs in session logs and ADRs.

**Last session:** 005 · 2026-10-09
**Next session:** 006: Epic 0 walking skeleton (`bmad-spec` + `bmad-ticket`, publish to GitHub, queue story 1). Then 007: `bmad-prd`

---

## Goal

Build SlotBook (frontend + backend) through a **sandbox agentic ecosystem**. The
developer plans with BMAD (epic → feature → story → task). Claude Code builds each
story in a Docker sandbox and opens a PR. The developer comments, the agent replies
and fixes, and the developer merges. Process: `docs/process/SANDBOX-WORKFLOW.md`.

## Current position

| Track | Status |
|---|---|
| **BMAD planning** | Brief ✅ · Epic 0 spec/tickets ⬜ next · PRD ⬜ · UX ⬜ · Architecture ⬜ · `bmad-ticket` GitHub store configured ✅ |
| **Pipeline** | 🟡 Host ready ✅ · sandbox image + `run-job.sh` ✅ (plan, implement, fix all tested by hand, 2026-10-09) · models per mode (`sandbox/models.conf`) ✅ · ⬜ `review` mode, CI, worker, dashboard |
| **GitHub** | Bot `ashik71-slotbot` (Write) ✅ · ruleset `protect-main` ✅ · `ai:*` + BMAD store labels ✅ · no issues yet · Project board ⬜ (needs `gh auth refresh -s project`) |
| **Code** | None. `src/` is empty; the first epic's foundation story creates the solution |
| **Blocked on** | Nothing. Planning and pipeline setup can run in parallel |
| **Sequencing** | Option C (2026-10-09): build an Epic 0 walking skeleton (Angular + .NET 10 + `/health`, no DB) through the sandbox while the PRD and architecture continue |

## Decisions made

| ADR | Decision | Status |
|---|---|---|
| 0003 | Target .NET 10 (all projects `net10.0`) | Accepted |
| 0004 | Sandbox agentic delivery: BMAD planning; agent codes FE + BE; PR comment loop; developer merges | Accepted |
| 0005 | Angular for the frontend (deciding factor: the developer's review fluency) | Accepted |

*ADR-0002 is reserved for modular monolith vs microservices (architecture step).*

## Decisions deliberately still open

Don't let a session assume an answer to any of these.

| Question | Blocks | Target |
|---|---|---|
| Reserved-slot timing vs publication; do practitioner bookings count towards limits | Booking rules | PRD |
| Per-number limits, booking horizon, cancellation cut-off, offline door check | Booking rules | PRD |
| Bounded contexts; is Slot inside the Booking aggregate? | Concurrency design | Architecture |
| Multi-tenancy isolation model | First schema | Architecture, **before the first table** |
| Database and ORM | First schema | Architecture |
| Does a free SMS quota for local numbers exist? (else email/push OTP) | OTP design | Architecture |
| RabbitMQ vs Kafka split | Messaging epic | Architecture |
| OpenSpec change folders alongside BMAD specs, or drop OpenSpec (recommended: drop) | Story workflow | **Session 006, before Epic 0's tickets** |
| Repo layout (`src/` frontend/backend split), test runners, Angular major | Epic 0 story 1 | Session 006 |
| Where the worker and dashboard live (default: `sandbox/` here, dashboard as its own epic) | Pipeline build | Architecture |

## Open threads

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
| 005 | `gh auth refresh -s project` (for the Project board) | Open |
| 005 | `colima stop` after each sandbox session | Habit |

---

## How to resume (for an AI session)

1. Read `CLAUDE.md`, then this file
2. Read the newest `docs/sessions/` log if more context is needed
3. Check `_bmad-output/initiative-booking-management/` for in-progress BMAD drafts
   and offer to resume them
4. **Don't re-derive settled decisions.** If an ADR exists, the question is decided
