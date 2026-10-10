---
type: epic
title: "Practitioners run their day: who is coming, who is genuine"
parent: initiative-booking-management
covers: [FR-11, FR-20, FR-21, FR-24, FR-25, FR-40, FR-41]
after: []
assignee: ""
risk: medium
---

# Practitioners run their day: who is coming, who is genuine

## Description

The staff side of bookings, which is the practitioner's job "run my day". Staff see each
day's bookings and counts, and owners export a week as CSV. At the door, a practitioner
scans or types a ticket code, sees an unambiguous result, and checks the patient in;
each ticket works once. Staff cancel bookings with a reason. After closing a slot, an
owner cancels the affected bookings in bulk. Staff find a patient by number or attendee
name, and an owner can sign a patient out everywhere.

## Outcome

UJ-4 and the edge case of UJ-1 work. The signal is SM-2, door part: forged, reused,
wrong-day, cancelled and lapsed tickets are all rejected at the door.

## Done when

1. Practitioners see their own day view, and owners see every practitioner's. Each slot
   shows capacity, taken, reserved and free. Each booking shows its sequence number,
   attendee name and age, reason category, masked number, state and the
   practitioner-booking flag. The owner's CSV export has FR-40's columns, full numbers,
   escaped formula cells and no OTP, and is audited (FR-40, NFR-9, FR-6).
2. A camera scan (current mobile Chrome and Safari), a typed code and a hardware scanner
   typing into the field each give exactly one of FR-24's six results, chosen by FR-24's
   lookup and order. Each result shows FR-24's details. Code matching ignores case,
   accepts the code with or without the prefix and hyphen, accepts Bangla and
   Arabic-Indic digits, and survives a change of prefix (FR-22). Lookups are limited to
   60 per minute per account, with one audit entry per refused minute. Every *Not
   genuine* result is logged, so SM-12 can be read (FR-24).
3. Check-in records the time and the actor. Two simultaneous check-ins of one ticket
   succeed once. Forged, reused, wrong-day, cancelled and lapsed codes are rejected, each
   with a test. A cancel racing a check-in ends with exactly one of them, and a test
   proves it (FR-25, FR-21, NFR-1).
4. A practitioner cancels bookings in their own slots, and an owner any booking. A reason
   is required; the place is freed (or returned to reserved); the patient is notified
   with the reason; the action is audited. After closing a slot or date, the owner sees
   the affected bookings and cancels them in bulk with one reason, and the result is
   reported per booking (FR-20, FR-11).
5. Patient lookup works by number or by attendee name, with reason category and note
   shown only as FR-16 allows. Opening the full number is audited. An owner signs a
   patient out everywhere: their sessions and known devices end, and the action is
   audited (FR-41).
6. The standard epic checks pass (initiative, *Standard epic checks*).

## Boundaries

The boundary is the staff side of bookings: lists, door, check-in, clinic cancel, lookup.
It does not include the public ticket check (epic 9) or practitioner booking and
reserved places (epic 8). How the work is shared with other epics:

- **FR-11:** epic 3 delivers closing and reopening. This epic delivers the affected list
  and the bulk cancel.
- **FR-21:** epic 5 owns the lifecycle rule. This epic adds the check-in transition and
  its races.

**Handoffs out:** the FR-24 lookup rule and FR-22 code matching, as a reusable query, go
to epic 9.

## References

- parent — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md §4.5 (FR-22, FR-24, FR-25), §4.4 (FR-20, FR-21), §4.2 FR-11, §4.10 (FR-40, FR-41), §2.3 UJ-1, UJ-4
- constraint — the same PRD §5.1 NFR-1, §5.2 NFR-6, §5.3 NFR-9
- addendum — _bmad-output/initiative-booking-management/prd-slotbook/addendum.md, For UX (the door check's big unambiguous result)
- architecture — not yet written; sections for the concurrency design (check-in), the threat model (ticket forgery and enumeration)
- ux — not yet written; the day view, the door check, the patient lookup

## Notes

- Waits on epic-booking-core because: it needs bookings with origin and kind of place, the
  lifecycle rule, and ticket-code storage.
- Waits on epic-notifications because: a clinic cancel must reach the patient with its
  reason.
- Waits on epic-schedule-and-publication because: it needs slot closing for the bulk
  cancel, and the rate limiter.
- Waits on epic-patient-sign-in because: it needs the revoke-all-sessions function.
