# Reconciliation — brief-slotbook (final) + brief addendum vs PRD

Session 008, 2026-10-10.

**Result: no unintended gaps.** Every *Solution — v1* item, constraint, success
criterion and open question in the brief maps to a PRD FR, NFR, SM, non-goal or
resolved assumption.

| Brief item | PRD | Note |
|---|---|---|
| Notifications: SMS, else email or browser push | FR-27, NFR-17 | **Intentional divergence (A19, session 008).** Live OTP is SMS only; email removed from v1; the email/push fallback survives only as the pre-launch development channel. Reason recorded in the PRD addendum |
| Zero cost before a sale | NFR-16 | Now states that paid SMS is the one expected cost after a sale |
| Open questions: limits, reserved timing, horizon/cut-off, offline door check | FR-17, FR-12/14, FR-9/18, §6 | All answered |
| Qualitative: "requirements precise enough that an agent can't misread them" | §0, every FR | Carried as testable consequences |
