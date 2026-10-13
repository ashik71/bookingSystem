---
tracker_id: "32"
remote: "https://github.com/ashik71/bookingSystem/issues/32"
id: 11
type: story
title: "Traces, metrics and logs reach Grafana Cloud without personal data"
parent: epic-deploy-platform
after: [9]
hitl: true
risk: low
---

# Traces, metrics and logs reach Grafana Cloud without personal data

## Description

Story 11a of the #32 plan: exports OpenTelemetry traces, metrics and logs through ServiceDefaults to Grafana Cloud from the demo environment whenever the `OTEL_EXPORTER_OTLP_*` variables are set, with a scrubbing processor and the no-personal-data scan. The MongoDB operations counter, the Synthetics check on /health and the AD-18 alert rules moved to story 21 (#49).

## Acceptance Criteria

Verify: A request on the demo environment shows as a trace in Grafana Cloud with its logs linked by trace id; the scan finds no sentinel personal data in any span, metric tag or log.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-17
- architecture-slotbook/architecture-slotbook.md#ad-18
