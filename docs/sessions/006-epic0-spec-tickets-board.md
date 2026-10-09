# Session 006 — epic0-spec-tickets-board: Epic 0 specced, sliced and published; board automated; first real plan run

**Date:** 2026-10-09 · **Duration:** ~2.5h · **Stage:** planning (`bmad-spec`, `bmad-ticket`) + pipeline setup

## What was covered

- **OpenSpec dropped.** The folder was empty and untracked.
- **Repo layout and test stack settled** (ADR-0006). The developer asked whether the
  frontend and backend should live in separate repos. Settled as a monorepo with
  folders that deploy independently.
- **Board columns** set to the pipeline flow, with a GitHub Action that derives the
  card Status from the labels.
- **Epic 0 spec** written with `bmad-spec`: 4 capabilities.
- **Epic 0 sliced** with `bmad-ticket` into 2 stories, both with full acceptance
  criteria, and published to GitHub.
- **Story #4 queued** and the first real `plan` run started.

## Decisions reached

| Decision | Recorded in |
|---|---|
| Drop OpenSpec. BMAD owns specs and tickets | ADR-0004 (amended) |
| One repo with folders that deploy independently: `src/backend` (tests in `src/backend/tests`, amended later in the session), `src/frontend`, a root `SlotBook.slnx` and central props. Each side has its own path-filtered CI and deploy. A split later means `git filter-repo` | ADR-0006 |
| Test stack: xUnit v3 + Shouldly. Angular 22 + Vitest/jsdom, npm. Test commands: `dotnet test` (root) and `npm test` in `src/frontend` | ADR-0006, sandbox prompts |
| Board: 9 Status columns, one per pipeline state. A `board-sync` Action derives Status from the labels; the token is a classic PAT with `project` scope only | `SANDBOX-WORKFLOW.md`, `.github/workflows/board-sync.yml` |
| No Azure deployment in Epic 0; a later epic owns it | Epic 0 Notes, spec Non-goals |
| CI Option B: one workflow with change detection, and an always-running `ci-ok` check that is the only required check | Spec Constraints, story #5 |
| Stories carry full Given/When/Then criteria (`refine = true`), because the sandbox agent builds from the issue alone. This overrides BMAD's default | Epic 0 Notes |
| Third-party actions pinned to a commit SHA; the CI `GITHUB_TOKEN` is read-only | Story #5 Notes |
| Backend tests move into `src/backend/tests` (the developer deferred to the recommendation) | ADR-0006 (amended) |
| Tests run on Microsoft.Testing.Platform with `xunit.v3` 4.x. VSTest was dropped after the agent had to pin an old xunit | PR #6 |
| Backend style follows the developer's reference conventions, written fresh with no external names (client-IP firewall). Enforced as build errors; CS0108 and one-type-per-file kept on. Delivered as story #7 | ADR-0007 |
| A repo-root `nuget.config` with nuget.org only; Node ≥ 24.15 | PR #6 |

## Pressure-test — where the developer was challenged

- **"Separate repos for the frontend and backend. Will one repo hurt deployment?"**
  - The answer: no. Independent deploys come from path-filtered workflows, not from the
    repo boundary.
  - Two repos would break ADR-0004's one-story-one-PR rule: two blind sandbox runs would
    each guess the API contract.
  - The developer accepted the monorepo (Option A). The concern was a factual question,
    not a firm position.
- **Required checks.** The developer hadn't seen the trap: a skipped path-filtered
  workflow never reports, so it blocks the merge. Option B was chosen on the
  recommendation.
- **BMAD's default of unrefined stories versus `CLAUDE.md`'s "agent-proof criteria".**
  The conflict was raised explicitly, and the developer chose full criteria.

## Artifacts produced

- **Docs:**
  - ADR-0006;
  - ADR-0004 amended;
  - updates to `PROJECT-CONTEXT.md`, `SANDBOX-WORKFLOW.md` and STATE;
  - the `implement` and `fix` prompts now name the exact test commands.
- **Pipeline:** `.github/workflows/board-sync.yml`, and the repo secret `PROJECT_TOKEN`.
- **BMAD:**
  - `epic-walking-skeleton/` holds the spec (+ memlog), the epic, `tickets.toml` and
    two refined stories;
  - the initiative's `tickets.toml`.
- **GitHub:**
  - milestone `initiative-booking-management`;
  - epic #3, with stories #4 and #5 as sub-issues (#5 blocked by #4);
  - the board's Status options replaced with the pipeline columns.

## PRs reviewed

| PR | Story | Iterations | What the agent got wrong | Root cause (story / spec / CLAUDE.md / agent) |
|---|---|---|---|---|
| #6 | #4 (the health page) | plan + implement + 3 fix rounds, about $1.50 in total | 1. It pinned `xunit.v3` to an old 3.2.2 to stay on VSTest. 2. Tests lived outside `src/backend`. 3. The build broke on a machine with a second NuGet feed (`NU1507`). 4. The README said "Node 24", but Angular 22 needs ≥ 24.15 | 1: **my plan answer** (I told it to stay on VSTest). 2: **the ADR** (the layout was amended). 3 and 4: **spec gaps** (the sandbox is cleaner than a dev machine). The agent followed every instruction, disclosed each deviation, and replied once per thread naming the fixing commit |
| #8 | #7 (backend style) | plan + implement, 0 fix rounds, about $1.10 | Nothing. The plan **prototyped the config in a throwaway copy** and found two holes in ADR-0007 before any code was written: `this.` couldn't fail the build, and stable StyleCop raises a false SA1516. It also flagged that the ADR didn't list the IDE ids | **ADR gaps**, caught at plan time. Lesson: a plan run that prototypes is worth its cost ($0.85) |

## Rules added to `CLAUDE.md`

None.

## Gaps exposed

- **The token classifier blocks reads of Keychain secrets** from the session. Token
  permissions have to be checked by the developer. Story #5 can't start until the bot's
  classic PAT is confirmed to have the `workflow` scope.
- **Labels are still moved by hand.** No worker exists, and `run-job.sh` doesn't set
  `ai:*` labels.
- **No independent validation subagent was run** on the breakdown. The Set and
  Dependencies checks ran inline.
- **The sandbox can't see machine-specific failures.** Two portability bugs, extra NuGet feeds and the Node minimum, were found only by running on the Mac. A local smoke check before merging stays a manual step for now.
- **The sandbox image had a permission bug.** `/home/agent/.nuget` was owned by root, so the NuGet cache volume never filled. Fixed in the Dockerfile.
- **The initiative envelope** (`initiative-booking-management.md`) is still frontmatter
  only.

## Homework set

- [ ] Confirm that the bot's classic PAT has the `workflow` scope (blocks #5)
- [ ] Review the plan comment on #4. Approve it (→ `ai:implementing`) or comment on it
      (→ `ai:planning`)
- [ ] Write the brief entry in `docs/learning/LEARNINGS.md` (carried over from 004)
- [ ] `colima stop` after the sandbox runs

## Parked for later

- **Frontend code-style reference:** ADR-0007 covers the backend only; the frontend keeps the Angular CLI defaults.

- **The Azure deploy epic:** after the PRD and architecture.
- **Having `run-job.sh` set the `ai:*` labels:** with the worker.
- **Playwright E2E:** when there's a real user flow.

## Next session

**007:**
1. Run #5 (CI) through the sandbox. First confirm that the bot's classic PAT has the `workflow` scope.
2. After it merges, make `ci-ok` a required check and run the failing-test proof (criterion 7).
3. Close Epic 0 (closure check).
4. Then `bmad-prd`.
