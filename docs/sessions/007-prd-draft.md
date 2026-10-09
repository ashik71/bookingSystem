# Session 007 — prd-draft: the PRD drafted from the brief and a prior private plan

**Date:** 2026-10-09 · **Duration:** ~1.5h · **Stage:** planning (`bmad-prd`)

## What was covered

- **Epic #3 closed** by the developer. Epic 0 is complete.
- **`bmad-prd` started** in Create mode, at launch rigour. Coaching (journey-led) was
  chosen, but after the developer said "ask me what you want", the session switched to
  draft-then-review.
- **The developer's prior private plan for the first tenant was used as an input.**
  It contained a proposal, capability specs, design notes and UI mockups, and it lives
  outside the repo. Only a generic extract entered the repo
  (`prd-slotbook/input-prior-plan.md`); a firewall scan of the extract is clean. The
  pricing and negotiation document was not read for the PRD.
- **10 conflicts found between the final brief and the prior plan** (C1–C10). The
  developer decided all of the ones that matter.
- **The full PRD draft was written,** and three phase-blocking assumptions were
  resolved.
- **Not done:** Finalize (memlog audit, input reconciliation, reviewer gate, polish,
  `status: final`). It is waiting for the developer's read-through.

## Decisions reached

| Decision | Recorded in |
|---|---|
| C1: direct booking; no approval or triage step | PRD memlog, FR-15 |
| C2: a Slot belongs to a Practitioner, has capacity ≥ 1, and each booking gets a sequence number | PRD Glossary, FR-7 |
| C3: a practitioner reserves *places* in a slot, before or after publication, and can release them | FR-12, FR-13 |
| C4: the owner sets the booking limit (default 1 active per number); practitioner bookings don't count | FR-17 |
| C5: the staff door check shows details; the public check needs code **plus** mobile number and answers only genuine / not / cancelled | FR-24, FR-26 |
| C8: v1 adds official contacts with a home warning, fraud reports, announcements and chat | §4.7–4.9 |
| Horizon: any published future slot; patients can cancel until an owner-set cut-off (default 2h) | FR-9, FR-18 |
| The door check is online only; practitioners and the owner answer chat; no front-desk role | FR-24, FR-35, §6 |
| Booking requires attendee name, age and reason category; the note is optional; family bookings are allowed and the limit counts per number | FR-16 |
| Erasure de-identifies bookings; audit entries hold patient IDs only, so they stay append-only | FR-42, FR-44 |

## Pressure-test — where the developer was challenged

- **Chat in v1.** It is flagged as the largest extra. The free real-time tier fits
  about 125 chats a day and is wrong at about 10× that. The developer kept chat; the
  scale note is in the addendum for architecture.
- **Booking-form fields.** I recommended name plus a note, to keep health-adjacent data
  to a minimum. The developer chose age plus reason category, overriding the
  recommendation without stating a reason. The PRD mitigates it: the category and the
  note never appear on tickets, door checks, public checks or notifications (FR-16,
  NFR-11a).
- **Epic breakdown before architecture.** I flagged that stories which touch the
  database need the tenancy and DB decisions first. The developer chose *review, then
  finalize* before `bmad-ticket`.

## Artifacts produced

- `_bmad-output/initiative-booking-management/prd-slotbook/`:
  - `prd-slotbook.md` (draft). It has 8 user journeys, a glossary, FR-1 to FR-44, NFR-1
    to NFR-19, SM-1 to SM-10 plus 4 counter-metrics, Q1–Q7, and an assumptions index
    A1–A30;
  - `addendum.md`, with notes for architecture and UX and the rejected alternatives;
  - `input-prior-plan.md`, the generic extract with the conflict table;
  - `.memlog.md`.

## PRs reviewed

None.

## Rules added to `CLAUDE.md`

None.

## Gaps exposed

- **The brief and the client's real workflow diverged more than STATE showed.** The
  prior plan had triage, windows instead of practitioners, and a native app. Future
  sessions should treat `input-prior-plan.md` as evidence, while the PRD decisions
  stand.
- **The booking limit is a second concurrency invariant across slots.** A conditional
  update on the slot doesn't cover it. Architecture must design it explicitly, and
  this is SQL territory, the developer's weak spot.
- **The PRD now carries 27 open assumptions** (A1–A30 minus the 3 resolved). Most are
  default numbers, and two are architecture blockers: staff sign-in (A25) and the
  email fallback (A19).

## Homework set

- [ ] Read the PRD draft and comment, especially §4.4 (booking), §4.5 (ticket) and the
      assumptions index
- [ ] Write the brief entry in `docs/learning/LEARNINGS.md` (carried over from 004)
- [ ] Upgrade Node on the Mac to ≥ 24.15 (carried over from 006)

## Parked for later

- **The content library / CMS, sponsor banners and reports:** non-goals for v1.
- **The offline door check:** a signed QR for a later version.
- **The prior plan's no-show handling and paid/unpaid marking:** non-goals.

## Next session

**008:** finish `bmad-prd`. Bring in the developer's review comments, then run Finalize:
the memlog audit, input reconciliation, the reviewer gate, triage of the open items,
polish, and `status: final`. Then `bmad-ticket` slices the epics (§7.2 has a suggested
slicing). Story refinement for stories that touch the database waits for
`bmad-architecture`.
