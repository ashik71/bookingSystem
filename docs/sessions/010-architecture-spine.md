# Session 010 — architecture-spine: the SlotBook architecture with `bmad-architecture`

**Date:** 2026-10-10 · **Duration:** ~2 sittings (the first hit the usage limit) · **Stage:** planning (`bmad-architecture`)

## What was covered

- Extracted the developer's two reference architectures (Clean Architecture +
  event-driven; modular-monolith project layout) as generic patterns P-1–P-20 and
  M-1–M-6 in `architecture-slotbook/input-reference-architecture.md`. Patterns only.
- Ran the architecture questions to the end: decisions D1–D10 plus the tech additions.
- Drafted the architecture document (AD-1–AD-19), the threat model (NFR-8) and
  ADRs 0002 and 0008–0014.
- **The review step.** The first sitting launched five reviewers and hit the usage
  limit before any wrote a file. The second sitting committed the draft (`21c1d74`)
  and reran them two at a time: adversarial (24 holes, 5 critical), tech currency, PRD
  reconcile (2 critical), epics reconcile, rubric walk (2 critical). All findings are
  applied; after the rubric walk the rules section was rewritten in one pass
  (`67e87ae`) and split into AD-1–AD-22, with Epic 1 proofs and an enforcement
  inventory. The threat model and ADRs 0006, 0007, 0010, 0012 and 0013 were amended.
- **Not done:** the developer's read of the spine, `status: final`, the team slide
  deck, and the correct-course step (epic envelopes + PRD changes).

## Decisions reached

| Decision | Recorded in |
|---|---|
| One deployable: modular monolith on one free Render web service; Angular as a separate static site that rewrites `/api/*` | ADR-0002, AD-1 |
| MongoDB on Atlas Free (reasons confirmed by the developer) | ADR-0008 |
| Hosting on Render free tier; Azure later as its own epic | ADR-0009 |
| RabbitMQ via MassTransit **v8** (v9 is commercial); MongoDB bus outbox + inbox | ADR-0010, AD-10 |
| Shared collections; tenant from the server-side session; anonymous calls send `X-Tenant`; **no tenant in any route** | ADR-0011, AD-5 |
| One invariant, one guard document; version-checked saves in one transaction; whole-handler retry | ADR-0012, AD-6, AD-7 |
| Module structure: `Modules/{Module}/` + `.Contracts`; 7 modules, Booking and Identity layered (module map confirmed) | ADR-0013, AD-3 |
| Staff: password + mandatory TOTP; server-side opaque sessions for both audiences | ADR-0014, AD-12 |
| Threat-model values: lockout 5 failures / 15 min, password ≥ 12, staff session 12 h | threat-model.md |
| Tech additions: Aspire locally, OpenTelemetry to Grafana Cloud free, zoneless Angular + signals + PWA. Declined: passkeys, Kafka, Minimal APIs, SSE | spine Stack, memlog |
| **Goal shift:** near-term target is a team demo; clinic launch parked, $0 still holds | memlog; epics follow-up pending |
| OTP SMS sent directly after commit, never through the outbox (FR-2 hash-only wins) | AD-4, ADR-0010 amendment |
| Reason categories are retire-only, no delete (PRD FR-39 change pending) | AD-3 |
| Fitness tests on ArchUnitNET (NetArchTest stale); Playwright in CI only | AD-19, ADR-0006 amendment |

## Pressure-test — where the developer was challenged

- The developer challenged the tenant-in-route design (7c). Their instinct held: with one
  tenant per session, a route tenant is redundant and tamperable. Amended twice, ending
  with no tenant in any route and `X-Tenant` for anonymous calls.

## Artifacts produced

- `architecture-slotbook/`: spine, threat model, reference input, memlog
- ADRs 0002, 0008–0014
- Memory: modular-monolith structure reference

## PRs reviewed

None.

## Rules added to `CLAUDE.md`

None.

## Gaps exposed

- Several free-tier behaviours are unproven and land on epic 1: the keep-awake ping,
  `Set-Cookie` through the static-site rewrite, Atlas M0 multi-document transactions.
- Running five reviewer agents in parallel burned the usage limit. Run them in pairs.
- Applying reviews as patches, round after round, left contradictions between rules.
  The fifth review was mostly about the scars from the first four. Batch the fixes and
  rewrite once.
- PRD changes to record in correct-course: FR-39 (retire-only), FR-42 (append-only on
  the demo tier, if Atlas Free can't scope a role), FR-2/FR-6 (development outbox
  exemption).

## Homework set

- [ ] Read the architecture document (AD-1–AD-22, Epic 1 proofs) and approve or correct it
- [ ] Custom-domain fallback: own a domain, or accept ~$10/year as an NFR-16 exception?

## Parked for later

- SMS provider, backups, retention, uptime, production: clinic launch (spine Deferred)
- Kafka, Azure, passkeys: declined or parked by the developer

## Next session

The developer reads the spine and answers the domain question; mark it `final`. Build
the team slide deck from it. Then correct-course: update the epic envelopes listed in
`reviews/review-reconcile-epics.md`, record the three PRD changes, and re-sequence for
the demo.
