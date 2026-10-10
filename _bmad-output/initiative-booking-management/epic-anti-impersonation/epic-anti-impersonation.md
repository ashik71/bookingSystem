---
type: epic
title: "Patients can tell the real clinic from an impersonator"
parent: initiative-booking-management
covers: [FR-26, FR-30, FR-31, FR-32]
after: []
assignee: ""
risk: medium
---

# Patients can tell the real clinic from an impersonator

## Description

SlotBook can't stop someone impersonating the clinic outside the system, but it gives
patients ways to check:
- a public ticket check, which needs the code **and** the mobile number, so the code
  space can't be probed;
- one official contacts list with the date it was last confirmed;
- a permanent warning that the clinic takes money only through those contacts;
- a way to report fraud.

## Outcome

UJ-7 works: a patient who was sold a fake ticket finds out, sees the real contacts, and
reports it. The signals are SM-2 (public part) and SM-12 (fraud reports counted).

## Done when

1. The public check takes a code and a mobile number. It answers only *Genuine for
   ⟨date⟩*, *Cancelled* or *Not genuine*, using epic 7's lookup and code matching, and
   it never shows a name, practitioner, time or sequence number. A code given with the
   wrong number gets the same answer as an unknown code. *Not genuine* shows the official
   contacts and a link to report fraud (FR-26).
2. The public check is limited to 10 per IP and 5 per number per hour. Hitting a limit
   shows *Too many checks, try later*, which never reveals whether the ticket exists.
   Forged, wrong-number, cancelled, checked-in and lapsed codes never show *Genuine*,
   each with a test (FR-26, SM-2).
3. Official contacts are public. Each has a kind, a label per language and a value, and
   links open in a new tab. The page shows when an owner last confirmed the list, and
   says that no number outside it belongs to the clinic. Owners can re-confirm the list
   without changes, and only owners can edit it; every change is audited. Sign-in's
   "contact the clinic" messages now link here (FR-30, FR-3).
4. The home page and the ticket page always show the warning, which can't be dismissed.
   An owner writes it per language, or it keeps the default text (FR-31).
5. Filing a fraud report needs a patient session, and each number may file 3 a day. A
   report has what happened, the impersonator's number or link, and a date. Owners see
   reports newest first and mark them reviewed, which is audited. Patients see their own
   reports and their status (FR-32).
6. The standard epic checks pass (initiative, *Standard epic checks*).

## Boundaries

The boundary is public verification and the clinic's official voice. It does not
include the door check (epic 7) or announcements (epic 11). How the work is shared:

- **FR-26:** uses epic 7's lookup rule rather than defining its own.
- **FR-31:** puts the warning on epic 3's home page and epic 5's ticket page.
- **FR-44:** epic 10 deletes fraud-report text.

## References

- parent — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md §4.7 (FR-30 to FR-32), §4.5 FR-26, §2.3 UJ-7
- constraint — the same PRD §5.2 NFR-6, NFR-7, NFR-8
- addendum — _bmad-output/initiative-booking-management/prd-slotbook/addendum.md, sections Ticket code, Official booking address
- architecture — not yet written; sections for rate limiting and the threat model (enumeration)
- ux — not yet written; the public check, official contacts, warning, fraud report

## Notes

- Waits on epic-clinic-day because: the public check reuses the FR-24 lookup and FR-22
  code matching.
- Waits on epic-booking-core because: the warning goes on the ticket page.
- Waits on epic-schedule-and-publication because: the warning goes on the home page, and
  the public check uses the rate limiter.
- Waits on epic-patient-sign-in because: fraud reports need a patient session.
- Waits on epic-reserved-places because: both change staff navigation and the audit
  action list, so the changes are made one after the other.
