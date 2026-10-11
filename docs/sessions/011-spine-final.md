# Session 011 — spine-final: final review of the architecture spine, single tenant, marked final

**Date:** 2026-10-11 · **Duration:** ~1h · **Stage:** planning (`bmad-review`, `bmad-architecture` follow-up)

## What was covered

- The developer asked Claude to review the spine instead of reading it themselves. Four
  lenses ran in pairs: adversarial (29 findings), edge-case (40), structure (15) and
  prose (36). Claude checked the findings against the text and triaged them in
  `reviews/review-final-read.md`.
- The developer pushed back on the complexity of the report. The scope was narrowed to
  the team demo: **single tenant, free hosting only**.
- Applied in one pass: the single-tenant AD-5, the free-only fallbacks, and the
  MongoDB write-mechanics fixes an agent would otherwise build wrong. ADR-0011 amended;
  threat model updated; the spine set to `status: final`.
- **Epic 1 incepted and building.** The envelope was updated for the final spine and
  sliced into stories (`tickets.toml`); story 2 came back from its plan at about 45 files
  and was split into #12, #14 and #15. Claude added the MongoDB and RabbitMQ sidecars to
  `run-job.sh`. The developer then delegated review and merge to Claude **for the day**:
  #12 (PR #16, one fix round for AD-2 and AD-17) and #15 (PR #17) are merged; #14
  implement and #18 plan were running at the end.
- **Not done:** the team slide deck and the correct-course step for epics 2–12.

## Decisions reached

| Decision | Recorded in |
|---|---|
| One deployment serves one tenant, from configuration, seeded at startup. `tenantId` stays on every document; no `X-Tenant`, no host mapping | ADR-0011 amendment, AD-5 |
| Demo hosting is free only; no paid fallback. Custom-domain question closed; the rewrite fallback is the API serving the Angular build from `wwwroot` | AD-18, Epic 1 proofs |
| On `UnknownTransactionCommitResult`, retry only the commit; never catch a write error and continue inside a transaction; insert-if-absent writes use upsert `$setOnInsert` | AD-7, AD-8, AD-20, AD-21 |
| Keyed guard documents get deterministic `_id`s (the one exception to Guid v7) | AD-6, AD-8, AD-9 |
| The consumer's unit of work commits together with the inbox mark (new Epic 1 proof) | AD-10, Epic 1 proofs |
| FR-26 and FR-32 limits are counter documents with a conditional `$inc` | AD-22 |

## Pressure-test — where the developer was challenged

- Claude offered to drop `tenantId` entirely or keep it. The developer kept it, so
  multi-tenancy stays a concept in the data and later only changes how the tenant is
  resolved.
- The developer challenged Claude, not the reverse: "why you always made thing
  complex". That held. The review surfaced about 100 findings without filtering them
  for the demo scope.

## Artifacts produced

- `architecture-slotbook/architecture-slotbook.md`: `final`
- `architecture-slotbook/reviews/review-final-read.md`
- `architecture-slotbook/threat-model.md`, `.memlog.md`
- `docs/adr/0011-...` amended
- Memory: filter reviews to the demo scope

## PRs reviewed

| PR | Story | Iterations | What the agent got wrong | Root cause |
|---|---|---|---|---|
| #16 | #12 layout and error format | 2 | Application didn't reference Domain and Api referenced Domain directly (AD-2); `[LoggerMessage]` methods inline instead of `LoggerExtensions.cs` (AD-17) | agent: rules are in the spine but not yet enforced by architecture tests (story 3) |
| #17 | #15 clock, tenant, readiness | 1 | nothing | — |

## Rules added to `CLAUDE.md`

None.

## Gaps exposed

- Reviewer output has to be filtered against the current scope before it reaches the
  developer. The demo scope is: single tenant, free hosting, and only what an agent
  would build wrong.
- MongoDB transaction semantics differ from SQL Server in ways that matter here: any
  write error aborts the transaction, and a commit result can be unknown. These are now
  rules in AD-7.

## Homework set

- [ ] Write the brief entry in `docs/learning/LEARNINGS.md` (carried over from 004)
- [ ] Create the free accounts for the deploy lane: Render, Atlas M0, CloudAMQP Little Lemur, Grafana Cloud

## Parked for later

- The structure lens's AD index and section moves, and most prose fixes. They help, but
  the spine is usable as it is. Revisit if agents keep missing rules.

## Next session

Check the open PRs (#14's, #18's). Finish epic 1's backend lane: #13, then stories 5–8.
Start the deploy lane once the accounts exist. The team slide deck and correct-course: the epic envelopes, the three PRD changes, the
single-tenant change to the tenant epic, and re-sequencing for the demo.
