---
tracker_id: "30"
remote: "https://github.com/ashik71/bookingSystem/issues/30"
id: 9
type: story
title: "A merge to main deploys the demo environment"
parent: epic-deploy-platform
after: [1]
hitl: true
risk: medium
---

# A merge to main deploys the demo environment

## Description

Adds deploy.yml: after ci succeeds on main it calls the Render deploy hooks by changed path, deploys the API first, waits until /health/ready reports the new commit SHA, then deploys the static site, with Render auto-deploy off.

## Acceptance Criteria

Verify: Merging a change deploys it with no manual step, and a deliberately broken readiness check leaves the previous version running.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-18
