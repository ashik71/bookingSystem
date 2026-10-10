---
reviewer: adversary
target: architecture-slotbook.md (draft, 2026-10-10)
lens: "Construct two units one level down that each obey every AD to the letter yet still build incompatibly"
date: 2026-10-10
---

# Adversarial review — SlotBook architecture spine

**Verdict: NOT READY to bind stories.** The spine is strong on the single-module write
path (AD-6/AD-7) but leaves cross-module mutation paths, shared contract shapes and
refusal semantics open. Each finding below names two units an agent could build in
separate PRs, both obeying the ADs as written, that would not fit together. Each ends
with the exact AD text to add or tighten.

Severity: **critical** = data corruption, security hole or a build that cannot be
reconciled without rework of a merged story; **high** = two incompatible shapes very
likely; **medium** = drift likely, fixable; **low** = cosmetic or process.

---

## A-1 — Refusals that must persist something roll back with the DomainException (critical)

**Units.** Epic 4 story "verify OTP" (Identity) vs the AD-4 rule as epic 2/5 stories
implement it in the dispatcher.

**How both obey yet clash.** AD-4: "A transition from the wrong state throws
`DomainException`, which becomes 422." AD-7: the unit of work commits once, at the end of
the handler. A wrong OTP is a business refusal, so the epic 4 agent throws
`DomainException` from `OtpChallenge.Verify()` — and the incremented wrong-attempt
counter is rolled back with it. FR-2's "invalidated after 5 wrong attempts" never fires:
an unlimited brute-force of a 6-digit OTP (bounded only by the per-IP limiter). The same
shape breaks:

- FR-3: a send refused because the daily budget is exhausted must **raise an owner
  alert** (persist) and refuse (422);
- FR-38 / SC-4: a practitioner refused for lack of a role must be **audited** — the
  refusal happens in authorization, before any handler or unit of work exists;
- FR-24: door-check throttling writes one audit entry per refused minute — the refusal
  happens in the rate limiter middleware, before any handler.

A second agent, reading the same ADs, may instead return a result object and commit —
two incompatible refusal styles across the codebase.

**AD text to add (AD-4, new bullet):**
> **Refusals with effects.** A refusal that must persist state (an attempt counter, an
> owner alert, an audit entry) is not a `DomainException`. The aggregate method records
> the effect and returns a refusal result; the handler commits the unit of work and then
> returns `Result.Refused({code})`, which the API maps to 422 (or 429). `DomainException`
> is reserved for refusals that change nothing. Refusals decided outside a handler
> (authorization, rate limiting) are recorded by one host-level `IRefusalRecorder` in
> `SlotBook.Api` that dispatches a `RecordRefusalCommand` to the Audit module in its own
> transaction. Concurrency test: 6 parallel wrong OTPs for one challenge leave it
> invalidated.

---

## A-2 — Active-booking count goes stale on check-in and lapse (critical)

**Units.** Epic 5 story "lapse job" (Booking) and epic 7 story "check-in" (Booking) vs
epic 5 story "patient books" (PatientLedger).

**How both obey yet clash.** The glossary: an active booking is a `confirmed` booking;
check-in and lapse end it. FR-17's "max active bookings per number" therefore drops on
check-in and on lapse. AD-6's table says the PatientLedger is written by "patient book,
cancel of a counted booking" — **not** by check-in or lapse. The epic 5 lapse job and the
epic 7 check-in obey AD-6 exactly, write only the Booking guard document, and leave
`ledger.active` at 1 forever. With the default limit of 1, every patient who attended
once can never book again. The reconciliation job (NFR-2) is specified for slot counts
only, so it never notices.

**AD text to tighten (AD-6 table, PatientLedger row):**
> | Booking limit: active (confirmed) patient bookings, and patient bookings per week, per patient | PatientLedger (one per tenant + patient) | patient book; **every transition out of `confirmed`** of a counted booking (cancel, check-in, lapse, erasure cancel) |

**And add to AD-6:** "A booking stores `countsTowardLimit` (true for patient bookings,
false for practitioner bookings). Every transition method on Booking returns whether the
ledger must change; the handler loads and saves the ledger in the same transaction. The
weekly count is decremented only by cancel (FR-17), never by check-in or lapse. The
reconciliation job (AD-11) checks ledgers as well as slots."

---

## A-3 — Practitioner booking must create a Patient that Booking does not own (critical)

**Units.** Epic 8 story "practitioner books a new number" (Booking) vs epic 4 story
"patient record on first OTP" (Identity).

**How both obey yet clash.** FR-14: "If the number has no patient in the tenant yet, a
patient record is created", in the same action as the booking. AD-3: Identity owns
Patient; Booking "writes only its own collections"; cross-module paths are only **public
queries** and **integration events**. There is no cross-module *command*. The epic 8
agent has three AD-compliant-looking options, all bad:

1. publish `PatientRegistrationRequested` and book against a not-yet-existing
   `patientId` (the booking commits before the patient exists; FR-28 language lookup
   fails);
2. keep a Booking-side "patient stub" keyed by number (a second owner of Patient);
3. dispatch an Identity public "query" that inserts (a query that mutates, violating
   AD-4).

Meanwhile epic 4 creates Patient on OTP verify. With no unique index on the number named
anywhere, a phone call booked by a practitioner and a simultaneous OTP sign-in of the
same number create **two Patients for one number**, splitting the ledger (two limits)
and bookings.

**AD text to add (AD-3, new bullet):**
> **Cross-module commands.** When a use case must create or change another module's
> aggregate in the same transaction, the owner exposes a **public command** in its
> `.Contracts` (`EnsurePatientCommand(mobileNumber) → patientId`). The owner's handler
> runs inside the caller's unit of work (same session, same transaction), and is the only
> code that writes the owner's collections. Public commands are listed in the module
> table; adding one needs the developer's sign-off.

**And add to AD-8:** "Patient identity: unique index `ux_patients_tenantId_mobileNumber`
partial on `erased: false`. `EnsurePatient` is get-or-create and re-runs the handler on
`DuplicateKey` (AD-7)."

---

## A-4 — Jobs and consumers have no request, so they have no tenant (critical)

**Units.** Epic 5 story "lapse job" vs epic 6 story "BookingConfirmed consumer", against
AD-5.

**How both obey yet clash.** AD-5: "The repository and unit of work apply the
**request's** tenant filter to every read and write... Handlers never write a tenant
filter." A hosted-service job (AD-11) and a MassTransit consumer (AD-10) have no request
and no session. The epic 5 agent adds an `IgnoreTenantFilter()` escape hatch to scan
every tenant's confirmed bookings; the epic 6 agent reads `tenantId` from the message
headers and sets an ambient context. Two mechanisms; the escape hatch is the exact hole
AD-5 exists to close, and nothing in the spine says an integration event even carries a
tenant id.

**AD text to add (AD-5):**
> - Tenant context is one ambient `ITenantContext` set by exactly three entry points:
>   the HTTP pipeline (session or `X-Tenant`), the consumer pipeline (from the event's
>   `TenantId`), and the job runner. There is no unfiltered repository API except for
>   `[GlobalAggregate]` documents.
> - Every integration event derives from `TenantEvent { Guid EventId; Guid TenantId;
>   DateTimeOffset OccurredAt }` in `SharedKernel.IntegrationEvents`.
> - A job lists tenants through `Tenancy.Contracts.GetActiveTenantIdsQuery` and runs
>   its per-tenant body once per tenant inside a tenant scope. Leases and migrations are
>   the only `[GlobalAggregate]` writes.
> - A fitness test fails if any code outside `SharedKernel.Infrastructure` constructs an
>   unfiltered collection.

---

## A-5 — Owner alerts are raised by four modules but owned by one, with no write path (critical)

**Units.** Epic 4 "OTP budget exhausted" (Identity), epic 5 "count drift" (Booking job),
epic 6 "send failed after last retry" (Notifications) vs epic 2 "owner alert list"
(Audit).

**How both obey yet clash.** The module table gives OwnerAlert to Audit. AD-3's only
cross-module write exception is **audit entries** via the shared audit writer. Epic 2's
envelope promises an "owner-alert raise API" but the spine does not say what shape it
has. AD-compliant choices diverge:

- epic 4 publishes an integration event `OtpBudgetExhausted` for Audit to consume — but
  AD-10 restricts the broker to "notification sends and announcement fan-out";
- epic 5 treats an alert as "like an audit entry" and writes it through the audit writer
  in its own transaction — a second exception AD-3 does not grant;
- nobody deduplicates: FR-3 refuses **every** send after the budget is used up, so each
  refusal raises another alert; the banner lists hundreds of copies.

FR-45 also requires every alert to be written to the application log, which only works
if there is one write path.

**AD text to add (AD-3, extend the single exception):**
> The exceptions are audit entries **and owner alerts**: any module records them through
> `IAuditWriter.Record(...)` / `IOwnerAlertWriter.Raise(kind, dedupKey)` in
> `SharedKernel.Application`, inside its own transaction. An alert's `_id` is derived
> from `(tenantId, kind, dedupKey)`; a duplicate key is "already raised" (AD-8). Dedup
> keys: OTP budget → the tenant's local date; count drift → slot id + local date;
> notification failure → notification id; SMS provider down → the local date. The writer
> also logs the alert through one `[LoggerMessage]`. Alert kinds are an enum in
> `Audit.Contracts`.

---

## A-6 — "Over the broker" scope contradicts the event-driven integration rule (high)

**Units.** Epic 10 story "delete my data" (Identity publishes `PatientErased`) vs epic 7
/ epic 12 stories that consume it (Booking, Chat, ClinicContent, Notifications).

**How both obey yet clash.** AD-3 says reactions across modules are integration events,
and erasure is one `PatientErased` event. AD-10 says the broker carries "retryable side
effects only, **meaning** notification sends and announcement fan-out". An agent reading
AD-10 literally dispatches `PatientErased` in-process after commit (no outbox, no
retry): if the Chat consumer throws, conversation text survives an erasure the patient
was told is complete. Another agent sends it through the outbox. Both are defensible;
they are not the same system. The same ambiguity hits `BookingConfirmed` consumed by a
future non-notification reader, and the announcement-notify request from ClinicContent.

**AD text to tighten (AD-10, replace the two "Over/Never" bullets):**
> - **Every integration event** leaves through the outbox and is consumed through the
>   inbox. There is no in-process event dispatch across modules.
> - **Never an integration event:** the booking write itself and audit entries/owner
>   alerts (written in the acting transaction, AD-3).
> - Erasure consumers are idempotent and failure-visible: a `PatientErased` message in
>   the error queue raises an owner alert of kind `erasure_incomplete`.

---

## A-7 — Erasure races an in-flight booking and leaves personal data (high)

**Units.** Epic 10 "delete my data" (Identity + Booking consumer) vs epic 5 "patient
books" and epic 8 "practitioner books".

**How both obey yet clash.** Identity commits erasure and revokes sessions; Booking
receives `PatientErased` later and cancels + de-identifies the bookings it finds. A
booking request already past session validation (the session cache allows 30 s, AD-12),
or a practitioner booking for the number keyed by the old patient id, commits **after**
the consumer ran. Its attendee name, age and note survive erasure. FR-44's "first cancels
all active bookings, then removes" is also an ordering across modules that the event
cannot express. Nothing in AD-6 makes erasure a write path of any guard.

**AD text to add (AD-6 table, new row; and AD-3 erasure bullet):**
> | Erasure tombstone | PatientLedger (`erased: true`) | erasure consumer in Booking; every booking path (patient and practitioner) loads and writes the ledger and refuses with `not_found` when erased |
>
> AD-3: "Erasure in Booking cancels through the normal Cancel use case (actor `system`,
> `notify: false`), then de-identifies, in one transaction per booking, after setting the
> tombstone. `EnsurePatient` (A-3) never returns an erased patient id."

(Note: this makes practitioner booking write the ledger even though it is not counted —
which also gives practitioner booking a conflict point with erasure.)

---

## A-8 — Reminders have no owner and two plausible shapes (high)

**Units.** Epic 6 story "reminders" (Notifications) vs epic 5 story "lapse job" /
booking events (Booking).

**How both obey yet clash.** AD-11 lists a "Reminders" job with no module. The
capability map puts FR-29 in Notifications. Reminders need: booking state at send time,
slot start, the tenant's lead time **as it is now** (FR-29: a change applies to unsent
reminders). Notifications cannot read Booking's collections (AD-3). Agent A builds a
Notifications projection from `BookingConfirmed`/`BookingCancelled` with a precomputed
`remindAt` — but no `BookingLapsed` or `BookingCheckedIn` event is declared, and Tenancy
publishes no settings-changed event, so lead-time changes and lapses are missed. Agent B
builds the job in Booking, which scans its own bookings and publishes `ReminderDue`.
Both obey every AD.

**AD text to add (AD-11, reminders row and a rule):**
> | Reminders | 1 min | **Owner: Booking.** The job selects `confirmed` bookings with `slotStartsAt − leadTime(now) ≤ now < slotStartsAt` and `confirmedAt < slotStartsAt − leadTime`, and stages `BookingReminderDue { bookingId }` per booking. Notifications only sends; the Notification `_id` (A-10) makes re-staging harmless. Booking records `reminderStagedAt` on the Booking document (written with the Booking guard). |
>
> Booking reads the lead time through `Tenancy.Contracts.GetTenantSettingsQuery` at each
> run, never from a cached projection.

---

## A-9 — Event payloads: thin ids vs fat personal data (high)

**Units.** Epic 5 story "emit BookingConfirmed" vs epic 6 story "send confirmation SMS".

**How both obey yet clash.** AD-10 fixes naming and one-concrete-type, nothing about
content. FR-28 needs practitioner display name (Identity StaffAccount), date and time
(Booking, rendered in the tenant zone), ticket code with prefix (Booking + Tenancy),
the number and stored language (Identity Patient). Agent A emits ids only and expects the
consumer to query; agent B puts the full number, attendee name and rendered local time
in the event because "the consumer cannot read our collections". Fat events copy
personal data into the outbox collection, the broker and the error queue, where erasure
(FR-44) and the log-scan rule (AD-17) cannot reach them; thin events need consumers to
call three modules' public queries. The cancel reason (FR-20) is personal data but must
reach the SMS.

**AD text to add (AD-10):**
> **Event content.** An integration event carries ids, enums, instants (UTC) and
> immutable non-personal facts only. It never carries a mobile number, attendee name,
> age, note, OTP or message text. Consumers resolve personal data at handling time
> through the owners' public queries (`Identity.Contracts.GetPatientContactQuery`,
> `Booking.Contracts.GetBookingForNotificationQuery`), so an erased patient resolves to
> "gone" and nothing is sent. The cancellation reason is read from the Booking at send
> time. Each event lists its fields in its Contracts XML doc; adding a field is
> additive-only.

---

## A-10 — Notification identity collides for fan-out, channels and OTPs (high)

**Units.** Epic 11 "announcement fan-out" and epic 6 "push in addition to SMS" vs AD-8's
`_id` rule; epic 4 "send OTP" vs epic 6 "OTP on the high priority endpoint".

**How both obey yet clash.**

- AD-8: a non-booking notification's `_id` comes "from the source event id". One
  `AnnouncementPublished` event fans out to N patients: every one of the N notifications
  gets the same `_id`; the second insert is treated as "already done" and N−1 patients
  are silently skipped.
- FR-27: push goes *in addition to* SMS. One `_id` per (booking, kind) means either the
  SMS or the push is deduped away, or a retry of a failed push resends a delivered SMS.
- OTP: AD-10 puts OTP on the Notifications high-priority endpoint, so Identity must
  publish the **plaintext OTP** in an event — persisted in the outbox collection and the
  broker, contradicting FR-2 "stored only as a hash". The FR-27 SMS interface is
  "owned by epic 4" (Identity) while sends live in Notifications; a module cannot
  reference another module's project (AD-3), so the interface location is undecidable.

**AD text to tighten (AD-8 notification bullet; AD-10):**
> - A Notification's `_id` is derived from **(source key, recipient patient id,
>   channel)**, where the source key is (booking id, kind) for booking notifications,
>   the announcement id for fan-out, and the challenge id for an OTP. Each channel is
>   sent and retried independently.
> - The SMS channel interface `ISmsChannel` lives in `SharedKernel.Application`;
>   implementations are registered by Notifications. **The OTP is the one documented
>   exception to AD-4's "no external effect before commit"**: Identity commits the
>   challenge (hash only), then sends the plaintext synchronously through `ISmsChannel`,
>   with the high-priority limiter in process. The plaintext OTP is never written to
>   MongoDB, the outbox or the broker. (Developer decision needed: this trades the
>   outbox's retry for FR-2's "stored only as a hash"; the patient's "resend" is the
>   retry.)

---

## A-11 — "Last enabled owner" and staff-role changes are write skew with no guard (high)

**Units.** Epic 2 story "disable a staff account" vs epic 2 story "change roles" (or two
owners acting at once), both in Identity.

**How both obey yet clash.** FR-38: the last enabled owner can't be disabled. This
invariant spans several StaffAccount documents. Owner A disables owner B while B disables
A: each transaction reads two enabled owners under snapshot isolation, writes a
different StaffAccount, and both commit — zero owners, a tenant nobody can administer.
AD-6's table has no row for it, and AD-6 says a new invariant needs an ADR "before any
code is written", which a story agent cannot do — so it will either skip the guard or
invent one. Separately, AD-12 stores **roles in the session record**; a story that
removes a staff member's owner role updates the StaffAccount only, and the old session
keeps owner rights for up to 12 hours.

**AD text to add (AD-6 table, new row; AD-12 bullet):**
> | At least one enabled owner per tenant | StaffRoster (one per tenant, in Identity: enabled owner count) | disable, re-enable, add or remove the owner role, create owner |
>
> AD-12: "Any change to a staff account's roles or enabled state deletes that account's
> sessions in the same transaction and evicts them from the session cache."

Also add to AD-6: "A story that discovers a cross-document invariant not in this table
stops and comments on the issue; it does not invent a guard."

---

## A-12 — Slot overlap is a cross-document invariant with a partial guard (high)

**Units.** Epic 3 story "owner adds a slot to a published week" (FR-10) vs epic 3 story
"practitioner adds a draft slot" / "copy last week".

**How both obey yet clash.** FR-7: two slots of one practitioner never overlap. Two
inserts of overlapping slots write two *different* Slot documents — classic write skew.
AD-6 accidentally covers it for draft weeks (adding a draft slot writes the Week), but
FR-10's "add a slot to a published week" is not a listed Week write path, so two owners
adding overlapping slots to a published week both commit. The draft-week guard also
makes every practitioner's draft edits conflict with each other (one Week document per
tenant per week), which an agent may "fix" by dropping the Week write.

**AD text to tighten (AD-6 table):**
> | No overlapping slots per practitioner | PractitionerWeek (one per tenant + practitioner + week start date) | add slot (draft or published), edit draft slot times, copy last week |
>
> | Publication | Week, plus each of its Slots | publish; **add a slot to a published week**; add or edit a draft slot |

---

## A-13 — Week identity and slot time shape are unstated (high)

**Units.** Epic 3 story "draft week + copy last week" vs epic 5 story "weekly booking
limit on the PatientLedger" vs epic 3 frontend "draft editor".

**How both obey yet clash.** AD-9 makes every id a Guid v7; AD-14 says a slot stores
its local date. Nothing says how a **week** is identified or how slot times are shaped.

- Epic 3 creates `Week { _id: Guid }` on the first slot added; two practitioners adding
  the first slot at once create **two Week documents for one week** (no unique key), and
  publication publishes only one of them.
- Epic 5 keys the ledger's per-week counts by `weekId` (Guid); another agent keys them by
  the ISO week number or by the slot's local date — FR-17 counts per *published week*,
  whose start day is a tenant setting, not ISO Monday.
- Slot times: agent A stores `startsAt`/`endsAt` UTC and copies by adding 7×24 h (wrong
  across a DST change); agent B stores local `HH:mm` and computes UTC on read. The
  frontend may send `2026-10-12T09:00:00+06:00` computed with the **device** zone, though
  AD-14 says only that the UI *renders* in the tenant zone.

**AD text to add (AD-14):**
> - **Week identity** is the natural key `(tenantId, weekStartDate)` where
>   `weekStartDate` is the local `yyyy-MM-dd` of the tenant's week start day; unique
>   index `ux_weeks_tenantId_weekStartDate`. Creating a week is get-or-create, retried on
>   `DuplicateKey`. Every per-week count (ledger) is keyed by `weekStartDate`.
> - **Slot shape:** `localDate`, `localStart` and `localEnd` (`HH:mm`) are the source of
>   truth; `startsAt` / `endsAt` (UTC) are computed in the domain from the tenant zone at
>   creation and never edited. Copy-last-week copies the local fields and recomputes UTC.
> - **API input** for staff-entered schedule data is local date + local time without
>   offset; the server applies the tenant zone. Instants from the server carry an offset.

---

## A-14 — Idempotency key contradicts AD-8 (high)

**Units.** Epic 5 story "booking rules: the idempotency key" (FR-15, Done-when 3) vs
AD-8 as written.

**How both obey yet clash.** FR-15 requires a client idempotency key: same patient +
slot + key returns the original result; same key with a different slot is refused.
AD-8: "There is **no separate idempotency key**. Identity is the dedup token." The epic 5
agent either violates the AD or drops a PRD requirement. If it builds an
`idempotency_keys` collection, it is a second guard document outside AD-6; if it relies
on `(tenantId, slotId, patientId)` uniqueness, a retry after a successful booking gets
`already_booked` instead of "the original result", and the different-slot case is
undetectable.

**AD text to tighten (AD-8):**
> - The booking's client key is stored **on the Booking** as `requestKey`, with a
>   partial unique index `ux_bookings_tenantId_patientId_requestKey`. A booking command
>   first looks up the patient's booking by `requestKey`: same slot → return it (200,
>   same body); different slot → 400 `validation_failed`. A `DuplicateKey` on this index
>   re-runs the handler, which then finds the original. There is no separate idempotency
>   collection.

---

## A-15 — Home warning, privacy notice and other per-language texts have two owners (high)

**Units.** Epic 2 story "owner-editable settings page" (Tenancy, owns "the whole
settings page" incl. home warning text and privacy notice, FR-39) vs epic 9 story "home
warning written per language" (ClinicContent, FR-31) and epic 12 "expected response
time" (Chat).

**How both obey yet clash.** The FR-39 table lists the home warning text as a tenant
setting; epic 2's envelope says it owns the whole settings page; the module table gives
Tenancy "settings FR-39". Epic 9's Done-when 4 builds the same text again in
ClinicContent next to official contacts. Two collections hold the warning; the home page
reads one, the settings page writes the other. The same pattern threatens reason
categories (Tenancy) vs "in use can be retired, not deleted" (needs Booking data, see
A-16).

**AD text to add (module table + AD-3):**
> Tenancy owns **every FR-39 setting, including per-language texts** (home warning,
> privacy notice, reason categories, default language, chat response time). Other
> modules read them only through `Tenancy.Contracts.GetTenantSettingsQuery` (one shape,
> one cached read per request). ClinicContent owns official contacts, announcements and
> fraud reports only. Per-language text everywhere has one shape:
> `LocalizedText { Dictionary<lang, string> }` in `SharedKernel.Domain`, with the
> FR-39 fallback to the default language applied by the reader in one shared helper.

---

## A-16 — Reason categories: "in use" is decided in a module that doesn't own usage (medium)

**Units.** Epic 2 "delete a reason category" (Tenancy) vs epic 5 "book with a category"
(Booking).

**How both obey yet clash.** Tenancy must refuse deleting a category in use. It asks
Booking via a public query ("any booking with category X?") — then a booking with X
commits after the query and before the delete: cross-module write skew, no guard
possible across modules. Separately, Booking may store the category's text (then
renaming changes nothing, and erasure keeps text that is not personal) or its id (then a
deleted category leaves dangling ids in de-identified records).

**AD text to add (AD-3 or FR-39 note in the spine):**
> Reason categories are **retire-only** in v1: there is no delete, so no cross-module
> "in use" check exists (FR-39's "can't delete a category in use" holds trivially; flag
> to the PM that unused categories can't be deleted either). A booking stores `reasonCategoryId`; Tenancy keeps every category
> forever with a `retired` flag. Booking validates "exists and not retired" through the
> settings query at booking time.

---

## A-17 — Audit entry shape and action names are left to each epic (medium)

**Units.** Epic 2 story "audit log browse + filter by action" vs epic 3/5/7/8 stories that
each write their own entries (SC-3).

**How both obey yet clash.** AD-3 names a shared audit writer; FR-42 lists the fields;
nothing fixes their types. Epic 2 builds a filter over a known action list before any
action exists; epic 3 writes `"PublishWeek"`, epic 7 writes `"booking.cancelled"`, epic
8 writes an enum value. The actor has no shape for a job (scheduled publication runs
"exactly as if an owner had selected Publish" — owner or system?), for a patient
(erasure), or for the maintenance identity (purge). The free-form `details` payload is
where personal data leaks (FR-42 forbids it).

**AD text to add (AD-3 or a new AD-20 "Audit entry contract"):**
> `AuditEntry { tenantId, at (UTC), actor { kind: staff|patient|system, id?, roles[] },
> action, target { type, id }, details }`. `action` is `"{module}.{noun}.{verb}"`
> (`booking.booking.cancelled`), declared as constants in `{Module}AuditActions` in
> `.Contracts`; the Audit module's browse filter lists them by reflection over the
> Contracts. `details` is a typed record per action containing ids, enums, numbers and
> instants only; a fitness test rejects `string` properties in details types except
> enums/ids. A scheduled publication is audited with actor = the owner who scheduled it
> and `details.trigger = scheduled`.

---

## A-18 — Mobile-number normalization exists in two modules (medium)

**Units.** Epic 4 "number normalization" (Identity, FR-1) vs epic 9 "public ticket check
by code + number" and epic 7 "patient lookup by number" (Booking).

**How both obey yet clash.** The public check (in Booking) must match the given number
to the booking's patient, and limit "5 per number per hour". Booking cannot reference
Identity's project, so the epic 9 agent writes its own normalizer (Bangla digits,
national prefix, country from Tenancy). The two diverge on an edge (leading `0`, `+880`
vs `00880`), and the per-number limiter and the match key on different strings. Also,
Booking stores only `patientId`, so the match needs an Identity query from number to id.

**AD text to add (Consistency Conventions, Mobile numbers row):**
> One `MobileNumber` value object in `SharedKernel.Domain` does all parsing and
> normalization (digits, country default from the tenant). Every module and every
> per-number rate-limit key uses `MobileNumber.E164`. Number → patient id resolution is
> `Identity.Contracts.FindPatientByNumberQuery`.

---

## A-19 — Ticket code stored with or without the prefix (medium)

**Units.** Epic 5 "issue ticket code" vs epic 7 "door-check code matching" and epic 9
"public check".

**How both obey yet clash.** AD-8 defines the unique index on `ticketCode` but not its
content. Epic 5 stores `"SB-4K7P"`; epic 7 strips prefixes before matching and misses
every code issued before a prefix change (FR-22 says matching ignores the prefix);
another agent stores the 4 characters and renders the prefix — current or at-issue?

**AD text to tighten (AD-8 ticket bullet):**
> `ticketCode` stores exactly the 4 canonical upper-case characters. The prefix is never
> stored on the booking; display uses the tenant's **current** prefix. One
> `TicketCode.Parse(input)` in `Booking.Domain` (case, spaces, optional prefix and hyphen,
> Bangla/Arabic-Indic digits) is the only parser, reused by the door check and the public
> check.

---

## A-20 — Bulk cancel: one transaction or N? (medium)

**Units.** Epic 7 "bulk cancel after closing" vs AD-7's "one handler, one transaction,
whole-handler retry".

**How both obey yet clash.** All affected bookings share one Slot guard document. One
handler cancelling 30 bookings in one transaction obeys AD-7 but any concurrent check-in
aborts the whole batch, and after 5 retries all 30 fail with 409 — FR-11 wants per-booking
results. Another agent loops 30 commands — also AD-compliant, with a different result
shape and audit pattern.

**AD text to add (AD-7):**
> A bulk action is N independent commands, one transaction each, dispatched sequentially
> by an application service that is not itself a handler; it returns a per-item result
> list `{ id, outcome: done|refused, code }`. No transaction spans more than one
> aggregate instance of the same type, except publication (Week + its Slots).

---

## A-21 — Announcement recipients are resolved by whom, and when? (medium)

**Units.** Epic 11 "announcement notify count shown before confirm" (ClinicContent) vs
epic 11 / epic 6 "fan-out" (Notifications).

**How both obey yet clash.** The count comes from Booking data (active bookings, chosen
dates). ClinicContent queries Booking for the preview count; at confirm it publishes an
event; Notifications re-queries Booking at fan-out time and gets a different set — or
ClinicContent puts N patient ids into one event (large message, personal linkage in the
broker). FR-33 "each notify send is audited": one entry per patient (by Notifications)
or one per announcement (by ClinicContent)?

**AD text to add (AD-10 fan-out bullet):**
> Fan-out: ClinicContent stages `AnnouncementNotifyRequested { announcementId,
> filter }`; the Notifications consumer resolves recipients once through
> `Booking.Contracts.GetPatientsWithActiveBookingsQuery(filter)` and stages one send
> per patient (`_id` per A-10). The preview count uses the same query. One audit entry
> per announcement notify, written by ClinicContent at confirm, with the recipient count
> in `details`.

---

## A-22 — Cross-module display data on hot read paths (medium)

**Units.** Epic 3 "anonymous availability" and epic 7 "door check result" (Booking) vs
epic 2 "staff display names" (Identity).

**How both obey yet clash.** Availability and door check show practitioner display
names, and the door check shows "who checked in". Booking stores `practitionerId`.
Agent A dispatches an Identity public query per slot (N+1 queries against the 100 ops/s
Atlas budget); agent B denormalizes the display name onto the Slot (then a renamed
practitioner shows two names). Neither is forbidden.

**AD text to add (AD-3):**
> Public queries that serve lists are batch-shaped (`GetStaffDisplayNamesQuery(ids[])`)
> and may be cached in process for 60 s by the caller. Modules never copy another
> module's mutable display data into their own documents; immutable facts (ids, slot
> dates) may be copied.

---

## A-23 — Error codes, 404 vs 422, and `validation_failed` (low)

**Units.** Epic 5 "book" (FR-15 lists `not_found` and `validation_failed` as refusal
reasons) vs AD-13's status table (404 for not found, 400 for validation).

**How both obey yet clash.** One agent returns 422 `not_found` for an unpublished slot
(FR-15 says it is one of the "exactly one reason" refusals); another returns 404. Codes
are "defined once, in a `{Module}Errors` class", so two modules may each define
`not_found` or `rate_limited` with different meanings for the same `errors.{code}`
translation key.

**AD text to tighten (AD-13):**
> `not_found` is always 404 and `validation_failed` always 400, even when the PRD lists
> them among refusal reasons. Error codes are globally unique; shared codes (`not_found`,
> `validation_failed`, `concurrency_conflict`, `rate_limited`) live in
> `SharedKernel.Application.CommonErrors`; a fitness test fails on a duplicate code
> across `{Module}Errors` classes.

---

## A-24 — Committed OpenAPI file and generated types under parallel PRs (low)

**Units.** Any two stories in flight at once that both add endpoints.

**How both obey yet clash.** AD-13 commits the OpenAPI document and generates Angular
types from it; AD-15 puts them in `shared/`. Two PRs each regenerate the single file;
the second merge has a conflict the agent resolves by hand-editing generated output.

**AD text to add (AD-13):**
> The generated OpenAPI document and Angular types are never hand-edited; on a merge
> conflict the agent regenerates both from the merged code. Generated types live in one
> folder, `shared/api/generated/`, and patient/staff areas import only from it.

---

## Summary table

| # | Hole | Severity | AD to change |
| --- | --- | --- | --- |
| A-1 | Refusals with effects roll back (OTP brute-force, unaudited refusals) | critical | AD-4 |
| A-2 | Active count stale on check-in/lapse | critical | AD-6 |
| A-3 | Practitioner booking creates Patient across modules; duplicate patients | critical | AD-3, AD-8 |
| A-4 | No tenant context in jobs/consumers | critical | AD-5 |
| A-5 | Owner alerts: no write path, no dedup | critical | AD-3 |
| A-6 | Broker scope contradicts event integration | high | AD-10 |
| A-7 | Erasure races in-flight bookings | high | AD-6, AD-3 |
| A-8 | Reminder job ownership | high | AD-11 |
| A-9 | Event payload content | high | AD-10 |
| A-10 | Notification `_id` collisions; plaintext OTP in outbox | high | AD-8, AD-10 |
| A-11 | Last-owner write skew; stale session roles | high | AD-6, AD-12 |
| A-12 | Slot overlap guard incomplete | high | AD-6 |
| A-13 | Week identity and slot time shape | high | AD-14 |
| A-14 | Idempotency key vs AD-8 | high | AD-8 |
| A-15 | Two owners of per-language settings texts | high | module table, AD-3 |
| A-16 | Reason category "in use" across modules | medium | AD-3 |
| A-17 | Audit entry shape and action names | medium | AD-3 / new AD |
| A-18 | Two number normalizers | medium | Conventions |
| A-19 | Ticket code storage | medium | AD-8 |
| A-20 | Bulk cancel transaction shape | medium | AD-7 |
| A-21 | Announcement recipient resolution | medium | AD-10 |
| A-22 | Cross-module display data | medium | AD-3 |
| A-23 | Error code statuses and uniqueness | low | AD-13 |
| A-24 | Generated OpenAPI under parallel PRs | low | AD-13 |
