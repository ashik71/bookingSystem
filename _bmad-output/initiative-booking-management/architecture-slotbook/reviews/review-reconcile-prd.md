---
type: review
lens: reconcile-prd
reviews: architecture-slotbook.md (+ threat-model.md)
against: prd-slotbook.md (§3, §4, §5, §6), addendum.md ("For architecture")
created: '2026-10-10'
---

# Review — PRD and addendum reconciliation (architecture-slotbook)

**Verdict: pass with changes.** Every addendum architecture question is answered or
deferred, and the clinic-only items (SMS provider, backups, retention, uptime,
production) are all under Deferred. What did not land are quiet requirements. These
are mostly numbers and timing in FR-27, FR-9, FR-39 and NFR-6, plus four shared
concerns that the spine leaves to each epic: server-side localized text, image
storage, refusal parameters and the client IP. Two findings block the near-term team
demo: patients can't receive an OTP in the deployed demo, and the 200 → 10 test can
produce an outcome that FR-15 forbids.

Out of scope: findings already applied from `review-adversary.md` and
`review-tech-currency.md`, and FR detail that belongs in stories.

## Critical

### R-1 — The development channel has no lawful output in the deployed demo

- **PRD:** FR-27 ("The development channel shows OTPs and notifications to the
  developer... can't be enabled in production: the application refuses to start");
  FR-6 ("No ... API response contains an OTP"); AD-12 "the demo uses the development
  channel".
- **Spine:** AD-4 (OTP sent through `ISmsChannel`), AD-12, AD-17 ("A log never contains
  ... an OTP"), AD-18 (the demo runs on Render).
- **Problem:** in the Render demo the development channel is the only channel. AD-17
  forbids logging the OTP, FR-6 forbids returning it, and the spine names no other
  place to show it. So nobody on the team can sign in as a patient in the demo. The
  spine also doesn't define "production" for the start-up guard. If the guard keys on
  `ASPNETCORE_ENVIRONMENT=Production`, the Render demo (Production by default) refuses
  to start. Epics 4 and 6 will each improvise, for example by logging the OTP or by
  returning it in the API.
- **Fix (rule):** "The development channel writes OTPs and notifications only to a
  `dev_outbox` collection. The only reader is a staff-owner-only `/dev/outbox` page that
  exists only when `Sms:Channel=Development`. It is exempt from FR-6 because no real
  patient exists. The start-up guard refuses `Development` when `SlotBook:Environment
  = Production`, an explicit setting separate from `ASPNETCORE_ENVIRONMENT`; the demo
  sets `Demo`." Name the AD-17 exemption explicitly.

### R-2 — `409 concurrency_conflict` is a booking outcome that FR-15 and SM-1 don't allow

- **PRD:** FR-15 ("fails with exactly one reason: `not_found`, `slot_closed`,
  `slot_started`, `slot_full`, `already_booked`, `limit_reached` or
  `validation_failed`"; "200 concurrent patients competing for 10 places end with
  exactly 10 bookings, and 190 receive `slot_full`"); SM-1 on every CI run; NFR-13
  (peak of about 100 at once).
- **Spine:** AD-7 (re-run up to 5 attempts, then 409 `concurrency_conflict`), AD-13.
- **Problem:** 200 handlers write one Slot guard document. Under snapshot isolation
  each wave commits about one writer and aborts the rest. With a fixed 5-attempt cap,
  some requests run out of attempts before the slot is full and get 409 instead of
  `slot_full`. That fails SM-1 nondeterministically, and the CI suite turns flaky. The
  same hot-document pattern hits the per-tenant `OtpBudget` (AD-6) at the publication
  peak, when about 100 new patients request OTPs at once. Epic 5 will either raise the
  cap ad hoc or loosen the test.
- **Fix (rule):** "On the guard-document paths of AD-6, the retry bound is a time
  budget (for example 3 s of jittered retries), not an attempt count. A handler that
  re-loads and finds the refusal condition returns that refusal and never 409. The
  NFR-1 suites assert zero 409 responses." If the developer prefers to keep the cap,
  record a PRD change that adds `concurrency_conflict` as an allowed FR-15 outcome and
  rewrites SM-1 to "exactly 10 bookings; every other request refused".

## High

### R-3 — The FR-27 retry window (5 retries within 30 minutes) contradicts AD-10's retry policy

- **PRD:** FR-27 ("A failed send is retried up to 5 more times within 30 minutes. If
  the last attempt also fails, the send is recorded as failed and raises an owner
  alert"); FR-45; A18 confirmed.
- **Spine:** AD-10 (transient retry in memory at 1 s, 5 s, 15 s and 30 s, so 4
  retries in about 51 s; no broker delayed messages). AD-3 has a "notification failure
  → notification id" dedup key, but nothing raises that alert.
- **Problem:** the spine's policy covers less than a minute, during which a provider
  outage usually isn't over. After the last retry the message goes to the error queue,
  and only `PatientErased` has an "error queue → owner alert" rule. A notification
  failure therefore never reaches FR-45, and the `failed` state isn't recorded.
- **Fix (rule):** "A Notification document holds a state, `attempts` and
  `nextAttemptAt` for each channel. The consumer makes the first attempt. A 1-minute
  catch-up job (AD-11) makes up to 5 more on a backoff that spans 30 minutes, then
  marks the channel `failed` and raises `notification_failed` (dedup: notification id)
  in the same transaction. The consumer's in-memory retry covers only broker and
  database transients, never provider failures." Add the job to the AD-11 table.

### R-4 — Per-IP rate limits behind the `/api` rewrite: the client IP is unspecified

- **PRD:** FR-3 (100 sends per client IP per hour), FR-26 (10 per IP per hour), NFR-6
  ("every public endpoint has a rate limit per IP").
- **Spine:** AD-12 (built-in limiter, per IP), AD-18 (the static-site rewrite and its
  epic 1 proof, which covers bodies, headers and `Set-Cookie` only).
- **Problem:** every request reaches the API through Render's edge and the static-site
  rewrite. If `RemoteIpAddress` is the proxy's address, all patients share one IP
  bucket, and at publication FR-3's 100 an hour locks out the whole tenant. If
  `X-Forwarded-For` is trusted carelessly, any client can choose its own IP and get
  past every per-IP limit. Epics 4 and 9 would each pick a key.
- **Fix (rule):** "Epic 1 also proves which header carries the real client IP through
  the rewrite. The API uses ForwardedHeaders with only that hop trusted (a known
  proxy count of 1, or Render's documented header). Every per-IP limiter and every
  refused-send log reads one `IClientIp`." If the IP doesn't survive the rewrite, the
  AD-18 fallback (a custom domain) is mandatory before epic 4.

### R-5 — NFR-6 leaves some limits to architecture; the spine sets none of them or their windows

- **PRD:** NFR-6 ("limits not given in an FR are set in architecture, loose enough for
  many phones behind one carrier IP"); §3 ("Per hour means a rolling 60 minutes. A day
  means a calendar day in the tenant's time zone"); FR-3, FR-32 (3 a day per number),
  FR-34 (20 an hour).
- **Spine:** AD-12 (in-memory limiter, per IP and per number); the threat model (an
  unspecified per-IP limit on staff sign-in).
- **Problem:**
  - The spine sets no numbers for anonymous availability, OTP verify, staff sign-in or
    the anonymous content endpoints.
  - It doesn't say which limits are 429 rate limits and which are business refusals.
    FR-32 is "3 a day", and FR-3's per-number 60 s and 5-an-hour rules have their own
    messages.
  - The built-in fixed-window limiter is not a "rolling 60 minutes". No in-memory
    limiter can express a tenant-local calendar day.
  - In-memory state resets on every restart and deploy. The per-number 5-an-hour rule
    protects a person from harassment, and a restart silently resets it.
- **Fix (rule):** add a limits table to AD-12. It gives each limit a number, key,
  window type (sliding with at least 6 segments for "per hour"; tenant-local calendar
  day for "a day"), store and response:
  - per-IP limits stay in memory and answer 429 `rate_limited`;
  - per-number limits for OTP sends (FR-3) and fraud reports (FR-32) are counted from
    stored documents (OTP challenges kept for at least 1 hour, fraud reports by local
    date), so they survive restarts. They answer 429 with their own codes.

### R-6 — Server-side localized text has no home and no completeness check

- **PRD:** FR-28 (notifications in the patient's stored language, or the tenant
  default), FR-43 ("Every built-in ... string is available in `bn`, `en` and `ar`. A
  missing translation ... fails the build"; dates with month names in the active
  language; ASCII digits), FR-29, FR-33, FR-37; addendum *SMS* (Unicode segments).
- **Spine:** AD-13 ("The API never returns user-facing text"), AD-16 (Transloco JSON
  for Angular only).
- **Problem:** SMS and push bodies (confirmation, cancellation, reminder,
  announcement, chat reply, the OTP SMS itself) are built-in strings produced on the
  server. They need bn, en and ar text, a date format in the tenant's zone and
  language, and ASCII digits. The spine gives them no resource format, no owner and no
  CI check. Epics 4, 6, 11 and 12 will each choose a different mechanism (`.resx`,
  hard-coded strings or reused Angular JSON), and a missing `ar` template only fails
  at send time.
- **Fix (rule):** "Server-generated user text (SMS, push and the OTP SMS) lives only in
  Notifications as `.resx` per language, rendered by one `IMessageRenderer(kind,
  language, model)` that formats dates in the tenant's zone with ASCII digits. An
  architecture test fails on a key missing in any of bn, en or ar. Each template states
  its segment budget (70 or 67 UCS-2 characters)."

### R-7 — Announcement images (FR-33) have no storage decision

- **PRD:** FR-33 ("an optional single image (up to 1 MB, JPEG/PNG/WebP)"); NFR-16
  ($0); NFR-7.
- **Spine:** silent. The module table gives Announcement to ClinicContent. AD-18 lists
  no blob store. Render's filesystem is ephemeral. Atlas Free holds 0.5 GB. The CSP is
  `default-src 'self'`.
- **Problem:** epic 11 must pick a store (Render disk, MongoDB GridFS, base64 inside the
  document, or a third-party bucket that may not be free) and a serving path that fits
  the CSP. It must also decide on content checks. Re-encoding strips EXIF location
  data and defeats polyglot files. Each choice affects the 0.5 GB cap and the threat
  model.
- **Fix (rule):** "Announcement images are stored in MongoDB GridFS (bucket
  `announcement_images`, tenant-filtered). They are accepted only after a server-side
  decode and re-encode, which strips metadata and enforces 1 MB and an allowed type.
  They are served by an anonymous `[TenantFromHeader]` endpoint under `/api` with
  `Content-Type` set from the re-encode and `X-Content-Type-Options: nosniff`." Add a
  threat-model row for image uploads.

## Medium

### R-8 — The 5-second availability cache contradicts "correct at the moment of reading"

- **PRD:** FR-9 ("Free-place counts are correct at the moment of reading"); FR-13
  ("Released places become free and bookable by patients at once"); FR-11 (a closed
  slot is shown as *closed*).
- **Spine:** AD-12 (anonymous availability served from a 5-second in-process cache for
  each tenant and week).
- **Problem:** for up to 5 s after a booking, release, close or publication, patients
  see stale counts or states. The PRD only tolerates staleness between reading and
  booking (FR-9's last bullet), not at read time. The cache lives in the one process
  (AD-1), so evicting it is cheap.
- **Fix (rule):** "After the unit of work commits, every command that writes a Slot or a
  Week evicts that tenant and week's availability entry. The 5 s TTL is only a
  backstop." Alternatively, record a PRD change: "counts may be up to 5 s stale".

### R-9 — Caching tenant settings would break "applies from the moment it is saved"

- **PRD:** FR-39 ("A changed limit or cut-off applies to new actions from the moment it
  is saved"); FR-29 (a changed lead time applies to unsent reminders).
- **Spine:** AD-3 (callers may cache public queries for 60 s) and AD-3's
  `GetTenantSettingsQuery`.
- **Problem:** the 60-second caching allowance is worded loosely ("queries that serve
  lists"). An epic 5 agent may cache settings to save Atlas operations. A limit change
  would then be ignored for up to a minute, and the epic 2 test "change limit, then
  book" would fail at random.
- **Fix (rule):** "`GetTenantSettingsQuery` is never cached by callers. If Tenancy
  caches it, Tenancy evicts the entry when settings are saved. The 60 s allowance
  covers display-name queries only."

### R-10 — Field-level visibility (FR-16) and masking (NFR-9) have no single enforcing module

- **PRD:** FR-16 (the reason category and note are visible only to the patient, the
  slot's practitioner and owners); FR-35 and FR-41 (the same rule in the inbox and in
  lookup); NFR-9 (full numbers only where needed); NFR-11a.
- **Spine:** the Consistency Conventions row for mobile numbers ("Masked everywhere
  except ..."). It doesn't say whether the server or the client masks, or which module
  applies FR-16 when Chat shows a patient's bookings.
- **Problem:** Chat (epic 12) gets bookings from Booking and could apply FR-16 itself,
  or not at all. Angular could receive full numbers and mask them in the UI. Either
  way, the data is already in the API response.
- **Fix (rule):** "The owning module applies FR-16 visibility and NFR-9 masking inside
  its public queries and endpoints, given the viewer (staff id and roles). Callers
  never receive a field they may not show. The API never returns a full mobile number
  except on the patient's own endpoints, the audited detail view and the CSV export.
  The cross-patient suite asserts this."

### R-11 — Refusals need parameters, but problem details carry only `code` and `traceId`

- **PRD:** FR-3 ("states the remaining wait in seconds"), FR-10 ("states how many
  places are taken and reserved"), FR-12 ("states how many are free"), FR-17 ("names
  the limit"), FR-1 and FR-16 (a message that names the field), NFR-6 ("429 with a
  retry hint").
- **Spine:** AD-13 (the body carries `code` and `traceId`; Angular maps
  `errors.{code}`).
- **Problem:** every epic must carry values to Angular for interpolation. Without one
  shape, epic 3 adds `extensions.taken`, epic 4 relies on `Retry-After` only, and
  epic 5 encodes the limit inside the code. The generated client then has no stable
  type.
- **Fix (rule):** "A refusal carries its parameters as `params: { name: number | string
  }` in problem details. Translation keys interpolate them. A 400 carries `errors: {
  field: [code] }`, keyed by the camelCase field name. A 429 also sets `Retry-After`."

### R-12 — Two FR-45 alert sources have no kind and no dedup key, and the OTP send failure path is undefined

- **PRD:** FR-45 (an alert when "the SMS provider fails or its quota is exhausted");
  NFR-17 ("an owner alert says that sign-in for new devices is unavailable").
- **Spine:** AD-3 (dedup keys only for the OTP budget, count drift, notification failure
  and incomplete erasure); AD-4 (the OTP is sent synchronously after commit).
- **Problem:**
  - Provider failure and quota exhaustion have no alert kind or dedup key. Every failed
    OTP send could raise a new alert, or none.
  - The spine doesn't say what the patient receives when the synchronous OTP send
    fails, or whether the failed send still counts against the per-number limits and
    the daily budget.
- **Fix (rule):** "Add the kind `sms_unavailable`, deduplicated by tenant and local date.
  It is raised by `ISmsChannel` on a provider failure or a quota refusal. A failed OTP
  send keeps its budget and limit counts and answers 503 `sms_unavailable`, which
  Angular maps to the NFR-17 message."

### R-13 — Tenant and first-owner bootstrap is unassigned

- **PRD:** §6 (no self-service onboarding and no operator console); FR-39 (setup-only
  country, time zone and week start); FR-38 (an owner creates the other accounts;
  multi-factor authentication is mandatory); NFR-3 (a second test tenant).
- **Spine:** the module table (Tenancy, built by epic 2), with no creation path. AD-5
  has no rule for how a tenant record first exists.
- **Problem:** epic 1 might seed data in a migration, while epic 2 adds a hidden admin
  endpoint. The first owner's TOTP enrolment (there is no other owner to reset it) and
  the second test tenant would then each be built differently.
- **Fix (rule):** "Tenants and each tenant's first owner are created only by an
  idempotent start-up seeder from configuration (slug, country, IANA zone, week start
  day, owner e-mail-less login name and a one-time password). The first sign-in forces
  TOTP enrolment. No API creates a tenant. Integration tests use the same seeder for
  tenant B."

### R-14 — Where security headers live, and the CSP's compatibility with Angular

- **PRD:** NFR-5 (HSTS), NFR-7.
- **Spine:** AD-12 ("HSTS, and a CSP of `default-src 'self'`"), stated as a backend
  rule. AD-18 (Angular is served by the Render static site).
- **Problem:** the HTML document that the CSP must protect comes from the static site,
  not the API, so a backend-only header protects nothing. A bare `default-src 'self'`
  also blocks the inline `<style>` tags that Angular emits and its critical-CSS inline
  handler. Epic 1 and the first UI epic will reconcile this differently, for example
  by adding `'unsafe-inline'` silently.
- **Fix (rule):** "The static site's headers in `render.yaml` set the CSP and HSTS for
  documents: `default-src 'self'`, with `style-src 'self'` plus the build's style
  hashes or `'unsafe-inline'` for styles only, and never for scripts. `inlineCritical`
  is turned off. The API sends `default-src 'none'` on JSON responses." Epic 1 proves
  that the deployed app loads with zero CSP violations.

### R-15 — Audit of refusals: one entry per refusal, or one per account per minute?

- **PRD:** FR-24 ("one audit entry is written per account for each minute in which
  lookups were refused"); FR-38 and FR-42 (refused staff actions are recorded).
- **Spine:** AD-4 (`IRefusalRecorder` records refusals decided outside a handler).
- **Problem:** the recorder as written writes an audit entry for every refused request.
  That breaks FR-24's one-per-minute rule. It also makes a 429 flood a write
  amplifier against Atlas Free's 100 ops/s, which threat-model §7 is meant to prevent.
- **Fix (rule):** "`IRefusalRecorder` deduplicates by the audit entry `_id` derived
  from (tenant, actor, action, the UTC minute), the same mechanism as owner alerts.
  Rate-limit (429) refusals are always deduplicated this way. Role refusals (403) are
  deduplicated the same way."

## Low

### R-16 — The offline ticket cache can outlive sign-out and erasure

- **PRD:** FR-5 (signing out forgets the device), FR-44 (deletion ends every session),
  FR-16 (the attendee's name on the ticket).
- **Spine:** AD-15 (the service worker serves the offline ticket view).
- **Fix (rule):** "The service worker caches only the ticket response for active
  bookings. Sign-out, sign-out everywhere (on next load) and erasure clear that cache.
  No other personal API response is cached."

### R-17 — NFR-1's wording versus optimistic concurrency

- **PRD:** NFR-1 ("enforced by the data store at commit time, never by a
  read-then-write in application code").
- **Spine:** AD-6 and AD-7 (the handler reads, the domain decides, and a version-checked
  write fails at commit).
- **Problem:** the design meets the intent, because the store rejects a stale write.
  A reviewer reading literally could still flag every handler.
- **Fix (rule):** add to AD-7: "A version-checked write of the guard document is how
  NFR-1's 'enforced by the data store at commit time' is met. An unguarded
  read-then-write is the forbidden case."

### R-18 — The interim append-only enforcement for the audit log is weaker than FR-42 (PRD change to record)

- **PRD:** FR-42 ("The application's own database access must not allow it, and an
  automated test proves it"); the addendum *Audit log* (remove update and delete
  rights).
- **Spine:** Deferred (a database role, if Atlas Free allows one). Until then the
  repository exposes insert and find only.
- **Fix (rule):** if epic 2 finds that Free can't scope a role to one collection,
  record a PRD change: "On the demo tier, append-only is enforced by the repository and
  an architecture test. Database-level enforcement is required before the clinic
  launch." Add that to the Deferred row.

### R-19 — Reason categories are retire-only (PRD change to record)

- **PRD:** the FR-39 table ("a category in use can be retired but not deleted", which
  implies that unused categories can be deleted).
- **Spine:** AD-3 ("Reason categories are retire-only ... there is no delete").
- **Note:** this is a deliberate deviation, not a defect. Amend FR-39 to "categories
  can be retired, never deleted" so that stories and the PRD agree.

### R-20 — Anonymous tenant data needs a public projection

- **PRD:** FR-2 (the privacy notice before OTP), FR-31 (the home warning on the home
  page), FR-8 ("Next week opens on ..."), FR-43 (the first visit uses the tenant's
  default language), FR-30.
- **Spine:** AD-5 (the list of anonymous endpoints doesn't include tenant settings);
  AD-3 (`GetTenantSettingsQuery` returns every setting).
- **Fix (rule):** "One anonymous `[TenantFromHeader]` endpoint returns the public
  tenant profile: default language, publication day text, home warning, privacy
  notice, ticket prefix and chat response time. It never returns limits, the OTP budget
  or other internal settings."

### R-21 — NFR-15 has no per-epic rule

- **PRD:** NFR-15 (every patient page usable at 360 px, WCAG 2.2 AA for contrast,
  labels and keyboard use; the OTP field accepts paste and `one-time-code` autofill).
- **Spine:** only the capability map's "Launch readiness (accessibility audit)".
  AD-15 is silent.
- **Fix (rule):** add to AD-15: "Every patient page has a component or e2e test at
  360 px width with an automated accessibility check (axe), run in CI. The audit at
  launch confirms; it doesn't discover."

## Addendum "Open for the architecture step": disposition

| Question | Where the spine answers it |
| --- | --- |
| Slot as its own aggregate; where the booking limit lives | AD-6 (Slot and PatientLedger guards) |
| Isolation level per write path | AD-7 (snapshot read concern, majority write concern, guard documents); see R-2 and R-17 |
| Ticket-code uniqueness | AD-8 (`codeLive` partial index plus a daily job) |
| SMS provider and default daily OTP budget (Q1) | Deferred; the PRD default of 300 stands |
| Staff sign-in (Q4) | AD-12 |
| RabbitMQ versus Kafka | AD-10 (RabbitMQ); Kafka under Deferred |
| Chat transport | Deferred to epic 12 (polling by default; it must meet FR-34's and FR-35's 10 s) |
| Threat model coverage | threat-model.md (NFR-8) |

Deferred covers the clinic-only items: Q1, NFR-19, Q3 with NFR-11, Q5, and
production.

## Summary

| # | Finding | Severity |
| --- | --- | --- |
| R-1 | No output path for the development channel in the demo; "production" undefined | critical |
| R-2 | 409 after 5 retries breaks FR-15 and SM-1 (200 → 10); OtpBudget is a hot document | critical |
| R-3 | FR-27's 5 retries in 30 min versus AD-10's 51 s; no failure alert | high |
| R-4 | Client IP behind the rewrite is undefined; per-IP limits collapse or can be spoofed | high |
| R-5 | NFR-6 limits not set; windows, store and restart behaviour undefined | high |
| R-6 | Server-side bn, en and ar templates have no home or CI check | high |
| R-7 | No storage for announcement images | high |
| R-8 | The availability cache contradicts FR-9 and FR-13 | medium |
| R-9 | Settings caching would break FR-39 "from the moment it is saved" | medium |
| R-10 | FR-16 and NFR-9 not enforced server-side in the owning module | medium |
| R-11 | No parameter shape for refusals or field errors | medium |
| R-12 | Alert for SMS unavailable is missing; OTP send failure undefined | medium |
| R-13 | Tenant and first-owner bootstrap unassigned | medium |
| R-14 | CSP on the wrong tier; it clashes with Angular's inline styles | medium |
| R-15 | Refusal audit per request versus FR-24's one per minute | medium |
| R-16 | Offline ticket cache outlives sign-out and erasure | low |
| R-17 | NFR-1 wording versus optimistic concurrency | low |
| R-18 | Append-only interim is weaker than FR-42 (PRD change) | low |
| R-19 | Reason categories retire-only (PRD change) | low |
| R-20 | No public tenant-profile projection | low |
| R-21 | NFR-15 has no per-epic rule | low |
