# Session 003 — Sequencing change + Phase 1 lesson set

**Date:** 2026-10-01 · **Block:** ROADMAP Phase 1 · **Topic:** 01–08

## What was covered

- Cleaned `Program.cs` — removed the WeatherForecast template cruft. Nothing else
  touched; the pipeline is still template-default
- **Sequencing changed** (see below), which was the substantive outcome
- Wrote the complete **Phase 1 lesson set**: 8 topics, ~3,700 lines, in
  `docs/lessons/phase-1/`
- Reconciled CLAUDE.md and STATE.md with the new sequencing

**Not covered:** no teaching dialogue, no pressure test, no code written by the
developer. This session produced reference material and a process decision, not
a taught topic. The lessons are the input to Phase 1, not evidence it is learned.

## Decisions reached

| Decision | Recorded in |
|---|---|
| **Sequencing: follow ROADMAP phase order, not CURRICULUM risk order** | `STATE.md` §Current position |
| CURRICULUM.md demoted to *topic depth* reference; ROADMAP drives *when* | `STATE.md`, `CLAUDE.md` docs map |
| Controllers, not Minimal APIs | Lesson 01 header |
| ADR-0002 homework (unaided monolith-vs-microservices) **dropped** | `STATE.md` §Homework |
| ADR-0002 *number* stays reserved; the decision moves to A5 / lesson 05 | `STATE.md` |
| New `docs/lessons/` tree, topic-named files, phase subfolders | `docs/lessons/README.md` |

### The sequencing change, in full

CURRICULUM.md ordered topics by **architectural risk** — decide the irreversible
things (bounded contexts, aggregates, multi-tenancy) before writing code. The
developer chose **build order** instead: learn each concept at the point an app
build naturally reaches it, vertical slice by vertical slice.

Both are defensible. The developer's call stands. The risk it accepts is recorded
under "Parked" below.

## Pressure-test — where the developer was challenged

**AI argued against building the middleware pipeline now.** Position: six of eight
middleware positions depend on undecided questions (B1 tenant resolution, B5 rate
limiting, C2–C4 auth), so building it means encoding guesses.

**Developer's response:** overruled on sequencing grounds — the roadmap puts
middleware in Phase 1, and learning it at the point the build reaches it is the
whole point of the chosen order.

**Did the defence hold?** Yes, on process. The AI had been treating CURRICULUM as
authoritative when ROADMAP.md was the developer's actual plan — the AI was wrong
about *which document governs*. The underlying technical caution still stands and
is recorded in lesson 02 §7: the pipeline can be *learned* now, but the tenant
resolution position should not be *committed* until B1 is decided.

**AI challenged the lesson-only approach once more** by asking whether to write
`/health` directly; developer held the learning contract. Correct call — contract
intact, no production code written by the AI this session.

**Not pressure-tested:** the developer has not yet defended any technical position
in Phase 1. That has not happened yet and should, once code exists.

## Code the developer wrote

None this session.

AI changes, both non-implementation:
- `src/BookingSystem.Api/Program.cs` — deleted WeatherForecast endpoint, record and
  `summaries` array. Pure subtraction, explicitly authorised. Build clean, 0 warnings
- Docs only otherwise

## Review findings

Nothing to review — no developer code.

Two **pre-existing issues** found while reading the repo:

1. **Session 002 was never logged.** `STATE.md` claimed A2 teaching had started, but
   `docs/sessions/002-*.md` does not exist and `docs/design/` is empty. The content
   of that session is lost. CLAUDE.md calls the end-of-session write
   non-negotiable; it did not happen
2. **CLAUDE.md had contradictory rows** for ROADMAP.md — one calling it the live
   plan, one calling it reference. Fixed

## Gaps exposed

Nothing about the developer's knowledge — no quiz ran, no code was written. The
gaps found were **in the documentation system**, not the developer:

- The continuity system failed once already (session 002) and nothing detected it
  until a later session read the directory
- `STATE.md` had drifted: "Code written so far: None. Default template only" was
  false — the Booking module skeleton already existed
- The roadmap's staleness (.NET 8, NHibernate, Lamar, AWS-primary) was undocumented,
  so each session risked re-deriving it. Now tabulated in `STATE.md`

## Homework set

- [ ] Work through the Phase 1 lessons in order
- [ ] **Before any other build work:** add `Directory.Build.props` with
      `TreatWarningsAsErrors` (lesson 08 §6) — free now, painful to retrofit
- [ ] **Decide controller placement** — host or modules (lesson 05 §4). Everything
      else in Phase 1 inherits this
- [ ] Phase 1 build: module structure, error-handling middleware, Swagger,
      `/health`, versioning, `.editorconfig`

## Parked for later

| Parked | Reason |
|---|---|
| **A2 — bounded contexts / domain modeling** | Build order reaches it with the first real domain slice. CURRICULUM calls it the highest-leverage session; it is deferred, **not** dropped |
| Middleware pipeline *composition* | Taught in lesson 02; the tenant-resolution position stays uncommitted until B1 |
| ⚠️ **Multi-tenancy isolation model** | ROADMAP puts it at Phase 14, but **Phase 2 creates the first tables.** Retrofitting `TenantId` across a built schema is expensive. Decide before Phase 2 schema work |
| Session 002's lost content | Unrecoverable. If A2 ground was covered, it will be re-covered |

### The risk this sequencing accepts

Build order reaches multi-tenancy (Phase 14) long after the schema exists (Phase 2),
and bounded contexts after modules are already scaffolded. Both are the exact
"cannot cheaply reverse" decisions CURRICULUM Block A was ordered to front-load.
Mitigation: the Phase 2 gate above. Recorded so the choice is deliberate rather
than accidental.

## Next session

**ROADMAP Phase 1 build**, or review of it if the developer builds first.

Ready before it starts:
- Lessons 01–03 read (mechanics: controllers, middleware, DI)
- Controller placement decided
- `Directory.Build.props` in place

The developer has written no code in three sessions. Next session should end with
code reviewed and a position defended, or the learning contract is only producing
documentation.
