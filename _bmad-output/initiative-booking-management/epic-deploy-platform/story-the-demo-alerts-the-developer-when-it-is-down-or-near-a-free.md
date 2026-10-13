---
tracker_id: "49"
remote: "https://github.com/ashik71/bookingSystem/issues/49"
id: 21
type: story
title: "The demo alerts the developer when it is down or near a free-tier limit"
parent: epic-deploy-platform
after: [11]
hitl: true
risk: low
---

# The demo alerts the developer when it is down or near a free-tier limit

## Description

Story 11b of the #32 plan: the MongoDB operations counter (`slotbook.mongodb.operations`, tagged only with the command name), the outbox backlog gauge (`slotbook.outbox.pending`), and the runbook `docs/ops/grafana-cloud.md` (Synthetics check on `/health`, email contact point, AD-18 alert rules backed by data, Atlas storage alert in Atlas, the local Aspire dashboard by `docker run`).

## Acceptance Criteria

Verify: Stopping the service fires the Synthetics alert email; the MongoDB operations counter is visible in Grafana and the >70/s rule exists; each rule in the runbook is listed with its query.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-17
- architecture-slotbook/architecture-slotbook.md#ad-18

## Notes

- Plan: the approved plan is pasted as the first comment on this issue (extracted from #32). Build only this part.
