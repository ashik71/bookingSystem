# Review — reconcile inputs against the spine (epics, initiative, reference patterns)

- **Reviewer:** independent reconcile reviewer
- **Date:** 2026-10-10
- **Spine:** `architecture-slotbook.md` (state after the adversary and tech-currency reviews)
- **Inputs:** `initiative-booking-management.md` (SC-1..SC-8, touch points, open-question list),
  the 12 epic envelopes, `input-reference-architecture.md` (P-1..P-20, M-1..M-6), `.memlog.md`, `docs/adr/`.
- **Scope rule:** only real gaps. Items already raised in `review-adversary.md` / `review-tech-currency.md`
  are not repeated. Decisions in the memlog are not reopened.


## Verdict

**Pass with changes.** No critical gaps: every memlog decision (D1–D10, the D2 amendments, the tech
additions, the OTP-direct and retire-only rulings) lands in the spine, and every Adopt/Adapt pattern
except P-9 is reflected or deliberately replaced. Three **high** gaps remain where an epic's Done-when
or touch point has no architectural home, plus a set of medium/low precision gaps, and a list of
epic-envelope updates the spine now forces.

| Tier | Count |
| --- | --- |
| Critical | 0 |
| High | 3 |
| Medium | 7 |
| Low | 8 |
| Epic-envelope updates (not spine defects) | 11 epics + initiative |

---

## High

### R-1 — FR-27's "5 retries within 30 minutes, then failed + owner alert" has no mechanism (P-9 dropped)

- **Source:** PRD FR-27 (A18 "Confirmed: 5 retries over 30 minutes"); epic-notifications Done-when 2;
  initiative Notes open question "background jobs … retries"; reference P-9 (Adopt for notification sends).
- **Spine:** AD-10 retry rule (in-memory 1 s, 5 s, 15 s, 30 s, then error queue; no broker delayed
  messages); AD-11 job table (no retry job); AD-8 Notification identity; AD-3 alert key "notification failure → notification id".
- **Gap:** the in-memory schedule gives 4 retries in about 51 s, then the error queue; nothing reaches
  "5 more attempts within 30 minutes", nothing records the send as *failed*, and nothing raises the alert
  (only `erasure_incomplete` is wired to the error queue). P-9's "mark processing, commit, send, record
  outcome, recovery job" is silently dropped, so a crash between the provider call and the commit
  re-sends (breaks "at most once per event") or leaves no visible record.
- **Fix:** add to AD-10/AD-11: a `Notification` per (source, recipient, channel) with status
  `pending → sending → sent | failed`, `attempts`, `nextAttemptAt`; the consumer only creates it, and a
  1-minute **notification send** job (catch-up, leased) claims due ones with the P-9 two-commit pattern,
  backs off to 6 attempts within 30 min, then marks `failed` and raises the `notification_failed` alert.
  State what happens to a `sending` record left by a crash. FR-27's "at most once" suggests marking
  it `failed` with an alert rather than resending; the developer decides.

### R-2 — Announcement image storage (initiative touch point) is neither decided nor deferred

- **Source:** initiative Boundaries touch point "file storage for announcement images — owner: epic-announcements";
  epic-announcements Done-when 1 (image up to 1 MB, JPEG/PNG/WebP) and 5 (SC-7 covers image storage).
- **Spine:** silent: not in AD-9, AD-18, Stack, Structural Seed or Deferred. Render's filesystem is
  ephemeral (memlog) and Atlas Free caps data at 0.5 GB.
- **Fix:** either decide (for example: images stored in MongoDB as one binary document per image ≤ 1 MB,
  served through a cached `/api/public/announcement-images/{id}` endpoint, with a per-tenant byte budget
  checked against the 0.5 GB cap; re-encode on upload to strip metadata) or add a Deferred row
  "Announcement image storage — epic 11 inception, must be $0 (SC-7)". Also add upload validation
  (magic bytes, size) to the threat model.

### R-3 — Browser tests and accessibility checks have no architectural support (SC-8, epic 3, ADR-0006 gap)

- **Source:** epic-schedule-and-publication Done-when 6 ("Browser tests run in CI for the patient pages";
  handoff "the browser-test setup goes to every epic with screens"); SC-8 (360 px, WCAG 2.2 AA);
  epic-clinic-day Done-when 2 (camera scan in mobile Chrome and Safari); epic-launch Done-when 4.
- **Spine:** AD-15/AD-19 cover unit, integration and fitness tests only. ADR-0006 says the sandbox image
  has **no browser** and defers Playwright "until there's a real user flow"; epic 3 is that flow.
- **Gap:** the agent has no rule for where browser tests live, which tool, or whether they run in the
  sandbox (no browser) or only in CI, so epic 3's Done-when 6 can't be built consistently and SC-8 is
  checked by hand only.
- **Fix:** add to AD-19 (and propose an ADR-0006 follow-up): Playwright under `src/frontend/e2e/`, run in
  CI only (or in a browser sidecar container, like MongoDB), at a 360 px viewport, with an axe-core
  accessibility scan on every patient page as the automated part of SC-8; the sandbox agent runs them
  only through CI. Camera scanning stays a manual check recorded in the PR.

---

## Medium

### R-4 — Domain events (in-module) are not addressed; P-10 "internal vs external" not reconciled

- **Source:** initiative open-question list "domain events, the outbox, …"; reference P-10 (Adapt).
- **Spine:** AD-10 covers integration events only ("no in-process event dispatch across modules").
- **Fix:** one line in AD-4 or AD-10: either "aggregates raise no domain events; the handler stages the
  integration event directly" or "aggregates record domain events, the handler maps them to integration
  events before commit; no in-process dispatch" — so stories don't invent a MediatR-style notification bus.

### R-5 — The development-channel production refusal (FR-27) has no environment model

- **Source:** PRD FR-27 ("refuses to start with it configured there, and a test proves it");
  epic-patient-sign-in Done-when 5; initiative open question "environments … and the development-channel rule".
- **Spine:** AD-12 "the demo uses the development channel"; AD-18 names "demo environment" and "production later" but not how the process knows which it is.
- **Fix:** AD-18: name the environments (`Development`, `Demo`, `Production` via `ASPNETCORE_ENVIRONMENT`)
  and state the rule "`Production` + development SMS channel = startup failure; `Demo` may use it",
  with the test living in `SlotBook.IntegrationTests`.

### R-6 — Tenant document scope and slug resolution contradict AD-5's "no unfiltered reads"

- **Source:** AD-5 itself vs Tenancy's needs; epic-tenant-and-staff Done-when 1 (tenant created by hand),
  epic-launch Done-when 3.
- **Spine:** AD-5 says only `[GlobalAggregate]` documents are read unfiltered and only leases and
  migrations are global writes; yet resolving the `X-Tenant` slug and `GetActiveTenantIdsQuery` must read
  `tenants` with **no** tenant context. The Tenant aggregate's scope is never stated, nor how a tenant and its first owner are created "by hand".
- **Fix:** state that `Tenant` is `[GlobalAggregate]` (unique index on `slug`), that slug → tenantId
  resolution and the active-tenant list are the only global reads (cached in process), that settings
  writes run under the tenant's own scope, and that the by-hand setup is a one-off CLI command / migration-style
  seeder in the host (not an API endpoint), which also creates the first owner and StaffRoster.

### R-7 — Fresh-OTP step-up for Delete my data (FR-44, FR-4) is missing from AD-12

- **Source:** PRD FR-44 / FR-4 ("never satisfies a fresh OTP"); epic-launch Done-when 1; epic-patient-sign-in boundaries.
- **Spine:** AD-12 covers OTP sign-in and sessions only; the Capability map row for FR-44 cites AD-12.
- **Fix:** AD-12: "an action marked `[RequiresFreshOtp]` needs an OTP challenge verified for the
  session's own number within the last 5 minutes, recorded on the session; a known-device skip never
  sets it". Add the fitness/integration test.

### R-8 — NFR-6 limits that the PRD hands to architecture are not set; limiter partitions incomplete

- **Source:** PRD NFR-6 ("limits not given in an FR are set in architecture"; 429 "with a retry hint");
  epic-schedule-and-publication Done-when 5; epic-clinic-day Done-when 2 (60/min **per account**);
  epic-anti-impersonation Done-when 5 (3 fraud reports per number per day); epic-chat Done-when 1 (20 messages per hour).
- **Spine:** AD-12 "per IP and per number" only; no values for availability, staff sign-in or OTP verify per IP;
  no `Retry-After`. The threat model holds FR values but not the architecture-set ones.
- **Fix:** add a limits table to AD-12 (availability per IP, staff sign-in per IP, OTP verify per IP),
  name the partition keys (IP, E.164 number, staff account id, patient id), require `Retry-After` on 429,
  and state that **daily business quotas** (fraud reports 3/day) are counted in MongoDB, not the in-memory
  limiter, because a restart would reset them.

### R-9 — Problem-details extension members are unspecified

- **Source:** epic-booking-core Done-when 3 ("up to 3 suggested slots on `slot_full`"); NFR-6 retry hint;
  AD-7 bulk result list.
- **Spine:** AD-13 says the body carries `code` and `traceId` only.
- **Fix:** AD-13: refusals may carry typed extension members declared next to the error code
  (`slot_full` → `suggestedSlotIds[]` with their local times; 429 → `retryAfterSeconds` plus the header),
  and these appear in the committed OpenAPI document so Angular gets them typed.

### R-10 — Capability map contradicts the module table on two rows

- **Source:** the adversary-review rules A-8 (reminders owned by Booking) and A-15 (home warning is a Tenancy setting).
- **Spine:** Capability map row "Notifications, reminders (FR-27 to FR-29) | Notifications" and row
  "Official contacts, home warning, fraud report … | ClinicContent", against the module table and AD-11.
- **Fix:** change the rows to "Notifications (send); reminders job in Booking (AD-11)" and "home warning text in
  Tenancy settings; ClinicContent renders contacts and fraud reports".

---

## Low

### R-11 — Owner-alert kinds list misses NFR-17's SMS quota alert
- **Source:** epic-notifications Done-when 5 (quota exhausted → alert "new-device sign-in unavailable").
- **Spine:** AD-3 dedup-key list (OTP budget, count drift, notification failure, erasure incomplete).
- **Fix:** add `sms_quota_exhausted` with dedup key = tenant local date (or defer it with Q1 in the Deferred row).

### R-12 — Masking convention omits the CSV export exception
- **Source:** epic-clinic-day Done-when 1 (full numbers in the owner's CSV, audited); threat-model.md masking row.
- **Spine:** Consistency Conventions "Mobile numbers" lists only the patient's own pages and the patient detail view.
- **Fix:** add "the owner's CSV export (audited, formula-escaped)" to the exceptions.

### R-13 — P-5 / P-16 outbox test pitfalls not carried over
- **Source:** reference P-5 pitfalls (concrete-type staging, round-trip test, drain before host disposal).
- **Spine:** AD-10 keeps "one concrete type" but no test.
- **Fix:** AD-19: a test round-trips every integration event type through MassTransit serialization, and
  integration tests wait for the outbox to drain before disposing the host.

### R-14 — P-7 retry-hazard rule dropped
- **Source:** reference P-7 / P-16 (a `DomainException` must not stand for a transient "not found" in a retried consumer; fitness test).
- **Spine:** AD-10 sends `DomainException` straight to the error queue.
- **Fix:** AD-10: consumers throw `DomainException` only for deterministic refusals; a "not found yet" on an
  outbox-ordered read is a transient exception. One fitness or review rule.

### R-15 — External-API fakes and test authentication are unstated (P-11, P-16)
- **Source:** reference P-11 (replace only authentication in tests) and P-16 (HTTP fakes for outside APIs).
- **Spine:** AD-19 silent; the cross-tenant and cross-patient suites need signed-in sessions.
- **Fix:** AD-19: integration tests create sessions through a test-only session seeder (authorization stays real),
  and outside HTTP providers (SMS, Web Push) are faked at the `ISmsChannel` / push-sender seam.

### R-16 — RabbitMQ topology (initiative open question) is implied, not stated
- **Source:** initiative open-question list "RabbitMQ topology (exchanges, queues, …)"; Little Lemur's 100-queue / 10k-queued caps.
- **Spine:** AD-10 names two priority endpoints only.
- **Fix:** one line: MassTransit default publish topology, receive endpoints named `{module}-{purpose}` in
  kebab-case, a queue budget (count of endpoints and error queues) kept under 100. Note SMS delivery-status
  webhooks (P-8) as deferred with Q1.

### R-17 — "Stop and raise" applies only to invariants (P-18)
- **Source:** reference P-18 (Adopt): spine wins; if a change cannot fit a rule, stop and raise it.
- **Spine:** AD-6 only, for new invariants.
- **Fix:** add a general line to the spine preamble (or propose it for CLAUDE.md, the developer decides):
  "A story that cannot satisfy an AD stops and comments on the issue; it never works around the rule."

### R-18 — Render deploy health check and job ownership in the platform slice
- **Source:** epic-deploy-platform Done-when 1 ("a failed deploy leaves the previous version running");
  epic-schedule-and-publication handoff "the scheduler for timed jobs goes to epics 4, 5, 6 and 10".
- **Spine:** AD-18 no `healthCheckPath`; Capability map platform row doesn't list the job runner/leases although the Source tree puts them in SharedKernel.Infrastructure.
- **Fix:** AD-18: `healthCheckPath: /health/ready` in `render.yaml`. Capability map: the lease-based job runner is part of the epic-1 platform slice (epic 1 must build it to prove timers), not epic 3.

---

## Epic-envelope updates the spine now forces (not spine defects)

| Epic | Envelope text | Spine rule | Update |
| --- | --- | --- | --- |
| initiative | SC-1 "from epic 10 on, production"; Done-when 1–5 clinic metrics; References "architecture — not yet written" | Goal shift to team demo; Deferred "Production environment" | Correct-course (already planned in memlog): SC-1 = demo environment until the clinic is unparked; point References at the spine |
| 1 deploy-platform | DW5 daily backup + tested restore; Notes "architecture chooses another way" if timers fail; "validators"; "pre-launch" | Deferred NFR-19; AD-11 fallback already chosen; AD-9 has no collection validators; AD-18 "demo environment" | Move DW5 to clinic-later; add the AD-18 proofs (rewrite passes POST, headers, `Set-Cookie`; outbox shares the UoW session; Atlas transactions) and the Synthetics check; drop "validators"; rename environment |
| 2 tenant-and-staff | DW4 "database identity can't update or delete audit entries, automated test proves it"; Notes Q4 open | Deferred DB role; audit repository insert+find only + fitness test; AD-12 TOTP decided | Reword DW4 to the repository + fitness-test guarantee until the role is available; close Q4 |
| 3 schedule-and-publication | Hands "the scheduler for timed jobs" to others | Job runner is platform (R-18) | Point to epic 1; keep browser tests pending R-3 |
| 4 patient-sign-in | DW5 "Expired OTPs are cleaned up by a timed job"; Boundaries "retries … go to epic 6"; Notes "budget 300 until Q1 answered by architecture"; waits on epic 3 for OTP cleanup | AD-11 TTL indexes; AD-4 OTP sent directly, resend is the retry; Deferred Q1 | TTL instead of a job; OTP has no automatic retry; budget default stays 300, Q1 deferred; drop that wait reason |
| 5 booking-core | DW5 event "carries the actor, the reason and a notify flag"; DW4 "a category in use can be retired but not deleted"; Notes "tier reconsidered" | AD-10 events carry no cancel reason (resolved by query); AD-3 retire-only, no delete at all; Deferred single conditional update first | Event carries ids/enums/notify flag only; categories are retire-only; first fallback is the single conditional update |
| 6 notifications | DW3 reminders built as a Notifications job; DW5 "OTPs first"; DW5 live SMS provider + Q1 from architecture; Notes MassTransit version unknown | AD-11 reminders owned by Booking; AD-4/AD-10 OTP outside the pipeline; Deferred Q1; AD-10 v8.5.11 | Reminder job lives in Booking (Notifications only sends); priorities = booking notifications high, bulk low; live provider is clinic-later; close the version note |
| 9 anti-impersonation | DW4 "an owner writes [the warning] per language" (implies ClinicContent owns it) | Module table: home warning is a Tenancy setting (FR-39 page, epic 2) | Epic 9 renders the warning from `GetTenantSettingsQuery`; editing is on the settings page |
| 10 launch | Production, live SMS, daily backups, DNS, purge "under a separate maintenance identity" | Deferred production, Q1, NFR-19, Q3; audit DB role deferred | Split into a demo-scope part (FR-44, completeness, accessibility) and a clinic-later part |
| 11 announcements | DW3 "Each notify send is audited"; DW5 "in production"; image storage | AD-10: one audit entry per announcement notify, with recipient count; R-2 | Align audit wording; demo environment; image storage per R-2 |
| 12 chat | DW5 "in production"; transport | Deferred chat transport (epic 12 inception) | Demo environment; transport at inception (already consistent) |

Epics 7 (clinic-day) and 8 (reserved-places) are consistent with the spine.

---

## Coverage notes (checked, no gap)

- Initiative open questions: tenancy (AD-5), data access + migrations (AD-9), staff MFA / sessions / audiences
  (AD-12), concurrency (AD-6/7/8), outbox + MassTransit version (AD-10), timers on Render (AD-11), error format
  and 429 (AD-13), time (AD-14), threat model (companion), frontend shape (AD-15), chat transport (Deferred),
  pipeline worker location (Deferred). Gaps only where noted (R-1, R-4, R-5, R-16).
- SC-1 AD-18; SC-2 AD-5 + suite; SC-3 AD-3 contract; SC-4 AD-12 + `IRefusalRecorder`; SC-5 AD-16 + AD-12 (no HTML);
  SC-6 AD-17 scan; SC-7 AD-1/AD-18 (except R-2); SC-8 only R-3.
- Reference: P-1, P-3, P-4 (own dispatcher), P-5 (MassTransit outbox replaces hand-built), P-6, P-7 (except R-14),
  P-11 (session replaces scope check), P-12, P-13, P-14, P-15, P-16 (sidecars decided), P-17 decided, P-19 decided,
  P-20 deferred, M-1..M-6 adopted. P-2 is replaced by M-1/M-5. P-8 is implicit (R-16). P-9 dropped (R-1).
