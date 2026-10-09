# ADR-0006: Monorepo with deploy-independent folders; test stack

- **Status:** Accepted
- **Date:** 2026-10-09
- **Deciders:** MD Ashik Ashrafe

## Context

Epic 0's first story creates the solution, so the repo layout, the Angular major and the
test runners must be fixed before its tickets are written. The developer asked whether
the frontend and backend should live in separate repos, and whether one repo makes it
harder to deploy them independently.

Constraints:
- ADR-0004 makes a story a vertical slice (API + UI + tests) sized to **one PR**, built in
  one sandbox run.
- The sandbox image has the .NET 10 SDK and Node 24, and **no browser**.
- The sandbox prompts say only "run `dotnet test` and the frontend tests", which isn't
  precise enough for an agent.

## Options considered

### Option A: Monorepo with deploy-independent folders (chosen)

`src/backend` and `src/frontend` in one repo. Each side gets its own path-filtered CI
and deploy workflow.

**Pros:**
- One story, one PR, one sandbox run. The agent sees both sides of the API contract.
- One `CLAUDE.md`, one set of planning docs, one `.ai/<issue>.md`, one board.
- Deploying independently still works. That comes from the workflows (path filters,
  separate build outputs), not from the repo boundary.

**Cons:**
- Clone and CI scope cover both sides, so path filters have to be right.
- Splitting out later needs `git filter-repo`. It stays cheap only while the folders
  stay cleanly separated.

### Option B: Two repos (frontend and backend)

**Pros:**
- Hard boundaries for access and ownership.
- Either side can be made public alone.

**Cons:**
- Every story becomes two PRs, built in two sandbox runs that can't see each other.
  Each side guesses the API contract.
- The pipeline, `CLAUDE.md` and the planning docs are duplicated or synced.
- It breaks ADR-0004's one-story-one-PR rule.

### Option C: Monorepo with self-contained `backend/` and `frontend/` at the root

**Pros:** each side carries its own solution and config, so a later split is slightly
easier.

**Cons:** more ceremony than A for no current benefit. It also departs from the `src/`
convention in `CLAUDE.md`.

## Decision

**Option A.** Layout:

```
SlotBook.slnx                 solution (root), so `dotnet test` works from the root
Directory.Build.props         net10.0, nullable, implicit usings, warnings as errors
Directory.Packages.props      central package versions
src/backend/<Project>/        backend projects
src/backend/tests/<Project>.Tests/   backend tests (amended 2026-10-09)
src/frontend/                 Angular workspace (specs colocated: *.spec.ts)
```

*Amended 2026-10-09:* backend tests moved from a root `tests/backend/` into `src/backend/tests/`, so each side lives in one folder. The frontend already keeps its specs inside `src/frontend`. Splitting the backend out later is then one path, and its CI filter is one path too.

**Rules that keep the split cheap:**
- Nothing under `src/frontend` references anything under `src/backend`, and the reverse
  also holds.
- They share only the HTTP contract.
- Each side has its own CI and deploy workflow, filtered on its own path.

**Test stack:**

| Side | Choice | Test command (the contract the agent runs) |
|---|---|---|
| Backend | **xUnit v3** + **Shouldly**; `WebApplicationFactory` for API integration tests | `dotnet test` from the repo root |
| Frontend | **Angular 22** (pinned in `package.json`), CLI defaults; **Vitest + jsdom** (the Angular CLI default); npm | `npm test` in `src/frontend`, which runs once and exits non-zero on failure (no watch mode) |

- FluentAssertions was ruled out because v8 needs a commercial licence.
- Karma was ruled out because it is deprecated and would put Chromium in the sandbox
  image.

## Consequences

**Positive:**
- The agent has one exact test command per side.
- Both test stacks run in the sandbox image as it stands.
- Independent Azure deployment of the API and the web app stays possible from day one.

**Negative:**
- No browser-level tests yet. Rendering and layout bugs are left to PR review and manual
  checks.

**Neutral / follow-up:**
- This settles the test-runner item that ADR-0005 left open, and pins its "current
  stable major" as Angular 22.
- The backend's internal project structure (Clean Architecture layers, modules) is
  **not** decided here. It belongs to the architecture document and ADR-0002. The
  skeleton has a single API project.
- The Playwright E2E suite is deferred until there's a real user flow. Browsers would
  then be added to the sandbox image.

## Revisit when

- The frontend and backend get separate owners, need different access rights, or one
  side goes public on its own. Then split with `git filter-repo`.
- CI time becomes painful despite the path filters.
- Bugs that only a real browser would catch start reaching review.
