---
tracker_id: "31"
remote: "https://github.com/ashik71/bookingSystem/issues/31"
id: 10
type: story
title: "The free-tier proofs pass on the demo environment"
parent: epic-deploy-platform
after: [6, 7, 8, 9, 17, 18, 19, 20]
hitl: true
risk: high
---

# The free-tier proofs pass on the demo environment

## Description

Adds Aspire's AppHost for local runs and runs the Epic 1 proofs on the deployed demo: the /api rewrite with POST, headers, Set-Cookie and client IP, the 24-hour timer through quiet hours with the Synthetics ping, transactions on Atlas Free, zero CSP violations, TLS and encryption at rest; each failing proof applies its free fallback.

## Acceptance Criteria

Verify: Each proof in the spine's Epic 1 proofs table is recorded as passed, or its free fallback is merged and the affected ADs are listed in a PR comment.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#epic-1-proofs
