---
type: initiative
title: BookingManagement
parent: none
covers: [FR-1, FR-2, FR-3, FR-4, FR-5, FR-6, FR-7, FR-8, FR-9, FR-10, FR-11, FR-12, FR-13, FR-14, FR-15, FR-16, FR-17, FR-18, FR-19, FR-20, FR-21, FR-22, FR-23, FR-24, FR-25, FR-26, FR-27, FR-28, FR-29, FR-30, FR-31, FR-32, FR-33, FR-34, FR-35, FR-36, FR-37, FR-38, FR-39, FR-40, FR-41, FR-42, FR-43, FR-44, FR-45]
after: []
assignee: ""
risk: high
---

# BookingManagement

## Description

SlotBook, a web booking system for one live clinic, built tenant-aware from day one.
Patients book a slot from a phone and hold a ticket that the practitioner checks at
the door. Owners publish each week and set the booking rules. The final PRD owns the
features, NFRs, non-goals and success metrics (see References). This initiative
delivers all of it, as epics built through the sandbox pipeline. Epics 1 to 10 come
before the clinic's first real booking, and epics 11 and 12 follow right after.

## Outcome

For one clinic and its patients, booking moves online while phone and walk-in stay a
channel through reserved places. The signals are the PRD's primary metrics SM-1 to
SM-5, plus SM-10 (online booking share), read from production.

## Done when

1. The clinic takes real bookings through SlotBook in production for 4 weeks, with no
   double booking and no booking-limit breach (SM-1, SM-3), and the concurrency suite
   is green on every CI run.
2. Every FR from FR-1 to FR-45 is live in production for the clinic in `bn`, `en` and
   `ar`, and the SM-9 demo runs end to end.
3. The cross-tenant suite (SM-5) and the forged-ticket suite (SM-2) pass on every CI
   run.
4. The running cost before the first sale is $0 (SM-8).
5. Reserved places are in use after launch (SM-11), and SM-10 and SM-12 are read
   weekly.

## Standard epic checks

Every epic's Done when ends with "the standard epic checks pass". It means all of these
hold for the work that epic adds:

- **SC-1 Deployed:** merged to main and running in the deployed environment, with the
  epic's own Done-when checks passing there. Until epic 10, that is the pre-launch
  environment. From epic 10 on, it is production.
- **SC-2 Tenant isolation (NFR-3):** the cross-tenant suite covers every endpoint the
  epic adds. A user or session of tenant A can't read or change tenant B's data.
- **SC-3 Audit (FR-42):** every action in FR-42's list that the epic introduces writes
  an audit entry with no personal data, and each has a test.
- **SC-4 Roles (FR-38, NFR-4):** each staff endpoint states the roles it accepts. A
  practitioner is refused on owner-only endpoints, and the refusal is audited. A patient
  session is refused on every staff endpoint, and a staff session on every patient
  endpoint.
- **SC-5 Languages and text (FR-43, NFR-7):** every built-in string the epic adds exists
  in `bn`, `en` and `ar`. Its pages mirror right to left in `ar`. Text that users enter
  is never rendered as HTML.
- **SC-6 Logs (NFR-11a, from epic 4 on):** the log scan finds no full mobile number,
  age, reason category or note.
- **SC-7 Cost (NFR-16):** any service the epic adds runs within a free tier.
- **SC-8 Patient pages (NFR-15):** every patient page the epic adds is usable at 360 px
  width, with labels and keyboard use to WCAG 2.2 AA. Epic 10 audits them all.

## Boundaries

The boundary is capability. Each epic is one PRD capability, or a tightly coupled pair,
delivered by one owner: the developer reviewing, the sandbox agent building. The split
is finer than that only where the outcome differs. Out of scope: the PRD's §6
non-goals, and the sandbox pipeline's remaining work (review mode, worker, dashboard),
which this PRD doesn't cover.

Tracer path across epics: UJ-3. An owner publishes a week (3), a patient signs in (4),
books a place and holds the ticket (5), on top of the deploy platform (1) and the
tenant (2). The first demo comes at the end of epic 5.

- Touch point: GitHub Actions CI from epic 0 — a deploy job is added; owner: epic-deploy-platform
- Touch point: Render (hosting) — configured; owner: epic-deploy-platform
- Touch point: the MongoDB host — provisioned and configured; owner: epic-deploy-platform
- Touch point: the SMS provider — configured; owner: epic-notifications (the development channel: epic-patient-sign-in)
- Touch point: the browser push service (Web Push) — configured; owner: epic-notifications
- Touch point: RabbitMQ (hosted broker) — configured; owner: epic-notifications
- Touch point: file storage for announcement images — owner: epic-announcements
- Touch point: the real-time transport for chat — owner: epic-chat
- Touch point: DNS and the official booking address — owner: epic-launch

## References

- prd — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md (final; §4 FR-1 to FR-45, §5 NFRs, §6 non-goals, §8 metrics)
- constraint — the same PRD, §5 Cross-Cutting NFRs
- addendum — _bmad-output/initiative-booking-management/prd-slotbook/addendum.md (input for architecture and UX; its SQL sketches predate the MongoDB decision)
- brief — _bmad-output/initiative-booking-management/brief-slotbook/brief-slotbook.md
- process — docs/process/SANDBOX-WORKFLOW.md
- architecture — not yet written (`bmad-architecture`, next step); home of the cross-epic decisions listed in Notes
- ux — not yet written (`bmad-ux`); every epic with screens waits on it at inception

## Notes

- Decision: epics 1 to 10 make up the go-live release. Epics 11 (announcements) and 12
  (chat) are fast-follow and ship to production after the clinic's first real booking.
  The v1 scope in PRD §7.1 is unchanged: all 45 FRs are v1, and this decides only the
  order around launch. The PRD memlog deferred this line to `bmad-ticket` (developer,
  2026-10-10).
- Decision: every epic ships to a deployed **pre-launch environment** from epic 1 on.
  It is not "production" in FR-27's sense, so the development SMS channel is allowed
  there (PRD §4.6). Epic 10 stands up production from the same environment
  definition. Fast-follow epics ship to production (developer, 2026-10-10).
- Decision: hosting is **Render**, for now. A later move to Azure would be a new epic
  (developer, 2026-10-10).
- Decision: the database is **MongoDB** (developer, 2026-10-10). The architecture step
  writes the ADR, with the deciding reasons, and works every NFR-1 invariant through in
  MongoDB terms. The addendum's SQL sketches are background only.
- Decision: messaging is **RabbitMQ through MassTransit**; Kafka is skipped for now
  (developer, 2026-10-10). The architecture step settles the MassTransit version: v8
  is open source, current releases are commercial. It also settles the RabbitMQ host:
  a free tier is assumed, to keep NFR-16. MassTransit types stay in Infrastructure, so
  a later swap touches nothing else.
- Decision: the epic set departs from PRD §7.2 (a non-binding hint) in these places
  (developer accepted, 2026-10-10):
  - a deploy-platform epic (1) is added, because epic 0 deferred deployment;
  - the ticket code and ticket page (FR-22, FR-23) move into booking core (5),
    because FR-15 issues the code when the booking is confirmed;
  - a clinic's-day epic (7) takes FR-20, FR-24, FR-25, FR-40 and FR-41, plus FR-11's
    bulk cancel. §7.2 placed neither FR-40 nor FR-41;
  - the public ticket check (FR-26) joins anti-impersonation (9) as UJ-7;
  - notifications (6) move up to come right after booking core;
  - there is no languages epic. Epic 2 builds the language infrastructure, every
    epic ships its own strings (SC-5), and epic 10 runs the completeness pass.
- Decision: the sandbox pipeline's remaining work (review mode, worker, dashboard)
  isn't part of this epic set. Where it lives is an open architecture question
  (2026-10-10).
- Parked: Azure, focus area 1, has no job while hosting is on Render. It comes back
  when the developer schedules the move.
- Parked: Kafka (half of focus area 3) has no job while messaging is RabbitMQ only.
  The natural later job is an event log of booking events for audit, reconciliation
  and reporting (addendum, *Messaging*).
- Assumption: MongoDB is hosted on MongoDB Atlas's free tier [ASSUMPTION], because
  Render has no managed MongoDB. The architecture step confirms it. If it holds:
  - the free tier has no backups, so NFR-19 needs our own daily dump (epic 1);
  - the free tier caps throughput, so NFR-13 at the publication peak must be
    measured (epic 5);
  - whether the free tier allows a custom role limited to insert and find on the
    audit collection decides how FR-42's "the application can't alter entries" is
    enforced (epic 2).
- Open question: these decisions are shared by several epics and go to the
  architecture spine:
  - the tenancy isolation model in MongoDB (a database per tenant, or shared
    collections with a tenant key) — all epics;
  - data access (the official driver, or an EF Core provider) and migrations — all;
  - staff sign-in and the MFA method (Q4); the patient session and known-device
    design; audience separation — 2, 4;
  - the concurrency design: the Slot document; capacity, reserved and taken; the
    booking-limit guard; sequence numbers; ticket-code uniqueness; and the
    transaction, write concern and read concern for each write path — 3, 5, 7, 8;
  - domain events, the outbox, MassTransit's version and the RabbitMQ topology
    (exchanges, queues, retries, delayed messages) — 5, 6, 7, 8, 11, 12;
  - background jobs on Render (scheduled publication, lapse, reminders, retries,
    reconciliation, purge), given that free services sleep when idle — 1, 3, 5, 6, 10;
  - the API error format, and business reason codes against 429 — all;
  - time: UTC storage, shown in the tenant's time zone — all;
  - the threat model (NFR-8) — 2, 4, 5, 7, 9;
  - environments: pre-launch and production on Render, and the development-channel
    rule — 1, 4, 6, 10;
  - the frontend's shape: patient and staff surfaces in one Angular app or two — every
    epic with screens;
  - the real-time transport for chat — 12.
- Open question: retention (Q3) and uptime (Q5) are answered with the clinic before
  epic 10 can finish. Epic 10 waits on both.
- Unknown: whether a timed job fires reliably on Render's free tier, where a web service
  sleeps after a period without traffic. Epics 3, 5, 6 and 10 depend on timers, so
  epic 1 proves it first.
- Waits on epic-deploy-platform: nothing can be deployed or store data before it.
- Waits on architecture (not an epic): every epic's inception.
