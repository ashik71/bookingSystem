# Session 008 — prd-finalize: assumptions resolved, PRD reviewed and finalized

**Date:** 2026-10-10 · **Duration:** ~2.5h · **Stage:** planning (`bmad-prd`, Finalize)

## What was covered

- **All 31 PRD assumptions were resolved** in five batches: sign-in, the OTP channel,
  booking rules, the rest, and staff MFA. One new assumption (A31) came up and was
  resolved.
- **Finalize was run end to end:**
  - memlog audit (clean);
  - input reconciliation against the brief and the prior-plan extract (3 small fixes,
    1 item set aside);
  - reviewer gate with two reviewers (the PRD rubric and an agent-proof acceptance
    criteria check);
  - all findings fixed with the simplest standard rule;
  - deferred open questions logged with owners;
  - structure and prose polish;
  - `status: final`.
- The PRD gained **FR-45, Owner alerts**, and two outcome metrics, **SM-11** and
  **SM-12**.

## Decisions reached

| Decision | Recorded in |
|---|---|
| OTP send limit: 100 per IP per hour as an abuse brake (because of carrier-grade NAT), plus a tenant-wide daily OTP budget (default 300 until Q1) | PRD FR-3, memlog |
| The known-device marker is a server-issued random secret bound to the number; deleting data always needs a fresh OTP | FR-4, FR-44, addendum |
| **A19:** live OTP is SMS only (free quota, else paid after a sale); a development channel before launch; **no email in v1** | FR-27, NFR-16/17, addendum |
| Booking limits are owner settings: 1 active, 2 per published week by default; cancelled bookings never count | FR-17 |
| The week start day is chosen at tenant setup and fixed in v1 | §3, FR-39 |
| MFA is required for all staff, owners and practitioners | FR-38 |
| WCAG 2.2 AA; the OTP field accepts paste and autofill | NFR-15 |
| Capacity model: a place is exactly free, reserved or taken. A practitioner booking takes a reserved place first and works until the slot ends (walk-ins) | §3, FR-14, FR-15 |
| Queue (sequence) numbers are never reused; ticket codes use a 31-symbol alphabet and are unique across today's and future bookings | §3, FR-22 |
| Patients have no name of their own; staff see attendee names | §3, FR-11/35/41 |
| Retention (Q3) and uptime (Q5) are deferred to before launch. The SMS quota (Q1), staff sign-in method (Q4) and threat model (NFR-8) go to architecture | memlog |

## Pressure-test — where the developer was challenged

- **Per-IP OTP limit (A5).** I argued that 20 per IP would lock out real patients
  behind a shared carrier IP at the publication peak. The developer accepted it.
- **Known-device marker.** The draft called it "spoofable", but skipping the OTP grants
  a full session, which would allow account takeover. The developer accepted the fix.
- **Email OTP fallback (A19).** Email OTP proves an email address, not a number, so the
  per-number limits would collapse. The developer chose SMS only and dropped email.
- **The weekly limit (A14).** I framed it around cancellation counting and which week a
  booking belongs to. **The developer pushed back hard:** the admin sets slots and
  limits, so keep it simple. The developer's position held, and the details were
  settled quietly in the simplest form.
- **Review findings.** The developer said a standard doctor–patient booking system
  already answers most edge cases, and that they should be applied, not asked. They
  were applied that way.

## Artifacts produced

- `_bmad-output/initiative-booking-management/prd-slotbook/`:
  - `prd-slotbook.md`: **final**;
  - `addendum.md`: updated with sizing, the OTP and known-device corrections, the
    ticket-code count, SMS priority and segments, and the official-address default;
  - `reconcile-brief.md` and `reconcile-prior-plan.md`;
  - `review-rubric.md` and `review-agent-proof.md`;
  - `.memlog.md`.

## PRs reviewed

None.

## Rules added to `CLAUDE.md`

None.

## Gaps exposed

- **The draft carried two critical contradictions:** "reserved" had two meanings, and
  FR-14 bypassed capacity. Only the agent-proof review caught them. Any requirements
  document that the sandbox agent builds from should get that review before it is
  called done.
- **My question style cost time.** Asking about edge cases that a standard clinic
  booking system already settles frustrated the developer. Standard conventions are
  now applied by default.
- **SQL-heavy invariants are waiting for architecture:** the capacity counter with
  reserved places, the per-number limit across slots, sequence numbers under
  concurrency, and ticket-code uniqueness by slot date. This is the developer's weak
  spot, so the architecture document must work each one through explicitly.
- **There is no go-live line yet.** All 45 FRs are v1, but nothing says which epics must
  ship before the clinic goes live.

## Homework set

- [ ] Ask the clinic to count its booking calls for 2 weeks before launch (the SM-10
      baseline)
- [ ] Optional: skim the changed core rules: §3 *Place*, FR-14, FR-22, FR-24, FR-26
      and FR-45
- [ ] Write the brief entry in `docs/learning/LEARNINGS.md` (carried over from 004)
- [ ] Upgrade Node on the Mac to ≥ 24.15 (carried over from 006)

## Parked for later

- **The go-live vs fast-follow line:** decide during `bmad-ticket` sequencing.
- **The retention period (Q3), the uptime target (Q5) and the privacy notice text:**
  before launch, with the clinic.
- **The official booking address:** by default the clinic's own domain if it has one,
  otherwise a free subdomain. Confirm when hosting is chosen.

## Next session

**009:** `bmad-ticket` slices the epics from PRD §7.2 and sets the go-live line. Then
`bmad-architecture` decides tenancy, the database and ORM, the concurrency design for
the invariants above, Q1, Q4 and the threat model. Stories that touch the database
wait for architecture.
