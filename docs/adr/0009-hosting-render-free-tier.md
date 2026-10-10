# ADR-0009: Render free tier for hosting, for now

- **Status:** Accepted
- **Date:** 2026-10-10
- **Deciders:** MD Ashik Ashrafe

## Context

Azure is focus area 1, but SlotBook has to cost nothing until it earns money (NFR-16,
SM-8). The near-term goal is a demo for the developer's team, with the clinic launch
parked.

## Options considered

### Option A: Render free tier
One free web service for the API and one free static site for Angular. Deploys from
GitHub.
**Pros:** $0; simple. **Cons:** the service sleeps after 15 minutes idle; there are no
free workers or cron jobs; 750 instance-hours a month.

### Option B: Azure
**Pros:** focus area 1; a full platform. **Cons:** costs money, which the project can't
spend before revenue.

## Decision

**Render's free tier, for now.** The developer's deciding reason: Azure costs money and
SlotBook needs free services. A later move to Azure would be its own epic. Environments
are declared in a Render Blueprint (`render.yaml`, spine AD-18), so production for the
clinic comes from the same definition.

## Consequences

**Positive:** $0 hosting; infrastructure as code from day one.

**Negative:**
- One always-on service at most (ADR-0002).
- A free external ping keeps it awake.
- Timers must catch up after any sleep (spine AD-11).

**Neutral / follow-up:** Azure, focus area 1, is parked until the developer schedules
the move.

## Revisit when

- The clinic launch is unparked and needs an uptime commitment (Q5).
- A paid plan becomes acceptable.
- Render changes its free tier.
