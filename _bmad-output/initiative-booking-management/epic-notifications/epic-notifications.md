---
type: epic
title: "Patients are told about every change to their booking"
parent: initiative-booking-management
covers: [FR-27, FR-28, FR-29]
after: []
assignee: ""
risk: high
---

# Patients are told about every change to their booking

## Description

The asynchronous notification pipeline, which is the product's main messaging workload.
Events from the outbox reach a queue. Idempotent consumers send each notification at
most once per event: by SMS, and also by browser push if the patient turned it on.
Reminders go out at the owner's lead time. A booking never waits on, or fails because
of, a notification. A failed send is retried and then raises an owner alert. The live
SMS provider sits behind epic 4's interface and is selected by configuration.

## Outcome

Every confirmation, cancellation and reminder reaches the patient exactly once, and the
booking path never slows down for it. This is the messaging focus area's first real
job.

## Done when

1. Each of these events produces exactly one notification:
   - confirmed (patient booking);
   - cancelled by the patient;
   - cancelled by the clinic, with the reason;
   - a reminder that is due.

   Each is in the patient's stored language and carries the practitioner, date, time and
   ticket code. A cancellation whose notify flag is off sends nothing (FR-28, FR-44
   contract). The clinic-cancel case is proven with a synthetic event until epic 7 adds
   the trigger.
2. A booking or cancellation commits with the channel down. A failed send is retried up
   to 5 more times within 30 minutes, then recorded as failed with an owner alert. A
   redelivered or retried message never produces a duplicate SMS, and a test proves it
   (FR-27, FR-45).
3. Reminders follow FR-29:
   - lead time is off, or 1–168 hours;
   - a changed lead time applies to every unsent reminder;
   - nothing is sent for a booking cancelled or lapsed by then;
   - nothing is sent for a booking made after its reminder time.
4. A patient can turn on browser push from My bookings. Booking notifications and
   reminders then go by push as well as SMS, never instead of it, and push is never
   required to book (FR-27).
5. The live SMS provider passes its contract test, and switching between it and the
   development channel is configuration only (FR-27). An exhausted SMS quota degrades as
   NFR-17 says: known devices still book, and an owner alert says new-device sign-in is
   unavailable. Notifications go out at priorities: OTPs first, then booking
   notifications, then bulk sends.
6. The standard epic checks pass (initiative, *Standard epic checks*). SC-7 covers the
   message broker and the push service.

## Boundaries

The boundary is the patient notification pipeline and the live SMS provider. It does not
include:

- the clinic-cancel trigger (epic 7);
- the practitioner-booking trigger (epic 8);
- announcement fan-out (epic 11) and chat-reply push (epic 12). Both reuse this pipeline.

Staff are never notified (PRD §6). Touch points: the SMS provider, the push service and
the message broker are configured here.

How the work is shared: on FR-27, epic 4 owns the SMS interface and the development
channel, and this epic owns the rest. The first real SMS sends in epic 10, in production.

**Handoffs out:**
- clinic-cancel notifications with the reason go to epic 7;
- practitioner-booking confirmations go to epic 8;
- the notification pipeline with priority classes goes to epic 11;
- push goes to epic 12.

## References

- parent — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md §4.6 (FR-27 to FR-29), FR-45, §2.3 UJ-1, UJ-3, UJ-6
- constraint — the same PRD §5.5 NFR-16, NFR-17
- addendum — _bmad-output/initiative-booking-management/prd-slotbook/addendum.md, sections SMS and Messaging
- architecture — not yet written; sections for the outbox and event contract, the RabbitMQ/Kafka split, the SMS provider (Q1), push, background jobs

## Notes

- Open question: which SMS provider, and is there a free quota for local numbers (Q1)?
  The architecture step answers it. Done when 5 waits on it.
- Unknown: whether a message broker is available on a free tier. The architecture step's
  RabbitMQ/Kafka choice must satisfy SC-7.
- Waits on epic-booking-core because: it consumes the booking events from the outbox.
- Waits on epic-patient-sign-in because: it needs the SMS channel interface, the
  development channel, and the patient's stored language.
- Waits on epic-tenant-and-staff because: it needs the owner-alert API and the reminder
  lead-time setting.
- Waits on epic-schedule-and-publication because: it needs the scheduler for reminders
  and retries.
