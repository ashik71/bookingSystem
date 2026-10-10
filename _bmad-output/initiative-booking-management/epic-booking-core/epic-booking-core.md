---
type: epic
title: "Patients book a free place and hold a ticket"
parent: initiative-booking-management
covers: [FR-15, FR-16, FR-17, FR-18, FR-19, FR-21, FR-22, FR-23]
after: []
assignee: ""
risk: high
---

# Patients book a free place and hold a ticket

## Description

The centre of the product. A signed-in patient books one free place in a published, open
slot, with no approval step, and gets a ticket: sequence number, ticket code and a QR code.
They see their bookings and cancel before the cut-off. The guarantee: a slot never has
more taken places than its capacity, and a number never exceeds its booking limit, under
any concurrency. Both are enforced by the database at commit, never by a read-then-write.
Bookings lapse on their own when the slot ends. Each confirmation and cancellation emits
one event for the notifications epic to send.

## Outcome

UJ-3 and UJ-6 work end to end for patients. The signals are SM-1 (no double bookings),
SM-3 (limits hold) and SM-6 (first booking in under 2 minutes).

## Done when

1. In the pre-launch environment, a patient books a free place with attendee name and age,
   a reason category and an optional note. They see the ticket: attendee name,
   practitioner, date, time, sequence number, the code in a fixed-width font, and a QR
   code that scans from a phone screen at 360 px. The booking appears under My bookings
   with its state timeline (FR-15, FR-16, FR-19, FR-22, FR-23). Another patient's booking
   or ticket returns *not found* (FR-5).
2. The concurrency suite runs in CI on every backend PR. Each of these ends as stated
   (NFR-1, SM-1, SM-3):
   - 200 concurrent patients for 10 places → exactly 10 bookings, and 190 get `slot_full`;
   - 20 parallel bookings from one number with a limit of 1 → exactly 1;
   - parallel bookings never share a sequence number;
   - lowering capacity racing a booking, and closing a slot racing a booking, each end
     consistent with FR-10 and FR-11.
3. Booking rules follow FR-15, FR-17 and FR-18:
   - every failure reason;
   - `already_booked`;
   - up to 3 suggested slots on `slot_full`;
   - the idempotency key;
   - both limits, with cancelled bookings never counted, and a known-device sign-in never
     skipping them (FR-4);
   - cancellation before the cut-off frees the place at once and stops the code
     validating; after the cut-off it is refused.
4. Two rules about later changes hold:
   - a changed limit, cut-off or reason-category list never changes existing bookings,
     and a category in use can be retired but not deleted (FR-39);
   - disabling a practitioner leaves their slots and bookings unchanged (FR-38).

   Lifecycle and codes follow FR-21 and FR-22:
   - transitions go through one domain rule;
   - a booking still `confirmed` at slot end becomes `lapsed` within 15 minutes;
   - ticket codes are unique among the tenant's today-or-later bookings under concurrent
     confirmations;
   - lowering capacity below taken places is refused end to end (FR-10, taken part).
5. Every confirmation and cancellation writes exactly one event to the outbox, in the
   booking's own transaction. The event carries the actor, the reason and a notify flag.
   Each booking records its origin (patient or practitioner) and the kind of place it took
   (free or reserved). The reconciliation job raises an owner alert on count drift
   (NFR-2).
6. At about 100 concurrent patients, p95 availability < 500 ms and p95 booking commit
   < 1 s, server time (NFR-13). A scripted first-time booking takes under 2 minutes
   (NFR-12, SM-6). The log scan also finds no age, reason category or note (NFR-11a).
   The standard epic checks pass (initiative, *Standard epic checks*).

## Boundaries

The boundary is the patient side of booking. Not covered here:

- **Sending notifications:** this epic emits the events; epic 6 sends them.
- **Clinic cancel, check-in and the door check:** epic 7. This epic owns the lifecycle
  rule they use (FR-21 shared: 7 adds the check-in transition and its races).
- **Booking into reserved places:** epic 8. The capacity predicate excludes reserved
  places from the start. Origin is always "patient" and the kind of place always "free"
  until epic 8, and cancelling returns a reserved-kind place to reserved.
- **The anti-impersonation warning on the ticket page:** epic 9.

**Handoffs out:**
- booking events (confirmed; cancelled, with actor, reason and notify flag) go to epics 6,
  7, 8 and 10;
- the lifecycle domain rule goes to epics 7 and 8;
- the origin and kind-of-place fields go to epics 7 (FR-40's "practitioner booking"
  column) and 8, and to SM-10;
- ticket-code storage and matching data go to epics 7 and 9;
- the ticket page goes to epic 9.

## References

- parent — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md §4.4 (FR-15 to FR-21), §4.5 (FR-22, FR-23), §2.3 UJ-3, UJ-6, §3 (Place, Booking, Active booking, Sequence number, Ticket code, Booking limit)
- constraint — the same PRD §5.1 (NFR-1, NFR-2), §5.3 NFR-11a, §5.4 (NFR-12, NFR-13, NFR-15)
- addendum — _bmad-output/initiative-booking-management/prd-slotbook/addendum.md, sections Capacity, Booking limit, Ticket code, Time (SQL sketches are background only; the store is MongoDB)
- architecture — not yet written; sections for the concurrency design (each write path's transaction, write concern and read concern), the outbox and event contract, background jobs, the error format

## Notes

- Unknown: if the database runs on a free tier with a throughput cap, a booking
  transaction of several operations each may hit that cap at the publication peak.
  Done when 6 measures it. If it fails, the database tier is reconsidered after a sale.
- Waits on epic-tenant-and-staff because: it needs the booking limits, cut-off, reason
  categories, ticket-code prefix and the owner-alert API.
- Waits on epic-schedule-and-publication because: it needs published slots with their
  counts, and the scheduler (lapse, reconciliation).
- Waits on epic-patient-sign-in because: it needs patient sessions and the patient record.
