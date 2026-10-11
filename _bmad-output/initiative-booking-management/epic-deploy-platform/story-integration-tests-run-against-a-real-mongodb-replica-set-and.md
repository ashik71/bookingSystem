---
tracker_id: "18"
remote: "https://github.com/ashik71/bookingSystem/issues/18"
id: 4
type: story
title: "Integration tests run against a real MongoDB replica set and RabbitMQ"
parent: epic-deploy-platform
after: [2, 0.2]
hitl: false
risk: medium
---

# Integration tests run against a real MongoDB replica set and RabbitMQ

## Description

Adds MongoDB (single-node replica set) and RabbitMQ as CI service containers, matching the sandbox sidecars already in sandbox/run-job.sh, and a test harness that reads ConnectionStrings__mongodb and ConnectionStrings__rabbitmq and gives each test class its own database.

## Acceptance Criteria

Verify: An integration test that writes a document in a multi-document transaction and publishes a message passes in the sandbox and in CI.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-19

## Notes

- The sandbox sidecars are done on the host (fae9037): mongo:8.0 --replSet rs0 as host mongo, rabbitmq:4-alpine as host rabbitmq; the sandbox gets ConnectionStrings__mongodb=mongodb://mongo:27017/?replicaSet=rs0 and ConnectionStrings__rabbitmq=amqp://guest:guest@rabbitmq:5672/. CI must use the same images and the same variable names.
