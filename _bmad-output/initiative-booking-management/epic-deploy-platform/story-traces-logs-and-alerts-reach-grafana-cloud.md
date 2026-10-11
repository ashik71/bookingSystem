---
tracker_id: "32"
remote: "https://github.com/ashik71/bookingSystem/issues/32"
id: 11
type: story
title: "Traces, logs and alerts reach Grafana Cloud"
parent: epic-deploy-platform
after: [9]
hitl: true
risk: low
---

# Traces, logs and alerts reach Grafana Cloud

## Description

Exports OpenTelemetry traces, metrics and logs through ServiceDefaults to Grafana Cloud from the demo environment, with the no-personal-data scan, the MongoDB operations counter, the Synthetics check on /health and the AD-18 alert rules.

## Acceptance Criteria

Verify: A request on the demo environment shows as a trace in Grafana Cloud, and stopping the service fires the Synthetics alert email.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-17
- architecture-slotbook/architecture-slotbook.md#ad-18
