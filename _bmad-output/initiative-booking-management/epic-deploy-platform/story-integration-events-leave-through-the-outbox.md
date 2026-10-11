---
tracker_id: "27"
remote: "https://github.com/ashik71/bookingSystem/issues/27"
id: 6
type: story
title: "Integration events leave through the outbox"
parent: epic-deploy-platform
after: [15]
hitl: false
risk: high
---

# Integration events leave through the outbox

## Description

Story 6a of the #27 plan: MassTransit 8.5.11 packages, TenantEvent, IUnitOfWork.AddIntegrationEvent, AddSlotBookMessaging (RabbitMQ transport, MongoDB bus outbox, consumer scan), UnitOfWorkMongoDbContext so the outbox writes on the unit of work's session, publish-on-commit, RabbitMQ options, ConnectionStrings__rabbitmq in render.yaml, and the outbox-drain test helper.

## Acceptance Criteria

Verify: A test commits an aggregate and an event in one transaction, a handler that throws publishes nothing, a re-run after a conflict publishes once, and every event type round-trips through MassTransit serialization.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-10
- architecture-slotbook/architecture-slotbook.md#ad-7

## Notes

- Plan: the approved plan on #27 (issue comment), split into 6a/6b; build only this part, with the review answers in the approval comment.
