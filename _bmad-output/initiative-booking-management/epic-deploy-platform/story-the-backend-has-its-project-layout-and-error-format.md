---
tracker_id: "12"
remote: "https://github.com/ashik71/bookingSystem/issues/12"
id: 2
type: story
title: "The backend has its project layout and error format"
parent: epic-deploy-platform
after: [0.1, 0.3]
hitl: false
risk: medium
---

# The backend has its project layout and error format

## Description

Lays out the four SharedKernel projects, ServiceDefaults, the integration-test and SharedKernel test projects per AD-2 and AD-19, with RFC 9457 problem-details errors (DomainException, CommonErrors) and the W3C trace id on every log line.

## Acceptance Criteria

Verify: dotnet test passes, and a request to a deliberately failing action returns an RFC 9457 body with a code and traceId and a log line with the same trace id.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-2
- architecture-slotbook/architecture-slotbook.md#ad-13
- architecture-slotbook/architecture-slotbook.md#ad-17
