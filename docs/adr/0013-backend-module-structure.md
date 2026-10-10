# ADR-0013: Backend structure: modules, contracts, a shared kernel, one thin host

- **Status:** Accepted
- **Date:** 2026-10-10
- **Deciders:** MD Ashik Ashrafe

## Context

ADR-0002 makes SlotBook a modular monolith. The sandbox agent builds it one story at a
time, so module boundaries must be hard to cross by accident. The developer asked
SlotBook to follow the project structure of their modular-monolith reference, a
private repo described as "a solid modular monolith". Only its generic shape is used,
never its names or code.

## Options considered

### Option A: one project per layer, modules as namespaces
**Pros:** few projects. **Cons:** module isolation rests on namespace fitness tests
alone, and naming discipline drifts.

### Option B: modules as folders of projects, with a contracts project each (the reference's shape)
**Pros:** the compiler enforces module isolation; a module can later move to another
host. **Cons:** more projects.

### Option C: a project per CQRS role (commands, handlers, queries, ...)
**Pros:** none for SlotBook. **Cons:** the reference itself is moving away from it.

## Decision

**Option B.**
- **Layout:** `src/backend/Modules/{Module}/`.
- **Module shape:**
  - **Booking** and **Identity** are layered: separate Domain, Application and
    Infrastructure projects.
  - **Tenancy, Notifications, ClinicContent, Chat and Audit** are simple: one project
    with those three folders.
- **Contracts:** every module has a `.Contracts` project holding its integration events
  and its public queries.
- **References:** a module references only `SharedKernel.*` and other modules'
  `.Contracts`.
- **Host:** `SlotBook.Api` is the one host. It calls `Add{Module}Module()` once per
  module, and keeps controllers in `Controllers/{Module}/`.
- **Use cases:** each is a folder holding the command or query, its handler and a
  FluentValidation validator.
- **Tests:** they stay under `src/backend/tests/` (ADR-0006), with one unit-test project
  per module.
- **Rules:** spine AD-2, AD-3 and AD-4.

## Consequences

**Positive:**
- Crossing a module boundary is a compile error.
- The host stays thin.
- A worker host can be added later without moving code.

**Negative:** about 25 projects; a reference-graph fitness test is needed for the
simple modules' internal layering.

**Neutral / follow-up:** epic 1 scaffolds the SharedKernel, the host and the
architecture tests.

## Revisit when

- A simple module grows invariants that need compiler-enforced layering (split it).
- A module moves to its own host.
