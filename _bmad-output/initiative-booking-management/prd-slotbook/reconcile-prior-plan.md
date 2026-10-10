# Reconciliation — input-prior-plan.md (generic extract) vs PRD

Session 008, 2026-10-10. Conflicts C1–C10 were decided in session 007; this pass looks
for anything else the PRD dropped.

| # | Prior-plan item | PRD coverage | Action |
|---|---|---|---|
| G1 | Sizing: ~1,000 patients/week at peak, ~140/day average, ~300 tickets/day, ~100 concurrent at peak, 2–3 staff accounts | Only "100 at once" in NFR-13 | **Autofix:** sizing section added to the addendum for architecture |
| G2 | Door check works with a keyboard-wedge scanner (a scanner that types into a text field) | FR-24 has camera scan and typing | **Autofix:** FR-24 states the code field submits on Enter, so a hardware scanner works without extra setup |
| G3 | Treatment history with staff notes on past bookings | Not in PRD, not in non-goals | **Autofix:** added to §6 non-goals so nobody builds it by accident |
| G4 | Access to the patient's description (health-adjacent) is audited | FR-16/NFR-11a restrict who sees the category and note; viewing them is not audited | **Set aside:** the slot's practitioner sees them in the day view by design; auditing every list view would be noise. Revisit if a privacy review asks for it |
| G5 | Bulk actions report partial results per item, never as full success | FR-11 bulk cancel | Already implied (one notification + audit entry per booking); no change |
