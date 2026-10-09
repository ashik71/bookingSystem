---
tracker_id: "3"
remote: "https://github.com/ashik71/bookingSystem/issues/3"
type: epic
title: "Walking skeleton: the web page shows the API's health, checked by CI"
parent: initiative-booking-management
covers: []
after: []
assignee: ""
risk: medium
---

# Walking skeleton: the web page shows the API's health, checked by CI

## Description

This is the platform baseline that every later epic builds on. An Angular 22 page calls the
.NET 10 API's `/health` endpoint and shows the result. CI builds and tests each side on
every PR that touches it. There is no database and no deployment. The spec
`spec-walking-skeleton` owns the capabilities, constraints and non-goals, and this epic
delivers them through the sandbox pipeline.

## Outcome

For the developer: the sandbox pipeline is proven on real code across both sides, and the
first product epic has a solution, a test setup and CI to build on. The signal is the
spec's success signal.

## Requirements

The spec's capabilities, CAP-1 to CAP-4 (see References). This is the platform baseline,
so `covers` is empty: the initiative has no numbered source yet. The source for this
epic is session 006 and ADR-0006.

## Done when

1. From a clean clone, following the README, the page shows the API healthy. With the
   API stopped, the page shows it as unavailable.
2. `dotnet test` from the repo root and `npm test` in `src/frontend` both pass. They
   include the `/health` integration test and tests of both page states.
3. A PR touching only one side runs only that side's build and tests, and a failing test
   prevents the merge.
4. Every story in this epic was built by the sandbox (plan, implement, fix) and merged
   through PR review.

## Boundaries

Capability boundary: the platform baseline (scaffold, local run, CI). It is not
deployment or cloud resources, which are a later epic. The rest is out of scope as the
spec's Non-goals say.

## References

- spec — _bmad-output/initiative-booking-management/epic-walking-skeleton/spec-walking-skeleton/spec-walking-skeleton.md, sections Capabilities, Constraints, Non-goals
- constraint — docs/adr/0006-monorepo-layout-and-test-stack.md, section Decision (layout, test stack, test commands)
- constraint — docs/adr/0004-sandbox-agentic-delivery-with-bmad.md, section Decision (one story = one PR)
- process — docs/process/SANDBOX-WORKFLOW.md

## Notes

- Decision: Azure deployment is excluded from this epic; a later epic owns it (developer, 2026-10-09).
- Decision: the tracer bullet is entry 1. It goes from the API's `/health` to the Angular page, with tests on both sides and the local run (2026-10-09).
- Decision: required checks use Option B. One CI workflow detects which side changed and runs only that side's jobs. An aggregate job always runs and is the only required check (developer, 2026-10-09).
- Decision: entries 1 and 2 carry full acceptance criteria (`refine = true`), because the sandbox agent builds from the issue alone (developer, 2026-10-09).
- Decision: no refactor sweep, because the epic has only two entries (2026-10-09).
- Decision: the bot's token is a classic PAT. The developer confirms it has the `workflow` scope before entry 2 starts, as a hitl step on entry 2 (2026-10-09).
