---
type: epic
title: "Owners post announcements and notify affected patients"
parent: initiative-booking-management
covers: [FR-33]
after: [epic-launch]
assignee: ""
risk: medium
---

# Owners post announcements and notify affected patients

## Description

Fast-follow after launch. An owner writes notices (closures, new rules, changed hours)
that appear on the tenant's home page. When publishing, the owner can choose to notify
every patient with an active booking, or only those booked on chosen dates. The fan-out
from one event to N notifications runs at a lower priority than OTPs and booking
notifications, and never delays them.

## Outcome

Owners reach patients through the official channel instead of ad-hoc messages. The
fan-out is the messaging focus area's one-to-many job.

## Done when

1. An owner writes, publishes, edits and archives announcements. Each has a title (up to
   100 characters), a body (up to 2,000), optional text per language, an optional image
   (up to 1 MB, JPEG, PNG or WebP), an optional *show until* date, and a pinned flag
   (FR-33).
2. The home page shows published announcements, pinned first and then newest first.
   Archived ones are hidden. A *show until* announcement shows through the end of that
   date in the tenant's time zone.
3. Notify is optional, for all patients with an active booking or those booked on chosen
   dates. The page shows the count before the owner confirms. Each patient gets at most
   one notification per announcement. Without notify, nothing is sent. Each notify send
   is audited.
4. A load test shows announcement fan-out never delays OTPs or booking notifications
   (FR-33, addendum *SMS*).
5. The standard epic checks pass, in production (initiative, *Standard epic checks*).
   SC-7 covers image storage, and SC-4 refuses practitioners.

## Boundaries

The boundary is announcements and their fan-out. It reuses epic 6's pipeline and epic
3's home page. Touch point: file storage for images, configured here. Not a content
library or CMS (PRD §6).

## References

- parent — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md §4.8 FR-33, §2.3 UJ-1, UJ-3
- constraint — the same PRD §5.2 NFR-7, §5.5 NFR-16
- addendum — _bmad-output/initiative-booking-management/prd-slotbook/addendum.md, sections SMS (priority) and Messaging (fan-out)
- architecture — not yet written; sections for messaging and file storage
- ux — not yet written; the announcement editor and the home page

## Notes

- Decision: fast-follow, shipped after the go-live line (developer, 2026-10-10).
- Waits on epic-launch because: fast-follow epics ship to production after the first real
  booking.
- Waits on epic-notifications because: fan-out uses its pipeline and priority classes.
- Waits on epic-schedule-and-publication because: announcements go on its home page.
