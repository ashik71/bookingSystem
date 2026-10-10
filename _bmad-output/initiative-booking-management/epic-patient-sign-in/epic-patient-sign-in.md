---
type: epic
title: "Patients sign in with their mobile number"
parent: initiative-booking-management
covers: [FR-1, FR-2, FR-3, FR-4, FR-5, FR-6, FR-27]
after: []
assignee: ""
risk: high
---

# Patients sign in with their mobile number

## Description

A patient proves they control a mobile number with a 6-digit OTP sent by SMS, and then
holds a session. Before launch, the OTP goes through the development channel instead of
real SMS. Signing in again on a known device with the same number skips the OTP, because
SMS costs money, but the known-device marker is a server-issued secret, so it can't be
forged. Send limits and a tenant-wide daily budget cap cost and harassment. The OTP
screen warns that the clinic never asks for the code.

## Outcome

Patients can sign in from a phone in seconds. The SMS cost per sign-in falls on known
devices without weakening identity, which is the trade SM-C1 watches.

## Done when

1. Sign-in with a number and an OTP works in the pre-launch environment through the
   development channel. Local, separated and international formats, and Bangla and
   Arabic-Indic digits, are all normalized to one E.164 patient. The privacy notice link
   is shown before the first OTP (FR-1, FR-2).
2. The OTP is 6 digits from a cryptographic RNG, stored only as a hash, expires after 5
   minutes, is single-use, and is invalidated after 5 wrong attempts (FR-2). The send
   limits hold:
   - a resend within 60 seconds is refused;
   - a 6th send per number per hour is refused;
   - more than 100 per IP per hour are refused;
   - once the daily budget is used up, sends are refused and an owner alert is raised;
   - every refusal is logged with the number masked (FR-3).
3. A known device skips the OTP only when both device and number match. A guessed
   marker, or one issued for another number, never matches, and tests prove both. The
   marker lasts 90 days from last use. Each skip is logged, so SM-C1 can be read
   (FR-4).
4. Sessions follow FR-5:
   - a session lasts 30 days;
   - sign out forgets the device;
   - sign out everywhere ends every session and known device, and is audited;
   - a patient session is refused on staff endpoints (FR-5, NFR-4).
5. No staff screen or API response contains an OTP, and the OTP screen shows the warning
   (FR-6). The application refuses to start with the development channel in production
   configuration, and a test proves it (FR-27). Expired OTPs are cleaned up by a timed
   job.
6. The log-scan test exists and finds no full mobile number (NFR-11a). The standard epic
   checks pass (initiative, *Standard epic checks*).

## Boundaries

The boundary is patient identity. How the work is shared with other epics:

- **FR-27:** this epic owns the one SMS interface and the development channel. The live
  provider, retries and push go to epic 6.
- **FR-3:** this epic shows "contact the clinic through its official contacts" as text.
  Epic 9 turns it into a link to the contacts page.
- **FR-5:** "*not found* for another patient's booking" is checked by epic 5, once
  bookings exist.
- **FR-4:** "never skips the booking limit" is checked by epic 5, and "never satisfies a
  fresh OTP" by epic 10.
- **FR-6:** "no OTP in exports" is checked by epic 7, which builds the CSV export.

**Handoffs out:**
- the SMS channel interface and the development channel go to epic 6;
- the patient record and its stored language (FR-43, used by FR-28) go to epics 5 and 6;
- the revoke-all-sessions function goes to epic 7 (an owner signs a patient out, FR-41)
  and epic 10 (data deletion, FR-44);
- the log-scan test goes to every later epic (SC-6).

## References

- parent — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md §4.1 (FR-1 to FR-6), §4.6 FR-27, §3 (Patient, Mobile number, OTP, Known device)
- constraint — the same PRD §5.2 NFR-4, NFR-5, NFR-6, NFR-8; §5.3 NFR-11a; §5.5 NFR-17
- addendum — _bmad-output/initiative-booking-management/prd-slotbook/addendum.md, sections OTP and SMS
- architecture — not yet written; sections for the patient session and known-device design, rate limiting, the threat model, environments
- ux — not yet written; the number entry and OTP screens

## Notes

- Open question: the default daily OTP budget stays at 300 until Q1 (the SMS free quota)
  is answered by the architecture step.
- Waits on epic-tenant-and-staff because: it needs the country, daily OTP budget and
  privacy-notice settings, the owner-alert API and the audit-entry API.
- Waits on epic-schedule-and-publication because: it needs the rate limiter, the
  scheduler (OTP cleanup) and the patient web shell.
