---
id: 3
type: story
title: "Backend code style is enforced by the build"
parent: epic-walking-skeleton
covers: [CAP-5]
after: ["4"]
refined: true
hitl: false
risk: low
tracker_id: "7"
remote: "https://github.com/ashik71/bookingSystem/issues/7"
tracker_status: done
---

# Backend code style is enforced by the build

## Description

This adds the backend code style from ADR-0007 to every backend project, the test project
included: the `.editorconfig` rules, the build settings, the three analyzer packages and
`stylecop.json`. It then brings story 1's backend code into line, so the build passes with
no diagnostics. After this story, a style or naming violation fails `dotnet build`, so the
agent's own build and story 2's CI both enforce style with no separate job.

## Acceptance Criteria

1. **A style violation fails the build**
   **Given** a backend file with a violation that ADR-0007 enforces: a private field without the `_` prefix, a `this.` qualification, an opening brace on the same line, a `using` inside the namespace, or a second type in the file
   **When** `dotnet build` runs from the repo root
   **Then** the build fails, and the error names that rule's id
   **And** each of these five violations is tried once and reverted. None is committed. The PR body lists each violation with the rule id that fired
2. **The existing code is clean, not suppressed**
   **Given** the backend code from story 1
   **When** `dotnet build` and `dotnet test` run from the repo root
   **Then** the build has zero warnings and zero errors, and every test passes
   **And** no `#pragma warning disable`, `[SuppressMessage]`, `NoWarn`, or severity change outside the root `.editorconfig` was added to get there
3. **The rules match ADR-0007**
   **Given** the root `.editorconfig`, `Directory.Build.props` and `stylecop.json`
   **When** they are compared with ADR-0007
   **Then** every setting, naming rule, turned-off rule and suggestion-level rule in the ADR is configured as the ADR says
   **And** CS0108, SA1402 and MA0048 are errors
   **And** no rule the ADR doesn't name has been turned off or downgraded
4. **The analyzers apply to every backend project**
   **Given** StyleCop.Analyzers, Meziantou.Analyzer and Microsoft.VisualStudio.Threading.Analyzers
   **When** any backend project builds, the test project included
   **Then** all three analyzers run on it
   **And** their versions are set once, centrally, with none on a project's package reference
5. **`dotnet format` agrees with the build**
   **Given** the finished change
   **When** `dotnet format --verify-no-changes` runs from the repo root
   **Then** it exits 0 with no changes reported

## Boundaries

- Must not change: the behaviour of `GET /health`; what any test asserts; anything under `src/frontend`; the CI workflow (story 2); the board-sync workflow.
- Must not add: a `.ruleset` file, or a file header or company name in any source file.

## References

- parent — _bmad-output/initiative-booking-management/epic-walking-skeleton/epic-walking-skeleton.md
- constraint — docs/adr/0007-backend-code-style-and-analyzers.md, section Decision (every rule this story configures)
- constraint — docs/adr/0006-monorepo-layout-and-test-stack.md, section Decision (layout, central package versions)

## Notes

- Decision: style is enforced as build errors. CS0108 and one-type-per-file stay on. The frontend keeps the Angular CLI defaults (developer, 2026-10-09).
- ADR-0007 is the complete source for this story. Don't look for, copy or mention any other ruleset.
- Risk is low. The change is mechanical, and criteria 1 and 2 prove it.
