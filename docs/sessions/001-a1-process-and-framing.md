# Session 001 — A1: Process, tooling & framing

**Date:** 2026-10-01 · **Block:** A · **Topic:** A1

## What was covered

- Inspected the repo: default `net10.0` web template, WeatherForecast endpoint,
  empty `src/cases/`, two commits. Clean slate, nothing to unlearn
- Reconciled three conflicting inputs into one project definition:
  1. A proposed folder structure (multi-tenant SaaS, modular monolith, full
     infrastructure set)
  2. A client project plan in Bengali (single-tenant, payments removed, managed
     PostgreSQL, ~1,000 users/week)
  3. A 15-phase learning roadmap built around a specific job specification
- **OpenSpec vs BMAD** evaluated and decided
- Established the learning contract: developer writes all production code
- Set up the documentation structure and the session-continuity system

## Decisions reached

| Decision | Recorded in |
|---|---|
| OpenSpec for change management; BMAD rejected | ADR-0001 |
| Build the multi-tenant platform, not the single-tenant client app | PROJECT-CONTEXT §1 |
| Client work stays out of this repo entirely (IP firewall) | PROJECT-CONTEXT §2, CLAUDE.md |
| Six focus areas drive all scope decisions | PROJECT-CONTEXT §1 |
| Azure primary, AWS as a secondary comparison pass | PROJECT-CONTEXT §7 |
| Observability built on OpenTelemetry; backends interchangeable behind it | CURRICULUM F8–F8c |
| Curriculum reordered: decisions-first, not the original phase order | CURRICULUM.md |

## Scope cut from the original roadmap

.NET Framework 4.8 migration · Lamar · deep Angular · deep MongoDB ·
NHibernate as the main ORM · AWS as primary cloud.
Rationale per item in PROJECT-CONTEXT §1.

## Course corrections during the session

Worth recording, because both were the AI misreading the project:

1. **Initially framed the over-engineering as a mistake to argue out of.** It was
   deliberate — driven by a job specification. An ADR arguing to drop Kafka, Mongo
   and Redis was written and then removed. The correct framing is *"these are in
   scope; where does each genuinely belong, and can you defend the split?"*
2. **Initially put client pricing, margins and negotiation strategy into this
   repo.** The roadmap explicitly warns against reusing client material, and this
   repo may become public. Rewritten with a stated IP firewall.

## Pressure-test

None yet — this session was framing, not design. Pressure-testing starts at A2.

## Code the developer wrote

None. Documentation session.

## Gaps exposed

Not probed yet. Known from self-assessment: **SQL is the weak spot** (2 years) —
indexes, isolation levels, locking, query plans. This directly blocks Block D, so
A8 treats it as a real topic rather than a footnote.

## Homework set

- [ ] Write **ADR-0002: modular monolith vs microservices** unaided, using
      `docs/adr/0000-template.md`. Then defend it against push-back next session.
      The goal is practising the *form* of a decision record — options weighed
      honestly, consequences owned, a concrete revisit trigger — on a question
      where the answer is already fairly clear. The reasoning is the exercise.

## Parked for later

- `.NET 8 vs .NET 10` — installed SDK is 10.0.401, roadmap assumed 8. Not blocking
  anything yet; decide by A4
- `src/cases/` — empty directory of unclear purpose. Resolve when the module
  structure is designed (A4)
- The WeatherForecast endpoint is still in `Program.cs`. It goes when real
  structure arrives

## Next session

**A2 — Domain modeling & bounded contexts.** The highest-leverage session in the
plan: wrong boundaries here are inherited by every later module decision.

Before starting, the developer should have attempted ADR-0002.
