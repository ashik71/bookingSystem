# Session 009 — epic-slicing: the initiative split into epics, go-live line drawn

**Date:** 2026-10-10 · **Duration:** ~1.5h · **Stage:** planning (`bmad-ticket`, initiative into epics)

## What was covered

- Pushed the three PRD commits that were still local after session 008.
- **The initiative was split into epics** with `bmad-ticket`, working from the final PRD.
  PRD §7.2 served as a non-binding hint. Epic level only: no stories yet, because every
  epic's inception waits on architecture.
- **A draft tree went through the skill's tree and dependency check**, run by an
  independent reviewer. It found no coverage gaps and about a dozen mechanical gaps,
  all folded in before writing.
- **The files were written:**
  - the initiative envelope, which had been a frontmatter stub;
  - the initiative `tickets.toml`, now with 13 epics;
  - 12 new epic folders, each with its envelope;
  - Epic 0 marked `status: done`.
- **Not done:** publishing the epics to GitHub (deliberately after architecture), and
  any epic inception.

## Decisions reached

| Decision | Recorded in |
|---|---|
| **Go-live line:** epics 1–10 ship before the clinic's first real booking. Epics 11 (announcements) and 12 (chat) are fast-follow. The v1 scope (PRD §7.1) is unchanged | Initiative Notes, `tickets.toml` (`release` key) |
| Every epic ships to a **pre-launch environment** from epic 1 on, where the development SMS channel is allowed. Epic 10 stands up production from the same environment definition | Initiative Notes |
| **Hosting: Render**, for now. A later move to Azure would be its own epic | Initiative Notes; ADR to write in 010 |
| **Database: MongoDB.** The addendum's SQL sketches are now background only | Initiative Notes; ADR to write in 010 |
| **Architecture blueprint:** Clean Architecture and event-driven, following the developer's reference architecture (an external client repo; patterns only) | STATE open threads |
| **Messaging: RabbitMQ through MassTransit;** Kafka skipped for now. The version (v8 open source or a commercial licence) is settled in architecture | Initiative Notes; ADR to write in 010 |
| The epic boundaries differ from §7.2 in six places, accepted as drafted: <br>• a deploy-platform epic is added <br>• the ticket code and ticket page move into booking core <br>• a new clinic's-day epic takes FR-20, FR-24, FR-25, FR-40, FR-41 and FR-11's bulk cancel <br>• the public check joins anti-impersonation <br>• notifications move up <br>• there is no languages epic | Initiative Notes |
| **Standard epic checks SC-1 to SC-8** close every epic's Done when: deployed, cross-tenant, audit, roles, languages, logs, cost, patient pages | Initiative file |
| Publish the epics to GitHub after architecture, not now | This log |

## Pressure-test — where the developer was challenged

Little this session. The developer took the recommended option on every question: the
go-live line, the pre-launch environment, the boundaries, and when to publish. The
challenge ran the other way: the tree check challenged my draft, and most of its
findings were real. Examples:
- FR-40's "practitioner booking" column has no data source before epic 8;
- nobody owned production operations;
- free hosting may never fire a timer.

Render and MongoDB arrived mid-session as decisions. I didn't argue them. I recorded
the scale at which each goes wrong, as consequences:

- **Render's free tier:** a web service sleeps when idle, so scheduled publication
  (within 60 s), lapse, reminders and retries may never fire. Epic 1 must prove a timer
  fires through idle periods before any epic relies on one.
- **MongoDB on a free tier** (assumed to be Atlas, because Render has no managed
  MongoDB):
  - correctness is fine (conditional updates plus multi-document transactions);
  - there are no backups, so NFR-19 needs our own dump;
  - throughput is capped, so NFR-13 at the publication peak has to be measured;
  - it's unknown whether a custom role can make the audit log append-only (FR-42).

## Artifacts produced

- `_bmad-output/initiative-booking-management/initiative-booking-management.md`:
  the full initiative envelope (Done when, standard epic checks, boundaries, touch
  points, decisions, open questions for architecture)
- `_bmad-output/initiative-booking-management/tickets.toml`: epics 0–12 in build
  order, with `after` and needs, and a house `release` key
- 12 epic envelopes:
  - `epic-deploy-platform`
  - `epic-tenant-and-staff`
  - `epic-schedule-and-publication`
  - `epic-patient-sign-in`
  - `epic-booking-core`
  - `epic-notifications`
  - `epic-clinic-day`
  - `epic-reserved-places`
  - `epic-anti-impersonation`
  - `epic-launch`
  - `epic-announcements`
  - `epic-chat`
- `epic-walking-skeleton.md`: `status: done`

## PRs reviewed

None.

## Rules added to `CLAUDE.md`

None.

## Gaps exposed

- **PRD §7.2 was a hint, not a plan.** It left FR-40 and FR-41 unplaced. It also put
  the ticket code after the booking that issues it, and SMS after the OTP that needs
  it. A slicing hint written inside a PRD should get the same dependency walk as the
  tree.
- **The plan had assumed SQL.** The addendum, STATE and session 008 all framed the
  invariants as "SQL-heavy". With MongoDB, which is the developer's strong area, the
  risk moves. It is now in multi-document transactions under contention (transient
  errors, retries, write concern) and in free-tier limits. The architecture document
  must still work through every NFR-1 invariant, now in MongoDB terms.
- **Azure (focus area 1) has no job** while hosting is on Render. That's parked, not
  dropped.
- **The free-tier stack is now the main delivery risk.** Timers, backups, throughput,
  the message broker and the chat transport all depend on what each provider's free
  plan allows.

## Homework set

- [ ] Before session 010, note the deciding reason for MongoDB and for Render "for
      now". Both go into ADRs in the architecture step.
- [ ] Write the brief entry in `docs/learning/LEARNINGS.md` (carried over from 004)
- [ ] Upgrade Node on the Mac to ≥ 24.15 (carried over from 006)

## Parked for later

- **Azure:** comes back as a migration epic when the developer schedules it.
- **Kafka:** no job while messaging is RabbitMQ only. A later job could be an event log
  of booking events.
- **Publishing the 12 epics to GitHub:** after architecture adds the spine references.
- **The pipeline's remaining work (review mode, worker, dashboard):** not in this
  epic set; where it lives is an architecture question.

## Next session

**010:** `bmad-architecture`. Inputs that are already decided: .NET 10, Angular, the
monorepo layout, Render, MongoDB. It must settle the cross-epic decisions listed in the
initiative Notes:
- tenancy in MongoDB;
- data access;
- staff sign-in and MFA (Q4);
- the concurrency design for every NFR-1 invariant;
- the outbox and RabbitMQ/Kafka;
- background jobs on Render;
- the error format, time, the threat model, environments and the frontend's shape;
- the SMS provider (Q1).

It also writes the ADRs for MongoDB and Render. After that: publish the epics, and
incept epic 1 (it has no screens). `bmad-ux` must happen before epic 2 is incepted.
