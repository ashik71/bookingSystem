# ADR-0012: Concurrency: one invariant, one guard document

- **Status:** Accepted
- **Date:** 2026-10-10
- **Deciders:** MD Ashik Ashrafe

## Context

NFR-1 requires the store to enforce these at commit time, never by a read-then-write:
- capacity (free / reserved / taken);
- the per-number booking limit;
- sequence numbers;
- reserve and release;
- booking-state transitions.

Each has a named concurrency test. MongoDB transactions read from a snapshot. A write
to a document that changed after the snapshot aborts with a write conflict, so
lost updates on one document are caught. **Write skew** is not caught: two
transactions read the same state, write *different* documents, and together break a
rule.

## Options considered

### Option A: aggregates, version-checked saves, one transaction, whole-handler retry
The aggregates enforce the rules. The repository saves with `version` in the filter.
Everything commits in one multi-document transaction. Conflicts re-run the handler.

### Option B: bookings embedded in the Slot document
Single-document atomicity for capacity and check-in. But the limit and the ticket code
still span documents, slot documents grow, and "my bookings" queries get awkward.

### Option C: conditional `$inc` updates, with the rules written as query filters
One round trip. But the rules leave the domain.

## Decision

**Option A, governed by one rule: every write path that touches an invariant writes
that invariant's guard document in the same transaction.** Write skew then becomes a
write conflict, which MongoDB catches and the handler retries.

| Invariant | Guard document |
| --- | --- |
| Capacity, sequence numbers, slot status | Slot |
| Booking limit | PatientLedger |
| Booking state | Booking |
| Daily OTP budget | OtpBudget |
| Publication | Week, plus its Slots |

- Ticket-code uniqueness and "one number, one place per slot" are partial unique
  indexes.
- The details are in spine AD-6, AD-7 and AD-8. A new invariant gets a row in the
  table, in an ADR, before any code.

## Consequences

**Positive:**
- One mechanism for every invariant, which the agent can't forget, because the version
  check lives in the repository.
- The rules are unit-testable in the domain.

**Negative:**
- A patient booking costs about 7 operations, so Atlas Free's 100 ops/s caps it at
  about 14 bookings a second.
- Retries on a hot slot add load.

**Neutral / follow-up:** the NFR-1 suite runs against a real replica set (spine AD-19).

## Revisit when

The epic 5 load test misses NFR-13. First, change only the booking path to a single
conditional update. Then consider a paid tier. The guard-document rule stays either
way.
