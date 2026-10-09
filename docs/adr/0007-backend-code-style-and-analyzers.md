# ADR-0007: Backend code style and analyzers, enforced by the build

- **Status:** Accepted
- **Date:** 2026-10-09
- **Deciders:** MD Ashik Ashrafe

## Context

Under ADR-0004 the agent writes all the code, and the developer reviews it. Style drift
caught in review costs review time. Style drift that is not caught spreads, because the
agent copies the nearest existing pattern. The developer has a reference ruleset from
earlier .NET work and wants SlotBook's backend to follow it. This ADR records those
conventions in generic terms. It copies no external file or name.

The first backend code (story 0.1) was built with no style configuration.

## Options considered

### Option A: Reference conventions, enforced as build errors (chosen)

Same rules as the reference. Formatting and naming rules are raised from `suggestion` to
`warning`, which `TreatWarningsAsErrors` turns into build errors.

**Pros:**
- The agent's own `dotnet build` fails on a style violation before it can open a PR.
- CI enforces style with no extra job.

**Cons:**
- A rule that turns out to be noisy blocks work until it is demoted.

### Option B: Reference conventions as IDE suggestions only

**Pros:** never blocks a build.

**Cons:** suggestions are invisible to a headless agent. Drift is caught only in review.

### Option C: No shared ruleset (SDK defaults)

**Pros:** zero setup.

**Cons:** the agent's style varies from run to run, so review has to cover style as well
as substance.

## Decision

**Option A**, configured through a root `.editorconfig`, `Directory.Build.props` and
`stylecop.json`, using no `.ruleset` file, since that format is legacy.

### Build settings (all backend projects)

- `AnalysisMode` = `AllEnabledByDefault`, `EnableNETAnalyzers` = true
- `EnforceCodeStyleInBuild` = true
- `TreatWarningsAsErrors` = true, and `Nullable` = enable (already set by ADR-0006)
- Analyzer packages, applied to every project as global package references:
  - **StyleCop.Analyzers**
  - **Meziantou.Analyzer**
  - **Microsoft.VisualStudio.Threading.Analyzers**
- `stylecop.json`: no file header, no XML header, documentation not required, and `using`
  directives placed outside the namespace.
- `GenerateDocumentationFile` = true, so StyleCop's SA0001 doesn't fire. Docs are still
  not required, because CS1591 is off. *(Amended 2026-10-09, from the #7 plan.)*
- StyleCop.Analyzers is pinned to **1.2.0-beta.556**. The stable 1.1.118 raises a false
  SA1516 on top-level statements in `Program.cs`, and SX1101 needs 1.2. *(Amended
  2026-10-09.)*

### Formatting (`.editorconfig`)

- **Whitespace:** spaces only.
  - Indent: 4 for C#, and 2 for XML, project, props, JSON and shell files.
  - Trim trailing whitespace, and end every file with a newline.
  - Line endings are LF for shell scripts.
- **Braces:** every brace on a new line (Allman style). `else`, `catch` and `finally` start
  a new line. Braces are always used.
- **Usings:** placed outside the namespace, `System` directives first, with no blank lines
  between groups.
- **Expressions:**
  - `var` everywhere;
  - predefined types (`int`, not `Int32`);
  - no `this.` qualification;
  - accessibility modifiers always explicit;
  - fields are `readonly` where possible;
  - expression bodies for properties, indexers and accessors, but not for operators;
  - pattern matching, throw expressions, null propagation, coalescing and object or
    collection initializers preferred.
- **Modifier order:** `public, private, protected, internal, const, static, extern, new,
  virtual, abstract, sealed, override, readonly, unsafe, volatile, async`.

### Naming (all enforced as warnings, which become errors)

| Symbol | Style |
|---|---|
| Types, methods, properties, events, namespaces, local functions | PascalCase |
| Interfaces | `I` + PascalCase |
| Type parameters | `T` + PascalCase |
| Non-private fields and private constants | PascalCase |
| Private fields | `_camelCase` |
| Parameters and locals | camelCase |

### Rules turned off

| Rules | Why |
|---|---|
| SA1633 (file header), CS1591 (missing XML docs), SA1623, SA1629 (doc wording) | Documentation is not mandated |
| SA1101 (prefix with `this.`), SA1309 (no leading underscore) | Conflict with the naming above |
| CA2007, MA0004 (`ConfigureAwait`) | ASP.NET Core has no synchronisation context |
| CA1062 (validate arguments of public methods) | Nullable reference types cover this |
| CA1303, CA1308, MA0002, MA0006, MA0011 (culture and string-comparison rules) | Too noisy for a web API with no localised literals |
| CA1054, CA1056, CA2234 (URIs as `System.Uri`) | Strings are accepted for URLs |
| CA1825, CA1851, CA1852, CA1854, CA1859, CA1860, CA1861, CA1862, CA1863, CA1867, CA1868, CA1510, MA0015, MA0016, MA0025, MA0051, RCS1090 | Micro-performance and style preferences, not required |

**Set to suggestion** (visible in the IDE, but they don't fail the build): CA1002,
CA1008, CA1024, CA1711, CA1724, CA1848, CA2254, MA0026.

**Added** *(amended 2026-10-09)*: **SX1101** ("don't prefix local calls with `this.`") as a
warning. With SA1101 off, IDE0003 alone doesn't fail `dotnet build`, so without SX1101 the
"no `this.`" rule would only be enforced by `dotnet format`.

**Severity of the expression preferences:**
- Rules stated as absolutes are warnings, which become errors: `var`, predefined types, no
  `this.`, explicit accessibility, `readonly`, braces, `using` placement, modifier order.
- The "preferred" rules stay as IDE suggestions and don't fail the build: pattern
  matching, throw expressions, null propagation, coalescing, initializers, expression
  bodies. *(Amended 2026-10-09.)*

**Test method names** follow the same naming as everything else: PascalCase, with no
underscores. CA1707 stays on for test projects too. *(Decided 2026-10-09.)*

**Kept on, unlike the reference:**
- **CS0108** (a member hides an inherited member without `new`), because silent hiding
  is a real bug source.
- **SA1402 and MA0048** (one type per file, named after the file), because they keep agent
  PRs easy to navigate.

## Consequences

**Positive:**
- Style is no longer a review topic: the build enforces it.
- CI (story 0.2) needs no style job.

**Negative:**
- Analyzer upgrades can add new errors. Pin analyzer versions centrally and upgrade
  deliberately.
- Small records and DTOs each need their own file.

**Neutral / follow-up:**
- This covers the backend only. The frontend keeps the Angular CLI defaults (Prettier and
  `.editorconfig` as generated) until a frontend reference is chosen.
- To demote a rule, change its severity in `.editorconfig` and record the change here as
  an amendment.

## Revisit when

- A rule is suppressed inline (`#pragma`) more than a few times. Then the rule is wrong
  for this codebase, and it should be demoted here instead.
- The architecture document introduces source generators or test conventions that clash
  with these rules.
