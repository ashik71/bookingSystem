---
type: epic
title: "Owners publish the week; anyone sees what is bookable"
parent: initiative-booking-management
covers: [FR-7, FR-8, FR-9, FR-10, FR-11]
after: []
assignee: ""
risk: medium
---

# Owners publish the week; anyone sees what is bookable

## Description

UJ-1 up to publication. Practitioners enter their own slots into next week's draft. An
owner copies last week, edits, and publishes the whole week in one step, now or at a
scheduled time. Anyone can then see the bookable slots without signing in. After
publication an owner can add slots, raise or lower capacity within the rules, and close
or reopen slots and whole dates. This epic also opens the patient side of the web app:
its shell, the home page with the way into booking, and the language switcher.

## Outcome

For owners, publishing a typical week takes under 10 minutes (SM-7). For patients, the
published week is visible with correct free counts, which booking (epic 5) then commits
against.

## Done when

1. A practitioner drafts their own slots, and an owner drafts for any practitioner. Copy
   last week skips overlapping slots and lists them, and doesn't copy closed status.
   Patients never see a draft week or a half-published week through any page or API
   (FR-7, FR-8).
2. A scheduled publication fires within 60 seconds of its time, exactly as if an owner
   had selected Publish. Until then it can be changed or cancelled (FR-8).
3. Anonymous availability shows only published, future, unstarted slots. Each shows as
   free (with its count), reserved, full or closed, with free = capacity − reserved −
   taken. The publication day and time are shown to patients (FR-8, FR-9).
4. Changes to a published week follow FR-10: add a slot, raise capacity, lower it only
   down to taken + reserved (unit-tested with seeded counts), no other edit or delete.
   Closing and reopening a slot or a whole date follow FR-11. Publication, schedule
   changes, slot changes, closing and reopening are all audited.
5. A copied-and-edited week is published in under 10 minutes (NFR-14, SM-7).
   Availability is rate-limited per IP and returns 429 with a retry hint (NFR-6). A
   patient can switch language on any patient page; the choice is remembered in the
   browser, and a first visit uses the tenant's default language (FR-43).
6. Browser tests run in CI for the patient pages. The standard epic checks pass
   (initiative, *Standard epic checks*).

## Boundaries

The boundary is the schedule, up to publication and availability. It does not include
booking (epic 5) or reserved places (epic 8). `reserved` and `taken` exist on a slot from
this epic on and stay 0 until epics 8 and 5, so the capacity rule never changes shape.
How the work is shared with other epics:

- **FR-11:** this epic delivers closing and reopening. The list of affected bookings and
  the bulk cancel go to epic 7, which needs bookings to exist.
- **FR-10:** lowering capacity below taken + reserved is checked end to end by epic 5
  for taken and by epic 8 for reserved. Lowering capacity, or closing a slot, while a
  booking is racing it is tested in epic 5 (NFR-1).
- **FR-7:** "reserved places aren't copied" is checked by epic 8.
- **The home page:** this epic owns it. Epic 9 adds the warning, epic 11 the
  announcements and epic 12 the unread badge.

**Handoffs out:**
- the slot's count shape (capacity, reserved, taken, open or closed) goes to epics 5 and 8;
- the rate limiter goes to epics 4, 7, 9 and 12;
- the scheduler for timed jobs goes to epics 4, 5, 6 and 10;
- the patient shell and home page go to epics 4, 9, 11 and 12;
- the browser-test setup goes to every epic with screens.

## References

- parent — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md §4.2 (FR-7 to FR-11), §2.3 UJ-1, §3 (Week, Draft week, Publication, Slot, Capacity, Place)
- constraint — the same PRD §5.2 NFR-6, §5.4 NFR-14, NFR-15
- architecture — not yet written; sections for the Slot document, background jobs, time handling, rate limiting, the frontend's shape
- ux — not yet written; the draft week, publish, and availability screens

## Notes

- Unknown: scheduled publication relies on a timer firing within 60 seconds. Epic 1's
  timer proof settles whether the hosting allows it.
- Waits on epic-tenant-and-staff because: it needs staff sessions and roles, the time
  zone, week start and publication-day settings, the audit-entry API and the language
  catalogue.
