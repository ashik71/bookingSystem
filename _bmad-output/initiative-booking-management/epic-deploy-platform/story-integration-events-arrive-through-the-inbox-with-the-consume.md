---
tracker_id: "37"
remote: "https://github.com/ashik71/bookingSystem/issues/37"
id: 17
type: story
title: "Integration events arrive through the inbox, with the consumer retry rules"
parent: epic-deploy-platform
after: [6]
hitl: false
risk: high
---

# Integration events arrive through the inbox, with the consumer retry rules

## Description

Story 6b of the #27 plan: joined mode in the unit of work and MongoCommandExecution for consume scopes, SlotBookConsumerDefinition (explicit endpoint name, an inner jittered retry for write conflicts, the 1/5/15/30 s interval retry that ignores deterministic failures, then UseMongoDbOutbox), consumer logs, and architecture rules for events and consumer definitions.

## Acceptance Criteria

Verify: A test commits an aggregate and an event in one transaction, the consumer runs once, and a crash after the handler does not run it twice.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-10
- architecture-slotbook/architecture-slotbook.md#ad-7

## Notes

- Plan: the approved plan on #27 (issue comment), split into 6a/6b; build only this part (6b), with the review answers in the approval comment.
