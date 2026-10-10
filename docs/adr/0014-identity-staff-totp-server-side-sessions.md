# ADR-0014: Identity: staff password + TOTP, server-side sessions for both audiences

- **Status:** Accepted
- **Date:** 2026-10-10
- **Deciders:** MD Ashik Ashrafe

## Context

The PRD left the staff sign-in method open (Q4). It requires MFA for every staff
account (FR-38), with disabled accounts locked out within 1 minute. Patients sign in
with an SMS OTP and have 30-day sessions, and those sessions must be revocable
everywhere (FR-5, FR-44). Patient and staff sessions are distinct audiences (NFR-4).
The cost must be $0.

## Options considered

### Staff sign-in
- **Own accounts: password + mandatory TOTP authenticator + recovery codes** (chosen).
- An outside identity provider's free tier: one more set of changing limits, plus claim
  mapping, which is too much for 2–3 staff.
- SMS as the second factor: uses the quota, and SIM swap makes it weak.
- Passkeys were offered as a demo extra and declined for now.

### Sessions
- **Server-side opaque sessions** (chosen).
- JWT access and refresh tokens: revocation needs a deny list, and tokens kept in the
  browser are exposed to XSS.

## Decision

**Staff sign-in** uses a password plus a mandatory TOTP authenticator, built on
ASP.NET Core Identity primitives behind our own MongoDB store.

**Sessions, both audiences:** a random 256-bit token in a `Secure; HttpOnly;
SameSite=Strict` cookie, stored hashed with a TTL. Revoking a session deletes it, and
lookups are cached in process for at most 30 s.

- Two authentication schemes, `patient` and `staff`. Every endpoint declares exactly
  one, or is explicitly anonymous.
- CSRF protection comes from Angular's XSRF header plus antiforgery.
- The demo sends patient OTPs through the development channel.
- The rules are in spine AD-12.

## Consequences

**Positive:**
- Instant revocation.
- No outside identity dependency.
- The audiences are separated by construction.

**Negative:**
- We own credential storage, lockout and MFA recovery.
- The in-memory caches and rate limits assume one instance (ADR-0002).

**Neutral / follow-up:** staff lockout, password minimum and session length are
`[ASSUMPTION]` values in the threat model, to confirm before epic 2.

## Revisit when

- A clinic wants SSO with its own Microsoft or Google accounts (add an outside provider
  alongside).
- A second instance appears (move the session cache and rate limits to a shared store).
