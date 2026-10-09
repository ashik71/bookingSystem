# ADR-0001: Use OpenSpec for change management; reject BMAD

- **Status:** Superseded by ADR-0004 (2026-10-09)
- **Date:** 2026-10-01
- **Deciders:** MD Ashik Ashrafe
- **Hat:** Both

## Context

This project has two simultaneous goals that pull in different directions:

1. **Deliver** a booking app for a real paying client (Ruqaiya Center, ৳2,39,000,
   478 estimated hours, 5–6 months).
2. **Learn** modular monolith architecture, DDD, and multi-tenancy — properly, not
   by copying a template.

The developer has 6+ years of .NET but is new to these architectural patterns. The
explicit learning contract is that **the developer writes all production code**; the
AI acts as teacher, architect and reviewer (see `docs/PROJECT-CONTEXT.md` §3).

The project therefore needs a process that:
- Forces decisions to be reasoned about and written down *before* code exists
- Survives scope changes (one already happened: in-app payments were cut, which
  invalidated assumptions scattered across several documents)
- Produces documentation that is also useful to AI sessions, since context does not
  persist between them
- Does not depend on AI agents generating the implementation

Two candidate frameworks were evaluated.

## Options considered

### Option A — OpenSpec

Spec-driven change management. Each unit of work is a folder under
`openspec/changes/<change-id>/` holding `proposal.md` (why, what, assumptions),
`design.md` (how), and `tasks.md` (ordered checklist). Stable capability specs are
promoted into `openspec/specs/`. Changes are archived once shipped.

**Pros:**
- The proposal is a reviewable artifact *before* implementation. That is exactly the
  teaching surface needed: the developer can be argued with about a design while it's
  still cheap to change.
- Plain markdown in git. The process output *is* the documentation — no separate
  doc-writing chore that gets skipped.
- Traceability survives scope change. When in-app payments were cut, it was possible
  to name the exact affected artifacts (`tasks.md` 5.1, 6.6, 10.13; the "payment hold
  window 24 hours" assumption in `proposal.md`; hold-dependent logic in `design.md`).
  That audit is the whole value, and it was already demonstrated on this project.
- AI-friendly without being AI-dependent. Human and AI read the same files. A new
  session re-grounds itself by reading the change folder.
- Lightweight enough for one developer.

**Cons:**
- Ceremony per change; a trivial change still wants a folder. Needs a judgement call
  on when to skip.
- Does not cover long-lived decisions well — a change folder is transient by design.
  (Mitigated by pairing it with ADRs.)

### Option B — BMAD (Breakthrough Method for Agile AI-Driven Development)

Assigns agent personas — Analyst, PM, Architect, Scrum Master, Dev, QA — which run a
simulated agile ceremony to produce a PRD and architecture document, then shard those
into stories that Dev/QA agents implement.

**Pros:**
- Generates a lot of structured documentation quickly.
- The persona hand-offs impose a genuine discipline on requirements gathering.
- Its structured-elicitation step (interrogating the user with specific questions
  rather than accepting vague requirements) is a genuinely good idea.

**Cons:**
- **Its core engine is agents writing the code.** That is precisely the part this
  project switches off. Adopting BMAD and disabling its implementation loop leaves
  mostly ceremony.
- Documents arrive pre-reasoned. The developer receives conclusions instead of
  working through trade-offs — directly counter to goal 2.
- Heavyweight for a solo developer on a single product: multiple personas, templates
  and workflow phases to learn before any project work happens.
- Learning BMAD competes for attention with learning the architecture, which is the
  actual objective.

### Option C — No formal process; ad-hoc docs

**Pros:** zero overhead. **Cons:** this is what produced the scattered-assumptions
problem when payments were cut. Already tried, already failed.

## Decision

**Adopt OpenSpec** for change management, paired with two companions:

- **ADRs** in `docs/adr/NNNN-title.md` for decisions that outlive any single change.
  OpenSpec changes get archived; ADRs are permanent. Technology choices, the
  multi-tenancy model and layering rules are ADRs, not changes.
- **PRDs** in `docs/prd/` — one per product area, not per feature. Platform
  requirements and Ruqaiya's requirements are separate documents, because they are
  billed differently (`PROJECT-CONTEXT.md` §1).

**Reject BMAD**, but **borrow its structured elicitation**: the AI interrogates the
developer with specific, answer-forcing questions before writing any spec, rather
than accepting a vague requirement and filling gaps with plausible invention.

## Consequences

**Positive:**
- Every architectural decision gets written down with its alternatives, which is both
  the learning record and the onboarding document.
- A new AI session reads `PROJECT-CONTEXT.md` + relevant ADRs + the active change
  folder and is productive immediately.
- Scope changes become traceable instead of archaeological.

**Negative:**
- Real writing overhead. The developer must actually write proposals and ADRs, not
  just read them. This is deliberate — writing is the learning mechanism — but it
  will feel slow early on.
- Two layers (changes + ADRs) means a recurring judgement call about where something
  belongs. Rule of thumb: *if it will still be true after this feature ships, it's
  an ADR.*

**Neutral / follow-up:**
- Need a convention for when a change is too small to warrant a folder.
- The existing `add-serial-booking-core` change carries stale payment assumptions and
  must be revised (see `PROJECT-CONTEXT.md` §7 and the Bengali plan's OpenSpec notes).

## Revisit when

- The developer stops being the sole implementer (a team arrives, or the decision is
  made to let AI write production code). BMAD's story-sharding model becomes relevant
  at that point.
- Change folders are routinely created and abandoned, indicating the ceremony costs
  more than it returns.
