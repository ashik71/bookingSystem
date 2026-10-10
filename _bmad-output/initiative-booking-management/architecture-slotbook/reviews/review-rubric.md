# Review — rubric walk (architecture-slotbook)

- **Reviewer:** rubric walker (architecture reviewer gate)
- **Date:** 2026-10-10
- **Spine:** `architecture-slotbook.md` after four applied reviews (adversary, tech-currency, reconcile-epics, reconcile-prd)
- **Also read:** `threat-model.md`, `.memlog.md`, `docs/adr/0002`, `0006`–`0014`, `src/backend`, `src/frontend`, `.github/workflows/ci.yml`, PRD NFR-6/NFR-9/FR-3/FR-27
- **Scope rule:** findings already raised in the four earlier reviews are not repeated. Most findings here are contradictions introduced by the four edit rounds.

## Verdict

**Pass with changes:** the spine fixes the right divergence points, but the four edit rounds left about a dozen rule-level contradictions (status codes, OTP plaintext, the booking sequence diagram, Contracts references, global reads), several unenforceable "a fitness test or analyzer" rules, a stale threat model, and gaps in the operational envelope (deploy mechanism, alerting, Data Protection keys) that must be fixed before epic 1 is ticketed.


## Critical

### C-1 — Session lookup is a global read that AD-5 forbids (checklist 7)

- **Location:** AD-5 ("The only global reads are slug → tenant id and the active-tenant list"; "no unfiltered repository API except for `[GlobalAggregate]` documents"; fitness test "code outside `SharedKernel.Infrastructure` constructs an unfiltered collection") vs AD-12 (the tenant comes from the session record, looked up by token hash).
- **Problem:** the session lookup happens *before* any tenant is known, on every signed-in request, so it is necessarily unfiltered. The known-device lookup on sign-in is the same. The Data Protection key ring (H-8) and the lease/migration collections are also global. An agent building AD-12 either breaks the AD-5 fitness test or invents its own bypass. This blocks epic 2.
- **Fix (AD-5 rule text):** "The global reads are exactly: slug → tenant id, the active-tenant list, **session by token hash** and **known device by secret hash** (both `[GlobalAggregate]` reads in `SharedKernel.Infrastructure`'s session store, which returns the `tenantId` the HTTP pipeline then sets on `ITenantContext`). The global collections are `tenants`, `sessions`, `known_devices`, `leases`, `migrations`, `data_protection_keys` and MassTransit's outbox and inbox collections. Any other global read is an ADR."

### C-2 — `.Contracts` projects can't compile under AD-2's "follow the diagram and nothing else" (checklist 2, 7)

- **Location:** Design Paradigm diagram (`LC --> SKE` is the only edge out of Contracts); AD-2 ("Project references follow the diagram above and nothing else"); AD-3 (public queries and commands in Contracts; `GetTenantSettingsQuery` returns `LocalizedText`; alert kinds enum in `Audit.Contracts`; `IOwnerAlertWriter.Raise(kind, …)` in `SharedKernel.Application`); Source tree ("IntegrationEvents/ … public query base types") vs the diagram and Source tree putting `ICommand/IQuery` in `SharedKernel.Application`.
- **Problem:** three contradictions the reference fitness test will hit in epic 1–2:
  1. Public queries/commands in Contracts must implement `IQuery`/`ICommand`, which live in `SharedKernel.Application`, but Contracts may reference only `SharedKernel.IntegrationEvents`.
  2. Contracts results need `LocalizedText`/`MobileNumber` (`SharedKernel.Domain`), not reachable either.
  3. `IOwnerAlertWriter` in the kernel takes an alert `kind` whose enum lives in `Audit.Contracts`: the kernel would depend on a module (a cycle).
- **Fix (rule text):**
  - AD-2: "A `.Contracts` project references only `SharedKernel.Contracts` (renamed from `SharedKernel.IntegrationEvents`), which holds `TenantEvent`, `ICommand<T>`, `IQuery<T>`, `LocalizedText` and `MobileNumber`. No other kernel project is visible to Contracts." Update the diagram, Source tree, Conventions row and AD-5's `TenantEvent` location to match.
  - AD-3: "`OwnerAlertKind` is an enum in `SharedKernel.Application` next to `IOwnerAlertWriter`. Adding a kind is a one-line change there, listed in AD-3. `Audit` implements both writers." Remove "Alert kinds are an enum in `Audit.Contracts`".
  - AD-2: state that the fitness test checks **direct** `ProjectReference`s only, so transitive references through the diagram are allowed.

## High

### H-1 — OTP budget and per-number limits: three different status codes (checklist 7; contradicts PRD NFR-6)

- **Location:** AD-12 ("Per-number and daily limits (FR-3's OTP sends per number, FR-32's fraud reports per day, the OTP budget) … answer 429 with their own codes"); AD-13 table (422 includes `otp_budget_exhausted`; 429 "rate limits only"); AD-4 ("`Result.Refused(code)` … 422 (or 429)"; failed send → 503); PRD NFR-6 ("Business limits such as `limit_reached` or the daily OTP budget use their own error codes, **not 429**").
- **Problem:** AD-12 sends the OTP budget to 429, which NFR-6 forbids and AD-13 contradicts. AD-13's status table also has no 503 (`sms_unavailable`, AD-4) and no 500.
- **Fix (rule text):**
  - AD-12: "Per-number and per-day **rate limits** (FR-3 per-number sends, FR-26 per-number checks, FR-32 reports per day) are counted from stored documents and answer 429 with their own code and `Retry-After`. The daily **OTP budget is a business limit**: 422 `otp_budget_exhausted`, never 429 (NFR-6)."
  - AD-13 table: add "503 | A required outside provider is unavailable (`sms_unavailable`) — the only 503" and "500 | Unhandled; body has `code: internal_error` and `traceId` only, never a stack trace or exception text outside `Development`" (this also carries the threat-model mitigation in H-10).

### H-2 — The booking write-path diagram contradicts AD-7 and AD-10 (checklist 7)

- **Location:** Structural Seed, "Booking write path": `U-->>H: re-run whole handler (max 5)` and `R->>R: Notifications consumer: Notification _id = (booking, kind) -> send once`.
- **Problem:** AD-7 now bounds retries by a 3 s time budget, not an attempt count. AD-8 derives the `_id` from (source key, recipient, channel), and AD-10 says the consumer only *creates* a `pending` Notification; the send job sends. An agent copying the picture builds the rejected design.
- **Fix:** change the alt line to `U-->>H: re-run whole handler (fresh loads, jitter, within 3 s)`; replace the last line with `R->>R: Notifications consumer creates Notification (_id = source, recipient, channel), status pending` plus a note "the 1-minute send job sends it (AD-10)".

### H-3 — The notification pipeline has three unreconciled shapes after the R-1 edit (checklist 1, 7)

- **Location:** AD-10 (Event content; Fan-out "stages one send per patient"; Notification sends via the job; "two priority endpoints … its own queue and concurrency"; "The job claims high-priority notifications first"); AD-3 ("never copies another module's mutable display data"); ADR-0010 (OTP in the high-priority endpoint).
- **Problems:**
  1. **When is personal data resolved?** "Consumers resolve personal data at handling time" but the send now happens later, in the job. If the consumer resolves, the Notification stores a mobile number and rendered text (a copy of mutable data, against AD-3, and data the erasure must then clean). If the job resolves, the consumer is a pure record-creator.
  2. **Fan-out "stages one send per patient"**: "stage" means an outbox message, i.e. one broker message per patient (CloudAMQP's 1M messages a month and 10k queued). Under the job model the consumer should create the Notification records directly.
  3. **Priority endpoints**: with sending done by one job, the high/low queues only prioritise record *creation*; the real priority is the job's ordering. ADR-0010 still lists the OTP as high priority, which AD-4 removed.
- **Fix (AD-10 rule text):** "A Notification document holds ids, kind, channel, priority (`high` for booking confirmation and cancellation, `low` for everything else), status and attempt fields only, never a number or rendered text. The **send job** resolves contact data through `GetPatientContactQuery` and renders through `IMessageRenderer` at each attempt; a patient resolved as gone marks the Notification `cancelled` and sends nothing. The fan-out consumer creates one Notification per recipient and channel directly in its own transaction (batched), never one outbox message per patient. The job claims due notifications ordered by priority, then `nextAttemptAt`. The two receive endpoints only keep a fan-out burst from delaying the creation of confirmation records." Propose an ADR-0010 amendment removing the OTP from the high-priority endpoint.

### H-4 — The OTP plaintext rule contradicts the development outbox (checklist 7)

- **Location:** AD-4 ("The plaintext never reaches MongoDB, the outbox or the broker (FR-2)") vs AD-17 ("The development channel … writes OTPs … only to a `dev_outbox` collection"; "the one exemption from FR-6 and from this AD's OTP rule").
- **Problem:** AD-17 exempts itself from FR-6 and from AD-17's log rule, but not from AD-4/FR-2's "never reaches MongoDB". In the demo the development channel is the only channel, so every OTP lands in MongoDB in plaintext. The agent must break one of the two rules.
- **Fix (AD-4 rule text):** "The plaintext never reaches MongoDB, the outbox or the broker, **except the development channel's `dev_outbox` (AD-17), which refuses to start in `Production` (AD-18)**. `dev_outbox` entries expire through a TTL index of 24 hours." Add the same exception to the threat model (H-10) and to the PRD changes list in the correct-course step (FR-2 next to FR-6).

### H-5 — `RecordRefusalCommand` breaks AD-3's write rules and has no anonymous actor (checklist 2, 7)

- **Location:** AD-4 ("`IRefusalRecorder` … dispatches a `RecordRefusalCommand` to Audit in its own transaction … `_id` derived from (tenant, actor, action, UTC minute) … never amplifies writes"); AD-3 ("Every public command is listed here … Current list: `Identity.EnsurePatient`"; audit entries are recorded "through `IAuditWriter.Record(...)` … inside its own transaction"; `actor.kind: staff|patient|system`).
- **Problems:**
  1. A public command not in AD-3's closed list, and a second audit write path besides `IAuditWriter`.
  2. Refusals on anonymous endpoints (rate limits on availability, OTP request, public check) have no tenant session and no actor kind; the dedup key is undefined.
  3. "Never amplifies writes": a duplicate-key insert still costs an Atlas operation, so a flood of 429s still costs one database operation per request.
- **Fix (AD-4 rule text):** "`IRefusalRecorder` (in `SlotBook.Api`) writes through `IAuditWriter` in its own unit of work; it is not a public command. It records only refusals with a signed-in actor (401/403 on signed-in endpoints, 429 on staff and patient partitions). Anonymous refusals are logged and counted as a metric, never audited. It keeps an in-process set of (tenant, actor, action, minute) keys and skips the write when the key is present, so a flood costs at most one write per actor per minute." Then remove `RecordRefusalCommand` from the text.

### H-6 — Several "fitness test" rules have no named test, and some name two tools (checklist 2)

- **Location:** AD-19 ("fitness tests named in AD-2, AD-3, AD-5, AD-12, AD-13, AD-14 and AD-17"); AD-4; AD-14 ("an analyzer **or** fitness test bans them"); AD-16 (".resx completeness architecture test"); AD-17 ("`[LoggerMessage]` only"); Deferred ("the audit repository exposes insert and find only, checked by a fitness test"); ADR-0007 (CA1848 set to *suggestion*).
- **Problems:** AD-4's `private set`, own-dispatcher-only and controller-dispatch-only rules, AD-3's public-command allow-list, AD-16's resx test and the audit insert-and-find-only test are not in AD-19's inventory. AD-14 leaves the tool open. "`[LoggerMessage]` only" is unenforced because ADR-0007 keeps CA1848 as a suggestion. AD-9 requires Guid v7 but nothing bans `Guid.NewGuid()`.
- **Fix (AD-19 rule text):** replace the inventory sentence with a table, one row per rule: rule, tool, test name. Add:
  - "**BannedApiAnalyzers** (`Microsoft.CodeAnalysis.BannedApiAnalyzers`, global package reference) with a root `BannedSymbols.txt` banning `DateTime.Now`, `DateTime.UtcNow`, `DateTime.Today`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow`, `Guid.NewGuid()` and `TimeProvider.System` outside the composition root." (AD-14 then says "BannedApiAnalyzers", not "an analyzer or fitness test".)
  - "CA1848 and CA2254 are raised to warning (ADR-0007 amendment)," so "`[LoggerMessage]` only" fails the build.
  - ArchitectureTests rows for: aggregate properties have no public setter (AD-4); no reference to MediatR or another mediator package (AD-4); controllers depend only on the dispatcher (AD-4); `ICommand` types in `*.Contracts` match AD-3's allow-list (AD-3); audit repository exposes insert and find only (AD-3); `.resx` keys complete in bn/en/ar (AD-16).
  - Add the stack rows (BannedApiAnalyzers version).

### H-7 — The deploy mechanism is undecided and the diagram picks one silently (checklist 6)

- **Location:** AD-18 ("A deploy runs only after `ci-ok` passes on `main`"); deployment diagram (`GH -->|deploy| Web`, `GH -->|deploy| Static`); ADR-0006 ("Each side has its own CI and deploy workflow, filtered on its own path"); `.github/workflows/ci.yml` (no deploy job exists).
- **Problem:** a Render Blueprint auto-deploys on push by default. The diagram says GitHub Actions deploys. Nothing says how the "ci-ok first" gate is enforced, whether a backend-only change redeploys the static site, or in which order the two deploy. AD-13's additive-API rule needs the API live before a frontend that uses a new field.
- **Fix (AD-18 rule text):** "Render auto-deploy is **off** for both services (`autoDeploy: false` in `render.yaml`). A `deploy.yml` workflow runs on `workflow_run` of `ci` succeeding on `main` and calls each service's deploy hook (hook URLs are GitHub secrets), filtered by path as in ADR-0006. When both sides changed, it deploys the API, waits for `/health/ready` on the new version, then deploys the static site. A PR that needs a new API field on the frontend lands the API part first (expand/contract, AD-13)." Update the diagram labels to "deploy hook".

### H-8 — ASP.NET Core Data Protection keys are not persisted (checklist 6, security)

- **Location:** AD-12 (antiforgery, ASP.NET Core Identity primitives); AD-18 (Render's ephemeral filesystem, restarts at any time).
- **Problem:** antiforgery tokens and Identity's token providers use the Data Protection key ring, which by default lives on the local filesystem. On Render free every restart or deploy regenerates the keys, invalidating every XSRF token and in-flight staff token. Two stories would solve it differently or not at all.
- **Fix (AD-12 rule text):** "Data Protection keys are persisted in the global `data_protection_keys` collection through one `IXmlRepository` in `SharedKernel.Infrastructure`, with the application name `SlotBook` set explicitly. Keys at rest are protected by Atlas encryption at rest (NFR-10). Epic 1 proves that an XSRF token survives a service restart." Add the collection to AD-5's global list (C-1).

### H-9 — The public tenant profile can't support AD-14's "render in the tenant's time zone" (checklist 7)

- **Location:** AD-5 (public tenant profile: default language, publication day, home warning, privacy notice, ticket prefix, chat response time) vs AD-14 ("The UI renders in the **tenant's** time zone, never the device's") and FR-43 languages.
- **Problem:** the anonymous Angular app has no source for the IANA zone, the week start day or the enabled languages, so each patient-facing epic will invent its own source (or use the device zone).
- **Fix (AD-5 rule text):** "The public tenant profile returns: tenant display name, IANA time zone, week start day, enabled languages and default language, publication day and time, home warning, privacy notice, ticket prefix and chat response time. Angular loads it once at start-up into a `TenantProfile` signal service in `shared/`, and every date pipe reads the zone from there."

### H-10 — The threat model is out of date and carries mitigations the spine lacks (checklist 9)

- **Location:** `threat-model.md` vs the spine.
- **Mitigations that live only in the threat model** (the agent builds from the spine, so these are unbuilt):
  - Staff lockout 5 failures / 15 min, password minimum 12 with no composition rules, staff session 12 h (developer-confirmed, memlog), plus the patient session 30 days and known device 90 days (memlog D6). → AD-12 must state: "Staff lockout: 5 failures, 15 minutes. Staff password: minimum 12 code points, no composition rules. Session lifetime: staff 12 h absolute, patient 30 days; known-device secret 90 days."
  - "It has no Docker socket **and no deploy secrets**"; "one approval plus `ci-ok` under `protect-main`"; "packages are pinned centrally". → AD-19: "The sandbox has no Docker socket, no deploy hooks, no Render or Atlas credentials and no GitHub token able to merge; `main` is protected by `protect-main` (one approval + `ci-ok`). All NuGet versions are central (`Directory.Packages.props`) and npm uses `npm ci` from the lockfile."
  - "Problem details never include stack traces or personal data outside Development (AD-13)". AD-13 doesn't say so (see H-1 fix).
  - "The OTP request answers the same way whether or not the number is a known patient" cites no FR or AD. → AD-12: "OTP request returns the same status, body and timing class for known and unknown numbers."
  - "Handlers scope 'own day' views to the acting practitioner" is not in the spine. → AD-3's visibility bullet: "practitioner-scoped queries take the practitioner id from the session, never from the request."
  - "TLS on every hop, Atlas encryption at rest; configuration review in epic 1" is not in AD-18. → add to AD-18's epic-1 proofs.
- **Contradictions with later rules:**
  - Masking row: "only the owner's CSV export has full numbers" contradicts NFR-9, AD-3 and the Conventions (the patient's own pages and the audited detail view also do). Fix the row.
  - Section 5 "takes effect within 30 s" vs AD-12 "evicts at once" (30 s is the cache bound). Say "at once; at most 30 s by the cache bound".
  - Section 7 "A 5-second cache on availability" is now "evicted on every Slot/Week commit; 5 s TTL backstop".
- **Threats added by the edit rounds and not modelled:** `X-Forwarded-For` spoofing to dodge per-IP limits (mitigation: AD-12's one trusted hop); the development outbox exposing OTPs to any owner in Demo (mitigation: H-4 + AD-18's Production refusal); CSV formula injection (Conventions' "formula-escaped"); refusal-audit flooding (H-5); Data Protection key loss (H-8); `X-Tenant` on the public tenant profile.
- **Form:** sections 6 and 7 have "Residual risk" instead of "Proven by", against the model's own rule "every threat names the test". Add a "Proven by" column there too. The trust-boundary diagram omits GitHub Actions → Render deploy hooks and the Grafana Synthetics caller.

## Medium

### M-1 — Binds lines and ADR-0012 were not updated when rows were added (checklist 7)

- **AD-6** binds "epics 3, 5, 7, 8", but its table now has StaffRoster (epic 2), OtpBudget (epic 4) and the erasure tombstone (epic 10). AD-6 also says "a new invariant gets a guard document and a row in this table, **in an ADR**, before any code", yet PractitionerWeek, StaffRoster and the tombstone were added without amending ADR-0012, whose table still has five rows. → Binds: "epics 2, 3, 4, 5, 7, 8, 10". Propose an ADR-0012 amendment adding the three rows.
- **AD-8** binds "epics 5, 6, 7": patient identity / `EnsurePatient` is used in epics 4 and 8, `TicketCode.Parse` in 9 (public check), notification ids in 11 and 12. → "epics 4, 5, 6, 7, 8, 9, 11, 12".
- **AD-11** binds omit epics 2 and 4, which own the TTL-expired sessions and OTP challenges named in the rule. → add 2 and 4.

### M-2 — Capability map "Governed by" cells point at the wrong or missing ADs (checklist 7)

| Row | Change |
| --- | --- |
| Clinic cancels, door check … | "AD-17 (masking)" → "AD-3 (visibility and masking), AD-8 (`TicketCode.Parse`)"; AD-17 covers logs |
| Platform slice | add AD-5 (filtered repository, `ITenantContext`), AD-11 (the job runner is listed in the text), AD-12 (ForwardedHeaders, CSP and rewrite proofs), AD-13 (problem details is listed in the text), AD-14 (clock) |
| Patient sign-in | add AD-4 (the OTP SMS exception) and AD-16 (OTP SMS text) |
| Delete my data | add AD-6 (tombstone) and AD-10 (`erasure_incomplete`) |
| Announcements | add AD-3 (audit at confirm), AD-8 (notification `_id`) |
| Official contacts … public ticket check | add AD-8 (`TicketCode.Parse`) |
| Notifications, reminders | add AD-4 (`ISmsChannel`), AD-16 (`IMessageRenderer`) |

### M-3 — AD-7's "one instance per aggregate type per transaction" rule collides with multi-slot writes (checklist 7)

- **Location:** AD-7 ("No transaction spans more than one instance of the same aggregate type, except publication (Week + its Slots)") vs AD-6 (copy last week writes PractitionerWeek and creates many Slots; closing a day or bulk capacity changes touch many Slots).
- **Fix (AD-7 rule text):** "Exceptions are exactly: publication (Week + its Slots) and **copy last week** (one PractitionerWeek per practitioner + the new draft Slots, one transaction per practitioner). Every other multi-slot action (close a day, bulk cancel) is N independent commands with a per-item result list."

### M-4 — MassTransit composition, consumer placement and fault handling are unstated (checklist 1)

- **Location:** AD-2 (MassTransit only in Infrastructure; Api may not depend on MassTransit), AD-3 (each module registers itself through `Add{Module}Module()`), AD-10 (`PatientErased` "in the error queue raises an owner alert").
- **Problem:** MassTransit requires one `AddMassTransit` call with all consumers inside it. Seven modules each calling it, or the host calling it, would both break. Where a consumer lives (MassTransit `IConsumer` is a MassTransit type) and what watches the error queue are left to the stories.
- **Fix (AD-10 rule text):** "`SharedKernel.Infrastructure` exposes one `AddSlotBookMessaging(IConfiguration, params Assembly[] moduleInfrastructureAssemblies)` that the host calls once; it registers every `IConsumer` found in those assemblies. Each consumer lives in its module's Infrastructure, is a thin adapter that dispatches an Application command, and holds no logic. `erasure_incomplete` is raised by a `Fault<PatientErased>` consumer in Audit."

### M-5 — Rate-limit partitions contradict the stored-count rule (checklist 7)

- **Location:** AD-12 ("Partition keys: IP, E.164 number, staff account id, patient id" for the in-memory limiter) vs AD-12 ("Per-number and daily limits … are counted from stored documents, never in the in-memory limiter").
- **Fix:** "The in-memory limiter partitions by IP, staff account id and patient id only. **Every per-number limit** (FR-3, FR-26's 5 per number) is counted from stored documents." Remove E.164 from the partition list.

### M-6 — Test project and folder naming drift from ADR-0006 and the existing code (checklist 5, 7)

- **Location:** AD-19 (`{Module}.UnitTests`, `SlotBook.IntegrationTests`, `SlotBook.ArchitectureTests`); Source tree (`tests/Modules/{Module}.UnitTests/`, `SharedKernel/Domain/`, `Modules/Booking/Domain/`, `Modules/Tenancy/Tenancy/`); Conventions (`SlotBook.SharedKernel.Domain`, `SlotBook.Modules.{Module}.Domain`); ADR-0006 (`src/backend/<Project>/`, `src/backend/tests/<Project>.Tests/`); the repo (`src/backend/tests/SlotBook.Api.Tests/`).
- **Problems:** three naming schemes for test projects; folder names that are not the project names, which ADR-0006 implies; an extra `tests/Modules/` level that AD-19 doesn't show; and the existing `SlotBook.Api.Tests` (WebApplicationFactory health test) is not mentioned, so story 1 will either keep, rename or duplicate it.
- **Fix (AD-19 + Source tree):** "Every project folder is named exactly after its project (`src/backend/Modules/Booking/SlotBook.Modules.Booking.Domain/`). Test projects are `src/backend/tests/SlotBook.Modules.{Module}.UnitTests/`, `SlotBook.SharedKernel.UnitTests/`, `SlotBook.IntegrationTests/`, `SlotBook.ArchitectureTests/`. The existing `SlotBook.Api.Tests` is folded into `SlotBook.IntegrationTests` in epic 1." Propose an ADR-0006 amendment to the `<Project>.Tests` pattern.

### M-7 — The existing frontend health call breaks under AD-15/AD-18 (checklist 5)

- **Location:** `src/frontend/src/app/api-health.ts` calls `GET /health`; `proxy.conf.json` proxies only `/health`; AD-15 ("API calls go to the same origin, under `/api`"); AD-18 (the static site rewrites only `/api/*`); AD-17 (`/health`, `/health/ready` at the root).
- **Problem:** deployed, the Angular health indicator will hit the static site, not the API. The spine neither ratifies nor retires it.
- **Fix (AD-17 rule text):** "The API maps liveness at both `/health` (for Render and the uptime check) and `/api/health` (for the browser). Angular calls only `/api/...`; `proxy.conf.json` proxies `/api`." Epic 1 updates `api-health.ts`.

### M-8 — Operations: alerting and the incident path are missing; one metric source doesn't exist (checklist 6)

- **Location:** AD-11 (uptime check alerts on downtime), AD-18 ("the Grafana dashboard tracks" Atlas transfer and ops/s), AD-17, FR-45 owner alerts (in-app only).
- **Problems:**
  1. No rule says who is alerted, on what, or how. Owner alerts are in-app for clinic staff, not for the operator.
  2. Atlas Free exposes no metrics integration to Grafana, so the dashboard can't "track" Atlas ops/s or transfer unless the API measures them itself.
  3. No region is chosen; a Render web service and an Atlas cluster in different regions add a round trip to every majority-write transaction (NFR-13).
  4. `healthCheckPath: /health/ready` includes RabbitMQ, so a CloudAMQP outage makes Render treat a still-booking API as unhealthy.
- **Fix (AD-18 rule text):** "Grafana alert rules, delivered to the developer by email: uptime check failing 2 runs in a row; 5xx rate > 1% over 5 min; MongoDB operations > 70/s over 1 min (from the API's own MongoDB command counter); outbox backlog > 1,000 or any message in an `_error` queue; CloudAMQP queued messages > 5,000. The incident path is: alert → developer → Render rollback to the previous deploy; data fixes go through a migration, never by hand. Render and Atlas are in the same region, named in `render.yaml`. `healthCheckPath` is `/health/ready` checking MongoDB only; RabbitMQ is reported but not gating."

### M-9 — Epic-1 proofs and their fallbacks are scattered; one fallback costs money (checklist 3, 6, 10)

- **Location:** AD-11 (timers through quiet hours → external cron), AD-12 (client IP → custom domain "before epic 4"; zero CSP violations), AD-18 (rewrite pass-through → custom domain; shared outbox session; Atlas Free transactions), Deferred (Atlas role).
- **Problems:** six proofs, each of which changes other ADs if it fails, live in four places. The custom-domain fallback needs a registered domain, which is a running cost against NFR-16 ($0) unless the developer already owns one: an unrecorded open question.
- **Fix:** add a section "Epic 1 proofs" with one table: proof, AD it gates, fallback, which ADs change if the fallback is used. Add an Open Questions row: "Custom-domain fallback: does the developer own a domain, or is a ~$10/year domain an accepted exception to NFR-16?"

### M-10 — Frontend and tooling divergence points not fixed (checklist 1, 4)

- **Location:** AD-13 ("Angular's API types are generated" — tool unnamed; "CI fails when it's stale" — mechanism unnamed), AD-12 ("a lint rule enforces this"), AD-16 ("stylelint rule"; "CI fails on a key missing"), AD-15 (PWA), Stack.
- **Problems:** the repo has no ESLint, angular-eslint or Stylelint (`package.json`), and the Stack names none; nor a TypeScript generator, `@angular/service-worker`, Playwright, `@axe-core/playwright`, a Web Push (VAPID) server library, or QR generation and camera-scanning libraries. Each epic will pick its own. Lists (staff lists, audit browse, my bookings, inbox) have no pagination convention.
- **Fix:** add Stack rows (version checked at pin time in epic 1) for: `angular-eslint` + ESLint (with `no-restricted-syntax` for `innerHTML` bindings and `bypassSecurityTrust*`), Stylelint (`declaration-property-value-disallowed-list` / logical-properties plugin), the OpenAPI document generated at build by `Microsoft.Extensions.ApiDescription.Server` into `src/backend/SlotBook.Api/openapi.json`, one TypeScript generator (types only, consumed by hand-written `HttpClient` services per feature), `@angular/service-worker`, Playwright + `@axe-core/playwright`, one Web Push library, one QR library each side. AD-13: "Lists use cursor pagination: `?cursor=&limit=` (default 50, max 200), response `{ items, nextCursor }`, sorted by a stable key ending in `_id`."

### M-11 — EventId ranges are required but not assigned (checklist 2)

- **Location:** AD-17 ("an EventId range per module. A fitness test checks EventIds are unique").
- **Fix:** "EventId ranges: SharedKernel and Api 1–999, Tenancy 1000–1999, Identity 2000–2999, Booking 3000–3999, Notifications 4000–4999, ClinicContent 5000–5999, Chat 6000–6999, Audit 7000–7999. The fitness test checks uniqueness and range."

### M-12 — Deferred holds a live rule, misstates the PRD, and one revisit date is inside the build (checklist 3, 4)

- **Audit append-only row:** "until then the audit repository exposes insert and find only, checked by a fitness test" is a rule that binds epic 2 now. Move it into AD-3 and AD-19 (see H-6); leave only the database-role question in Deferred.
- **SMS row:** "default daily OTP budget" is listed as deferred, but FR-3 sets the default (300 a day until Q1). Change to "the provider and its quota (Q1); the OTP budget default stays FR-3's 300."
- **MassTransit row:** "Revisit when: end of 2026" is about 12 weeks from now, inside the 12-epic build. State the decision now: "Stay on 8.5.11 unsupported through the demo; revisit at the first sale." Same for the Stack row's note.

### M-13 — Readability: three ADs bury their key rules (checklist 10)

- **AD-3** is about 65 lines covering ownership, owner alerts, the audit contract, cross-module reads, masking, public commands, events, registration, erasure, settings, reason categories and mobile numbers. Split out **AD-3a "Audit and owner alerts"** (writers, dedup keys, entry contract, refusal recorder from AD-4) and move masking and visibility into AD-12 (enforced by the cross-patient suite). Each split carries its own Binds line, which fixes M-1-style drift.
- **AD-10:** move the notification send pipeline (Notification record, job, priority, back-off, crash rule) to its own AD, since it is epic 6's core rule and is now spread across AD-8, AD-10 and AD-11.
- **AD-12:** split edge protection (rate limits, client IP, CSP and headers, availability cache) from identity and sessions.

## Low

### L-1 — ADRs now stale against the spine (checklist 5)

- **ADR-0010** lists the OTP on the high-priority notification endpoint; AD-4 now sends it directly. Propose an amendment.
- **ADR-0006** says browsers would be added to the sandbox image for Playwright; AD-19 runs Playwright in CI only. Propose an amendment that records "CI only; the sandbox stays browserless".
- **ADR-0007** keeps CA1848 as a suggestion (see H-6).
- **Front matter** `sources` omits ADR-0002 and ADR-0008–0014, which the spine now implements.

### L-2 — Edit scars: two rules merged into one bullet (checklist 10)

These lines join an unrelated rule onto the end of a bullet, so the second rule is easy to miss:

- AD-3: "…→ the tenant's local date. Alert kinds are an enum…" and "…The cross-patient suite asserts this. A module never copies…";
- AD-4: "…against the Atlas budget. Test: 6 parallel wrong OTPs…" (an FR-2 test attached to the refusal recorder);
- AD-12: "…required before epic 4. Limits set in an FR follow the FR…" and "…zero CSP violations. Angular code never calls…".

Fix: split each into its own bullet; move the 6-wrong-OTPs test under the "Refusals with effects" bullet as its proof.

### L-3 — Source tree and paradigm diagram lists lag the rules (checklist 7)

- `SharedKernel/Domain/` lists "AggregateRoot, DomainException, Guid v7 ids" and omits `MobileNumber` and `LocalizedText` (in the diagram).
- `SharedKernel/Application/` lists "audit writer" only; add the owner-alert writer, `ISmsChannel`, `IMessageRenderer`, `Result`, `CommonErrors` and `ITenantContext`.
- `SharedKernel/Infrastructure/` says "rate limiting", but the limiter, `IClientIp` and `IRefusalRecorder` are host concerns (AD-4, AD-12); say where they live.
- ER diagram: `NOTIFICATION` hangs only off `BOOKING`; announcements and chat also create them. Either add the edges or label the diagram "booking path only".
- AD-1 alone carries `[ADOPTED]`; drop the tag or apply it to every AD.

### L-4 — Caching wording conflict (checklist 7)

AD-3 says callers may cache display-name queries "and nothing else", while AD-5 caches slug → tenant id and the active-tenant list, and AD-12 caches sessions and availability. These are owner-side caches, which AD-3 allows but doesn't say. Fix (AD-3): "Owner-side caches are listed in their AD (AD-5 slug and tenant list, AD-12 sessions and availability, Tenancy's settings with eviction on save). A **caller** caches only display-name queries, 60 s."

### L-5 — The `/internal/jobs/{job}` fallback needs a third authentication scheme (checklist 7)

AD-12 allows exactly two schemes or `[AllowAnonymous]`. AD-11's fallback endpoints are "protected" with no scheme. Fix (AD-11): "If the fallback is used, an ADR adds an `internal` scheme (a shared secret header compared in constant time, from a Render secret) and the AD-12 fitness test accepts it only under `/internal/`."

### L-6 — Small gaps in the send job and in unowned collections (checklist 1)

- AD-10's "a record found in `sending` after a crash" needs a test: "a Notification in `sending` whose `claimedAt` is older than 2 minutes is a crashed send."
- `sms_unavailable` is raised by `ISmsChannel` after Identity's commit (AD-4); say "in its own unit of work".
- Unowned collections: `leases`, `migrations` (SharedKernel), `dev_outbox` (Notifications, tenant-owned, TTL), `data_protection_keys` (SharedKernel, global). Add them to the module table or to AD-5's global list (C-1).
- Cookie names aren't set. Fix (AD-12): "`__Host-sb_patient`, `__Host-sb_staff`, `__Host-sb_device`" (the `__Host-` prefix pins `Secure`, `Path=/` and no `Domain`).

### L-7 — Mermaid syntax (checklist 8)

All four spine diagrams (module flowchart, deployment flowchart, ER, booking sequence) and the threat-model flowchart are syntactically valid: quoted labels, the `[( )]` and `[[ ]]` shapes, `<-->`, the `-.runs.->` dotted label, the `alt … end` block and the self-message all parse. Their content problems are listed in C-2, H-2, H-7, L-3 and H-10.

## Checklist coverage

| # | Item | Result |
| --- | --- | --- |
| 1 | Divergence points for epics | Mostly fixed; gaps in M-4, M-10 (tooling, pagination), H-9, L-6 |
| 2 | Enforceable rules | H-5, H-6, M-11 |
| 3 | Deferred can't cause divergence | M-12 (live rule in Deferred), M-9 |
| 4 | Tech currency | Only the MassTransit v8 end-of-support date is close (M-12) |
| 5 | Ratifies the codebase | M-6 (test project naming, `SlotBook.Api.Tests`), M-7 (`/health` from Angular). Otherwise consistent: .NET 10 SDK floor and MTP runner in `global.json`, xUnit v3 + Shouldly, central packages, analyzers per ADR-0007, Angular 22.2 zoneless (no zone.js), Vitest 5 + jsdom, `ng test --watch=false`, `ci-ok` aggregation |
| 6 | Operational envelope | H-7 (deploy), H-8 (Data Protection), M-8 (alerting, incident path, region, readiness); backups and production are properly deferred; migrations (AD-9) and secrets (AD-18) are decided |
| 7 | Internal consistency | C-1, C-2, H-1, H-2, H-3, H-4, M-1, M-2, M-3, M-5, L-3, L-4, L-5 |
| 8 | Mermaid validity | Pass (L-7) |
| 9 | Threat model | H-10 |
| 10 | Readability | M-13, L-2 |
