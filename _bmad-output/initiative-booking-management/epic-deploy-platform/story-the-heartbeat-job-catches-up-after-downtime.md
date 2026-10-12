---
tracker_id: "40"
remote: "https://github.com/ashik71/bookingSystem/issues/40"
id: 18
type: story
title: "The heartbeat job catches up after downtime"
parent: epic-deploy-platform
after: [7]
hitl: false
risk: low
---

# The heartbeat job catches up after downtime

## Description

Story 7b of the #28 plan: the catch-up sample `HeartbeatJob` on the 7a job runner (epoch-aligned ticks, a compare-and-set watermark in a `heartbeat:state` document in `leases`, capped `recentTicks`), its options (off by default), and the `/internal/jobs/{job}` seam built but not mapped in `Program.cs`.

## Acceptance Criteria

Verify: Two hosts record each heartbeat tick once; stopping the host and restarting it (and a seeded 60 s gap) processes every missed tick; the seam runs a job under its lease in a test host and the real app has no `/internal/jobs` route.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-11

## Notes

- Plan: the approved plan on #28 (issue comment), split into 7a/7b; build only this part (7b), with the answers in the approval comment on #28.

