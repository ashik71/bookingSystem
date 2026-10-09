---
id: 2
type: story
title: "Each PR is built and tested for the sides it changes"
parent: epic-walking-skeleton
covers: [CAP-4]
after: ["4"]
refined: true
hitl: true
risk: medium
tracker_id: "5"
remote: "https://github.com/ashik71/bookingSystem/issues/5"
tracker_status: backlog
---

# Each PR is built and tested for the sides it changes

## Description

This adds one CI workflow. It detects which side a PR changes and runs only that side's
build and tests, using story 1's test commands. An aggregate job named `ci-ok` always
runs and is the only check `main` will require. Two steps are the developer's. Before
the story starts, they confirm that the bot's classic PAT has the `workflow` scope.
After the merge, they make `ci-ok` a required check on `main` and confirm that a failing
test blocks the merge.

## Acceptance Criteria

1. **A frontend-only PR runs only the frontend**
   **Given** a PR that changes files only under `src/frontend`
   **When** CI runs
   **Then** the frontend job builds the app and runs `npm test`, the backend job is skipped, and `ci-ok` passes when the frontend job passes
2. **A backend-only PR runs only the backend**
   **Given** a PR that changes files only under `src/backend` (its tests included), or the root .NET build files (the solution file, `global.json`, `Directory.Build.props`, `Directory.Packages.props`)
   **When** CI runs
   **Then** the backend job builds the solution and runs `dotnet test` from the repo root, the frontend job is skipped, and `ci-ok` passes when the backend job passes
3. **Changes to both sides, or to CI itself, run both**
   **Given** a PR that changes both sides, or changes the CI workflow file
   **When** CI runs
   **Then** both jobs run, and `ci-ok` passes only when both pass
4. **A PR that changes neither side still passes**
   **Given** a PR that changes only files outside both sides, such as docs
   **When** CI runs
   **Then** both jobs are skipped and `ci-ok` passes
5. **A failing job fails the aggregate**
   **Given** a PR whose changed side has a failing test or a build error
   **When** CI runs
   **Then** that side's job fails, and `ci-ok` fails
6. **`main` is checked after every merge**
   **Given** a push to `main`
   **When** CI runs
   **Then** both jobs run, and `ci-ok` passes on the state of `main` after story 1 and this story are merged
7. **The developer can prove that a red check blocks the merge** (hitl, after merge)
   **Given** `ci-ok` is set as a required status check in the `protect-main` ruleset
   **When** the developer opens a throwaway PR with a deliberately failing test
   **Then** `ci-ok` is red and GitHub refuses the merge. The developer then closes the PR without merging.

## Boundaries

- Must not change: the board-sync workflow, the test commands from ADR-0006, or any code under `src/` or `tests/`, except for whatever CI needs in order to call the existing commands.
- Must not add: deployment steps, cloud credentials, or any repository secret.

## References

- parent — _bmad-output/initiative-booking-management/epic-walking-skeleton/epic-walking-skeleton.md
- spec — _bmad-output/initiative-booking-management/epic-walking-skeleton/spec-walking-skeleton/spec-walking-skeleton.md, sections Capabilities (CAP-4), Constraints (CI shape)
- constraint — docs/adr/0006-monorepo-layout-and-test-stack.md, section Decision (test commands)

## Notes

- Decision: Option B, one workflow with change detection plus an always-running aggregate check named `ci-ok` (developer, 2026-10-09).
- Hitl before start: the developer confirms that the bot's classic PAT has the `workflow` scope. Without it, the sandbox can't push the workflow file.
- Hitl after merge: the developer adds `ci-ok` as a required status check in `protect-main`, then runs criterion 7.
- Decision: the workflow's `GITHUB_TOKEN` is read-only, and every third-party action is pinned to a full commit SHA, for supply-chain safety (developer, 2026-10-09).
- Risk is medium because a wrong CI either blocks every merge or lets a red PR through. Criterion 7 is the check that a person runs.
