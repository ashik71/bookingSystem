---
title: "PRD Addendum: SlotBook"
created: 2026-10-09
updated: 2026-10-09
---

# Addendum — SlotBook PRD

Material for architecture and UX that doesn't belong in the PRD itself. None of this
is a decision; the architecture step decides. Each idea comes from the prior private
plan (`input-prior-plan.md`) or from this PRD conversation.

## For architecture

### Capacity: the invariant lives in the data store

The prior plan's design chose an **atomic conditional update** over
`SELECT … FOR UPDATE`:

```sql
UPDATE slots
   SET taken = taken + 1
 WHERE id = @slotId
   AND taken < capacity - reserved
   AND status = 'open'
```

Zero rows affected means full or closed, and the booking fails. The booking insert
shares the transaction. Its reasoning: one round trip, and the invariant can't be
defeated by later code that reads the row outside a lock. A forgotten `FOR UPDATE`
passes tests at low concurrency and overbooks in production. A counter is used
because `COUNT(*)` can't be checked and incremented atomically without a lock, and a
reconciliation job compares the counter with the true count (NFR-2).

With reserved places (FR-12) the predicate gains `reserved`. Practitioner bookings
into reserved places use `taken < capacity` and must decrement `reserved` in the same
statement. The booking limit (FR-17) is a second invariant across slots, keyed by the
number: it needs its own atomic guard, such as a per-patient counter row or a
serialisable check. A conditional update on the slot alone does not cover it. *The
developer's weak spot is SQL locking and isolation, so the architecture document
should work this through explicitly, including which isolation level each path needs
and why.*

Questions for the architecture step: is Slot its own aggregate or part of a Week
aggregate? Is the booking limit a domain invariant on Patient, or a policy checked
with a guard row?

### Ticket code

- 27-symbol alphabet, 4 characters: about 531k codes. Short on purpose, because the
  code is read aloud
- Security doesn't rest on entropy. It rests on uniqueness among active bookings (a
  partial unique index on active rows), staff-only detailed lookup, and a public check
  that needs code **plus** number (C5)
- Generated with a cryptographic RNG, retrying on a unique-violation. A code derived
  from the booking ID (hash or Feistel) was rejected in the prior plan because it
  becomes predictable if the derivation leaks

### Audit log

Append-only, enforced by **removing UPDATE and DELETE rights from the application's
database role**, rather than by convention or triggers. That makes the "no role can
alter it" consequence (FR-42) testable.

### OTP and SMS

- SMS sits behind a channel interface, so the free quota, the fallback channel and a
  paid provider are interchangeable by configuration (brief addendum). Free quotas
  change without notice
- OTPs stored hashed with `expires_at`, plus a cleanup job; no cache is needed at this
  scale
- The known-device marker is client-supplied and therefore spoofable. That is
  accepted, because a bypass only skips an SMS; ticket authenticity rests on the code

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

The prior plan estimated about 125 chats a day. A free real-time tier (for example,
Azure SignalR Free: 20 concurrent connections, 20k messages a day) fits that. **The
choice is wrong at about 10× that load:** move to polling, or to a paid tier.

### Time

Store everything in UTC and render in the tenant's time zone. The publication
boundary, reminders and the cut-off all depend on this, and mixing local and UTC times
is a defect that only shows at the boundaries.

### Text

Names and content in Bangla, English and Arabic: UTF-8 throughout, and unbounded text
columns with limits validated in the application. In Bangla, grapheme clusters make
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
- The prior plan's staff dashboard showed today's bookings, unread chats and the most
  urgent work first

## Rejected alternatives (decided in this PRD run)

| Topic | Rejected | Why |
|---|---|---|
| Booking model (C1) | A request plus staff triage | One clinic's workflow, not a generic one; moves the concurrency problem away from the patient path |
| Capacity (C2) | 1:1 slots only, or windows without practitioners | The slot-with-capacity model covers both clinic styles with one invariant |
| Public check (C5) | Code only, or staff-only | Code only is enumerable with 4 characters; staff-only leaves a scammed patient no way to check |
| Door check | Offline | Real extra scope (two devices checking in offline); deferred |
| Roles | A front-desk role | Not needed for one clinic; practitioners and the owner cover it |
