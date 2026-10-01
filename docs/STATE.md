# STATE — where the project actually is

> **Every session reads this file first and updates it last.** It is the handoff
> between sessions. Keep it short; detail belongs in the session log and ADRs.

**Last session:** 002 · 2026-10-01
**Next session:** 003 — continue/finish **A2: Domain modeling & bounded contexts**

---

## Current position

| | |
|---|---|
| **Current block** | A — Foundations you cannot cheaply reverse |
| **Last topic completed** | A1 — Process, tooling & framing |
| **Next topic** | **A2 — Domain modeling & bounded contexts** |
| **Phase of A2** | In progress — teaching started session 002 |
| **Code written so far** | None. Default template only |
| **Blocked on** | Nothing |

## What exists right now

- `docs/` scaffolding complete: context, curriculum, ADR template, state tracking
- ADR-0001 accepted (OpenSpec over BMAD)
- `src/BookingSystem.Api/` — untouched `net10.0` template, WeatherForecast endpoint
  still present
- No Angular project, no docker-compose, no tests, no schema

## Decisions made (the short list)

| ADR | Decision | Status |
|---|---|---|
| 0001 | OpenSpec for change management; BMAD rejected | Accepted |
| 0003 | Target .NET 10 (all projects `net10.0`) | Accepted |

*ADR-0002 is reserved for the developer's homework: modular monolith vs microservices.*

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

- Nothing parked yet.

## Homework outstanding

| Set in | Task | Status |
|---|---|---|
| 001 | Write ADR-0002 (modular monolith vs microservices) **unaided**, then defend it against push-back | ⬜ Not started |

---

## How to resume (for an AI session)

1. Read `CLAUDE.md` — the rules, especially *you do not write production code*
2. Read this file — where we are
3. Read `docs/sessions/` for the last session's log, if more context is needed
4. Read the ADRs relevant to the next topic
5. Confirm with the developer: *"Last session we finished A1. Next is A2 — domain
   modeling. Did you complete the homework? Shall we start A2?"*
6. **Do not re-derive settled decisions.** If an ADR exists, it is decided. Reopen
   only if the developer asks or new evidence contradicts it
