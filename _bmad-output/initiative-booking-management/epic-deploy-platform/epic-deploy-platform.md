---
tracker_id: "11"
remote: "https://github.com/ashik71/bookingSystem/issues/11"
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

Second half of the platform baseline: the backend's shared foundation, environments,
deployment and operations. When this epic is done, a merge to main deploys the API, the
Angular app and the database's schema changes (indexes, migrations) to the **demo
environment** on Render's free tier, backed by MongoDB Atlas Free and CloudAMQP, with
no manual step. The shared kernel every module builds on (unit of work, outbox and
inbox, dispatcher, error format, job runner) exists and is proven on the free tiers.
The environment is described once, so production can later come from the same
definition. Every later epic ships through it.

## Outcome

For the developer, every later epic is running in a deployed environment the moment it
merges, at $0, and the riskiest free-tier behaviours (timers on a service that sleeps,
the `/api` rewrite, transactions on Atlas Free) are proven before any feature depends on
them.

## Requirements

Platform baseline, so `covers` is empty. Each line cites its source:

- The demo environment on Render, Atlas Free, CloudAMQP Little Lemur and Grafana Cloud
  Free (spine AD-1, AD-18; ADR-0002, ADR-0008, ADR-0009, ADR-0010)
- One configured tenant, `tenantId` on every document (spine AD-5, ADR-0011 amendment)
- HTTPS only, HSTS, no secret in the repo (PRD NFR-5, transport and secrets part)
- Encryption in transit and at rest (PRD NFR-10)
- $0 hosting, every fallback free (PRD NFR-16; spine AD-18)
- Health and readiness endpoints, and a correlation ID in every request's log (PRD NFR-18)

## Done when

1. A merge to main deploys the API, the web app and the database's schema changes to the
   demo environment with no manual step. The page at the environment's HTTPS address
   shows the API healthy, and a failed deploy leaves the previous version running.
2. HTTP redirects to HTTPS, responses carry HSTS, and the deployed app loads with zero
   CSP violations. Secrets live only in the host's secret settings, never in the repo or
   an image (NFR-5, NFR-10).
3. Health and readiness endpoints answer, every request's log line carries a
   correlation ID, and traces and logs arrive in Grafana Cloud (NFR-18).
4. CI and the sandbox run database-backed integration tests against a real MongoDB
   replica set and RabbitMQ. A timed job set to run every few minutes runs on schedule
   for 24 hours on the demo environment, including through periods with no traffic.
5. Every Epic 1 proof in the spine passes, or its free fallback is in place: the `/api`
   rewrite passes POST bodies, headers, `Set-Cookie` and the client IP; the unit of
   work, the outbox and the inbox commit in one transaction; transactions work on Atlas
   Free; an XSRF token survives a restart; TLS on every hop and Atlas encryption at rest.
6. Every service in use is on a free plan, and the bill is $0 (NFR-16). The standard
   epic checks pass (initiative, *Standard epic checks*; SC-1 and SC-7 apply).

## Boundaries

The boundary is the platform baseline: the shared kernel, environments, deployment,
operations. It does not include:

- production itself (clinic launch, from this epic's environment definition);
- daily backups and the restore test (NFR-19: clinic launch, spine Deferred);
- the application's modules and collections (epic 2 creates the first);
- the live SMS provider (clinic launch).

Touch points: the CI workflow from epic 0 gains service containers and a deploy
workflow; `sandbox/run-job.sh` gains the MongoDB and RabbitMQ sidecars (built on the
host by the developer's interactive session, not by the sandbox agent, which has no
Docker access); Render, Atlas, CloudAMQP and Grafana Cloud are configured here.

**Handoffs out:** the deploy pipeline, the shared kernel and the database to every later
epic. The database-backed test setup goes to epic 2 first (the cross-tenant suite), then
to epic 5 (the concurrency suite). The job runner and the timer proof go to epics 3, 5, 6
and 10.

## References

- parent — _bmad-output/initiative-booking-management/initiative-booking-management.md (Notes: hosting and database decisions; Standard epic checks)
- architecture — _bmad-output/initiative-booking-management/architecture-slotbook/architecture-slotbook.md (final; Capability Map row "Platform slice"; Epic 1 proofs; AD-1, AD-2, AD-5, AD-7, AD-9, AD-10, AD-11, AD-12, AD-13, AD-17, AD-18, AD-19)
- constraint — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md §5.2 NFR-5, §5.3 NFR-10, §5.5 NFR-16, NFR-18
- constraint — docs/adr/0006-monorepo-layout-and-test-stack.md (layout, test commands)
- constraint — docs/adr/0007-backend-code-style-and-analyzers.md
- prior epic — _bmad-output/initiative-booking-management/epic-walking-skeleton/epic-walking-skeleton.md (CI workflow, `ci-ok` required check)

## Notes

- Decision: hosting is Render for now, and the database is MongoDB (developer, 2026-10-10).
- Decision: MongoDB runs on Atlas Free (ADR-0008). Atlas Free has no backups of its own;
  backups wait for the clinic launch (spine Deferred, 2026-10-11).
- Decision: the timer fallback, if the 24-hour proof fails, is an external cron calling
  `/internal/jobs/{job}` (spine AD-11).
- Decision: scope is the team demo, one tenant, free hosting only; the `/api` rewrite
  fallback is the API serving the Angular build from `wwwroot` (developer, 2026-10-11).
- Decision: story 2 is split in three after its plan came back at about 45 files: 2 (layout and error format), 13 (dispatcher and validators), 14 (clock, tenant, readiness). Answers to the plan's questions: the failing test action lives in the test project; one request log line without bodies or query strings; the tenant id is a UUID derived from the slug; `/health/ready` has no checks until MongoDB arrives; the frontend moves to `/api/health` in story 1; no other shared-kernel types yet; validation codes come from one map; 405 and 415 are 400 `validation_failed` (developer, 2026-10-11).
- Decision: story 6 is split in two (6a outbox #27, 6b inbox #37). 6b keeps MassTransit's inbox cleanup service off (it stops the host when MongoDB is down at start); delivered inbox rows expire through a TTL index on `Delivered` that startup creates, on the same 30-minute duplicate-detection window (2026-10-12).
- Decision: story 7 is split in two: 7 (#28) the lease and job runner, 18 (#40) the heartbeat catch-up job and the unmapped `/internal/jobs` seam. Leases are per run and released at the end (no sticky leader), carry a per-acquisition token that renew and release match on, default 30 s renewed every 10 s; the heartbeat keeps a compare-and-set watermark in `leases` and is off by default (story 10 switches it on) (2026-10-12).
- Decision: story 8 is split in three: 8 (#29) OpenAPI document checked in CI and security headers, 19 (#43) XSRF survives a restart, 20 (#44) one authentication scheme per endpoint. `GET /api/xsrf-token`; XSRF failure is 400 `xsrf_invalid`; 401/403 are `unauthenticated`/`forbidden`; the fitness test covers every routed endpoint; the API sends HSTS outside Development; the static-site CSP adds `frame-ancestors 'none'; base-uri 'self'; object-src 'none'; form-action 'self'` to AD-18's list (2026-10-12).
