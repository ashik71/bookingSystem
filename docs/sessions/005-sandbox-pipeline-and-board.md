# Session 005 — sandbox-pipeline-and-board: sandbox built and tested; BMAD → GitHub board; Epic 0 sequencing

**Date:** 2026-10-09 · **Duration:** ~3h · **Stage:** pipeline setup

## What was covered

- **Sandbox built:** `sandbox/Dockerfile`, `entrypoint.sh`, `run-job.sh` and the
  `plan` / `implement` / `fix` prompts.
- **Tested by hand:** all three modes ran end to end on a dummy story (issue #1,
  PR #2). Both were deleted afterwards, along with the run logs.
- **Two failures fixed along the way:**
  - The Claude token stored in the Keychain was invalid. The developer re-stored it.
  - The push step failed with "dubious ownership". Git drops `-c` config for a local
    `upload-pack`, so `safe.directory` now goes in the `--upload-pack` command.
- **How to start building:** decided below, together with the frontend framework and
  the progress-file convention.
- **Board:** the developer's reference flow (a presentation about another team's AI
  delivery line) was mapped onto SlotBook. `bmad-ticket`'s native GitHub store was
  found and configured.
- **Model policy:** a model per mode, set in a config file.

The `review` mode was **not** built, and no Epic 0 tickets were written.

## Decisions reached

| Decision | Recorded in |
|---|---|
| The agent never holds the GitHub token. The container has two users: root (almost no capabilities) holds `GH_TOKEN` and pushes and posts; `agent` runs Claude with the Claude token only. Context is filtered to trusted authors | `SANDBOX-WORKFLOW.md` → The sandbox |
| Option C: build an Epic 0 walking skeleton through the sandbox while the PRD and architecture continue | STATE → Sequencing |
| Angular for the frontend | ADR-0005 |
| Progress file per story, `.ai/<issue>.md`, kept after merge as build history | `CLAUDE.md`, workflow doc, prompts |
| Board mapping follows `bmad-ticket`'s GitHub store: Initiative = milestone, Epic = issue, Story = sub-issue, Feature = label, dependencies = blocked-by. BMAD states map onto the `ai:*` labels | ADR-0004 (amended), `_bmad/custom/ticketing-store-config.toml` |
| Lower model writes, higher model reviews: plan and review on Opus 5.5, implement and fix on Sonnet 5.5 | `sandbox/models.conf`, workflow doc |
| `claude --resume` isn't used. A paused story resumes as a fresh `implement` run on the existing branch | Workflow doc → Usage limits |

## Pressure-test — where the developer was challenged

- **"Frontend epic first, then backend."** The developer proposed layer-first epics
  ("Frontend Project Setup": create the Angular project, scaffold it, landing page).
  - The objection: it contradicts ADR-0004's vertical slices; the agent would invent
    the API contract; and two of the three "stories" were really tasks.
  - Three options were offered, and the developer chose C (walking skeleton). This
    corrected the original plan rather than defending it.
- **"Code with Opus 5, review with 5.5."** The objection: Opus 5 costs more per token
  than Opus 5.5, so the "lower" writer would burn more usage. The developer clarified
  that Opus 5 was only an example. Sonnet 5.5 now writes the code.
- **"Start real building now."** The objection: the agent has nothing to build from
  (no PRD, architecture or tickets). Held, and resolved by Option C.

## Artifacts produced

- **`sandbox/`:** `Dockerfile`, `entrypoint.sh`, `run-job.sh`, `models.conf`, and
  `prompts/{plan,implement,fix}.md`.
- **Docker:** image `slotbook-sandbox` (about 2 GB, on the external drive), and named
  volumes `slotbook-nuget` and `slotbook-npm`.
- **Docs:** ADR-0005; ADR-0004 amended; the workflow doc (the sandbox, models, the
  hierarchy, setup step 7 ✅); `PROJECT-CONTEXT.md`.
- **BMAD:** the GitHub ticket-store config.
- **GitHub:** labels `epic`, `spike`, `backlog`, `hitl`, `risk:low`, `risk:medium`,
  `risk:high`, `P0`–`P3`.

## PRs reviewed

| PR | Story | Iterations | What the agent got wrong | Root cause |
|---|---|---|---|---|
| #2 (deleted test) | Smoke test | 1 fix run | The plan named its own branch (`ai/1-sandbox-smoke`); the job uses the issue-title slug instead. Harmless | Prompt: the plan prompt doesn't tell the agent the branch name |

## Rules added to `CLAUDE.md`

None. The progress-file rename was a convention change, not a lesson.

## Gaps exposed

- **Untested paths:**
  - stopping at the usage limit or at max turns (the WIP commit, resuming on an
    existing branch);
  - the `blocked.md` path;
  - the untrusted-comment filter, which was tested only on fake JSON, not on a real
    comment from a stranger.
- **Not built yet:** the `review` mode, CI, the worker and the dashboard.
- **Epic 0 needs decisions first:**
  - the repo layout (`src/` split between frontend and backend);
  - the test runners;
  - the Angular major version and the CLI defaults;
  - whether OpenSpec stays. STATE says this must be settled before the first epic's
    tickets, and that is now.
- **No visual board yet:** the developer's `gh` token lacks the `project` scope.

## Homework set

- [ ] Run `gh auth refresh -s project` so a GitHub Project board can be created
- [ ] Run `colima stop` after each sandbox session to free the VM's 4 GB of RAM
- [ ] Write the brief entry in `docs/learning/LEARNINGS.md` (carried over from 004)

## Parked for later

- **`review` mode** (an independent Opus 5.5 reviewer posting PR comments): build it
  once the skeleton PR shows what a review needs.
- **The worker:** run `run-job.sh` by hand until that gets tedious.
- **Bug investigation and an "AI fix" flow** (from the reference presentation): later.
  A bug issue plus `ai:queue` already works as "AI fix".
- **Sharing the run log or session for `--resume`** across containers: not needed
  while resuming works from `.ai/<issue>.md`.

## Next session

**006: Epic 0 (walking skeleton)**, running `bmad-spec`, then `bmad-ticket`, and
publishing to GitHub. Settle these first:

1. Drop OpenSpec? Recommended, since BMAD now owns specs and tickets end to end.
2. The repo layout and test runners for the skeleton.

Then queue story 1 and run `./sandbox/run-job.sh <n> plan`. Start Colima first
(`colima start`). The PRD (`bmad-prd`) follows in its own session after that.
