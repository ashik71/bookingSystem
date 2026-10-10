# ADR-0011: Tenancy: shared collections, tenant from the session, never from the route

- **Status:** Accepted
- **Date:** 2026-10-10
- **Deciders:** MD Ashik Ashrafe

## Context

SlotBook is tenant-aware from day one. NFR-3 and SC-2 require that a session of
tenant A can never read or change tenant B's data, proven by a cross-tenant suite. In
SlotBook a session belongs to exactly one tenant: a patient is a number inside one
clinic, and a staff account belongs to one clinic (FR-38). Atlas Free allows 500
collections in total.

## Options considered

- **Shared collections with `tenantId` on every document** (chosen).
- **A database per tenant:** strong isolation, but about 30 tenants on the free
  collection cap, and index setup per tenant.
- **A collection per tenant:** hits the cap fastest.

For locating the tenant in a request, three options were weighed:
- in every route, with a check that the route matches the session;
- from the host name, which is unreliable behind the static-site proxy;
- **from the session for signed-in calls and from an `X-Tenant` header for anonymous
  calls** (chosen).

## Decision

**One database with shared collections. No API route carries the tenant.**

- Signed-in calls take the tenant only from the server-side session record.
- Anonymous tenant-scoped calls read the `X-Tenant` header, which Angular sets from its
  own host name. Signing in binds the new session to that tenant.
- The repository applies the tenant filter, and handlers never write one.
- Unique indexes begin with `tenantId`.

Fitness tests enforce the route and header rules. The developer chose to remove the
tenant from routes on 2026-10-10. Spine AD-5 carries the rules.

## Consequences

**Positive:** a signed-in request can't even name another tenant; one database; simple
indexes.

**Negative:**
- Isolation lives in code, so the central filter and the cross-tenant suite are
  load-bearing.
- A forged `X-Tenant` header only reaches public data.

**Neutral / follow-up:** each tenant's data can later be copied into its own database
by `tenantId`.

## Revisit when

- A tenant needs contractual isolation (its own database, region or key).
- One identity must act across tenants, such as a platform operator or a practitioner
  at two clinics.
