---
title: SlotBook
status: final
created: 2026-10-09
updated: 2026-10-10
---

# PRD: SlotBook

## 0. Document Purpose

This PRD is for the developer (product owner and reviewer) and for the downstream
BMAD steps: UX (`bmad-ux`), architecture (`bmad-architecture`) and the epic and story
breakdown (`bmad-ticket`). The sandbox agent builds against the acceptance criteria
that come out of it, so every FR states **testable consequences**.

It builds on the final brief (`../brief-slotbook/brief-slotbook.md`) and its addendum,
and on a generic extract of an earlier private plan (`input-prior-plan.md`). Where the
brief and the prior plan disagreed, the developer decided; the decisions are in
`.memlog.md`.

How to read it:

- **§3 Glossary** fixes the vocabulary. Every other section uses those terms exactly.
- **§4 Features** groups the FRs. FR numbers are global and stable.
- **§5 Cross-cutting NFRs** covers concurrency, security, privacy, tenancy and cost.
- **§6 to §9** cover non-goals, MVP scope, success metrics and open questions.
- **§10** lists every assumption the draft made and how the developer resolved it.
- §3 also fixes the conventions for text limits, hours and days.
- Technical "how" lives in `addendum.md`, not here.

## 1. Vision

SlotBook is a web booking system for clinics that take appointments by phone today.
A patient opens the clinic's booking site on a phone, signs in with a mobile number
and a one-time code, chooses a practitioner and a free slot, and gets a ticket. The
ticket is a QR code plus a short code that the practitioner checks at the door. The
owner publishes each week's slots in one batch and sets the rules on who may book and
how often.

The product removes five problems of phone booking: missed bookings that can't be
fitted in again, slot hoarding and resale, false claims at the door, impersonation of
the clinic, and the phone bottleneck. Every control in it maps to one of these.

It runs one real clinic first. Its data is tenant-aware from day one, so more clinics
can be added later without a rewrite. It must cost nothing to run before a sale. It
is also the developer's vehicle for Azure, messaging, multi-tenancy, security, DDD and
agentic delivery. Each of those has a real job in this product, not a token one.

## 2. Target User

### 2.1 Jobs To Be Done

- **Patient:** *"Get a slot with the practitioner I need without phoning during
  opening hours, and know for certain that my booking is real."* Also: *"Know that
  the person asking me for money really is the clinic."*
- **Practitioner:** *"Run my day: see who is coming, let in only genuine ticket
  holders, and fit in a patient who missed their slot without breaking the week."*
- **Owner:** *"Publish next week in minutes, stop one person from taking or reselling
  many slots, and keep the clinic's official voice in one place."*

### 2.2 Non-Users (v1)

- **Platform operator.** No onboarding console; the first clinic is set up by hand.
- **Front-desk staff.** There is no separate role. Practitioners and owners do the
  door check and answer chat.
- **Patients without a mobile number.** Identity is the mobile number.

### 2.3 Key User Journeys

The protagonists are invented and generic.

- **UJ-1. Nadia, an owner, publishes next week on publication day.**
  Nadia signs in on her laptop on the clinic's publication day. The draft for next
  week is already there: during the week each practitioner entered their own slots,
  and Nadia selected **Copy last week**, which filled the days that were still empty.
  She checks each
  practitioner's day, raises one walk-in window from 10 to 12 places, closes Friday
  for a holiday, and selects **Publish**. Patients now see next week. **Edge case:**
  a practitioner falls ill on Wednesday after publication. Nadia closes that
  practitioner's Wednesday slots, and the system lists the 9 affected bookings. She
  cancels them with a reason, and each patient is notified.

- **UJ-2. Dr. Karim, a practitioner, holds back places for the week.**
  Karim opens his published week on his phone. His 09:00–11:00 window has capacity
  12, and he reserves 3 places in it. Patients see 9 bookable places and 3 marked
  *reserved*. On Wednesday he books a missed patient into one of them (UJ-5). On
  Thursday the other 2 are still reserved, so he releases them and they become
  bookable at once.

- **UJ-3. Amina, a patient, books from her phone.**
  Amina taps the clinic's official booking link. The home page shows the clinic's
  announcements and a warning: *the clinic only takes money through the numbers listed
  on this site*. She selects **Book**, picks Dr. Karim, sees next week with full days
  greyed out, and picks Tuesday 09:00–11:00 (4 places left). She enters her name, her
  age, a reason category (*first visit*) and her mobile number. A 6-digit code arrives
  by SMS, she enters it, and her ticket appears: practitioner, date, slot, sequence
  number 07, code `SB-4K7P`, and a QR code. An SMS confirms it. **Edge case:** the
  last place goes to someone else while she is entering the code. She is told the slot
  is now full and is shown the next free slots of the same practitioner; no ticket is
  created.

- **UJ-4. Dr. Karim checks tickets at the door.** A patient shows a QR code. Karim
  scans it with his phone's browser, and the screen shows a large green **Genuine**:
  the attendee's name and age, the masked mobile number, his slot and sequence number
  07. He asks for the last three digits of the number, they match, and he selects
  **Check in**. The next person shows a screenshot of a code; it comes back red,
  **Already checked in at 09:14**. A third code comes back **Not genuine**.

- **UJ-5. Dr. Karim rebooks a patient who missed their slot.**
  A patient missed Monday because of a bus strike. Karim opens his Wednesday slot,
  which still has a reserved place, enters the patient's mobile number, the attendee's
  name and age and a reason category, and confirms. The booking takes the reserved
  place. The patient
  receives the normal ticket and SMS. This booking does not count towards the
  patient's booking limit.

- **UJ-6. Amina cancels and rebooks.**
  Amina can't make Tuesday. She opens **My bookings**, cancels (the cut-off is 2 hours
  before the slot, and she is well before it), and her place is free again at once.
  She books Wednesday instead. Her limit of 1 active booking is not exceeded, because
  the Tuesday booking is no longer active.

- **UJ-7. Rahim checks a ticket someone sold him.**
  Rahim was phoned by someone "from the clinic" who took money and sent him a code.
  He opens the clinic's public **Check a ticket** page and enters the code and his
  mobile number. The result is **Not genuine**. The page shows the official contacts
  and a **Report fraud** button. He signs in with his number and a code, and files a
  report with the caller's number.

- **UJ-8. Amina asks the clinic a question.** Before booking, Amina opens **Chat**,
  signed in with her number, and asks whether she should bring her previous
  prescriptions. Dr. Karim answers from the clinic inbox that afternoon. Amina sees
  the reply the next time she opens the site, or gets a push notification if she
  turned push on.

## 3. Glossary

Downstream documents must use these terms exactly.

**Conventions.** Text limits count Unicode code points after NFC normalization. "Per
hour" means a rolling 60 minutes. "A day" means a calendar day in the tenant's time
zone.

- **Tenant** — one clinic's isolated space in SlotBook: its practitioners, schedule,
  rules, patients' bookings and content. In v1 there is exactly one live tenant. The
  word **clinic** is used for the same thing in user-facing text.
- **Owner** — a user who administers a tenant: practitioners, publication, booking
  rules, official contacts and announcements. One or more per tenant.
- **Practitioner** — a user who sees patients in a tenant. Practitioners manage their
  own slots in a draft week and their own reserved places, do door checks, and answer
  chat. An owner may also be a practitioner.
- **Patient** — a person identified by a **verified mobile number** within a tenant.
  There is no username, password or email, and a patient has no name of their own:
  names belong to attendees.
- **Mobile number** — stored in one canonical form (E.164). Two spellings of the same
  number are the same patient. A **masked** mobile number shows only its last 3
  digits.
- **OTP** — a 6-digit one-time code sent by SMS to prove that a patient controls a
  mobile number.
- **Known device** — a browser that has completed OTP for a given mobile number before.
  On a known device, entering that same number starts a patient session without an
  OTP (FR-4).
- **Week** — the 7 days of one schedule, in the tenant's time zone. An owner chooses
  the start day when the tenant is set up, and it can't change in v1.
- **Draft week** — a week that has not started and is not yet published; patients
  can't see it. It comes into existence when the first slot is added or copied into
  it.
- **Publication** — the act by which an owner makes a draft week visible and
  bookable. A week is published at most once, and after that it is a **published week**.
- **Slot** — a time range (start, end) on one date for one practitioner, with a
  **capacity**. Capacity 1 is a classic appointment; capacity N is a walk-in window.
- **Capacity** — the number of **places** in a slot; an integer ≥ 1.
- **Place** — one unit of a slot's capacity. A place is exactly one of **free**,
  **reserved** or **taken**. *Taken* places are those held by bookings that are not
  `cancelled`. *Reserved* places are held back and used by no booking. *Free* =
  capacity − reserved − taken.
- **Reserved place** — a place that the slot's practitioner, or an owner, holds back
  (FR-12). Patients see it but can't book it. Only a practitioner booking can use it,
  and it then becomes taken.
- **Release** — turning reserved places back into free places.
- **Closed slot** — a slot that accepts no new bookings. Its existing bookings stay
  until someone cancels them.
- **Booking** — one patient holding one place in one slot. States: `confirmed`,
  `checked_in`, `cancelled`, `lapsed`; plus `awaiting_payment`, which exists in the
  state model but is unused in v1.
- **Active booking** — a booking in state `confirmed`. A booking stops being active
  when it is checked in, cancelled or lapsed (FR-21).
- **Attendee** — the person who will attend a booking: the patient themselves, or a
  family member they book for. A booking has exactly one attendee (name and age).
- **Reason category** — one entry from a tenant-defined list (for example *first
  visit*, *follow-up*) that a booking must carry.
- **Patient booking / Practitioner booking** — a booking made by the patient, or by a
  practitioner or an owner on the patient's behalf (FR-14).
- **Ticket** — the patient's proof of a booking: attendee name, practitioner, date,
  slot time, **sequence number**, **ticket code** and a QR code of the code.
- **Sequence number** — the booking's number in its slot's booking order (FR-15).
- **Ticket code** — a short code that the system generates, for example `SB-4K7P`. It
  is unique among a tenant's bookings whose slot date is today or later (FR-22).
- **Door check** — a practitioner or owner verifying a ticket at the clinic and
  checking the patient in.
- **Public ticket check** — the unauthenticated page where anyone enters a ticket code
  and a mobile number and learns whether the ticket is genuine.
- **Booking limit** — the owner-set maximums per mobile number: active bookings, and
  patient bookings per week.
- **Cancellation cut-off** — the number of hours before a slot starts after which a
  patient can no longer cancel online.
- **Official contacts** — the tenant's list of real phone, payment and social
  numbers and links, with the date an owner last confirmed it.
- **Fraud report** — a report that a patient files about someone impersonating the
  clinic.
- **Announcement** — a notice from an owner shown on the tenant's home page.
- **Conversation** — the single chat thread between one patient and the clinic.
  Messages are sent by the patient, a practitioner or an owner.
- **Clinic inbox** — the shared list of all conversations, which practitioners and
  owners answer (FR-35).
- **Notification** — a message the system sends to a patient (confirmation, reminder,
  cancellation, chat reply, announcement) by SMS or push.
- **Owner alert** — a notice to the tenant's owners about a problem that needs their
  attention (FR-45). It is shown in the admin pages and is not a notification.
- **Audit entry** — an immutable record of a significant action: who, what, which
  record, and when.

## 4. Features

### 4.1 Patient sign-in (mobile number and OTP)

**Description:** A patient proves that they control a mobile number with an OTP, and
then holds a session. Signing in again on a known device with the same number skips
the OTP, because SMS costs money. The OTP screen warns that the clinic never asks for the
code. Realizes UJ-3, UJ-6, UJ-8.

#### FR-1: Mobile number entry and normalization

A patient can enter a mobile number in the local format, with or without separators,
or in international format.

**Consequences (testable):**
- Every accepted input is stored in E.164 form. `01712-345678` and `01712345678`
  resolve to the same patient. Digits typed in Bangla or Arabic-Indic script are
  accepted (FR-43).
- The country is a setup-only tenant setting (FR-39), and v1 accepts only that
  country's mobile numbers.
- Input that is not a valid mobile number for the tenant's country is rejected with a
  message, in the active language, that names the mobile number field. No OTP is
  sent.
- Within a tenant, one mobile number maps to at most one patient.

#### FR-2: OTP issue and verification

A patient can request an OTP for a mobile number and enter it to start a session.

**Consequences (testable):**
- The OTP is 6 numeric digits, generated with a cryptographic RNG and stored only as
  a hash.
- An OTP expires **5 minutes** after it is sent, and it is single-use.
- A wrong, expired or already-used code is rejected. The response never reveals the
  correct code, and it says whether the patient should retry or ask for a new code.
- After **5 wrong attempts** for one OTP, that OTP is invalidated.
- A successful verification creates a patient session (FR-5) scoped to that patient
  and that tenant, and marks the browser as a known device for that number (FR-4).
- Before the first OTP is requested, the page links to the clinic's privacy notice
  (FR-39) and states, in the active language, that continuing accepts it.

#### FR-3: OTP send limits

The system limits how often OTPs are sent, to cap SMS cost and to stop harassment of
the person who holds the number.

**Consequences (testable):**
- A resend for the same number within **60 seconds** of the last send is refused, and
  the response states the remaining wait in seconds.
- A 6th send for the same number within one rolling hour is refused, with a message to
  contact the clinic through its official contacts.
- Sends are also limited to **100 per client IP per hour**. This limit only stops
  obvious abuse. It is loose on purpose, because mobile carriers put many phones
  behind one public IP, and many patients sign in at once just after publication.
- The tenant has a **daily OTP budget** (FR-39): the maximum number of OTP sends per
  calendar day in the tenant's time zone. The default is 300 sends a day until Q1 sets
  a better one.
- Once the budget is used up, further sends that day are refused with a message to
  contact the clinic through its official contacts, an owner alert is raised (FR-45),
  and known devices can still sign in and book (FR-4, NFR-17).
- Every refused send is logged with the number (masked) and the IP.

#### FR-4: Known-device skip

A patient on a known device does not need a new OTP to sign in again with the same
mobile number.

**Consequences (testable):**
- The device and the number must both match an earlier successful verification;
  either one alone requires an OTP.
- The known-device marker is a random secret that the server issues at a successful
  verification. It is bound to that mobile number and stored only as a hash. A guessed
  marker, or a marker issued for another number, never matches, and a test proves both.
- When both match, the patient gets a normal patient session (FR-5) without an OTP.
- A known-device marker lasts **90 days** from its last use. It is forgotten when the
  patient signs out on that browser, and it can be revoked by the patient (sign out
  everywhere, FR-5) or by an owner (FR-41).
- Skipping the OTP never skips the booking limit (FR-17) or any other rule, and never
  satisfies an action that requires a fresh OTP (FR-44).

#### FR-5: Patient session

A signed-in patient can see their bookings, tickets and conversation without signing
in again on every visit.

**Consequences (testable):**
- A patient session gives access only to that patient's own data in that tenant.
  Asking for another patient's booking returns *not found*, never *forbidden*, so that
  existence isn't disclosed.
- A session lasts **30 days** from sign-in and doesn't extend with use.
- Signing out ends the session on that browser and forgets the browser as a known
  device.
- **Sign out everywhere** ends every session and forgets every known device of that
  number. An owner can do the same for a patient (FR-41).
- A patient session never satisfies a practitioner or owner endpoint.

#### FR-6: The clinic never asks for the code

**Consequences (testable):**
- No practitioner or owner screen, export or API response contains an OTP, active or
  past.
- The OTP entry screen shows, in the active language, a warning that the clinic will
  never ask for this code by phone or message.

---

### 4.2 Weekly schedule and publication

**Description:** Practitioners enter their own slots into next week's draft. An owner
reviews the whole draft and publishes it on the tenant's publication day. Once a week
is published, patients can book any future slot in it that has a free place. After
publication an owner can still add slots, change capacity and close slots, within
rules that protect existing bookings. Realizes UJ-1.

#### FR-7: Draft week

An owner or practitioner can prepare a week before it is published.

**Consequences (testable):**
- Any week that has not started can be a draft week.
- A practitioner can create, edit and delete **their own** slots in a draft week. An
  owner can do the same for **any** practitioner.
- A slot has a date, a start time, an end time (later than the start) and a capacity
  (an integer ≥ 1). Two slots of the same practitioner can't overlap in time. A slot
  ending at 11:00 and one starting at 11:00 don't overlap.
- **Copy last week:** a practitioner can copy their own slots from the previous week
  into a draft week, and an owner can do it for one practitioner or for all. Each
  copied slot keeps its local start and end times and its capacity, on the date 7 days
  later. Reserved places and closed status aren't copied. A copied slot that would
  overlap an existing slot of the same practitioner is skipped, and the skipped slots
  are listed.
- Patients can't see draft weeks through any page or API.

#### FR-8: Publication

An owner can publish a draft week.

**Consequences (testable):**
- Publishing makes every slot in the week visible and bookable at once, in a single
  step: patients never see a half-published week.
- An owner can also **schedule** a publication for a date and time. Within 60 seconds
  of that time, the week publishes exactly as if an owner had selected **Publish**. Until
  then the schedule can be changed or cancelled, and the week's slots can still be
  edited.
- The tenant's publication day is a setting, shown to patients as *"Next week opens on
  ⟨day⟩ at ⟨time⟩"*. It is informational, independent of any scheduled publication,
  and does not block publishing on another day.
- A published week can't go back to draft.

#### FR-9: Patient view of availability

Anyone, without signing in, can see which practitioners, dates and slots can be
booked.

**Consequences (testable):**
- Only published weeks are shown. A date earlier than today, or a slot that has
  already started, is never shown as bookable. Practitioners appear by their display
  name (FR-38).
- A closed slot is shown as *closed* and can't be selected.
- For each slot the patient sees the time and the number of **free** places. Reserved
  places are shown as *reserved* and can't be selected. A slot with 0 free places is
  shown as *full* and can't be selected.
- A date on which the chosen practitioner has no slot with a free place is shown as
  unavailable.
- Free-place counts are correct at the moment of reading. A count that is stale by the
  time of booking is caught by FR-15, not prevented here.

#### FR-10: Changing a published week

An owner can change a published week without breaking existing bookings.

**Consequences (testable):**
- An owner can add a slot to a published week; it is bookable at once.
- An owner can raise a slot's capacity at any time.
- An owner can lower a slot's capacity only down to *taken + reserved* places. A lower
  value is refused, and the response states how many places are taken and reserved.
- Apart from closing (FR-11) and reserving or releasing places (FR-12, FR-13), no
  other change is allowed: a published slot's date and times can't be edited, and it
  can't be deleted. To move a slot, close it and add another.
- Practitioners can't change a published week's slots or capacity. They manage
  reserved places only (§4.3).

#### FR-11: Closing a slot

An owner can close a slot, for example for illness or a holiday, and can close a whole
date for one practitioner or for the clinic.

**Consequences (testable):**
- A closed slot accepts no new booking of any kind.
- Closing a whole date closes every slot that exists on that date at that moment. A
  slot added to the date later is open.
- Closing never cancels bookings on its own. It lists the affected bookings (attendee
  name, masked number, sequence number) for an owner to handle.
- An owner can cancel the affected bookings in bulk with one reason. Each cancelled
  booking produces its own notification (FR-28) and its own audit entry. The result
  is reported per booking: a booking that was checked in or cancelled in the meantime
  is reported as not cancelled, and the bulk action never reports full success when
  any booking failed.
- A closed slot can be reopened; its places then follow the normal rules again.

---

### 4.3 Reserved places

**Description:** A practitioner holds back some places so that a patient who missed a
booking can be fitted in later in the week. Patients see that the places exist but
can't book them. The practitioner can release reserved places to everyone at any
time. Realizes UJ-2, UJ-5.

#### FR-12: Reserve places

A practitioner can reserve places in their own slots, and an owner in any slot, in a
draft or a published week.

**Consequences (testable):**
- The number reserved can't exceed the slot's free places at that moment. A request
  for more is refused and states how many are free.
- Reserving is atomic against patient bookings: if a patient takes the last free place
  at the same moment, exactly one of the two operations succeeds.
- A slot that has already started can't have places reserved.

#### FR-13: Release reserved places

A practitioner can release reserved places in their own slots, and an owner in any
slot.

**Consequences (testable):**
- Released places become free and bookable by patients at once.
- The number released can't exceed the slot's reserved places; a request for more is
  refused.
- Releasing is atomic against bookings, as reserving is (FR-12).

#### FR-14: Book a patient into a place

A practitioner can book a patient into their own slot, and an owner into any slot,
by entering the patient's mobile number and the booking details of FR-16 (the
attendee's name and age, and a reason category).

**Consequences (testable):**
- If the number has no patient in the tenant yet, a patient record is created. No OTP
  is required, because the practitioner vouches for the patient.
- The booking is a **practitioner booking**: it gets a normal ticket and a
  confirmation notification (FR-28).
- The booking takes a reserved place if the slot has one, otherwise a free place, and
  records which kind it took (FR-18 depends on it).
- It is refused, with a specific reason, if the week isn't published (`not_found`),
  the slot is closed (`slot_closed`), the slot has ended (`slot_ended`), the slot has
  no reserved and no free place (`slot_full`), or the patient already has a booking
  in the slot that isn't cancelled (`already_booked`).
- Unlike a patient booking, a practitioner booking is allowed after the slot starts
  and until the slot ends, so a late walk-in can be fitted in.
- A practitioner booking never counts towards the patient's booking limit (FR-17) and
  is never refused by it.
- **Reserved means reserved:** a patient booking request has no field that can choose
  a reserved place. In a slot with 0 free places and at least 1 reserved place, a
  patient booking fails with `slot_full`, whether it comes from a page or from a
  direct API call. A test proves it.

---

### 4.4 Booking and cancellation

**Description:** A signed-in patient books one free place in a slot directly; there is
no approval step. The guarantee at the centre of the product is that a slot never has
more taken places than its capacity, even when many patients try for the last place at
the same moment. Patients cancel online up to the cancellation cut-off; to change a
booking, they cancel and book again. Realizes UJ-3, UJ-6.

#### FR-15: Book a free place

A signed-in patient can book one free place in a published, open slot that has not
started.

**Consequences (testable):**
- The booking succeeds only if, at commit time, the slot is open, has not started and
  has at least 1 free place (capacity − reserved − taken). Otherwise it fails with
  exactly one reason: `not_found` (unknown or unpublished slot), `slot_closed`,
  `slot_started`, `slot_full`, `already_booked`, `limit_reached` or
  `validation_failed` (FR-16). No booking is created and no place is used.
- **No overbooking:** taken places never exceed capacity, and patient bookings never
  use a reserved place, under any concurrency. Load test: 200 concurrent patients
  competing for 10 places end with exactly 10 bookings, and 190 receive `slot_full`.
- In any one slot, a patient can hold at most one booking that isn't cancelled
  (`already_booked`). After cancelling, they can book the same slot again.
- On success the booking is `confirmed`, gets a sequence number and a ticket code
  (FR-22), and a confirmation notification goes out (FR-28).
- The first booking in a slot gets sequence number 1; each later booking, patient or
  practitioner, gets the highest number already issued in that slot plus 1. Numbers
  are never reused, so a cancellation leaves a gap, and a number can be higher than
  the capacity. Concurrent bookings in one slot never get the same number; a test
  proves it.
- When a booking fails with `slot_full`, the patient is shown up to 3 bookable slots
  (open, with a free place) of the same practitioner: the next ones by start time
  after the requested slot, within published weeks.
- Retrying the same booking request (same patient, same slot, same idempotency key)
  never creates a second booking and returns the original result. The same key with
  a different slot is refused (`validation_failed`).

#### FR-16: Booking details

A patient gives the details the clinic needs when booking.

**Consequences (testable):**
- Required:
  - the **attendee**'s name (1–100 characters, any script);
  - the attendee's age (an integer, 0–120);
  - a **reason category** chosen from the tenant's list (FR-39);
  - the mobile number (taken from the session).
- Optional: a short note to the practitioner (up to 500 characters).
- A missing or invalid required field is rejected, with a message in the active
  language that names the field. No booking is created, and no place is used.
- The last attendee name and age used are pre-filled next time, and the patient can
  edit them.
- A patient may book for someone else, such as a child or a parent, by entering that
  person as the attendee. The booking still belongs to the mobile number, and the
  booking limit (FR-17) counts per number, not per attendee.
- The reason category and the note are visible only to the patient, the practitioner
  of the slot and owners. They never appear on the ticket, in the door-check result,
  on the public ticket check or in notifications.
- The attendee's name and age are visible to the patient and to staff (door check,
  booking lists, patient lookup). The ticket shows the name only. Neither appears on
  the public ticket check or in notifications.

#### FR-17: Booking limit

The tenant's booking limit is enforced on every patient booking.

**Consequences (testable):**
- An owner sets **max active bookings per number** (default 1) and **max patient
  bookings per number per week** (default 2), and can change both at any time
  (FR-39). Each is a whole number, 1 or more. The weekly limit counts the patient
  bookings in the published week that the slot belongs to.
- A patient booking that would exceed either limit fails with `limit_reached` and
  names the limit.
- **Limits hold under concurrency:** a test that fires 20 parallel booking requests
  from one number into different slots, with a limit of 1, ends with exactly 1
  booking.
- Practitioner bookings (FR-14) are not counted and are not blocked by the limit.
- Cancelled bookings never count towards either limit, whoever cancelled them.

#### FR-18: Patient cancels

A patient can cancel their own `confirmed` booking before the cancellation cut-off.

**Consequences (testable):**
- An owner sets the cut-off in whole hours before slot start (default 2, range 0–168).
  Cancelling online is allowed while the current time is earlier than slot start
  minus the cut-off.
- Before the cut-off: the booking becomes `cancelled`, the place becomes free at once
  (or reserved again, if the booking took a reserved place, FR-14), the ticket code
  stops validating, and a cancellation notification goes out.
- After the cut-off, cancelling online is refused, with a message to contact the
  clinic.
- A patient can't cancel another patient's booking; the response is *not found*.

#### FR-19: My bookings

A signed-in patient can see their bookings.

**Consequences (testable):**
- Upcoming bookings come first, soonest slot first. Then past bookings (`checked_in`,
  `cancelled`, `lapsed`, and any booking whose slot has ended), latest slot first.
- Each booking shows the practitioner, date, slot time, state, and a timeline of state
  changes with timestamps.
- Selecting an active booking opens its ticket (FR-23).

#### FR-20: Clinic cancels

A practitioner can cancel bookings in their own slots, and an owner any booking, with a
reason.

**Consequences (testable):**
- A reason is required (free text, 1–200 characters).
- The place becomes free, or reserved again, as in FR-18, the patient is notified with
  the reason, and an audit entry records who cancelled. The reason text is stored on
  the booking, not in the audit entry (FR-42).
- The cut-off doesn't apply to clinic cancellations.

#### FR-21: Booking lifecycle

**Consequences (testable):**
- States and allowed transitions: `confirmed → checked_in`,
  `confirmed → cancelled` and `confirmed → lapsed`. No other transition is allowed,
  and `checked_in`, `cancelled` and `lapsed` are final.
- A booking still `confirmed` when its slot ends becomes `lapsed` automatically within
  15 minutes. From the slot's end it can't be checked in, and the door check reports
  it as *Lapsed*, whether or not the automatic change to `lapsed` has run yet. Lapsing
  has no penalty in v1 (no no-show tracking).
- `awaiting_payment` exists in the state model but no v1 path reaches it.
- All transitions go through one domain rule, so an endpoint can't bypass them.
- A transition applies only if the booking is still `confirmed` at commit. A cancel
  racing a check-in ends with exactly one of them; a test proves it.

---

### 4.5 Ticket and verification

**Description:** Every confirmed booking has a ticket with a short code that the system
generates and a QR code of that code. At the door, a practitioner scans or types the
code and sees whether the ticket is genuine, belongs to this patient, is for today and
is unused; then they check the patient in. Anyone can also check a ticket on a public
page, but only by giving the code **and** the mobile number it was booked with.
Realizes UJ-4, UJ-7.

#### FR-22: Ticket code

**Consequences (testable):**
- The code is issued once, at confirmation, by the system. No user input can set it.
- The format is a prefix plus 4 characters from the alphabet `A–Z` and `2–9`,
  excluding `O`, `I` and `L` (31 symbols). The prefix is a tenant setting of 1–4
  letters followed by a hyphen, default `SB-`. It is for display only: matching uses
  the 4 characters, so changing the prefix never invalidates a ticket.
- Among a tenant's bookings whose slot date is today or later, no two share a code,
  whatever their state. A code can be issued again only after the slot dates of all
  bookings holding it have passed. Concurrent confirmations get different codes.
- The code is matched case-insensitively. Surrounding spaces, the prefix and the
  hyphen are optional when typed.

#### FR-23: Ticket presentation

A patient can open the ticket of an active booking.

**Consequences (testable):**
- The ticket shows the attendee name, practitioner, date, slot time, sequence number,
  the code in a fixed-width font, and a QR code that encodes the code.
- The ticket states, in the active language, that a ticket is valid only if it checks
  as genuine with the clinic.
- The ticket page works on a phone browser at 360 px width, and its QR code scans from
  a phone screen at normal brightness.

#### FR-24: Door check

A practitioner or owner can verify a ticket by scanning its QR code with the device
camera in the browser, or by typing or pasting the code.

**Consequences (testable):**
- The result is exactly one of the following, each shown with the details listed:
  - **Genuine:** the attendee name and age, the masked mobile number, the
    practitioner, the slot time and the sequence number.
  - **Not today:** the booking's date.
  - **Already checked in:** the time and who checked the patient in.
  - **Cancelled:** the time it was cancelled.
  - **Lapsed.**
  - **Not genuine**, when no booking in this tenant has that code.
- **Lookup:** the booking with that code whose slot date is today or later; if there
  is none, the booking with that code whose slot date is latest.
- The result for the booking found is decided in this order: *Cancelled*, *Already
  checked in*, *Lapsed* (state `lapsed`, or `confirmed` after the slot has ended),
  *Not today*, otherwise *Genuine*.
- A practitioner can verify tickets for any practitioner in the tenant, because the
  door is shared.
- Lookups are limited to 60 per minute per account. Further lookups are refused with
  HTTP 429, and one audit entry is written per account for each minute in which
  lookups were refused.
- The camera scan works in current mobile Chrome and Safari. Typing is always
  available as a fallback. The code field submits on Enter, so a hardware scanner
  that types into the field works with no extra setup.

#### FR-25: Check in

From a **Genuine** result, the practitioner or owner can check the patient in.

**Consequences (testable):**
- The booking becomes `checked_in`, with the time and the acting user recorded.
- **A ticket is single-use:** checking in the same ticket twice, including from two
  devices at the same moment, succeeds once; the other attempt gets *Already checked
  in*.
- **Forged tickets fail:** forged, reused, wrong-day and cancelled codes are all
  rejected (an automated test covers each case).

#### FR-26: Public ticket check

Anyone, without signing in, can check whether a ticket is genuine.

**Consequences (testable):**
- The page takes a ticket code and a mobile number. It answers only **Genuine for
  ⟨date⟩**, **Cancelled** or **Not genuine**. It never shows a name, practitioner,
  time or sequence number.
- It uses the FR-24 lookup, and the booking found must belong to the given number.
  *Genuine for ⟨date⟩* means a `confirmed` booking whose slot hasn't ended;
  *Cancelled* means a cancelled booking. Everything else is *Not genuine*, including
  a ticket that was already checked in or has lapsed, because it can't be used any
  more.
- A code with the wrong mobile number gives the same answer as an unknown code (**Not
  genuine**), so the code space can't be probed without the number.
- Limited to 10 checks per IP per hour and 5 per mobile number per hour. When a
  limit is hit, the page says *Too many checks, try later*; this never reveals
  whether the ticket exists.
- A **Not genuine** result shows the official contacts (FR-30) and a link to file a
  fraud report (FR-32).

---

### 4.6 Notifications

**Description:** Patients are told about everything that changes their booking.
Notifications and OTPs go by SMS: from a provider's free quota if one exists,
otherwise from a paid provider once real patients use the product. A patient can also
turn on browser push. Before launch, a development channel stands in for SMS. There is
no email in v1. Sending is asynchronous; a booking never waits on, or fails because
of, a notification. Realizes UJ-1, UJ-3, UJ-5, UJ-6, UJ-8.

#### FR-27: Notification channel

**Consequences (testable):**
- SMS sits behind one interface. Switching between a free quota, a paid provider and
  the development channel is configuration and needs no code change.
- The development channel shows OTPs and notifications to the developer instead of
  sending them. It can't be enabled in production: the application refuses to start
  with it configured there, and a test proves it.
- A booking or cancellation commits even if the channel is down. A failed send is
  retried up to 5 more times within 30 minutes. If the last attempt also fails, the
  send is recorded as failed and raises an owner alert (FR-45).
- Each notification is sent at most once per event: a retry or a redelivered message
  never produces a duplicate SMS.
- A patient can turn on browser push from their bookings page. Push is never required
  to book.
- Booking notifications, reminders and announcements (FR-28, FR-29, FR-33) go by push
  in addition to SMS, never instead of it. Chat replies go by push only (FR-37).
- SlotBook never asks a patient for an email address.

#### FR-28: Booking notifications

**Consequences (testable):**
- A notification goes out for each of these events: a booking is confirmed (patient or
  practitioner booking), the patient cancels, the clinic cancels (with the reason),
  and a reminder is due (FR-29).
- Each notification contains the practitioner, the date, the slot time and the ticket
  code. Cancellations also contain the reason, if one was given.
- Notifications are in the patient's last-used language, which is stored on the
  patient. A patient who has never visited (for example, one created by a practitioner
  booking) gets the tenant's default language.

#### FR-29: Reminder

**Consequences (testable):**
- A reminder goes out for each active booking at a lead time before the slot starts.
  An owner sets the lead time: off, or 1–168 whole hours (default 24).
- A changed lead time applies to every reminder not yet sent.
- No reminder is sent for a booking that is cancelled or lapsed by the scheduled time.
- A booking made after its reminder time gets no reminder.

---

### 4.7 Anti-impersonation

**Description:** The product can't stop someone impersonating the clinic outside the
system, but it gives patients a way to check. There is one official booking address,
one official contacts list, a permanent warning, the public ticket check (FR-26) and a
way to report fraud. Realizes UJ-3, UJ-7.

#### FR-30: Official contacts

An owner can maintain the tenant's official contacts, and anyone can view them without
signing in.

**Consequences (testable):**
- Each entry has a kind (phone, payment number, messaging, social, web), a label (with
  text per language) and a value. An entry that is a link opens in a new tab.
- The page shows the date an owner last confirmed the list, and an owner can
  re-confirm it without changes. The page states that no number or account outside
  the list belongs to the clinic.
- Only owners can change the list. Every change is an audit entry.

#### FR-31: Home warning

**Consequences (testable):**
- The tenant's home page and the ticket page always show a warning that the clinic
  takes money only through the official contacts, with a link to them. An owner
  writes it (with text per language) or leaves the default text. It can't be
  dismissed.

#### FR-32: Fraud report

A signed-in patient can report an impersonation attempt.

**Consequences (testable):**
- The report form has: what happened (required, up to 1,000 characters), the number or
  link the impersonator used (optional), and the date (default today). There are no
  file uploads in v1.
- Reports require a patient session (FR-5), and each number can file at most 3 a
  day.
- Owners see the reports in a list, newest first, and can mark each one *reviewed*.
  The patient sees their own reports and their status.

---

### 4.8 Announcements

**Description:** An owner posts notices such as closures, new rules or a change of
hours. They appear on the tenant's home page, and they can be sent to patients who
have active bookings on the affected days. Realizes UJ-1, UJ-3.

#### FR-33: Post an announcement

An owner can write, publish, edit and archive announcements.

**Consequences (testable):**
- An announcement has a title (up to 100 characters), a body (up to 2,000 characters),
  optional text per language, an optional single image (up to 1 MB, JPEG/PNG/WebP), an
  optional *show until* date, and a pinned flag.
- Published announcements appear on the home page, pinned first and then newest
  first. Archived ones don't appear, and an announcement with a *show until* date
  appears through the end of that date in the tenant's time zone.
- An owner can choose to **notify** when publishing: either all patients with an active
  booking, or those with an active booking on chosen dates. Each patient receives at
  most one notification per announcement.
- Without the notify option, no notification is sent.
- Before an owner confirms **notify**, the page shows how many patients will be
  notified.
- Announcement notifications are sent after OTPs and booking notifications and never
  delay them.

---

### 4.9 Chat

**Description:** Each patient has one conversation with the clinic. Practitioners and
owners answer from the shared clinic inbox, with canned replies for common
questions. It is asynchronous messaging, not a live-support promise: the patient sees
an expected response time. Realizes UJ-8.

#### FR-34: Patient conversation

A signed-in patient can send messages to the clinic and read the replies.

**Consequences (testable):**
- Messages are text only, with no attachments, up to 1,000 characters each.
- The patient sees all their messages and the clinic's replies in order, with times,
  and which practitioner or owner sent each reply.
- The conversation page shows the expected response time that an owner sets, in
  whole hours (for example *"We usually reply within 24 hours"*), and a note to use
  the official phone number in an emergency.
- A patient can send at most 20 messages an hour.
- A new reply appears within 10 seconds, without a page reload, while the
  conversation is open.

#### FR-35: Clinic inbox

Practitioners and owners can read and answer all patients' conversations.

**Consequences (testable):**
- A conversation is *unanswered* when its last message is from the patient. The
  inbox lists unanswered conversations first, then the rest by latest activity, and
  shows the number of unanswered conversations. This state is shared by all staff.
- Opening a conversation shows the masked number, the attendee name of the patient's
  latest booking (if any) and their active bookings. Reason category and note follow
  FR-16: they appear only for bookings in the viewer's own slots, or to owners.
- A practitioner or owner can mark a conversation *done*. A new patient message
  reopens it.
- New patient messages appear in the inbox within 10 seconds, without a page reload.

#### FR-36: Canned replies

An owner can manage canned replies, and practitioners and owners can insert one into a
reply and edit it before sending.

**Consequences (testable):**
- A canned reply has a title and a body, with text per language.

#### FR-37: Reply notification

**Consequences (testable):**
- When the clinic replies and the patient isn't viewing the conversation, the patient
  gets a push notification if they turned push on, otherwise none. Chat replies
  never go by SMS.
- The tenant's home page shows a badge when there's an unread reply.

---

### 4.10 Clinic administration

**Description:** Owners set up practitioners and the tenant's rules, and see simple
booking lists. Every significant action is recorded in an immutable audit log,
because investigating a fraud claim afterwards is one of the product's reasons to
exist. Realizes UJ-1, UJ-4.

#### FR-38: Staff accounts

An owner can create, disable and re-enable practitioner and owner accounts.

**Consequences (testable):**
- Practitioners and owners sign in with credentials separate from patient sign-in.
  The method is decided in architecture.
- *Owner* and *practitioner* are roles on a staff account, and one account may hold
  both. Each staff account has a display name, which patients see for practitioners
  (FR-9).
- Every staff account, owner and practitioner alike, signs in with multi-factor
  authentication. A sign-in without the second factor is refused.
- A disabled account can't sign in, and its open sessions stop working within 1
  minute.
- A practitioner can't manage accounts, settings, official contacts, announcements or
  canned replies. An attempt is refused and recorded.
- The last enabled owner of a tenant can't be disabled.
- Disabling a practitioner never changes their slots or bookings. An owner closes or
  cancels them as in FR-11.

#### FR-39: Tenant settings

An owner can set the tenant's rules.

**Consequences (testable):**
- **Setup-only** settings are chosen when the tenant is set up and can't change in
  v1: the country (FR-1), the time zone and the week start day (§3).
- **Owner-editable** settings:

  | Setting | Allowed values | Default | Behaviour in |
  |---|---|---|---|
  | Reason category list | At least one entry; a category in use can be retired but not deleted | — | FR-16 |
  | Max active bookings per number | A whole number, 1 or more | 1 | FR-17 |
  | Max patient bookings per number per week | A whole number, 1 or more | 2 | FR-17 |
  | Cancellation cut-off | 0–168 whole hours | 2 | FR-18 |
  | Reminder lead time | Off, or 1–168 whole hours | 24 | FR-29 |
  | Publication day and time shown to patients | A weekday and a time | — | FR-8 |
  | Ticket code prefix | 1–4 letters | `SB-` | FR-22 |
  | Expected chat response time | 1–168 whole hours | — | FR-34 |
  | Home warning text | Text per language | Default text | FR-31 |
  | Privacy notice text | Text per language | — | FR-2 |
  | Daily OTP budget | A whole number, 1 or more | 300 | FR-3 |
  | Default language | `bn`, `en` or `ar` | — | FR-43 |

- Reason categories, the home warning, the privacy notice, official-contact labels,
  announcements and canned replies have a version per language. When the active
  language has no version, the tenant's default-language version is shown.
- A changed limit or cut-off applies to new actions from the moment it is saved. It
  never cancels or changes existing bookings.

#### FR-40: Booking lists

Practitioners and owners can see bookings.

**Consequences (testable):**
- A day view per practitioner lists each slot with its bookings: sequence number,
  attendee name and age, reason category, masked number, state, and whether it is a
  practitioner booking. A practitioner sees only their own day view; the default is
  today.
- An owner can see every practitioner's day, and can export one week's bookings as
  CSV with these columns: date, slot time, practitioner, sequence number, attendee
  name, age, reason category, full mobile number, state, and practitioner booking
  (yes/no). A cell that starts with `=`, `+`, `-` or `@` is escaped, so a spreadsheet
  never runs it as a formula.
- Counts per slot: capacity, taken, reserved and free.

#### FR-41: Patient lookup

Practitioners and owners can find a patient by mobile number, or by any attendee
name on the patient's bookings.

**Consequences (testable):**
- The result shows the masked number, the attendee names used, and the patient's
  bookings (all states). Reason category and note follow FR-16: they appear only for
  bookings in the viewer's own slots, or to owners. The full number is shown only on
  the patient's detail view, and opening it is an audit entry.
- An owner can sign a patient out everywhere (FR-5), which ends their sessions and
  forgets their known devices.

#### FR-42: Audit log

**Consequences (testable):**
- An audit entry is written at least for:
  - publication, and scheduling or cancelling a publication;
  - every slot change after publication;
  - closing and reopening;
  - reserving and releasing;
  - practitioner bookings;
  - every cancellation (who cancelled; the reason text stays on the booking);
  - check-ins;
  - viewing a full mobile number, and the CSV export;
  - official-contact changes, setting changes and account changes;
  - staff actions refused for lack of a role;
  - door-check throttling;
  - session revocations;
  - announcement notify sends;
  - fraud reports marked reviewed;
  - patient data deletion.
- Each entry records the tenant, the actor (user and role), the action, the affected
  record, the time (UTC) and a details payload.
- An entry refers to patients only by internal ID. It never contains a name, age,
  mobile number, reason category, note, cancellation reason or message text, so
  erasure (FR-44) never needs to change one.
- No role can edit or delete an entry, owners included. The application's own
  database access must not allow it, and an automated test proves it.
- Once a retention period is set (Q3), purging entries older than it is the only
  deletion allowed. It runs under a separate maintenance identity, and each purge is
  itself an audit entry.
- An owner can browse the log newest first and filter it by actor, action and date
  range.

#### FR-45: Owner alerts

Owners see problems that need their attention. *(Numbered after FR-44 so that
existing IDs stay stable.)*

**Consequences (testable):**
- An owner alert is raised when: the daily OTP budget is used up (FR-3), the SMS
  provider fails or its quota is exhausted (NFR-17), a notification fails after its
  last attempt (FR-27), or a slot's place counts drift from its bookings (NFR-2).
- An alert appears in an alert list, and as a banner on every owner page until an
  owner acknowledges it. Practitioners don't see alerts.
- Alerts are never sent by SMS or push. Each alert is also written to the application
  log.

---

### 4.11 Languages and layout

**Description:** Every page works in Bangla, English and Arabic, with a full
right-to-left layout for Arabic. Realizes SM-9.

#### FR-43: Languages

**Consequences (testable):**
- Every built-in patient-facing and staff-facing string is available in `bn`, `en`
  and `ar`. A missing translation of a built-in string fails the build. Text that
  owners enter follows FR-39.
- In `ar` the layout mirrors right-to-left. Ticket codes, mobile numbers and times
  stay left-to-right in every language.
- The patient can switch language on any page, and the choice is remembered in the
  browser and, once the patient signs in, stored on the patient (FR-28). The first
  visit uses the tenant's default language.
- Text that users enter (names, notes, announcements, chat) is stored and shown in any
  script without corruption, including mixed Bangla and English.
- Dates and times appear in the tenant's time zone, with month and day names in the
  active language.
- All digits are shown as ASCII digits (0–9) in every language. Mobile numbers, OTPs
  and ticket codes typed with Bangla or Arabic-Indic digits are accepted and
  normalized to ASCII digits.

---

### 4.12 Patient data rights

**Description:** A patient can delete their personal data. The clinic keeps only
de-identified booking facts for its records.

#### FR-44: Delete my data

A signed-in patient can delete their data.

**Consequences (testable):**
- Deleting always requires a fresh OTP for the patient's number, even in a valid
  session or on a known device. Without it, the request is refused and nothing
  changes.
- Deleting first cancels all active bookings (the cut-off doesn't apply, and no
  cancellation notification is sent), then removes the patient's number, attendee
  names and ages, notes, cancellation reasons, conversation and fraud-report text.
- Deletion ends every session, known-device marker and push subscription of that
  number.
- Bookings are kept for the clinic's records but de-identified: they keep the
  booking's facts (slot, state, timestamps, reason category) and lose every name,
  age, number, note and reason text.
- Audit entries are untouched, because they never hold personal data (FR-42).
  Deletion is itself an audit entry, with the patient as the actor.
- After deletion the same number can sign in again as a new patient with no history.

## 5. Cross-Cutting NFRs

### 5.1 Correctness under concurrency

- **NFR-1:** Capacity, the booking limit, sequence numbers, reserving, releasing and
  booking-state transitions are enforced by the data store at commit time, never by a
  read-then-write in application code. Each has an automated concurrency test:
  200 → 10 places (FR-15), 20 parallel bookings from one number (FR-17), parallel
  bookings never sharing a sequence number (FR-15), a reserve racing a booking
  (FR-12), a release racing a booking (FR-13), lowering capacity racing a booking
  (FR-10), closing a slot racing a booking (FR-11), a double check-in (FR-25), and a
  cancel racing a check-in (FR-21).
- **NFR-2:** Each slot's place counts can be reconciled against its actual bookings. A
  scheduled check raises an owner alert (FR-45) for any drift and logs it.

### 5.2 Security

- **NFR-3:** Tenant isolation. Every read and write is scoped to one tenant. Automated
  tests that use a second test tenant prove that a user or session of tenant A can't
  read or change any data of tenant B, even though v1 has only one live tenant.
- **NFR-4:** Patient sessions and staff sessions are distinct audiences; one can never
  satisfy the other's endpoint. Owner and practitioner are roles on a staff account
  (FR-38), and each staff endpoint states which roles it accepts.
- **NFR-5:** HTTPS only, HSTS, and secure, HttpOnly, SameSite cookies or equivalent
  tokens. No secret is in the repo.
- **NFR-6:** Every public endpoint (OTP request, OTP verify, public ticket check,
  availability) has a rate limit per IP and, where it applies, per mobile number. The
  fraud report needs a session and is limited by FR-32. FR-3, FR-24, FR-26, FR-32 and
  FR-34 set specific limits; limits not given in an FR are
  set in architecture, loose enough for many phones behind one carrier IP (FR-3). A
  rate limit returns HTTP 429 with a retry hint. Business limits such as
  `limit_reached` or the daily OTP budget use their own error codes, not 429.
- **NFR-7:** Free text that users enter is never rendered as HTML.
- **NFR-8:** A threat model covering OTP abuse, ticket forgery and enumeration,
  cross-patient and cross-tenant access, and staff-account takeover is written before
  the security-sensitive epics start. The architecture step owns it.

### 5.3 Privacy

- **NFR-9:** Full mobile numbers appear only where a feature needs them (the patient's
  own pages, the patient detail view). Lists, door checks and exports mask them, except
  that an owner's CSV export contains full numbers and is an audit entry.
- **NFR-10:** Data is encrypted in transit and at rest.
- **NFR-11:** A retention period for past bookings, conversations and audit entries is
  set before launch. *(Open question Q3.)*
- **NFR-11a:** The reason category, the age and the note are health-adjacent. They are
  shown only as FR-16 allows and never written to application logs: a log scan in the
  test suite finds none of them, and no full mobile number either.

### 5.4 Performance and usability

- **NFR-12:** A first-time patient on a mid-range phone on a 4G connection can go from
  opening the site to holding a ticket in under 2 minutes, excluding SMS delivery
  time.
- **NFR-13:** At the first tenant's peak (about 100 patients at once just after
  publication), 95% of availability reads complete in under 500 ms and 95% of booking
  commits in under 1 s, server time.
- **NFR-14:** An owner can publish a typical week (a copied previous week with
  edits) in under 10 minutes.
- **NFR-15:** Every patient page is usable at 360 px width and meets WCAG 2.2 AA for
  contrast, labels and keyboard use. The OTP field accepts paste and the browser's
  one-time-code autofill.

### 5.5 Cost and operations

- **NFR-16:** The running cost before the first sale is $0: hosting, database,
  messaging, SMS and push all stay within free tiers. Any component without a
  free tier needs a free alternative before it is adopted. After a sale, paid SMS is
  the one expected running cost.
- **NFR-17:** An exhausted SMS quota or daily OTP budget (FR-3) degrades the product
  but never breaks booking: new devices can't sign in, known devices still book, and
  an owner alert (FR-45) says that sign-in for new devices is unavailable.
- **NFR-18:** Health and readiness endpoints exist, and errors are logged with a
  correlation ID per request. No uptime target is set for v1 (Q5).
- **NFR-19:** The data is backed up daily, and a restore has been tested at least once
  before launch.

## 6. Non-Goals (Explicit)

- **Native mobile apps.** Web only, for the phone browser (brief, *Constraints*).
- **Payment** of any kind, including paid/unpaid marking at the clinic. Only the
  `awaiting_payment` state is kept, as a seam.
- **An approval step or triage** before a booking counts. Patients book directly.
- **No-show tracking or penalties.** `lapsed` is recorded with no consequence.
- **An offline door check.** It needs connectivity. A signed, offline-verifiable QR
  code is a candidate for a later version.
- **A content library or CMS** (curated articles, audio, Q&A).
- **Sponsor advertising.**
- **Chat attachments, voice or video**, and video consultations.
- **Reviews and ratings.**
- **Treatment history** or staff notes on past bookings.
- **Reporting and analytics** beyond the booking lists and the CSV export in FR-40.
- **Self-service clinic onboarding**, a platform-operator console, or a second live
  tenant.
- **A front-desk role.**
- **Rescheduling** as its own action: cancel and book again.
- **Changing a patient's mobile number.** A new number signs in as a new patient; the
  old number's history doesn't move.
- **Staff notifications** (for a new booking or chat message). Staff see the inbox
  and the day view.
- **Practitioner profiles.** Patients see each practitioner's display name only.

## 7. MVP Scope

### 7.1 In Scope

Everything in §4, FR-1 to FR-45, for one live tenant, plus the NFRs in §5.

### 7.2 Suggested slicing for the epic breakdown

These are not requirements, only a hint for `bmad-ticket`. Each epic leaves something
demonstrable:

1. **Tenant and staff foundation:** tenancy, staff accounts, settings, the audit log
   and owner alerts (FR-38, FR-39, FR-42, FR-45, NFR-3)
2. **Schedule and publication:** draft week, publication, availability and closing
   (FR-7 to FR-11). The slot's reserved count exists from here, at 0 until epic 6, so
   the capacity rule never changes shape
3. **Patient sign-in:** OTP, known device and sessions (FR-1 to FR-6)
4. **Booking core:** booking, limits, cancellation and the lifecycle (FR-15 to FR-21,
   NFR-1)
5. **Ticket and door check:** the code, the ticket, the door check, check-in and the
   public ticket check (FR-22 to FR-26)
6. **Reserved places and practitioner booking** (FR-12 to FR-14)
7. **Notifications:** the channel, the events and reminders (FR-27 to FR-29); the main
   messaging workload
8. **Anti-impersonation:** official contacts, the home warning and fraud reports
   (FR-30 to FR-32)
9. **Announcements** (FR-33)
10. **Chat** (FR-34 to FR-37)
11. **Languages and RTL** (FR-43), threaded through every epic, with a final
    completeness pass
12. **Patient data rights and launch hardening** (FR-44, NFR-11, NFR-16 to NFR-19)

## 8. Success Metrics

**Primary**
- **SM-1: No double bookings.** The load test of 200 concurrent patients for 10 places
  ends with exactly 10 bookings, on every CI run of the concurrency suite. Validates
  FR-15, NFR-1.
- **SM-2: Forged tickets fail.** At the door check, forged, reused (already checked
  in), wrong-day, cancelled and lapsed codes are all rejected. At the public check,
  forged, wrong-number, cancelled, checked-in and lapsed codes all fail to show
  *Genuine*. 100% of these cases pass in the test suite. Validates FR-22, FR-24 to
  FR-26.
- **SM-3: Limits hold.** Parallel bookings from one number never exceed the limit.
  Validates FR-17.
- **SM-4: Reserved means reserved.** No patient session can take a reserved place,
  including by calling the API directly. Validates FR-14.
- **SM-5: Tenant isolation.** The cross-tenant test suite passes: no reads, no writes.
  Validates NFR-3.

**Secondary**
- **SM-6: Fast booking.** The median first-time booking takes under 2 minutes in a
  scripted phone test. Validates NFR-12.
- **SM-7: Fast publishing.** A copied-and-edited week is published in under 10
  minutes. Validates FR-7, FR-8, NFR-14.
- **SM-8: Zero cost.** $0 of running cost before the first sale. Validates NFR-16.
- **SM-9: Demo-ready.** Publish, reserve, book, ticket, door check, and rebooking into
  a reserved place all run end to end in `bn`, `en` and `ar`. Validates the core flow
  of §4.2 to §4.5 and FR-43.
- **SM-10: Phone traffic drops.** After launch, the clinic's booking calls drop by at
  least half within 4 weeks, as reported by the clinic. The baseline is the clinic's
  own count of booking calls over the 2 weeks before launch.
- **SM-11: Missed patients fitted in.** After launch, practitioner bookings into
  reserved places are counted each week from the audit log. Zero for 4 weeks in a row
  means reserved places aren't doing their job. Validates FR-12 to FR-14.
- **SM-12: Door claims and impersonation caught.** *Not genuine* door checks and fraud
  reports are counted each week, from the logs and the fraud-report list. A falling
  trend after launch means the ticket and warnings work. Validates FR-24, FR-26,
  FR-32.

**Counter-metrics (do not optimize).** The developer reads each one from the logs or
the audit log; none is a product report.

- **SM-C1: OTP skip rate.** Don't raise it by lengthening the known-device lifetime or
  loosening its matching. Saving SMS must not weaken identity. Counterbalances SM-8.
- **SM-C2: Booking volume per number.** Higher is not better: a rise suggests
  hoarding. Counterbalances SM-6.
- **SM-C3: Chat response time.** Don't promise live support. The expected response
  time is a setting, not a target to push down. Counterbalances FR-34.
- **SM-C4: Free places at publication.** Reserving many places isn't a failure, but
  it leaves patients without slots, so watch the free-place count rather than maximize it.
  Counterbalances FR-12.

## 9. Open Questions

1. **Q1 — SMS free quota.** Is there a free SMS quota for local numbers, and how large
   is it? Live use is SMS either way (FR-27). The answer sets the pre-sale cost and the
   default daily OTP budget (FR-3). *(architecture)*
2. **Q2 — Booking details.** *Resolved 2026-10-09:* attendee name, age and reason
   category are required; a note is optional (FR-16).
3. **Q3 — Retention.** How long are past bookings, conversations and audit entries
   kept? *(before launch; privacy policy)*
4. **Q4 — Staff authentication.** Which sign-in method do staff use? *(architecture;
   MFA is required for all staff, FR-38)*
5. **Q5 — Uptime.** What availability does the clinic expect, and who responds out of
   hours? *(before launch)*
6. **Q6 — Multiple owners.** *Resolved 2026-10-10:* a tenant can have more than
   one owner account.
7. **Q7 — Booking for someone else.** *Resolved 2026-10-09:* allowed; the limit
   counts per number (FR-16, FR-17).

## 10. Assumptions Index

Every assumption the draft made, with its resolution. All were resolved in sessions
007 and 008; none is open.

| # | Where | Assumption |
|---|---|---|
| A1 | §3 Owner, Q6 | **Confirmed:** a tenant can have more than one owner account |
| A2 | §3 Week | **Confirmed:** the start day is chosen at tenant setup and fixed in v1 |
| A3 | FR-1 | **Confirmed:** the country is a tenant setting; v1 accepts one country's numbers |
| A4 | FR-2 | **Confirmed:** the OTP expires after 5 minutes |
| A5 | FR-3 | **Changed:** 100 per IP per hour as an abuse brake, plus a tenant-wide daily OTP budget |
| A6 | FR-4 | **Confirmed:** 90 days from last use; the marker is a server-issued secret bound to the number |
| A7 | FR-5 | **Confirmed:** a patient session lasts 30 days |
| A8 | FR-8 | **Confirmed:** scheduled publication is in v1 |
| A9 | FR-10 | **Confirmed:** practitioners can't edit published slots; they manage reserved places only |
| A10 | FR-15 | **Confirmed:** a `slot_full` failure suggests up to 3 nearest free slots |
| A11 | FR-16, Q2 | **Resolved:** attendee name, age and reason category are required; the note is optional |
| A12 | FR-16, Q7 | **Confirmed:** one number may book for a family member; the limit applies per number |
| A13 | FR-17 | **Confirmed:** default 2 per published week; an owner sets both limits |
| A14 | FR-17 | **Changed:** cancelled bookings never count towards either limit |
| A15 | FR-24 | **Confirmed:** any practitioner can door-check any practitioner's tickets |
| A16 | FR-24 | **Confirmed:** 60 door-check lookups per minute per account |
| A17 | FR-26 | **Confirmed:** 10 per IP and 5 per number per hour |
| A18 | FR-27 | **Confirmed:** 5 retries over 30 minutes |
| A19 | FR-27 | **Resolved:** OTP is SMS only (free quota, else paid after a sale); a development channel before launch; no email in v1 |
| A20 | FR-32 | **Confirmed:** no file uploads in v1 |
| A21 | FR-32 | **Confirmed:** OTP session; 3 per number per day |
| A22 | FR-34 | **Confirmed:** chat is text only |
| A23 | FR-34 | **Confirmed:** 20 chat messages an hour |
| A24 | FR-37 | **Confirmed:** chat replies never go by SMS |
| A25 | FR-38, Q4 | **Resolved:** MFA for all staff; the method is decided in architecture |
| A26 | FR-44 | **Confirmed:** erasure de-identifies. Audit entries reference patient IDs and hold no personal data, so they stay append-only |
| A27 | NFR-9 | **Confirmed:** full numbers in an owner's CSV export, audited |
| A28 | NFR-15 | **Changed:** WCAG 2.2 AA; the OTP field allows paste and autofill |
| A29 | NFR-18, Q5 | **Confirmed:** no uptime target for v1 |
| A30 | SM-10 | **Confirmed:** the clinic counts booking calls for 2 weeks before launch |
| A31 | FR-27 | **Confirmed:** push in addition to SMS, never instead |
