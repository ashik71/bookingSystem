---
tracker_id: "14"
remote: "https://github.com/ashik71/bookingSystem/issues/14"
id: 13
type: story
title: "Commands and queries go through the dispatcher and its validators"
parent: epic-deploy-platform
after: [2]
hitl: false
risk: medium
---

# Commands and queries go through the dispatcher and its validators

## Description

Adds ICommand and IQuery, our own dispatcher with assembly scanning, FluentValidation validators run first with a code map, Result, and the 400 and 422 mappings.

## Acceptance Criteria

Verify: A command with an invalid field returns 400 validation_failed with errors.{field} codes and its handler never runs; a Result.Refused returns 422 with its code.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-4
- architecture-slotbook/architecture-slotbook.md#ad-13
