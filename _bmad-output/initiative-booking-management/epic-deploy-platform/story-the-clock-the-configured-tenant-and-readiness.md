---
tracker_id: "15"
remote: "https://github.com/ashik71/bookingSystem/issues/15"
id: 14
type: story
title: "The clock, the configured tenant and readiness"
parent: epic-deploy-platform
after: [2]
hitl: false
risk: low
---

# The clock, the configured tenant and readiness

## Description

Adds IClock on TimeProvider, the Tenancy:Tenant options validated on start with a UUID derived from the slug as ITenantContext, and the /api/health and /health/ready endpoints.

## Acceptance Criteria

Verify: Startup fails when Tenancy:Tenant is missing, ITenantContext returns the configured tenant, and /health, /api/health and /health/ready answer 200.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-5
- architecture-slotbook/architecture-slotbook.md#ad-14
