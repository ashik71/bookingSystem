---
type: epic
title: "Practitioners keep places for phone, walk-in and missed patients"
parent: initiative-booking-management
covers: [FR-12, FR-13, FR-14]
after: []
assignee: ""
risk: high
---

# Practitioners keep places for phone, walk-in and missed patients

## Description

Phone and walk-in booking stay a normal channel on purpose. A practitioner, or an owner,
holds back places in a slot. Patients see that those places exist but can't book them
online. The practitioner books phone callers, walk-ins and patients who missed a booking
into them, without an OTP, until the slot ends, and releases whatever is left. Reserving,
releasing and practitioner booking are atomic against patient bookings.

## Outcome

UJ-2 and UJ-5 work. The signals are SM-4 (reserved means reserved, even through the API)
and, after launch, SM-11 (reserved places in use).

## Done when

1. A practitioner reserves 3 of a slot's 12 places. Patients see 9 bookable and 3
   reserved. A practitioner booking takes a reserved place, and releasing makes the rest
   bookable at once. Reserving is refused beyond the free places and for a slot that has
   started. Releasing is refused beyond the reserved places (FR-12, FR-13).
2. Reserving racing a patient booking ends with exactly one success. So does releasing
   racing a booking. Tests prove both (NFR-1).
3. In a slot with 0 free places and at least 1 reserved place, a patient booking fails
   with `slot_full`, from the page and from a direct API call. A test proves it (FR-14,
   SM-4).
4. A practitioner booking follows FR-14:
   - a patient record is created for a new number, with no OTP;
   - it takes a reserved place first, else a free one, and records which;
   - it is allowed after the slot starts, until it ends;
   - it is never counted towards, or refused by, the booking limit;
   - every refusal reason works;
   - it records its origin as practitioner;
   - its confirmation notification goes out.

   Cancelling it returns the place to reserved. Lowering capacity below taken + reserved
   is refused end to end (FR-10, reserved part). Copy last week doesn't copy reserved
   places (FR-7).
5. Reserving, releasing and practitioner bookings are audited, which is the data source
   for SM-11. The standard epic checks pass (initiative, *Standard epic checks*).

## Boundaries

The boundary is reserved capacity and practitioner booking, built on epic 5's booking
transaction and epic 3's slot. Booking lists and the door check are not included (epic 7).

## References

- parent — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md §4.3 (FR-12 to FR-14), §2.3 UJ-2, UJ-5, §3 (Place, Reserved place, Release, Patient booking / Practitioner booking)
- constraint — the same PRD §5.1 NFR-1
- addendum — _bmad-output/initiative-booking-management/prd-slotbook/addendum.md, section Capacity (the reserved-place predicate; background only)
- architecture — not yet written; section for the concurrency design (reserve, release, practitioner booking)
- ux — not yet written; the reserve, release and book-a-patient screens

## Notes

- Waits on epic-booking-core because: it extends the booking transaction and the capacity
  predicate, and uses the origin and kind-of-place fields.
- Waits on epic-notifications because: a practitioner booking sends the normal
  confirmation.
- Waits on epic-clinic-day because: both change the Booking aggregate's transitions, so
  the changes are made one after the other.
