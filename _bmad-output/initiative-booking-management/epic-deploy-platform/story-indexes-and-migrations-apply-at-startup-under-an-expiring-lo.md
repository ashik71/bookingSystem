---
tracker_id: "26"
remote: "https://github.com/ashik71/bookingSystem/issues/26"
id: 16
type: story
title: "Indexes and migrations apply at startup under an expiring lock"
parent: epic-deploy-platform
after: [5]
hitl: false
risk: medium
---

# Indexes and migrations apply at startup under an expiring lock

## Description

Story 5c of the #24 plan: one index configuration per aggregate, a blocking schema initializer (create collections, run migrations, then create indexes; about 60 s startup timeout), and the migrations runner recorded in migrations under an expiring lock document in leases, in a minimal shape that story 7's job runner extends.

## Acceptance Criteria

Verify: Indexes exist after startup and a restart is a no-op; two hosts starting together apply each migration once, in order; an expired lock does not block startup.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-9
- architecture-slotbook/architecture-slotbook.md#ad-11

## Notes

- Plan: the approved plan on #24 (issue comment), split into 5a/5b/5c; build only this part, with the review answers in the approval comment.
