# ADR-0003: Target .NET 10

- **Status:** Accepted
- **Date:** 2026-10-01
- **Deciders:** MD Ashik Ashrafe
- **Hat:** Platform

## Context

The installed SDK is **10.0.401** and the scaffolded project already targets
`net10.0`. The original learning roadmap assumed **.NET 8**, because the job
specification that shaped that roadmap named .NET 8.

That roadmap's framing has since been superseded: the project is now organised
around six focus areas (Azure, system design, messaging, multi-tenancy, security,
code architecture) rather than one job posting. So "the spec said 8" carries much
less weight than it did.

Relevant facts:
- .NET 8 is an LTS release; .NET 10 is also **LTS** (even-numbered releases are).
  This is not a choice between LTS and STS.
- None of the six focus areas depend on a specific .NET version. Clean
  Architecture, DDD, multi-tenancy, messaging patterns and observability are all
  version-independent concerns.
- The developer has 6+ years of .NET, so new language and runtime features are a
  minor cost, not a learning obstacle.
- Library ecosystem risk is the real question: packages for messaging, ORM,
  observability and cloud SDKs must support the target.

## Options considered

### Option A — Target .NET 10

**Pros:**
- Matches the installed SDK and the existing project file; zero setup friction
- LTS, so supported for the full life of this project and beyond
- Current runtime and framework improvements come free
- Demonstrates currency; working on the latest LTS is a mild positive signal
- Avoids a mid-project upgrade, which would be busywork with no learning payoff

**Cons:**
- Marginally thinner ecosystem for very new or poorly maintained packages
- Tutorials and reference material (including the original roadmap's links) are
  mostly written against .NET 8, so occasional small translation is needed
- Any employer running .NET 8 in production means a (trivial) version gap

### Option B — Downgrade to .NET 8

**Pros:**
- Matches the original roadmap's resource links exactly
- Matches what many enterprises actually run today
- Maximum library compatibility

**Cons:**
- Requires deliberately changing a project that already targets `net10.0`
- Eventually forces an upgrade anyway, or leaves the portfolio piece looking dated
- No learning benefit whatsoever against the six focus areas

### Option C — Multi-target

Rejected without serious consideration. Multi-targeting an application host (rather
than a library) adds build complexity for no benefit here.

## Decision

**Target .NET 10.** All projects use `net10.0`.

The deciding factor is that **no focus area depends on the framework version**, so
the choice should be made on friction grounds alone — and .NET 10 is what is
already installed and already scaffolded. .NET 10 being LTS removes the only
substantive argument for .NET 8.

Where reference material is written for .NET 8, the small translation cost is
accepted. In practice this affects almost nothing at the architectural level this
project operates at.

## Consequences

**Positive:**
- No setup friction; no mid-project upgrade
- Supported runtime for the whole project lifetime
- The portfolio piece stays current

**Negative:**
- Occasional need to translate .NET 8-era documentation and samples
- A package may lag; if one blocks progress, pin an older version of that package
  rather than downgrading the whole solution

**Neutral / follow-up:**
- Verify target support as each major dependency arrives: the ORM (A7), the
  messaging clients (Block E), OpenTelemetry and its exporters (F8), and the Azure
  SDKs (Block F). Check at the point of adoption, not speculatively.
- Any employer-facing note should mention the version used; it is a non-issue but
  worth stating.

## Revisit when

- A dependency essential to a focus area has no .NET 10 support and no workable
  pinned version. (Downgrade that package first; the solution target is a last
  resort.)
- A specific target employer or client requires .NET 8 for production parity, and
  that parity is worth more than currency.
