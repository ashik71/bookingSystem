---
tracker_id: "24"
remote: "https://github.com/ashik71/bookingSystem/issues/24"
id: 5
type: story
title: "Writes go through one unit of work with retries"
parent: epic-deploy-platform
after: [4]
hitl: false
risk: high
---

# Writes go through one unit of work with retries

## Description

Builds the MongoDB unit of work and repository base: version-checked saves, one transaction with majority and snapshot, the AD-7 retry rules (whole handler, commit-only on unknown result, 3 s budget), the tenant filter, deterministic ids, indexes at startup and the migrations runner with an expiring lock, shown on a sample aggregate.

## Acceptance Criteria

Verify: A concurrency test with 50 parallel writers to one guard document ends with exact counts and zero 409s, and a first-use race on a keyed guard creates one document.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-6
- architecture-slotbook/architecture-slotbook.md#ad-7
- architecture-slotbook/architecture-slotbook.md#ad-9
- architecture-slotbook/architecture-slotbook.md#ad-5

## Notes

- From #13: this story creates the repository base, so the AD-5 rule "no unfiltered collection; every aggregate is tenant-owned or [GlobalAggregate]" now has code to check. Add it to SlotBook.ArchitectureTests with a fixture violation, following the existing rules (rule name in the failure message, a "fails on a violation" test).
