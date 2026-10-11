---
tracker_id: "29"
remote: "https://github.com/ashik71/bookingSystem/issues/29"
id: 8
type: story
title: "Cookies, CSRF and security headers are set up"
parent: epic-deploy-platform
after: [2, 5]
hitl: false
risk: medium
---

# Cookies, CSRF and security headers are set up

## Description

Persists Data Protection keys in MongoDB, adds antiforgery with Angular's XSRF header, the two authentication schemes' skeleton and the endpoint-access fitness test, the CSP and HSTS headers in render.yaml, and the OpenAPI document generated and checked in CI.

## Acceptance Criteria

Verify: An XSRF token issued before a host restart is accepted after it, and CI fails when openapi.json is stale.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-12
- architecture-slotbook/architecture-slotbook.md#ad-13
