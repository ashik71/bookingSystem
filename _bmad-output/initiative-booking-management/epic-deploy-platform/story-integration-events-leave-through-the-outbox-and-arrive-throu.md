---
tracker_id: "27"
remote: "https://github.com/ashik71/bookingSystem/issues/27"
id: 6
type: story
title: "Integration events leave through the outbox and arrive through the inbox"
parent: epic-deploy-platform
after: [15]
hitl: false
risk: high
---

# Integration events leave through the outbox and arrive through the inbox

## Description

Adds AddSlotBookMessaging with MassTransit 8.5.11, the MongoDB bus outbox on the unit of work's session, the inbox in consumers sharing the consume context's session, the consumer retry rules and the event serialization round-trip test.

## Acceptance Criteria

Verify: A test commits an aggregate and an event in one transaction, the consumer runs once, and a crash after the handler does not run it twice.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-10
