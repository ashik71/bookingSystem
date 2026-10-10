---
type: epic
title: "The first clinic goes live, with patients' privacy rights in place"
parent: initiative-booking-management
covers: [FR-44, FR-43]
after: [epic-deploy-platform, epic-tenant-and-staff, epic-schedule-and-publication, epic-patient-sign-in, epic-booking-core, epic-notifications, epic-clinic-day, epic-reserved-places, epic-anti-impersonation]
assignee: ""
risk: medium
---

# The first clinic goes live, with patients' privacy rights in place

## Description

The go-live gate. Patients can delete their data, which de-identifies their bookings for
the clinic's records. A retention period is set and enforced. Production is stood up from
epic 1's environment definition, with the live SMS provider, at the official booking
address. The clinic's tenant is set up there by hand. The whole core flow is proven in
all three languages, and the patient pages pass an accessibility audit. After this epic,
the clinic takes real bookings.

## Outcome

The first clinic uses SlotBook in production. The initiative's Done-when clock (4 weeks
of real bookings) starts here.

## Done when

1. Delete my data follows FR-44:
   - it needs a fresh OTP, even on a known device or in a valid session (FR-4);
   - it cancels active bookings with no cut-off and no notification;
   - it removes the number, attendee names and ages, notes, cancellation reasons and
     fraud-report text;
   - it de-identifies the bookings it keeps;
   - it ends every session, known device and push subscription;
   - it is audited, with the patient as actor.

   The same number then signs in as a new patient with no history.
2. The retention period (Q3) is set. A purge older than it runs under a separate
   maintenance identity, is the only deletion of audit entries allowed, and is itself
   audited (NFR-11, FR-42). The privacy notice text is in place in every language.
3. Production runs from epic 1's environment definition:
   - with the live SMS provider, and it refuses the development channel;
   - at the official booking address over HTTPS with HSTS;
   - with the clinic's tenant set up by hand.

   Encryption at rest, daily backups, health and readiness, and correlation IDs are each
   re-checked in production (NFR-5, NFR-10, NFR-18, NFR-19). A real OTP and a real
   booking confirmation arrive by SMS.
4. The SM-9 demo (publish, reserve, book, ticket, door check, rebook into a reserved
   place) runs end to end in production in `bn`, `en` and `ar`. Every built-in string
   exists in all three (FR-43, completeness pass). The patient pages pass a WCAG 2.2 AA
   audit at 360 px (NFR-15).
5. The pre-launch environment ran a full month at $0, and production runs on free plans
   (NFR-16, SM-8). The uptime position (Q5) is recorded. A merge to main deploys to
   production, which epics 11 and 12 use. The standard epic checks pass (initiative,
   *Standard epic checks*).

## Boundaries

The boundary is the go-live gate: privacy rights, retention, production and the launch
checks. No new booking features. How the work is shared:

- **FR-44:** this epic deletes the data that exists at launch. Epic 12 extends deletion
  to conversation text.
- **FR-43:** this epic runs the completeness pass. The infrastructure is epic 2's, and
  every epic ships its own strings.

Touch point: DNS and the official booking address (the clinic's own domain if it has
one, else a free subdomain; addendum *Official booking address*).

## References

- parent — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md §4.12 FR-44, §4.11 FR-43, §8 SM-8, SM-9
- constraint — the same PRD §5.3 (NFR-10, NFR-11), §5.4 NFR-15, §5.5 (NFR-16 to NFR-19)
- addendum — _bmad-output/initiative-booking-management/prd-slotbook/addendum.md, section Official booking address
- architecture — not yet written; sections for environments, background jobs (purge), the threat model

## Notes

- Open question: the retention period (Q3), the uptime expectation and out-of-hours
  response (Q5), and the privacy notice text are agreed with the clinic. Done when 2 and
  5 wait on them.
- Open question: the clinic's own domain, or a free subdomain, is confirmed once hosting
  is in place (addendum).
- Waits on every go-live epic (1 to 9) because: this is the gate before the first real
  booking.
