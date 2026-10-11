---
tracker_id: "21"
remote: "https://github.com/ashik71/bookingSystem/issues/21"
id: 1
type: story
title: "The health page is live on the demo environment"
parent: epic-deploy-platform
after: [0.1]
hitl: true
risk: high
---

# The health page is live on the demo environment

## Description

Adds the API's Dockerfile (runtime image with tzdata and ICU for IANA zones) and render.yaml (a Render Blueprint in the Singapore region: Docker web service plus static site that rewrites /api/* to it, free plans, autoDeploy off, healthCheckPath /health/ready), a MongoDB readiness check on /health/ready, and moves the Angular page to /api/health; the developer then creates the services from the Blueprint and enters the secrets.

## Acceptance Criteria

Verify: After the developer applies the Blueprint, the page at the static site's HTTPS address shows the API healthy through the /api rewrite, and /health/ready reports MongoDB connected.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-18
- architecture-slotbook/architecture-slotbook.md#ad-17

## Notes

- Secrets and tenant values are never in render.yaml: ConnectionStrings__mongodb and the Tenancy__Tenant__* variables are declared with sync: false so the developer enters them in Render. ASPNETCORE_ENVIRONMENT=Demo.
- Atlas M0 is in AWS Singapore (ap-southeast-1); Render services use region singapore.
- The /api rewrite proof (POST bodies, Set-Cookie, client IP) is story 10's; if it fails, the free fallback is the API serving the Angular build from wwwroot.
