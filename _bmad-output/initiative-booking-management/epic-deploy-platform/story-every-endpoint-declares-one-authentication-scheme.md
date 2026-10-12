---
tracker_id: "44"
remote: "https://github.com/ashik71/bookingSystem/issues/44"
id: 20
type: story
title: "Every endpoint declares one authentication scheme"
parent: epic-deploy-platform
after: [19]
hitl: false
risk: medium
---

# Every endpoint declares one authentication scheme

## Description

Story 8b of the #29 plan: the `patient` and `staff` authentication schemes as a skeleton (handlers return `NoResult`), `[AuthorizePatient]` / `[AuthorizeStaff(roles)]`, 401/403 problem details, health endpoints marked anonymous, and the endpoint-access fitness test over every routed endpoint.

## Acceptance Criteria

Verify: Each fixture violation fails the fitness test with the rule name; patient and staff probe endpoints answer 401 problem details (`unauthenticated`), and a staff cookie does not satisfy a patient endpoint.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-12
- architecture-slotbook/architecture-slotbook.md#ad-19

## Notes

- Plan: the approved plan is pasted as the first comment on this issue (extracted from #29). Build only this part.

