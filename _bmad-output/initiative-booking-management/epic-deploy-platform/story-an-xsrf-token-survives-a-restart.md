---
tracker_id: "43"
remote: "https://github.com/ashik71/bookingSystem/issues/43"
id: 19
type: story
title: "An XSRF token survives a restart"
parent: epic-deploy-platform
after: [8]
hitl: false
risk: medium
---

# An XSRF token survives a restart

## Description

Story 8a of the #29 plan: Data Protection keys persist in MongoDB (`data_protection_keys`, application name `SlotBook`), antiforgery validates Angular's `X-XSRF-TOKEN` header on every unsafe method, `GET /api/xsrf-token` issues the token, and the Angular initializer fetches it once.

## Acceptance Criteria

Verify: An XSRF token issued before a host restart is accepted after it; an unsafe request without the header is refused with 400 `xsrf_invalid`.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-12
- architecture-slotbook/architecture-slotbook.md#ad-13

## Notes

- Plan: the approved plan is pasted as the first comment on this issue (extracted from #29). Build only this part.

