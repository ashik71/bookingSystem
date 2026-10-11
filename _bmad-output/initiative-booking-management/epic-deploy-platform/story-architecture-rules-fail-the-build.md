---
tracker_id: "13"
remote: "https://github.com/ashik71/bookingSystem/issues/13"
id: 3
type: story
title: "Architecture rules fail the build"
parent: epic-deploy-platform
after: [13, 14]
hitl: false
risk: low
---

# Architecture rules fail the build

## Description

Adds the ArchitectureTests project (ArchUnitNET) and BannedApiAnalyzers with the AD-19 inventory rules that exist at this point: layer and reference direction, no public setters, no tenant parameters, banned time APIs and Guid.NewGuid.

## Acceptance Criteria

Verify: A deliberate violation of each rule fails the build or the architecture tests with the rule's name.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-19
