# STATE — where the project actually is

> **Every session reads this file first and updates it last.** It is the handoff
> between sessions. Keep it short; detail belongs in the session log and ADRs.

**Last session:** 004 · 2026-10-09
**Next session:** 005 — `bmad-prd` (create) from the finished brief

---

## Current position

| | |
|---|---|
| **Delivery model** | **Sandbox agentic workflow (ADR-0004)** — developer plans and reviews, agent writes all code |
| **Current stage** | **BMAD planning.** Brief ✅ final (`_bmad-output/initiative-booking-management/brief-slotbook/`); PRD next |
| **Planning chain** | brief → PRD → UX → architecture → per-epic spec + tickets → GitHub Issues |
| **Code** | **None.** `src/` and the `.sln` were wiped (004) so the pipeline starts from scratch |
| **BMAD initiative** | `initiative-booking-management` (`_bmad-output/initiative-booking-management/`) |
| **Sandbox infra** | ⬜ Not started — guideline Phases 0–2 (Mac setup, sandbox image, `ai:*` labels) |
| **Blocked on** | Nothing |

> **Pivot (004, developer's call):** the project switched from "developer writes all
> code" to full sandbox agentic delivery. ADR-0001 is superseded by ADR-0004.
> The SlotBook product definition and the six focus areas are **unchanged**.
> Do not re-argue the delivery model at the start of a session.

## What exists right now

- `docs/` — context, curriculum, roadmap, Phase 1 lessons, ADRs, state tracking
- `docs/AIAgenticGuideline/` — the sandbox workflow guideline (**untracked; contains a
  client reference — scrub before committing**)
- BMAD installed (`_bmad/`, `.claude/skills/bmad-*`), initiative set
- No code, no solution, no tests, no schema, no sandbox image

## Decisions made (the short list)

| ADR | Decision | Status |
|---|---|---|
| 0001 | OpenSpec for change management; BMAD rejected | **Superseded by 0004** |
| 0003 | Target .NET 10 (all projects `net10.0`) | Accepted |
| 0004 | Sandbox agentic delivery; BMAD for planning; agent codes, developer reviews and merges | Accepted |

*ADR-0002 is reserved for modular monolith vs microservices.*

## Decisions deliberately still open

These are *known unknowns* — don't let a session accidentally assume one. Most now
get settled in the BMAD architecture step.

| Question | Blocks | Target |
|---|---|---|
| Bounded context boundaries | Everything downstream | PRD / architecture |
| Is Slot inside the Booking aggregate? | The whole concurrency design | Architecture |
| Multi-tenancy isolation model | First schema | Architecture — **before the first table** |
| EF Core vs Dapper; PostgreSQL vs SQL Server | First schema | Architecture |
| Frontend framework (thin UI) | First UI story | Architecture |
| RabbitMQ vs Kafka split | Messaging epic | Architecture |
| OpenSpec vs BMAD spec/ticket for per-change specs | Story workflow | Before first epic's tickets |
| Does a free SMS quota for local numbers exist? (else email/push OTP) | OTP design | Architecture |
| Reserved-slot timing vs publication; do practitioner bookings count toward limits | Booking rules | PRD |
| Per-number limits, booking horizon, cancellation cut-off, offline door check | Booking rules | PRD |

## Open threads / parked items

- ⚠️ **Client-IP leak in git history:** ADR-0001's Context section names the client
  and a price, and it is already committed. Needs scrubbing from the file *and*
  history before the repo goes public
- `docs/learning/ROADMAP.md` and `CURRICULUM.md` assumed hand-written code; their
  phase order no longer drives the build — BMAD epics do. Still useful as topic-depth
  reference for planning and PR review

## Homework outstanding

| Set in | Task | Status |
|---|---|---|
| 004 | Guideline Phase 0: Mac setup, `claude setup-token`, repo-scoped fine-grained PAT, protect `main` | Open |
| 004 | Scrub the client reference from `docs/AIAgenticGuideline/` before committing it | Open |

---

## How to resume (for an AI session)

1. Read `CLAUDE.md` — the delivery contract (ADR-0004)
2. Read this file — where we are
3. Read the newest `docs/sessions/` log if more context is needed
4. Check `_bmad-output/initiative-booking-management/` for in-progress BMAD drafts
   and offer to resume them
5. **Do not re-derive settled decisions.** If an ADR exists, it is decided
