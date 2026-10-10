---
type: epic
title: "Owners run their tenant: staff, settings, audit log and alerts"
parent: initiative-booking-management
covers: [FR-38, FR-39, FR-42, FR-45, FR-43]
after: []
assignee: ""
risk: high
---

# Owners run their tenant: staff, settings, audit log and alerts

## Description

The tenant and its staff side, which every later epic builds on. An owner signs in with
multi-factor authentication, manages practitioner and owner accounts, and sets the
tenant's rules. Every significant action is written to an audit log that no role can
change. Owners see alerts about problems. Tenant isolation is proven with a second test
tenant from the first table onwards. The language infrastructure (three languages, a
build that fails on a missing string, right-to-left layout) is built here, so every
later screen is born translated.

## Outcome

For the owner: the tenant can be run by its staff with nothing done by hand after
setup. For the build: every later epic gets the audit, alert, settings and language
mechanisms ready to use. The signal is SM-5 (tenant isolation) passing from this epic
on.

## Done when

1. The tenant and its first owner are created by a documented by-hand setup step (PRD
   §2.2), which also sets the setup-only settings (country, time zone, week start). Staff
   sign in with MFA, and a sign-in without the second factor is refused (FR-38).
2. An owner creates, disables and re-enables staff accounts. Roles and display names
   work as FR-38 says. A disabled account's sessions stop working within 1 minute. The
   last enabled owner can't be disabled.
3. Every owner-editable setting in FR-39 can be set within its allowed values, with text
   per language where FR-39 says. Each change is audited (FR-39, FR-42).
4. The application's database identity can't update or delete audit entries, and an
   automated test proves it. An owner browses the log newest first and filters it by
   actor, action and date range (FR-42).
5. The alert list and the banner on every owner page work for a test-raised alert until
   an owner acknowledges it. Practitioners never see alerts, and each alert is also
   written to the application log (FR-45). Built-in strings exist in `bn`, `en` and
   `ar`; a missing translation fails the build; `ar` mirrors right to left; digits are
   shown as ASCII; Bangla and Arabic-Indic digits typed in are normalized (FR-43,
   infrastructure part).
6. The standard epic checks pass (initiative, *Standard epic checks*). The cross-tenant
   suite starts here with a second test tenant.

## Boundaries

The boundary is staff identity and tenant administration. It does not include patient
sign-in (epic 4). How the work is shared with other epics:

- **FR-45:** this epic builds the alert mechanism and the alert list. The alerts are
  raised by epic 4 (OTP budget), epic 5 (count drift) and epic 6 (SMS failure).
- **FR-42:** this epic builds the log and its browse view. Each later epic writes its
  own entries (SC-3).
- **FR-39:** this epic owns the whole settings page, including settings whose behaviour
  arrives in later epics.
- **FR-43:** this epic owns the language infrastructure. Each epic ships its own strings
  (SC-5), and epic 10 runs the completeness pass.
- **FR-38's** "disabling never changes slots or bookings" is checked by epic 5, once
  bookings exist.

**Handoffs out:**
- the audit-entry write API, the owner-alert raise API and the settings read API go to
  every later epic;
- the language catalogue and its build check go to every epic with screens;
- staff sessions and roles go to epics 3, 7, 8, 9, 11 and 12.

## References

- parent — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md §4.10 (FR-38, FR-39, FR-42, FR-45), §4.11 (FR-43), §2.2 (setup by hand)
- constraint — the same PRD §5.2 (NFR-3, NFR-4, NFR-7, NFR-8)
- architecture — not yet written; sections for the tenancy model, staff sign-in and MFA (Q4), the threat model, data access, i18n

## Notes

- Unknown: whether the MongoDB host's free tier allows a custom role limited to insert and
  find on the audit collection. Done when 4 depends on it. If it doesn't, the
  architecture step decides how FR-42's guarantee is enforced.
- Open question: the staff sign-in method and the MFA factor (Q4) come from the
  architecture step. Done when 1 waits on it.
- Waits on epic-deploy-platform because: this epic creates the first collections and runs
  the first database-backed tests in CI.
