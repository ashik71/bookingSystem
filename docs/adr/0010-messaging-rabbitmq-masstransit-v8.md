# ADR-0010: RabbitMQ through MassTransit v8, with the MongoDB outbox

- **Status:** Accepted
- **Date:** 2026-10-10
- **Deciders:** MD Ashik Ashrafe

## Context

Notifications (FR-27 to FR-29), announcement fan-out (FR-33) and chat-reply notices
are side effects of committed changes. They must be retried, must never slow a
request down, and must send at most one SMS per event. Messaging is focus area 3.
Kafka was parked on 2026-10-10.

MassTransit's current major, v9, needs a commercial licence. v8 stays available under
Apache 2.0, and its end of support is reported (unofficially) as the end of 2026.

## Options considered

### Option A: MassTransit v8.5 over RabbitMQ, with MassTransit's MongoDB outbox and inbox
**Pros:**
- Free.
- A tested transactional outbox that joins our MongoDB transaction.
- A consumer inbox for dedup.
- Retry and error queues built in.

**Cons:** pinned to a major whose support is ending.

### Option B: MassTransit v9
**Pros:** supported. **Cons:** paid, which breaks NFR-16.

### Option C: raw RabbitMQ client and our own outbox
**Pros:** no licence question. **Cons:** about a story and a half of platform code for
the agent to get right: the outbox worker, a type registry, retries.

## Decision

**Option A.**
- RabbitMQ is hosted on CloudAMQP's free plan.
- MassTransit types live only in Infrastructure.
- The unit of work commits aggregate changes and outbox messages in one MongoDB
  transaction.
- What goes over the broker, and what never does, is in spine AD-10. Audit entries and
  the booking write are never sent over the broker.
- Notifications use a high-priority endpoint (OTP, confirmations) and a low-priority
  one (reminders, fan-out). *Amended 2026-10-10:* the OTP is not sent through the
  broker at all; Identity sends it directly after commit so the plaintext is never
  stored (spine AD-4). Consumers only create Notification records; a 1-minute send job
  sends and retries them (spine AD-21).

## Consequences

**Positive:** reliable side effects with little platform code, and a real job for
RabbitMQ.

**Negative:**
- After v8's end of support we run an unsupported version until we buy v9 or move to a
  fork.
- The free broker has a cap of 1M messages a month and 20 connections.

**Neutral / follow-up:** Kafka stays parked. An Aiven free tier exists if it's ever
unparked.

## Revisit when

- v8 reaches end of support.
- The first sale funds a v9 licence.
- Message volume approaches the free cap.
