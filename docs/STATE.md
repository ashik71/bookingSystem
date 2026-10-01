# STATE — where the project actually is

> **Every session reads this file first and updates it last.** It is the handoff
> between sessions. Keep it short; detail belongs in the session log and ADRs.

**Last session:** 003 · 2026-10-01
**Next session:** 004 — **ROADMAP Phase 1**: Web API & architecture, first vertical slice

---

## Current position

| | |
|---|---|
| **Sequencing** | **ROADMAP.md phase order — build-order, not CURRICULUM risk-order** |
| **Current phase** | **ROADMAP Phase 1 — ASP.NET Core Web API & Architecture** |
| **Phase 1 teaching** | ✅ **Complete — all 8 topics written to `docs/lessons/phase-1/`** |
| **Phase 1 build** | ⬜ Not started — developer writes all of it |
| **Slice order** | endpoint → command/query → repository → middleware |
| **Last topic completed** | A1 — Process, tooling & framing |
| **Code written so far** | Module skeleton (developer); template cruft removed |
| **Blocked on** | Nothing |

> **Sequencing decision (003, developer's call):** the project follows
> **`docs/learning/ROADMAP.md` phase order**, learning each concept at the point an
> app build naturally reaches it. `CURRICULUM.md` stays as the *topic depth*
> reference — its A/B/C blocks say what to cover, the roadmap says when.
> A2 (domain modeling) is **not dropped**; it arrives with the first real domain
> slice. Do not re-open this ordering debate at the start of a session.

## What exists right now

- `docs/` scaffolding complete: context, curriculum, ADR template, state tracking
- ADR-0001 accepted (OpenSpec over BMAD)
- `src/BookingSystem.Api/` — `net10.0` host. WeatherForecast template cruft removed
  (003). Pipeline is still template-default: OpenAPI in dev + `UseHttpsRedirection`,
  no endpoints. Middleware order is deliberately **not** built out yet — tenant
  resolution (B1) and rate limiting (B5) decide its shape
- `src/Modules/Booking/` — four-project Clean Architecture skeleton; `BookingModule`
  is the host's single registration entry point, body still empty
- `docs/lessons/phase-1/` — **all 8 Phase 1 lessons written** (003). Each has a
  ten-second recall table, an interview question bank with weak/strong/follow-up
  answers, exercises, and a "what this does not settle" table
- No Angular project, no docker-compose, no tests, no schema

## Decisions made (the short list)

| ADR | Decision | Status |
|---|---|---|
| 0001 | OpenSpec for change management; BMAD rejected | Accepted |
| 0003 | Target .NET 10 (all projects `net10.0`) | Accepted |

*ADR-0002 is reserved for modular monolith vs microservices — now reached through
curriculum topic **A5**, not as standalone homework (dropped 003).*

## Decisions deliberately still open

These are *known unknowns* — don't let a session accidentally assume one.

| Question | Blocks | Target |
|---|---|---|
| Bounded context boundaries | Everything downstream | **A2 (next)** |
| Is Slot inside the Booking aggregate? | The whole concurrency design | A3 |
| Multi-tenancy isolation model | First schema | A6 |
| EF Core vs NHibernate vs Dapper | First schema | A7 |
| PostgreSQL vs SQL Server | First schema | A8 |
| RabbitMQ vs Kafka split | Block E | E4 |

## Open threads / parked items

**ROADMAP.md is the pre-refocus plan — these parts are superseded.** Follow its
*phase order*, not these specifics:

| ROADMAP says | Actually | Authority |
|---|---|---|
| .NET 8 | .NET 10 | ADR-0003 |
| NHibernate | EF Core (NHibernate = optional detour) | PROJECT-CONTEXT §1 |
| Lamar DI | Built-in DI | PROJECT-CONTEXT §1 |
| AWS primary | Azure primary, AWS secondary | PROJECT-CONTEXT §1 |
| Angular full build | Thinnest UI that exercises the backend | PROJECT-CONTEXT §1 |
| Phase 15 legacy migration | Dropped | PROJECT-CONTEXT §1 |

⚠️ **Multi-tenancy is ROADMAP Phase 14 (last) but is a focus area.** Retrofitting
`TenantId` across a built schema is expensive. **Decide the isolation model before
Phase 2 creates the first table** (CURRICULUM A6). Flagged in 003; not yet resolved.

## Homework outstanding

| Set in | Task | Status |
|---|---|---|
| — | None outstanding | — |

*Dropped in 003: the session-001 homework to write ADR-0002 unaided. The decision
itself is **not** dropped — it is covered by curriculum topic **A5**, and the
ADR-0002 number stays reserved for it. Do not re-set this as homework.*

---

## How to resume (for an AI session)

1. Read `CLAUDE.md` — the rules, especially *you do not write production code*
2. Read this file — where we are
3. Read `docs/sessions/` for the last session's log, if more context is needed
4. Read the ADRs relevant to the next topic
5. Confirm with the developer: *"Last session we finished A1. Next is A2 — domain
   modeling, the highest-leverage session in the plan. Shall we start there?"*
   (No homework is outstanding — do not ask about ADR-0002.)
6. **Do not re-derive settled decisions.** If an ADR exists, it is decided. Reopen
   only if the developer asks or new evidence contradicts it
