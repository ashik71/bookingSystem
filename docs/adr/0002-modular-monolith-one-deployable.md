# ADR-0002: A modular monolith on one free web service

- **Status:** Accepted
- **Date:** 2026-10-10
- **Deciders:** MD Ashik Ashrafe

## Context

SlotBook has to run at $0 (NFR-16). ADR-0009 puts it on Render. Render's free plan
offers only web services and static sites. Background workers, cron jobs and private
services are paid. A free web service sleeps after 15 minutes without HTTP traffic,
and a workspace gets 750 free instance-hours a month. That is enough to keep **one**
service running all the time (about 744 h), not two.

Several kinds of work have to happen off the request path:
- the outbox relay;
- notification consumers;
- scheduled publication (within 60 s, FR-8);
- lapse;
- reminders;
- reconciliation.

The focus areas still want real bounded contexts (DDD) and real messaging.

## Options considered

### Option A: modular monolith, one process
One .NET process hosts the API, the outbox delivery, the message consumers and the
timers as hosted services. Modules are bounded contexts that talk only through
contracts and events. Angular is a separate free static site.
**Pros:** fits the free tier exactly; transactions stay local; one deploy.
**Cons:** one process is one blast radius; heavy fan-out shares a CPU with booking.

### Option B: API plus a separate worker
**Pros:** background load is isolated. **Cons:** the worker would have to be a second
web service, and 2 × 744 h goes over the 750 free hours.

### Option C: microservices per bounded context
**Pros:** independent deploys. **Cons:** cost, and the booking-limit invariant would
be split across services.

### Option D: plain layered monolith, no modules
**Pros:** simplest. **Cons:** bounded contexts and DDD lose their job.

## Decision

**Option A.** Exactly one backend process on one Render free web service, plus one
static site. The deciding factor is the free tier: it is the only shape that stays at
$0 without tricks. Modules talk only through contracts, so moving one out later is a
hosting change, not a rewrite. Spine AD-1 and AD-3 carry the rules.

## Consequences

**Positive:** one deploy, local transactions, the simplest operations, and a real
module structure.

**Negative:**
- A free external ping must keep the service awake.
- In-memory rate limits and caches are valid only while there is one instance.

**Neutral / follow-up:** epic 1 proves two things:
- that timers keep firing through quiet hours;
- that the static site's `/api` rewrite passes cookies through.

## Revisit when

- Announcement fan-out or chat slows booking latency on one free instance (move
  notifications to a paid worker, about $7 a month).
- A second tenant's outage must not affect the first.
- Render stops tolerating the keep-awake ping.
