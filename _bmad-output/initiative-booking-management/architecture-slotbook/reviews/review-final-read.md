# Review — the final read of the spine (session 011)

**Content:** `architecture-slotbook.md` (AD-1–AD-22, Epic 1 proofs), after the rubric
rewrite `67e87ae`. A document that defines behavior.
**Lenses:** adversarial (29), edge-case-hunter (40), structure (15, 3 of them PRESERVE),
prose (36 + 6 minor). Run in pairs (session 010 lesson).
**Triage:** by Claude, checked against the spine text. Overlap between lenses is noted;
overlap means the finding is strong.

**Outcome (2026-10-11):** the developer narrowed the scope to the team demo, **single
tenant, free hosting only**. Applied: all of A; B1–B8, B10–B12 (B9 is moot with one
tenant); from C, the free fallback, the deploy SHA, the X-Tenant items (moot), TTL lag,
lock expiry, the Atlas storage alert, erasure sessions, XSRF re-issue and session-cache
eviction. Not applied (out of demo scope or low value): the rest of C, the structure
lens, and most of the prose lens. The spine is `final`.

---

## Triage

### A. Correctness: an agent would build these bugs (fix before `final`)

| # | Finding | ADs | Lenses |
|---|---|---|---|
| A1 | `UnknownTransactionCommitResult` re-runs the whole handler, but the commit may already have landed. A re-run double-applies (second OTP challenge, a false refusal on a cancel that succeeded) and stages events with new `EventId`s that the inbox can't dedup. **Fix:** on this label, retry `commitTransaction` only; re-run the whole handler only on `TransientTransactionError`, `WriteConflict`, a version mismatch or a named `DuplicateKey`. State whether the driver's `WithTransaction` helper is used (its own 120 s loop must not nest inside the 3 s budget) | AD-7 | adv, edge |
| A2 | **A `DuplicateKey` inside a transaction aborts the whole transaction.** Three rules treat it as a local event. The owner-alert "duplicate = already raised" (AD-20) kills the caller's transaction on the second alert of the day. `EnsurePatient` and week get-or-create "re-run on `DuplicateKey`" (AD-8, AD-14) read as local retries on an aborted session. Notification creation on redelivery (AD-21) aborts its batch every time. "Same number, same slot" (AD-8) has no mapping at all. **Fix:** alerts and notifications use an upsert with `$setOnInsert`; named `DuplicateKey`s join AD-7's whole-handler re-run list; the same-slot index re-runs, then refuses 422 `already_booked`; never catch-and-continue inside an open transaction | AD-7, AD-8, AD-14, AD-20, AD-21 | adv, edge, Claude |
| A3 | **First-writer race on guard documents.** PatientLedger, PractitionerWeek, OtpBudget (and StaffRoster) are "one per …" with no unique key or deterministic `_id`. Two first writers each insert their own guard, so the guard guards nothing: a new patient's two parallel bookings both count 1. **Fix:** deterministic `_id` or a unique index per keyed guard; get-or-create with whole-handler re-run; a first-use race case in each row's concurrency test | AD-6 | adv |
| A4 | The consumer's unit of work and MassTransit's MongoDB **inbox** are not required to share one session and transaction, and the Epic 1 proof covers only the bus outbox. A crash between them loses or doubles a reaction (erasure included). The fan-out consumer's "batched transactions" can't sit inside one inbox transaction at all. **Fix:** state the consumer-side enlistment; add an Epic 1 proof row; make fan-out resumable by idempotent upserts, not by the inbox | AD-10, AD-21, Epic 1 proofs | adv, edge, Claude |
| A5 | Send job: the 2-minute "crashed send" rule isn't tied to claim batch size, lease length or provider timeout. A slow batch is marked `failed` while still sending; the `sent` write then version-conflicts and AD-7 re-runs a handler that already sent. The job also never checks that the booking is still `confirmed`, so reminders go out for cancelled bookings. **Fix:** bound the claim batch (or claim one at a time); provider timeout well under 2 min; the stale sweep runs only under the lease; the final write never triggers a re-send; skip and cancel the Notification when its kind no longer applies | AD-21 | adv, edge, Claude |
| A6 | The reminder selection lacks `reminderStagedAt == null`, so every booking in the window is re-staged every minute: outbox, broker and a Booking version bump that conflicts with cancels | AD-11 | adv, edge |
| A7 | Per-number and per-day limits "counted from stored documents" are a read-count-then-insert with no guard (the write skew AD-6 forbids). FR-3 counts sit on OtpChallenge documents whose TTL can be shorter than the window, which silently resets the limit. **Decision needed** (see below) | AD-22, AD-6, AD-11 | adv, edge |

### B. Contradictions between rules (an agent stops, or a reviewer can reject either way)

| # | Finding | ADs |
|---|---|---|
| B1 | Slot UTC is "computed at creation and never edited", but "edit draft slot times" is a write path. Also: is the tenant zone or week start day mutable after the first week? | AD-14 vs AD-6 |
| B2 | "Global writes are exactly…" omits the tenant-settings save (FR-39). "Global reads are exactly…" omits the platform's own reads (leases, migrations, Data Protection keys, outbox) | AD-5 |
| B3 | "Never two instances of one aggregate type" vs fan-out batches of Notifications and "delete that account's sessions in the same transaction" | AD-7 vs AD-21, AD-12 |
| B4 | Who owns `sessions` and `known_devices`: Identity (module table) or the SharedKernel session store (AD-5)? | AD-3, AD-5 |
| B5 | The `seed-tenant` CLI writes Tenancy and Identity, but the host holds no module logic and the public-command list is closed at `EnsurePatientCommand` | AD-3, AD-5 |
| B6 | "Inside its own transaction" (audit writer) means the caller's transaction; "its own unit of work" two lines later means a separate one | AD-20 |
| B7 | Erasure cancels through the normal Cancel use case after the tombstone is set, and the tombstone refuses "every booking path" with `not_found`. Does Cancel count as a booking path? If so, erasure never completes | AD-3, AD-6 |
| B8 | `count_drift` dedup key is "slot id + local date", but reconciliation also covers ledgers. And does reconciliation only alert, or also correct? | AD-11, AD-20 |
| B9 | `Fault<PatientErased>` doesn't derive from `TenantEvent` and doesn't leave through our outbox; the consumer pipeline sets the tenant from the event's `TenantId` | AD-5, AD-10 |
| B10 | Copy last week's transaction exception names PractitionerWeek and Slots, not the Week that AD-6 says every draft-slot add must write | AD-6, AD-7 |
| B11 | "Owner-side caches are listed in their AD", then AD-3 allows an unlisted Tenancy settings cache | AD-3 |
| B12 | Derived `_id`s (Notification, OwnerAlert) vs "Ids are Guid v7". Name one helper (for example, a name-based UUID) as the only exception | AD-9, AD-8, AD-20 |

### C. Gaps to state (smaller, but each one is a story-level argument waiting to happen)

- **Epic 1 fallback:** the custom-domain fallback breaks Angular's XSRF header (only sent same-origin) and needs credentialed CORS. Add AD-12 and AD-22 to "ADs that change" (adv, Claude).
- **Epic 1 fallback:** "a paid tier" for transactions contradicts NFR-16 and isn't an Open Question (adv).
- **AD-18 deploy:** `/health/ready` exposes no version, so `deploy.yml` can't tell old from new. Expose the build SHA (adv, edge).
- **AD-5 `X-Tenant`:** missing, unknown or disabled tenant responses are undefined. So are a session for tenant A with a header naming B, and a session whose tenant was later disabled. There is also no host → tenant topology for the demo (one Render host means one tenant) (adv, edge).
- **AD-11 / AD-12 TTL lag:** the TTL monitor runs about every 60 s, so session lookup and OTP verify must check `expiresAt` themselves (edge).
- **AD-9, AD-11 locks:** the lease and migration lock need an expiry, an owner and takeover rules. The job runner must isolate one tenant's failure from the rest (edge).
- **AD-18 free-tier quotas not alerted:** Atlas M0's 512 MB storage (audit is append-only, retention waits on Q3), CloudAMQP monthly messages and connections, Render build minutes. Check against current vendor terms (adv).
- **AD-22 client IP:** the Render web service's own URL is public. A direct call with a forged `X-Forwarded-For` bypasses per-IP limits unless the trusted hop is pinned (edge).
- **AD-22 / AD-6 OtpBudget hot document:** every OTP issue in a tenant writes one document, which at publication time is the same contention as a hot Slot, against M0's 100 ops/s. "Timing class" in AD-12 isn't defined in a testable way (adv).
- **AD-22:** the public ticket check and the fraud report have no per-IP default in the table. State the ticket-code keyspace (adv).
- **AD-7 / NFR-1:** zero-409 is asserted on a local replica set. On Atlas, an election longer than 3 s gives 409s. Say so (edge).
- **AD-6:** the Booking state machine (cancel from `checked_in`? from `lapsed`?) and its ledger and slot effects aren't tabled (edge).
- **AD-3 erasure:** state explicitly that Identity deletes the patient's sessions and known devices and evicts the cache (edge).
- **AD-12:** re-issue the XSRF token on sign-in and sign-out. Evict the session cache on every session update, including fresh-OTP (edge).
- **AD-12 lockout:** an attacker can lock out a sole owner (edge). Threat-model trade-off; confirm or add a recovery path.
- **Minor (edge):** DST gaps and overlaps, an old ticket prefix at the door, bulk actions interrupted mid-loop, scheduled publication by a since-disabled owner, the `IRefusalRecorder` key set never evicting, cursor and `limit` validation, ledger per-week counts growing without bound, the `Fault` dedup key hiding a second failing module, and a resend after an SMS timeout leaving two live challenges.

### D. Editorial

- **Structure (do):** add a 22-row AD index with concern groups, and move the Capability Map to the top. Single-source the epic → AD mapping (it has already drifted from the Binds lines). Add an epic number ↔ slug key. Move the `dev_outbox` rule to AD-21. Move the booking write-path diagram into AD-7. Move the reminders bullet to AD-21. Move rules that live only in Conventions (CSV formula-escaping, NFC text limits, index naming) into their ADs.
- **Structure (cut):** the AD-19 project-layout bullet (stated three times), and the repeated sentence after the paradigm diagram.
- **Prose:** 36 fixes. The ones that change behavior for a literal agent: "its own" (AD-20, AD-5, AD-12), "alert" meaning both owner alerts and Grafana alerts, "which `Production` refuses" (AD-4), "keeps its budget and limit counts" (AD-4), examples in parentheses that read as specifications (prefix "for example,"), and "fitness test" vs "architecture test". There are also inconsistent names: `EnsurePatient` vs `EnsurePatientCommand`, "CloudAMQP Free" vs "Little Lemur".

---

## Findings by lens

### adversarial (29)

1. **AD-7 retry rule.** `UnknownTransactionCommitResult` is handled like a transient error, but the commit may have succeeded. Fix: retry the commit only, set `MaxCommitTime`, and state whether `WithTransaction` is used. If not fixed: a successful cancel comes back as 422, a second OTP challenge is issued, and events are duplicated with new EventIds.
2. **AD-20 / AD-8 alert dedup.** A `DuplicateKey` inside the caller's transaction aborts it. Fix: an upsert with `$setOnInsert`, and no catch-and-continue inside a transaction. If not fixed: after the first `otp_budget_exhausted`, every later refusal loses its effects, and the jobs fail on their second alert.
3. **AD-3, AD-8, AD-14 get-or-create.** "Re-run on `DuplicateKey`" reads as a local retry on an aborted session, and a race against an uncommitted insert shows up as WriteConflict instead. Fix: re-run the whole calling handler, add the named DuplicateKeys to AD-7, and test both outcomes. If not fixed: `NoSuchTransaction` and a 500 for a practitioner booking of a new patient.
4. **AD-6 keyed guard documents.** No unique key or deterministic `_id`. Fix: a deterministic `_id` or a unique index per guard, plus a first-writer race test. If not fixed: two ledgers or two PractitionerWeeks at first use, so the limit or the overlap rule breaks.
5. **AD-22 vs AD-6.** Stored-document limits are unguarded counts, and FR-3 counts can expire by TTL. Fix: add guard rows, or declare the limits best-effort; keep the counting source at least as long as the window. If not fixed: the limits are exceeded, the SMS limit resets, and the agents get conflicting instructions.
6. **AD-7 vs AD-21, AD-12.** Fan-out Notification batches and multi-session deletes break the one-aggregate-type rule. Fix: list them as exceptions, or comply with deleteMany and idempotent upserts. If not fixed: the rule loses authority.
7. **AD-21 / AD-10 fan-out redelivery.** A DuplicateKey on a re-delivered batch aborts it every time. Fix: insert-if-absent, and resumable fan-out. If not fixed: the message ends up stuck in the error queue and the remaining patients are never notified.
8. **AD-10 consumer inbox.** The unit of work isn't enlisted in the inbox transaction, and there's no Epic 1 proof for it. Fix: state the enlistment and add a proof. If not fixed: a reaction is lost or doubled, including erasure.
9. **AD-21 send job.** The 2-minute threshold isn't tied to batch size, lease length or timeout. Fix: bound the batch, run the sweep only under the lease, version-check the sent write, and test with a slow fake. If not fixed: delivered messages are marked failed, and false alerts fire.
10. **AD-11 reminders.** No `reminderStagedAt == null` filter. Fix: add it, add an index, and test with two runs. If not fixed: re-staging every minute burns M0 and CloudAMQP.
11. **AD-21 send resolution.** The booking's state isn't checked before sending. Fix: return the state and cancel Notifications that no longer apply. If not fixed: reminders for cancelled bookings.
12. **AD-5 global writes.** The settings save isn't listed. Fix: add the current tenant's own Tenant document. If not fixed: the agent stops, or writes an unfiltered save.
13. **AD-5 / AD-18 host topology.** One onrender.com host can't map to two tenants, and a session for A with a header naming B is undefined. Fix: state the topology and refuse a session/header mismatch. If not fixed: cross-tenant confusion, and a second tenant can't be onboarded.
14. **Epic 1 fallback vs AD-12 CSRF.** The XSRF interceptor works same-origin only, and the fallback needs CORS. Fix: add AD-12 and AD-22 to the fallback row, with an exact-origin CORS allow-list. If not fixed: 403s, or an agent disables antiforgery.
15. **AD-14 vs AD-6.** UTC "never edited" vs edit draft times; zone and week-start mutability is unstated. Fix: recompute UTC on edit, and state mutability. If not fixed: wrong lapse and reminder times, and split weekly counts.
16. **AD-6 vs AD-7 copy last week.** The exception omits Week. Fix: name Week in it, and test copy against publish. If not fixed: draft slots land in a just-published week.
17. **AD-8 same-slot index.** The DuplicateKey response is undefined, and `holdsPlace` must be kept in step. Fix: re-run, then 422 `already_booked`, and test `holdsPlace`. If not fixed: 500, 409 or loops, and the zero-409 suite flakes.
18. **AD-9 vs derived `_id`s.** The derivation and the type are unspecified. Fix: one name-based UUID helper as the only exception. If not fixed: mixed id types, and defeated dedup.
19. **AD-20 / AD-11 count_drift.** The dedup key has no form for ledgers, and correct-vs-alert is unstated. Fix: per-subject keys, and alert-only or a named correcting command. If not fixed: missed alerts, or a fix-up `$set` that creates drift.
20. **AD-5 seed-tenant vs AD-3.** No public commands for it. Fix: Seed commands in both modules' Contracts, plus a transaction and idempotency statement. If not fixed: Api writes collections, or a half-created tenant.
21. **Module table / AD-3 vs AD-5 sessions.** Ownership is contradictory. Fix: pick one owner and update all three. If not fixed: the fitness tests can't both pass, and live sessions remain after a disable or an erasure.
22. **AD-20 "its own transaction".** It's ambiguous. Fix: "the calling handler's unit of work". If not fixed: audit entries for rolled-back actions.
23. **AD-5, AD-7, AD-15 "every X has a test".** Not in the AD-19 inventory. Fix: endpoint, reflection and route scans, or mark them review-only with a PR checkbox. If not fixed: coverage erodes while CI stays green.
24. **AD-18 quotas.** No alerts for M0 storage (512 MB), CloudAMQP monthly limits or Render build minutes. Fix: add alerts, cleanup windows and interim caps. If not fixed: a mid-month outage that users find first.
25. **AD-6 OtpBudget / AD-12 timing.** A hot document at peak, and "timing class" is untestable. Fix: define timing; consider a sharded counter or a conditional `$inc`. If not fixed: 409s on OTP at peak, and number enumeration.
26. **AD-18 deploy.** No version check on `/health/ready`. Fix: expose the SHA and poll for it. If not fixed: the static site deploys against the old API.
27. **Epic 1 paid-tier fallback.** It contradicts NFR-16. Fix: an Open Question, or a $0 path. If not fixed: silent spend, or no agreed path.
28. **AD-22 public check and fraud report.** No per-IP limit, and the keyspace is unstated. Fix: add the defaults and the guess probability. If not fixed: ticket-code enumeration.
29. **AD-3 settings cache.** An unlisted owner cache, and eviction doesn't reach the second instance. Fix: forbid it, or list it with a TTL. If not fixed: stale limits during a deploy overlap.

*(Adversarial also raised `Fault<T>` tenant context, B9.)*

### edge-case-hunter (40)

| Location | Trigger | Guard | Consequence |
|---|---|---|---|
| AD-7 | UTCR when the commit succeeded; whole handler re-runs | Retry commit only on UTCR | Non-keyed commands double-apply or falsely refuse |
| AD-7 | Re-run after an unknown commit stages new EventIds | Deterministic EventId, or detect already-applied | Duplicate downstream effects |
| AD-7 | Budget expires while the commit outcome is unknown | 409 `commit_unknown`, replay by requestKey | 409 although the booking committed |
| AD-7 | Atlas election > 3 s | State the expected 409s; scope the zero-409 claim | 409 burst on elections |
| AD-8 | DuplicateKey on patients inside the caller's transaction | Re-run the whole calling handler | NoSuchTransaction |
| AD-8 | DuplicateKey on the same-slot index | Map to 422 `already_booked` | 500 or a loop |
| AD-8 | Booking without requestKey; key reused after cancel | requestKey required; define replay of a cancelled booking | No dedup; ambiguous replay |
| AD-3 | Erasure cancel after the tombstone | System erasure cancel bypasses the refusal | Erasure never completes |
| AD-3 | Erased patient keeps sessions and devices | Delete them in the erasure transaction and evict | Erased subject still authenticates |
| AD-6 | Transitions from checked_in or lapsed undefined | Allowed-transition table with effects | Inconsistent counts |
| AD-6 | Ledger per-week counts grow forever | Prune old weeks on write | Unbounded document |
| AD-10 | Consumer commits, then crashes before the inbox mark | One session; Epic 1 proof | Doubled reactions |
| AD-10 | Outage longer than the 51 s ladder, or poison message | Error-queue redrive procedure | Messages lost in `_error` |
| AD-21 | Redelivery with an existing Notification `_id` | Treat as success; unordered upsert | Batch aborts, error queue |
| AD-10 | Two modules fault on one PatientErased | Dedup by patient + endpoint | Second failure hidden |
| AD-11 | Reminder selection lacks `reminderStagedAt == null` | Add the filter | Re-staged every minute |
| AD-21 | Booking cancelled before send | Check state at send | Reminder for a cancelled booking |
| AD-21 | Provider call in flight past 2 min | Timeout below 2 min; claim-token check | False `failed`, conflict |
| AD-11 | Run exceeds the lease, or the holder crashes | Expiry plus heartbeat renewal | Double run, or a stall |
| AD-11 | One tenant throws in the per-tenant loop | Catch per tenant and continue | All tenants' jobs halt |
| AD-9 | Crash while holding the migration lock | Expiry, owner, takeover | Startups block, deploys fail |
| AD-11 | TTL monitor lag | Check `expiresAt` explicitly | Expired session or OTP accepted |
| AD-22 | Limit window longer than the challenge TTL | Counter doc with TTL ≥ window | FR-3 resets |
| AD-12 | Fresh-OTP write vs a 30 s cached session | Evict on every session update | Delete-my-data refused |
| AD-4 | SMS timed out but delivered; resend | Define challenge coexistence or invalidation | First OTP fails, or two live OTPs |
| AD-5 | X-Tenant missing, unknown or inactive | 400 / 404 with the same body and timing | Undefined; null tenant context |
| AD-5 | Session tenant later disabled | Check against the active list; delete sessions | Disabled tenant keeps access |
| AD-5 | seed-tenant runs in another process | Short TTL, no negative caching | New tenant unresolved until restart |
| AD-14 | DST gap or overlap | Reject a nonexistent time; define the offset for an ambiguous one | Wrong UTC, or exceptions |
| AD-14 | Zone, week start or tzdata changes | Immutable, or a recompute migration | UTC and week keys diverge |
| AD-8 | Ticket with an old prefix | Parse strips any prefix form | Valid tickets rejected |
| AD-7 | Bulk action interrupted | Continue server-side; idempotent resubmission | Partial run with no record |
| AD-20 | Scheduling owner disabled before publish | Define the actor or cancellation | Blocked, or wrong actor |
| AD-20 | Refusal key set never evicted | Keep the current minute only | Unbounded memory |
| AD-22 | Direct call to the web service URL | Pin the trusted proxy, or a rewrite secret | Spoofed IP bypasses limits |
| AD-22 | CGNAT shares one IP | Size limits for CGNAT, or a combined partition | Legitimate 429s |
| AD-12 | Lockout of the sole owner | Account + IP lockout, or a recovery path | Administration denied |
| AD-12 | XSRF token from before sign-in | Re-issue on sign-in and sign-out | First unsafe call fails |
| AD-13 | Bad `limit` or cursor | 400 outside 1..200; validate the cursor | Inconsistent handling, unbounded reads |
| AD-18 | Deploy polls readiness on the old version | Return the SHA and wait for it | Frontend before the API |

### structure (15)

1. Add an AD index with concern groups, and move the Capability Map under "Invariants & Rules" (+~200 words).
2. Single-source the epic → AD mapping. Declare the always-on ADs once. It has already drifted: the booking and erasure rows.
3. Add an epic number ↔ slug key.
4. Move the `dev_outbox` rule from AD-17 to AD-21. It is spread over 5 places today.
5. Cut the AD-19 project-layout bullet, which is stated three times.
6. Move rules that live only in Conventions into their ADs: CSV formula-escaping to AD-12, NFC text limits to AD-4, index naming to AD-9.
7. AD-3: move module registration to AD-2, nest reason categories under settings, and turn the Mobile numbers row into a Conventions pointer.
8. Move the booking write-path sequence diagram to the end of AD-7.
9. Move the reminders bullet from AD-11 to AD-21.
10. AD-5: set tenant setup and the public profile apart from the isolation invariants (or move them to AD-18 and AD-13).
11. Cut the repeated sentence after the paradigm diagram.
12. PRESERVE the paradigm bullets, which restate AD-2 for readers.
13. PRESERVE the AD-19 inventory alongside the per-AD fitness bullets.
14. AD-19 supply chain: move it to AD-18, or rename AD-19.
15. AD-10: point to Stack for the MassTransit pin.

Net change is about +80 words.

### prose (36 + 6)

The highest-impact rows are listed under triage D; the rest are condensed here and get
their exact wording when applied. In summary: "its own" ×4; "alert" has two meanings; "which" in AD-4; "keeps
its counts"; refusal status in a parenthesis; "both cached"; the Api clause scope in
AD-2; "and nothing else" in AD-3; the erasure parenthesis; `MobileNumber` terse; the
AD-20 flood key drops the action; "the listed ADs"; the banned-API scope in AD-14;
examples in parentheses; fitness vs architecture test; `EnsurePatient` naming; the
development channel naming; CloudAMQP plan naming; AD-21 verbless sentences; "per
announcement notify at confirm"; "not found yet" condition; lockout wording;
known-device parenthesis; "FR-38's 1 minute"; "regenerates both"; AD-13 500 cell scope;
deploy path filter; health endpoints sentence; profile fragment; "its own code" in
AD-22; "and similar" in Stylelint; P-5 undefined; "AD" never expanded; "Proof:" vs
Epic 1 proofs. Minor: Synthetics ping vs check, tenant list vs active-tenant list,
Session "global lookup", AD-8 verbless index sentence, Q3 pointer, "signals" repeated.
