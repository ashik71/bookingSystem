# Session 012 — sandbox-loop-epic1-backend: Claude drives the story loop through epic 1's backend lane

**Date:** 2026-10-11 · **Duration:** ~5h · **Stage:** build review

## What was covered

- Found STATE stale: story 1 (#21, PR #22) was merged after 011's log and the demo is
  live (`slotbook-api-sc87` and `slotbook-web` on Render, `/health/ready` reports
  MongoDB healthy).
- Built and merged #13 (architecture rules), story 5 split into 5a #24 / 5b #25 /
  5c #26, all merged. Story 6 split into 6a #27 / 6b #37; **PR #38 (6a) is green and
  `ai:pr-ready`, waiting for the developer's merge call**.
- Published every remaining epic 1 story to GitHub in build order (#27–#33, #37).
- Fixed a pipeline bug: `sandbox/entrypoint.sh` dropped the sidecar connection
  strings when switching to the agent user (`ec7d3d1`).
- Started correct-course for epics 2–12; **parked** at the developer's request until
  UX work starts.
- The developer created the CloudAMQP (Little Lemur) instance and set
  `ConnectionStrings__rabbitmq` in Render.

## Decisions reached

| Decision | Recorded in |
|---|---|
| New working mode: Claude runs the sandbox story by story (plan → approve plan → implement → review → fix → `ai:pr-ready`); **the developer alone merges** | memory `claude-drives-sandbox-loop` |
| The demo is the **full project**, not a subset; no re-sequencing | memory; correct-course (parked) |
| Correct-course and epic 2 inception wait for UX planning | this log |
| Unit of work writes every aggregate loaded through a write repository, changed or not (stricter than AD-7's wording) | #24 approval; `MongoUnitOfWork` XML doc |
| Startup blocks on the schema (collections → migrations → indexes, ~60 s timeout); migration lock in `leases` | #24 approval; PR #36 |
| `/health/ready` stays MongoDB-only; the bus is excluded (outbox covers broker downtime) | #27 approval; PR #38 |
| Consumers get an inner jittered retry for write conflicts before the 1/5/15/30 s policy; refusals are handled (no error queue) | #27 approval; #37 plan comment |
| Banned APIs also cover `Guid.CreateVersion7()` with no args and test projects | #13 approval |

## Pressure-test — where the developer was challenged

None. The developer delegated design-level answers on plans to Claude this session.

## Artifacts produced

- Issues #24–#33, #37 (epic 1 stories, split entries 15, 16, 17 in `tickets.toml`)
- PRs merged: #23, #34, #35, #36. Open: #38
- `sandbox/entrypoint.sh` fix

## PRs reviewed

| PR | Story | Iterations | What the agent got wrong | Root cause |
|---|---|---|---|---|
| #23 | #13 arch rules | 2 | Rule tests could pass on an empty architecture; unused package | agent |
| #34 | 5a unit of work | 2 | Synchronous transaction abort on dispose; `FindAsync` footgun undocumented | agent |
| #35 | 5b retries | 1 | nothing | — |
| #36 | 5c schema | 2 | Deleted the post-startup readiness tests instead of adapting them | agent |
| #38 | 6a outbox | 3 | Outbox collections created implicitly in a transaction; drain helper used wrong collection names; CI smoke test missing the RabbitMQ string | agent + reviewer (Claude reviewed before CI finished) |

Sandbox spend this session: about $16.

## Rules added to `CLAUDE.md`

None. Candidate if it repeats: "never delete a test to make a behaviour change pass;
adapt how the test reaches the state".

## Gaps exposed

- Reviewer process: wait for CI on the PR's first commit before reviewing (missed on #38).
- The 10-file guideline is too small for SharedKernel infrastructure stories; plans
  keep proposing splits, which worked well (each split plan answered once, then
  pasted into the child issues so they skip their plan runs).

## Homework set

- [ ] Merge PR #38 (6a) — reviewed, green
- [ ] Before story 9 (#30): Render deploy hooks as GitHub secrets (Claude will guide)
- [ ] Before story 11 (#32): Grafana Cloud free account (Claude will guide)
- [ ] Carried: brief entry in `docs/learning/LEARNINGS.md`; `colima stop` after sessions

## Parked for later

- Correct-course for epics 2–12 (envelope edits from `review-reconcile-epics.md`, PRD
  changes FR-39 retire-only / FR-42 append-only / FR-2+FR-6 dev outbox, single-tenant):
  waits for UX.
- The team slide deck.

## Next session

1. Check PR #38 merged; then run 6b **#37** straight to `implement` (plan already
   approved on #27, extracted on #37). 6b must turn MassTransit's inbox cleanup service
   back on without letting MongoDB loss stop the host.
2. Then #28 (story 7, timed jobs; needs a plan run), #29 (8, cookies/CSRF), #30 (9,
   deploy hooks, hitl), #31 (10, free-tier proofs, hitl), #32 (11, Grafana, hitl),
   #33 (12, refactor sweep).
3. Sync the ticket tree's states with GitHub (closed stories still show `backlog` locally).
