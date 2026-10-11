---
tracker_id: "25"
remote: "https://github.com/ashik71/bookingSystem/issues/25"
id: 15
type: story
title: "Conflicting writes re-run the handler within 3 s"
parent: epic-deploy-platform
after: [5]
hitl: false
risk: high
---

# Conflicting writes re-run the handler within 3 s

## Description

Story 5b of the #24 plan: the ICommandExecution seam in the dispatcher (fresh scope per attempt, nested public commands join the outer attempt), the write-error classifier, the whole-handler retry with jittered backoff and the commit-only retry on an unknown commit result, all within a 3 s TimeProvider budget, concurrency_conflict mapped to 409, and a test-only keyed guard sample.

## Acceptance Criteria

Verify: A concurrency test with 50 parallel writers to one guard document ends with exact counts and zero 409s, and a first-use race on a keyed guard creates one document.

## References

- parent — _bmad-output/initiative-booking-management/epic-deploy-platform/epic-deploy-platform.md
- architecture-slotbook/architecture-slotbook.md#ad-6
- architecture-slotbook/architecture-slotbook.md#ad-7
- architecture-slotbook/architecture-slotbook.md#ad-8

## Notes

- Plan: the approved plan on #24 (issue comment), split into 5a/5b/5c; build only this part, with the review answers in the approval comment.
