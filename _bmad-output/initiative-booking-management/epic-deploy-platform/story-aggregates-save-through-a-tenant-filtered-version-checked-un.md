---
tracker_id: "24"
remote: "https://github.com/ashik71/bookingSystem/issues/24"
id: 5
type: story
title: "Aggregates save through a tenant-filtered, version-checked unit of work"
parent: epic-deploy-platform
after: [4]
hitl: false
risk: high
---

# Aggregates save through a tenant-filtered, version-checked unit of work

## Description

Story 5a of the #24 plan: the aggregate base, TenantAggregateRoot and [GlobalAggregate], DeterministicId.ForGuard, BSON conventions, the Mongo:Database setting, TenantCollection (the tenant filter and stamp), the write repository base, and the unit of work that commits every aggregate loaded through a write repository with a version-checked save in one transaction (majority, snapshot); plus the AD-5 architecture rules for unfiltered collections and aggregate tenancy.

## Acceptance Criteria

Verify: A handler's two writes commit together or not at all, a stale version is refused, another tenant's document is invisible and never overwritten, and each AD-5 fixture fails with its rule name.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-5
- architecture-slotbook/architecture-slotbook.md#ad-7
- architecture-slotbook/architecture-slotbook.md#ad-9

## Notes

- Plan: the approved plan on #24 (issue comment), split into 5a/5b/5c; build only this part, with the review answers in the approval comment.
