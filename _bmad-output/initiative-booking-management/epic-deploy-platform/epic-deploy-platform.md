---
type: epic
title: "SlotBook deploys to Render on every merge"
parent: initiative-booking-management
covers: []
after: []
assignee: ""
risk: medium
---

# SlotBook deploys to Render on every merge

## Description

Second half of the platform baseline: environments, deployment and operations. Epic 0
left these to "a later epic". When this epic is done, a merge to main deploys the API,
the Angular app and the database's schema changes (indexes, validators) to a pre-launch
environment on Render, backed by MongoDB, with no manual step. The environment is
described once, so epic 10 can stand up production from the same definition. Every
later epic ships through it.

## Outcome

For the developer, every later epic is running in a deployed environment the moment it
merges, at $0, and the riskiest free-tier limit (timers on a service that sleeps) is
known before any feature depends on it.

## Requirements

Platform baseline, so `covers` is empty. Each line cites its source:

- The deployed environment: hosting on Render, the database on MongoDB (initiative
  Notes, decisions of 2026-10-10)
- HTTPS only, HSTS, no secret in the repo (PRD NFR-5, transport and secrets part)
- Encryption in transit and at rest (PRD NFR-10)
- $0 hosting before a sale (PRD NFR-16, hosting and database part)
- Health and readiness endpoints, and a correlation ID in every request's log (PRD NFR-18)
- A daily backup and one tested restore (PRD NFR-19)

## Done when

1. A merge to main deploys the API, the web app and the database's schema changes to the
   pre-launch environment with no manual step. The page at the environment's HTTPS
   address shows the API healthy, and a failed deploy leaves the previous version
   running.
2. HTTP redirects to HTTPS and responses carry HSTS. Secrets live only in the host's
   secret settings, never in the repo or an image (NFR-5, NFR-10).
3. Health and readiness endpoints answer, and every request's log line carries a
   correlation ID (NFR-18).
4. CI runs database-backed integration tests against a MongoDB that supports
   multi-document transactions, on every PR that touches the backend. A timed job set to
   run every few minutes runs on schedule for 24 hours on the deployed environment,
   including through periods with no traffic.
5. The database is encrypted at rest and backed up daily, with the backup stored
   encrypted outside the database host. One restore into a scratch database has been
   done and written down (NFR-10, NFR-19).
6. Every service in use is on a free plan, and the bill is $0 (NFR-16). The standard
   epic checks pass (initiative, *Standard epic checks*; SC-1 and SC-7 apply).

## Boundaries

The boundary is the platform baseline: environments, deployment, operations. It does
not include:

- production itself (epic 10 stands it up from this epic's environment definition);
- the application's collections and tenant model (epic 2 creates the first);
- the live SMS provider (epic 6).

Touch points: the CI workflow from epic 0 gains a deploy job; Render and the MongoDB host
are configured here.

**Handoffs out:** the deploy pipeline and the database to every later epic. The
database-backed test setup goes to epic 2 first (the audit-role test and the cross-tenant
suite), then to epic 5 (the concurrency suite). Proof that timers fire goes to epics 3,
5, 6 and 10. The environment definition goes to epic 10.

## References

- parent — _bmad-output/initiative-booking-management/initiative-booking-management.md (Notes: hosting and database decisions; Standard epic checks)
- constraint — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md §5.2 NFR-5, §5.3 NFR-10, §5.5 NFR-16, NFR-18, NFR-19
- constraint — docs/adr/0006-monorepo-layout-and-test-stack.md (layout, test commands)
- constraint — docs/adr/0007-backend-code-style-and-analyzers.md
- prior epic — _bmad-output/initiative-booking-management/epic-walking-skeleton/epic-walking-skeleton.md (CI workflow, `ci-ok` required check)
- architecture — not yet written; sections for hosting, environments, data access and background jobs

## Notes

- Decision: hosting is Render for now, and the database is MongoDB (developer, 2026-10-10).
- Assumption: MongoDB runs on MongoDB Atlas's free tier [ASSUMPTION], because Render has
  no managed MongoDB. That tier has no backups of its own, so Done when 5 needs our own
  scheduled dump. The architecture step confirms both.
- Unknown: whether a timed job runs reliably on Render's free tier, where a web service
  sleeps after a period without traffic. Done when 4 settles it. If it fails, the
  architecture step chooses another way, and epics 3, 5, 6 and 10 wait on that choice.
- Waits on epic-walking-skeleton because: the solution scaffold, the test commands and
  the CI workflow come from it.
