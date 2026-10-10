---
title: "PRD Addendum: SlotBook"
created: 2026-10-09
updated: 2026-10-10
---

# Addendum — SlotBook PRD

Material for architecture and UX that doesn't belong in the PRD itself. Most of it is
input, not a decision: the architecture step decides. Items marked **Decided** were
settled in this PRD run and are closed. The reasons for rejected options are in
*Rejected alternatives*. Each idea comes from the prior private plan
(`input-prior-plan.md`) or from the PRD conversation. C-numbers (C1, C2, C5) refer to
the conflicts between the brief and the prior plan, recorded in `.memlog.md`. A- and
Q-numbers (A19, Q1, Q4) refer to the PRD's *Assumptions Index* (§10) and *Open
Questions* (§9).

## For architecture

### Open for the architecture step

- Is Slot its own aggregate, and where does the booking limit live? See *Capacity* and
  *Booking limit*
- Which isolation level does each write path need? See *Capacity*
- How is ticket-code uniqueness guarded? See *Ticket code*
- Which SMS provider is used, and what is the default daily OTP budget (Q1)? See
  *SMS*
- How do staff sign in (Q4)? See PRD FR-38
- How is work split between RabbitMQ and Kafka? See *Messaging*
- What is the real-time chat transport? See *Chat at scale*
- What does the threat model cover? See PRD NFR-8

### Sizing (first tenant, from the prior plan)

- About 1,000 patients a week at the busiest, about 140 a day on average, and about
  100 at once at peak (just after publication)
- About 300 tickets a day; the load is highest on publication day and the day after
- About 125 chats a day
- 2–3 staff accounts
- A single-server workload. Overbooking is still a correctness bug at any traffic level

### Capacity: the invariant lives in the data store

*The developer's weak spot is SQL locking and isolation, so the architecture document
should work through the locking explicitly for every path in this section and the next,
including which isolation level each path needs and why.*

`taken` counts every booking that isn't cancelled (`confirmed`, `checked_in` and
`lapsed`), and `reserved` counts only reserved places that no booking uses (PRD §3
*Place*). The prior plan's design chose an **atomic conditional update** over
`SELECT … FOR UPDATE`:

```sql
UPDATE slots
   SET taken = taken + 1
 WHERE id = @slotId
   AND taken < capacity - reserved
   AND status = 'open'
```

Zero rows affected means the slot is full or closed, and the booking fails. The
booking insert shares the transaction. The prior plan's reasoning: one round trip, and
the invariant can't be defeated by later code that reads the row outside a lock. A
forgotten `FOR UPDATE` passes tests at low concurrency and overbooks in production. A
counter is used because `COUNT(*)` can't be checked and incremented atomically without
a lock. A reconciliation job compares the counter with the true count (NFR-2).

With reserved places (FR-12), the predicate subtracts `reserved`, as shown above.
Practitioner bookings into reserved places use `taken < capacity` and must decrement
`reserved` in the same statement.

Question for the architecture step: is Slot its own aggregate or part of a Week
aggregate?

### Booking limit: a second invariant

The booking limit (FR-17) is a second invariant across slots, keyed by the mobile
number. It needs its own atomic guard, such as a per-patient counter row or a
serialisable check. A conditional update on the slot alone does not cover it.

Question for the architecture step: is the booking limit a domain invariant on
Patient, or a policy checked with a guard row?

### Ticket code

- 31-symbol alphabet, 4 characters: about 923k codes. Short on purpose, because the
  code is read aloud
- Security doesn't rest on entropy. It rests on uniqueness, staff-only detailed
  lookup, and a public check that needs the code **plus** the mobile number (C5)
- Uniqueness covers every booking whose slot date is today or later, whatever its
  state (FR-22). That rule depends on the date, so a static partial unique index can't
  express it on its own. The architecture step picks the guard: for example, a check
  inside the confirming transaction plus a unique index on (tenant, code, slot date),
  or a separate code-lease table. At about 300 codes a day, 923k codes are plenty
- Generated with a cryptographic RNG, retrying on a unique violation. A code derived
  from the booking ID (hash or Feistel) was rejected in the prior plan because it
  becomes predictable if the derivation leaks

### Audit log

The audit log is append-only, enforced by **removing UPDATE and DELETE rights from the
application's database role**, rather than by convention or triggers. That makes the
"no role can alter it" consequence (FR-42) testable.

### OTP

- **Decided (A19):** live OTP is SMS only. The brief's email/push fallback survives
  only as the pre-launch development channel (FR-27)
- OTPs are stored hashed, with `expires_at` and a cleanup job; no cache is needed at this
  scale
- The known-device marker (FR-4) is, for example, 256 bits in an HttpOnly cookie,
  hashed like a session token. It must be unforgeable: skipping the OTP grants a
  session for that number, so a forgeable marker would let anyone take over a
  patient's bookings and chat, or trigger erasure of their data, by typing their number
- The daily OTP budget (FR-3) is the real cost cap, because the per-IP limit is loose
  on purpose. It needs an atomic counter per tenant per local day

### SMS

- SMS sits behind the FR-27 channel interface, because free quotas change without notice
- OTPs and booking notifications share one SMS quota. The daily OTP budget (FR-3)
  caps only OTPs. Announcement fan-out (FR-33) is sent at lower priority, so it never
  starves OTPs or booking notifications
- Bangla and Arabic SMS use Unicode encoding: 70 characters per segment, or 67 per
  segment when a message spans several. A notification with practitioner, date, time
  and code is likely 2 segments. The Q1 cost estimate needs the daily segment count at
  first-tenant sizing: roughly 300 confirmations and 300 reminders a day, plus OTPs
  and fan-out

### Messaging: real jobs for the focus area

- Notifications (FR-27 to FR-29): the outbox pattern on booking commit, a queue, and
  idempotent consumers (at most one SMS per event)
- Reminders: scheduled or delayed messages
- Announcement fan-out (FR-33): one event to N patient notifications
- Chat (FR-34, FR-35): real-time delivery to the inbox and to the patient
- Natural place for the RabbitMQ-vs-Kafka question: commands and work queues
  (notifications, reminders) against an event log (booking events for audit,
  reconciliation and future reporting)

### Chat at scale

At about 125 chats a day, a free real-time tier (for example, Azure SignalR Free: 20
concurrent connections, 20k messages a day) fits. **The choice is wrong at about 10×
that load:** move to polling, or to a paid tier.

### Official booking address

**Decided (default):** anti-impersonation (PRD §4.7) needs one recognisable address. Use
the clinic's own domain if it has one (paid by the clinic, not by SlotBook, so NFR-16
holds); otherwise, use a free platform subdomain, accepting that it is easier to imitate.

### Time

Store everything in UTC and render in the tenant's time zone. The publication
boundary, reminders and the cut-off all depend on this, and mixing local and UTC times
is a defect that only shows at the boundaries.

### Text

Names and content are in Bangla, English and Arabic. Use UTF-8 throughout, and
unbounded text columns with limits validated in the application (limits count code
points after NFC normalisation, PRD §0). In Bangla, grapheme clusters make
`varchar(n)` a poor proxy for display length.

## For UX

- **Mood:** patients often arrive anxious. The interface should feel calm and get
  them to a ticket in as few taps as possible (from the prior plan's UI notes)
- **The ticket is the signature element:** the digital form of a paper booking slip,
  with the code large, fixed-width and read-aloud friendly, and the QR code beside it
- **Calendar:** full days greyed out, days with free places marked, so a patient can't
  pick a day with no places
- **The booking form has as few fields as possible:** the attendee name and age, the
  reason category as tap targets rather than a dropdown, and an optional note (FR-16)
- **The OTP screen carries the warning** that the clinic never asks for the code
- **The door check shows a big unambiguous result:** colour plus a word, readable at
  arm's length
- **Staff dashboard (prior plan):** today's bookings, unread chats and the most urgent
  work first

## Rejected alternatives (decided in this PRD run)

| Topic | Rejected | Why |
|---|---|---|
| Booking model (C1) | A request plus staff triage | One clinic's workflow, not a generic one; and it would take the concurrency problem out of the patient path, where the design keeps it on purpose |
| Capacity (C2) | 1:1 slots only, or windows without practitioners | The slot-with-capacity model covers both clinic styles with one invariant |
| Public check (C5) | Code only, or staff-only | Code only is enumerable with 4 characters; staff-only leaves a scammed patient no way to check |
| OTP fallback (A19) | Email OTP, or push, as the live fallback | Email proves control of an address, not a number, so per-number limits become per-email ones, and free email accounts are unlimited. Push can't reach a browser that has never signed in |
| Door check | Offline | Real extra scope (two devices checking in offline); deferred |
| Roles | A front-desk role | Not needed for one clinic; practitioners and the owner cover it |
