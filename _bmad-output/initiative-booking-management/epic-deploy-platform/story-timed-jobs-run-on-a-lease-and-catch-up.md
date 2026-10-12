---
tracker_id: "28"
remote: "https://github.com/ashik71/bookingSystem/issues/28"
id: 7
type: story
title: "Timed jobs run on an expiring, renewed lease"
parent: epic-deploy-platform
after: [16]
hitl: false
risk: medium
---

# Timed jobs run on an expiring, renewed lease

## Description

Builds the lease-based job runner with expiring, renewed leases and a catch-up sample job that processes everything due, plus the /internal/jobs endpoint seam kept off until needed.

## Acceptance Criteria

Verify: Two hosts run the sample job and only one holds the lease; stopping the host for a minute and restarting it processes every missed tick.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-11
