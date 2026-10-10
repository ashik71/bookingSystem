# ADR-0008: MongoDB as the database

- **Status:** Accepted
- **Date:** 2026-10-10
- **Deciders:** MD Ashik Ashrafe

## Context

SlotBook needs one database for every module. The PRD's hardest requirement is
correctness under concurrency (NFR-1):
- capacity;
- the per-number booking limit;
- sequence numbers;
- reserve and release;
- booking-state transitions.

All of these have to be enforced by the store at commit time. The running cost must
be $0 before the first sale (NFR-16). Render has no managed MongoDB, so a hosted free
tier is needed.

## Options considered

### Option A: MongoDB (Atlas Free)
**Pros:**
- Multi-document transactions on a replica set.
- Conditional updates.
- The developer's strongest database.
- The reference architecture runs on it.

**Cons:**
- The free tier has no backups, a cap of 100 operations per second, and 0.5 GB.
- Isolation is snapshot isolation, so write skew needs a design rule.

### Option B: PostgreSQL
**Pros:** serializable isolation and row locks. **Cons:** Render's free Postgres expires
after 30 days. SQL locking is the developer's weak spot, so review would be weaker.

## Decision

**MongoDB**, hosted on **Atlas Free** for the demo.

Deciding reasons (confirmed by the developer, 2026-10-10):
- MongoDB is the developer's strongest database, which keeps PR review sharp under
  agentic delivery;
- Atlas has a permanent free tier, which keeps NFR-16;
- the reference architecture already proves the patterns on MongoDB.

Concurrency follows spine AD-6 and AD-7. Every invariant has one guard document, and
every write path that touches it writes that document in one transaction, with
version-checked saves and whole-handler retries.

## Consequences

**Positive:** strong review fluency, transactions, flexible documents for
multi-language content.

**Negative:**
- Write skew must be designed out, which is AD-6's job.
- The free tier needs our own backups before the clinic launch (NFR-19).
- The 100 ops/s cap has to be measured against NFR-13 in epic 5.
- Whether a custom insert-only role is possible for the audit log is unknown.

**Neutral / follow-up:** epic 1 proves transactions on Atlas Free.

## Revisit when

- The epic 5 load test misses NFR-13 even after the booking path becomes a single
  conditional update (move to a paid tier).
- Data passes 0.5 GB.
