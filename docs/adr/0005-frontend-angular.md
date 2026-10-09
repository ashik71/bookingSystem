# ADR-0005: Angular for the frontend

- **Status:** Accepted
- **Date:** 2026-10-09
- **Deciders:** MD Ashik Ashrafe

## Context

Under ADR-0004 the agent writes all frontend code, and the developer's PR review is the
quality gate. So the frontend framework should be the one the developer can review
fastest and most critically, not the one that is fastest to write.

The frontend is not thin. It has a public patient-facing booking flow, a practitioner
side, a tenant admin side, and RTL support (see the brief). The first story to need a
framework is the walking skeleton (Epic 0), which was brought forward on 2026-10-09
(Option C: build the skeleton while planning continues).

## Options considered

### Option A: Angular

**Pros:**
- The developer has years of Angular experience, so they can spot non-idiomatic agent
  code quickly.
- Batteries included: routing, forms, HTTP and DI give the agent fewer choices to get
  wrong.
- First-party SSR and i18n.

**Cons:**
- More boilerplate per feature.
- Agents have seen somewhat less Angular code than React code.

### Option B: React (+ Vite)

**Pros:**
- The largest body of training data, so agent output is arguably most fluent.
- A big ecosystem.

**Cons:**
- The developer would be reviewing in a framework they know less well.
- Routing, forms and state each need a library choice, which means more decisions for
  the agent to invent or for the architecture document to pin.

### Option C: Blazor

**Pros:** one language, end to end.

**Cons:**
- Weaker fit for a public, mobile-first booking page: WASM download size, and SSR
  trade-offs.
- Less of a frontend learning signal.

## Decision

**Angular**, at the current stable major when the skeleton is scaffolded, pinned in
`package.json`. The deciding factor is review quality: under agentic delivery, the
framework the reviewer knows best is the safest.

## Consequences

**Positive:**
- Faster, sharper PR review.
- Angular's opinionated defaults reduce how much the architecture document has to pin.

**Negative:**
- Somewhat more boilerplate for the agent to generate.

**Neutral / follow-up:** the architecture step still decides these:
- component conventions (standalone components, signals);
- state management;
- the UI component library;
- the test runner;
- the SSR need for the public pages.

The skeleton story uses the CLI defaults until then.

## Revisit when

- PR review of frontend code repeatedly misses defects that a different stack would have
  prevented.
- The public booking page fails its performance targets in a way that Angular's SSR
  can't fix.
