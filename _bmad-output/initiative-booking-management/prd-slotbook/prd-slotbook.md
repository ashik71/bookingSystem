---
title: SlotBook
status: draft
created: 2026-10-09
updated: 2026-10-09
---

# PRD: SlotBook

## 0. Document Purpose

This PRD is for the developer (product owner and reviewer) and for the downstream
BMAD steps: UX (`bmad-ux`), architecture (`bmad-architecture`) and the epic and story
breakdown (`bmad-ticket`). The sandbox agent builds against the acceptance criteria
that come out of it, so every FR states **testable consequences**.

It builds on the final brief (`../brief-slotbook/brief-slotbook.md`) and its
addendum, and on a generic extract of an earlier private plan
(`input-prior-plan.md`). Where the two disagreed, the developer decided; the decisions
are in `.memlog.md`.

How to read it:

- **§3 Glossary** fixes the vocabulary. Every other section uses those terms exactly.
- **§4 Features** groups the FRs. FR numbers are global and stable.
- **§5 Cross-cutting NFRs** covers concurrency, security, privacy, tenancy and cost.
- `[ASSUMPTION]` marks anything inferred without confirmation. All of them are
  indexed in **§10**.
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
- **Front-desk staff.** There is no separate role. Practitioners and the owner do the
  door check and answer chat.
- **Patients without a mobile number.** Identity is the mobile number.

### 2.3 Key User Journeys

The protagonists are invented and generic.

- **UJ-1. Nadia, the owner, publishes next week on publication day.**
  Nadia signs in on her laptop on the clinic's publication day. The draft for next
  week is already there: each practitioner has entered their own slots during the
  week, and the empty days were copied from this week's pattern. She checks each
  practitioner's day, raises one walk-in window from 10 to 12 places, closes Friday
  for a holiday, and selects **Publish**. Patients now see next week. **Edge case:**
  a practitioner falls ill on Wednesday after publication. Nadia closes that
  practitioner's Wednesday slots, and the system lists the 9 affected bookings. She
  cancels them with a reason, and each patient is notified.

- **UJ-2. Dr. Karim, a practitioner, holds back places for the week.**
  Karim opens his published week on his phone. His 09:00–11:00 window has capacity
  12, and he reserves 3 places in it. Patients see 9 bookable places and 3 marked
  *reserved*. On Thursday 2 of the reserved places are still unused, so he releases
  them and they become bookable at once.

- **UJ-3. Amina, a patient, books from her phone.**
  Amina taps the clinic's official booking link. The home page shows the clinic's
  announcements and a warning: *the clinic only takes money through the numbers
  listed on this site*. She selects **Book**, picks Dr. Karim, sees next week with full
  days greyed out, and picks Tuesday 09:00–11:00 (4 places left). She enters her name,
  her age and a reason category (*first visit*), and her mobile number. A 6-digit code arrives by SMS, she enters it, and her ticket
  appears: practitioner, date, slot, sequence number 07, code `SB-4K7P`, and a QR
  code. An SMS confirms it. **Edge case:** the last place goes to someone else while
  she is entering the code. She is told the slot is now full and is shown the nearest
  slots that are still free; no ticket is created.

- **UJ-4. Dr. Karim checks tickets at the door.**
  A patient shows a QR code. Karim scans it with his phone's browser, and the screen
  shows a large green **Genuine**: name, masked mobile number, today's date, his slot,
  sequence 07. He asks for the last three digits of the number, they match, and he
  selects **Check in**. The next person shows a screenshot of a code; it comes back
  red, **Already used at 09:14**. A third code comes back **Not genuine**.

- **UJ-5. Dr. Karim rebooks a patient who missed their slot.**
  A patient missed Monday because of a bus strike. Karim opens a reserved place in his
  Thursday slot, enters the patient's mobile number, the attendee's name and age and a reason
  category, and confirms. The patient
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
  and a **Report fraud** button, and he files a report with the caller's number.

- **UJ-8. Amina asks the clinic a question.**
  Before booking, Amina opens **Chat**, signed in with her number, and asks whether she
  should bring her previous prescriptions. Dr. Karim answers from the clinic inbox
  that afternoon. Amina sees the reply the next time she opens the site, or gets a
  notification if she allowed them.

## 3. Glossary

Downstream documents must use these terms exactly.

- **Tenant** — one clinic's isolated space in SlotBook: its practitioners, schedule,
  rules, patients' bookings and content. In v1 there is exactly one live tenant. The
  word **clinic** is used for the same thing in user-facing text.
- **Owner** — the user who administers a tenant: practitioners, publication, booking
  rules, official contacts and announcements. One or more per tenant.
  `[ASSUMPTION: a tenant may have more than one owner account]`
- **Practitioner** — a user who sees patients in a tenant. Practitioners manage their
  own slots in the draft week, their reserved places and their door checks, and they
  answer chat. An owner may also be a practitioner.
- **Patient** — a person identified by a **verified mobile number** within a tenant.
  There is no username, password or email.
- **Mobile number** — stored in one canonical form (E.164). Two spellings of the same
  number are the same patient.
- **OTP** — a 6-digit one-time code sent by SMS (or by the fallback channel) to prove
  that a patient controls a mobile number.
- **Known device** — a browser that has completed OTP for a given mobile number before.
  A booking from a known device for that same number skips the OTP.
- **Week** — the 7 days (Monday–Sunday) of one schedule. `[ASSUMPTION: weeks run
  Monday–Sunday in the tenant's time zone; the start day is a tenant setting]`
- **Draft week** — a week that is still being prepared; patients can't see it.
- **Publication** — the act by which the owner makes a draft week visible and
  bookable. A week has exactly one publication, and after it the week is a **published
  week**.
- **Slot** — a time range (start, end) on one date for one practitioner, with a
  **capacity**. Capacity 1 is a classic appointment; capacity N is a walk-in window.
- **Capacity** — the number of **places** in a slot; an integer ≥ 1.
- **Place** — one unit of a slot's capacity. A place is **free**, **reserved** or
  **taken**.
- **Reserved place** — a place that the practitioner holds back. Patients see it but
  can't book it. Only a practitioner or the owner can book into it.
- **Release** — turning unused reserved places back into free places.
- **Closed slot** — a slot that accepts no new bookings. Its existing bookings stay
  until someone cancels them.
- **Booking** — one patient holding one place in one slot. States: `confirmed`,
  `checked_in`, `cancelled`, `lapsed`; plus `awaiting_payment`, which is reserved and
  unused in v1.
- **Active booking** — a booking in `confirmed` whose slot has not ended.
- **Attendee** — the person who will attend a booking: the patient themselves, or a
  family member they book for. A booking has exactly one attendee (name and age).
- **Reason category** — one entry from a tenant-defined list (for example *first
  visit*, *follow-up*) that a booking must carry.
- **Patient booking / Practitioner booking** — a booking made by the patient, or by a
  practitioner or the owner on the patient's behalf (into a reserved or free place).
- **Ticket** — the patient's proof of a booking: practitioner, date, slot time,
  **sequence number**, **ticket code** and a QR code of the code.
- **Sequence number** — the booking's position within its slot (1…capacity), given in
  booking order.
- **Ticket code** — a short code that the system generates, unique among a tenant's
  active bookings, for example `SB-4K7P`.
- **Door check** — a practitioner or owner verifying a ticket at the clinic and
  checking the patient in.
- **Public ticket check** — the unauthenticated page where anyone enters a ticket code
  and a mobile number and learns whether the ticket is genuine.
- **Booking limit** — the owner-set maximums per mobile number: active bookings, and
  bookings per week.
- **Cancellation cut-off** — the number of hours before a slot starts after which a
  patient can no longer cancel online.
- **Official contacts** — the tenant's list of real phone, payment and social
  numbers and links, with the date the owner last confirmed it.
- **Fraud report** — a report that a patient files about someone impersonating the
  clinic.
- **Announcement** — a notice from the owner shown on the tenant's home page.
- **Conversation** — the single chat thread between one patient and the clinic.
  Messages are sent by the patient, a practitioner or the owner.
- **Notification** — a message the system sends to a patient (confirmation, reminder,
  cancellation, chat reply, announcement) through the **notification channel**.
- **Audit entry** — an immutable record of a significant action: who, what, which
  record, and when.

## 4. Features

### 4.1 Patient sign-in (mobile number and OTP)

**Description:** A patient proves that they control a mobile number with an OTP, and
then holds a session. A booking from a known device for the same number skips the
OTP, because SMS costs money. The OTP screen warns that the clinic never asks for the
code. Realizes UJ-3, UJ-6, UJ-8.

#### FR-1: Mobile number entry and normalisation

A patient can enter a mobile number in the local format, with or without separators,
or in international format.

**Consequences (testable):**
- Every accepted input is stored in E.164 form. `01712-345678` and `01712345678`
  resolve to the same patient.
- Input that is not a valid mobile number for the tenant's country is rejected with a
  message, in the active language, that names the mobile number field. No OTP is
  sent. `[ASSUMPTION: country is a tenant setting; v1 validates one country]`
- Within a tenant, one mobile number maps to at most one patient.

#### FR-2: OTP issue and verification

A patient can request an OTP for a mobile number and enter it to start a session.

**Consequences (testable):**
- The OTP is 6 numeric digits, generated with a cryptographic RNG and stored only as
  a hash.
- An OTP expires **5 minutes** after it is sent, and it is single-use.
  `[ASSUMPTION: the prior plan said 60 seconds; 5 minutes allows for slow SMS
  delivery]`
- A wrong, expired or already-used code is rejected. The response never reveals the
  correct code, and it says whether the patient should retry or ask for a new code.
- After **5 wrong attempts** for one OTP, that OTP is invalidated.
- A successful verification creates a patient session scoped to that patient and that
  tenant.

#### FR-3: OTP send limits

The system limits how often OTPs are sent, to cap SMS cost and to stop harassment of
a number's owner.

**Consequences (testable):**
- A resend for the same number within **60 seconds** of the last send is refused, and
  the response states the remaining wait in seconds.
- A 6th send for the same number within one rolling hour is refused, with a message to
  contact the clinic through its official contacts.
- Sends are also limited per client IP. `[ASSUMPTION: 20 per IP per hour]`
- Every refused send is logged with the number (masked) and the IP.

#### FR-4: Known-device skip

A patient booking from a known device for the same mobile number does not need a new
OTP.

**Consequences (testable):**
- The device and the number must both match an earlier successful verification;
  either one alone requires an OTP.
- A known-device marker lasts **90 days** from its last use and can be revoked by the
  patient (sign out everywhere) or by the owner. `[ASSUMPTION: 90 days]`
- Skipping the OTP never skips the booking limit (FR-17) or any other rule.

#### FR-5: Patient session

A signed-in patient can see their bookings, tickets and conversation without signing
in again on every visit.

**Consequences (testable):**
- A patient session gives access only to that patient's own data in that tenant.
  Asking for another patient's booking returns *not found*, never *forbidden*, so that
  existence isn't disclosed.
- A session lasts **30 days**; signing out ends it, and the owner can revoke it.
  `[ASSUMPTION: 30 days]`
- A patient session never satisfies a practitioner or owner endpoint.

#### FR-6: The clinic never asks for the code

**Consequences (testable):**
- No practitioner or owner screen, export or API response contains an OTP, active or
  past.
- The OTP entry screen shows, in the active language, a warning that the clinic will
  never ask for this code by phone or message.

---

### 4.2 Weekly schedule and publication

**Description:** Practitioners enter their own slots into next week's draft. The owner
reviews the whole draft and publishes it on the tenant's publication day. Once a week
is published, patients can book any future slot in it that has a free place. After
publication the owner can still add slots, change capacity and close slots, within
rules that protect existing bookings. Realizes UJ-1.

#### FR-7: Draft week

An owner or practitioner can prepare the next week before it is published.

**Consequences (testable):**
- A practitioner can create, edit and delete **their own** slots in a draft week. An
  owner can do the same for **any** practitioner.
- A slot has a date, a start time, an end time (later than the start) and a capacity
  (an integer ≥ 1). Two slots of the same practitioner can't overlap in time.
- A draft week can be **pre-filled by copying** the previous week's slots, shifted by 7
  days. Copying never overwrites slots already in the draft.
- Patients can't see draft weeks through any page or API.

#### FR-8: Publication

An owner can publish a draft week.

**Consequences (testable):**
- Publishing makes every slot in the week visible and bookable at once, in a single
  step: patients never see a half-published week.
- An owner can also **schedule** a publication for a date and time; at that moment the
  week publishes exactly as if the owner had pressed Publish.
  `[ASSUMPTION: scheduled publication is in v1]`
- The tenant's publication day is a setting, shown to patients as *"Next week opens on
  ⟨day⟩ at ⟨time⟩"*. It is informational and does not block publishing on another day.
- A published week can't go back to draft.

#### FR-9: Patient view of availability

A patient can see which practitioners, dates and slots can be booked.

**Consequences (testable):**
- Only published weeks are shown. A date earlier than today, or a slot that has
  already started, is never bookable.
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
- Practitioners can't change a published week's slots or capacity. They manage
  reserved places only (§4.3). `[ASSUMPTION]`

#### FR-11: Closing a slot

An owner can close a slot, for example for illness or a holiday, and can close a whole
date for one practitioner or for the clinic.

**Consequences (testable):**
- A closed slot accepts no new booking of any kind.
- Closing never cancels bookings on its own. It lists the affected bookings (patient
  name, masked number, sequence number) for the owner to handle.
- The owner can cancel the affected bookings in bulk with one reason. Each cancelled
  booking produces its own notification (FR-28) and its own audit entry.
- A closed slot can be reopened; its places then follow the normal rules again.

---

### 4.3 Reserved places

**Description:** A practitioner holds back some places so that a patient who missed a
booking can be fitted in later in the week. Patients see that the places exist but
can't book them. The practitioner can release unused reserved places to everyone at
any time. Realizes UJ-2, UJ-5.

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

A practitioner can release unused reserved places in their own slots, and an owner in
any slot.

**Consequences (testable):**
- Released places become free and bookable by patients at once.
- Up to the number of unused reserved places can be released; a request for more is
  refused.

#### FR-14: Book a patient into a place

A practitioner can book a patient into a reserved or free place in their own slot, and
an owner into any slot, by entering the patient's mobile number and the booking
details of FR-16 (the attendee's name and age, and a reason category).

**Consequences (testable):**
- If the number has no patient in the tenant yet, a patient record is created. No OTP
  is required, because the practitioner vouches for the patient.
- The booking is a **practitioner booking**: it gets a normal ticket and a
  confirmation notification (FR-28).
- A practitioner booking does **not** count towards the patient's booking limit
  (FR-17), and it is refused only if the patient already has a booking in the same
  slot.
- **Reserved means reserved:** no patient session, by any page or by calling the API
  directly, can take a reserved place. A test that calls the booking API directly with
  a patient session must be refused.

---

### 4.4 Booking and cancellation

**Description:** A signed-in patient books one free place in a slot directly; there is
no approval step. The guarantee at the centre of the product is that a slot never
holds more bookings than its capacity, even when many patients try for the last place
at the same moment. Patients cancel online up to the cancellation cut-off; to change a
booking, they cancel and book again. Realizes UJ-3, UJ-6.

#### FR-15: Book a free place

A signed-in patient can book one free place in a published, future, open slot.

**Consequences (testable):**
- The booking succeeds only if, at commit time, the slot is open, has not started and
  has a free place. Otherwise it fails with a specific reason: `slot_full`,
  `slot_closed`, `slot_started` or `limit_reached`. No booking is created and no place
  is used.
- **No overbooking:** taken places never exceed *capacity − reserved* for patient
  bookings, or *capacity* in total, under any concurrency. Load test: 200 concurrent
  patients competing for 10 places end with exactly 10 bookings, and 190 receive
  `slot_full`.
- A patient can hold at most one booking in the same slot.
- On success the booking is `confirmed`, gets the next sequence number in the slot and
  a ticket code (FR-22), and a confirmation notification goes out (FR-28).
- When a booking fails with `slot_full`, the patient is shown up to 3 nearest free
  slots of the same practitioner. `[ASSUMPTION]`
- Retrying the same booking request (same patient, same slot, same idempotency key)
  never creates a second booking.

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

#### FR-17: Booking limit

The owner's booking limit is enforced on every patient booking.

**Consequences (testable):**
- The owner sets **max active bookings per number** (default 1) and **max patient
  bookings per number per week** (default 2). `[ASSUMPTION: default 2 per week]`
- A patient booking that would exceed either limit fails with `limit_reached` and
  names the limit.
- **Limits hold under concurrency:** a test that fires 20 parallel booking requests
  from one number into different slots, with a limit of 1, ends with exactly 1
  booking.
- Practitioner bookings (FR-14) are not counted and are not blocked by the limit.
- Cancelled bookings don't count towards *active*. They do count towards *per week*
  if cancelled after the cut-off. `[ASSUMPTION]`

#### FR-18: Patient cancels

A patient can cancel their own `confirmed` booking before the cancellation cut-off.

**Consequences (testable):**
- The owner sets the cut-off in hours before slot start (default 2, minimum 0).
- Before the cut-off: the booking becomes `cancelled`, the place becomes free at once
  (or reserved again, if it was a practitioner booking made into a reserved place),
  the ticket code stops validating, and a cancellation notification goes out.
- After the cut-off, cancelling online is refused, with a message to contact the
  clinic.
- A patient can't cancel another patient's booking; the response is *not found*.

#### FR-19: My bookings

A signed-in patient can see their bookings.

**Consequences (testable):**
- Active bookings come first, then past bookings (`checked_in`, `cancelled`,
  `lapsed`), newest first.
- Each booking shows the practitioner, date, slot time, state, and a timeline of state
  changes with timestamps.
- An active booking opens its ticket (FR-23).

#### FR-20: Clinic cancels

A practitioner can cancel bookings in their own slots, and an owner any booking, with a
reason.

**Consequences (testable):**
- A reason is required (free text, 1–200 characters).
- The place is released as in FR-18, the patient is notified with the reason, and an
  audit entry records who cancelled and the reason.
- The cut-off doesn't apply to clinic cancellations.

#### FR-21: Booking lifecycle

**Consequences (testable):**
- States and allowed transitions: `confirmed → checked_in`,
  `confirmed → cancelled` and `confirmed → lapsed`. No other transition is allowed,
  and `checked_in`, `cancelled` and `lapsed` are final.
- A booking still `confirmed` when its slot ends becomes `lapsed` automatically within
  15 minutes. Lapsing has no penalty in v1 (no no-show tracking).
- `awaiting_payment` exists in the state model but no v1 path reaches it.
- All transitions go through one domain rule, so an endpoint can't bypass them.

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
- The format is a fixed prefix plus 4 characters from the alphabet `A–Z` and `2–9`,
  excluding `O`, `I` and `L` (27 symbols). The prefix is a tenant setting, default
  `SB-`.
- No two **active** bookings in a tenant share a code. Codes of final bookings may be
  reused. Concurrent confirmations get different codes.
- The code is matched case-insensitively, and surrounding spaces and the prefix are
  optional when typed.

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
- The result is exactly one of:
  - **Genuine:** the attendee name and age, the masked mobile number (last 3 digits shown),
    the practitioner, the slot time and the sequence number.
  - **Not today:** the booking's date.
  - **Already checked in:** the time and who checked the patient in.
  - **Cancelled:** when it was cancelled.
  - **Lapsed.**
  - **Not genuine:** no such active or recent ticket in this tenant.
- A practitioner can verify tickets for any practitioner in the tenant.
  `[ASSUMPTION: the door is shared]`
- Lookups are rate-limited per account. `[ASSUMPTION: 60 per minute]` Exceeding the
  limit is throttled and recorded as an audit entry.
- The camera scan works in current mobile Chrome and Safari. Typing is always
  available as a fallback.

#### FR-25: Check in

From a **Genuine** result, the practitioner can check the patient in.

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
- A code with the wrong mobile number gives the same answer as an unknown code (**Not
  genuine**), so the code space can't be probed without the number.
- Limited to 10 checks per IP per hour and 5 per mobile number per hour
  `[ASSUMPTION]`. When a limit is hit, the page says *Too many checks, try later*;
  this never reveals whether the ticket exists.
- A **Not genuine** result shows the official contacts (FR-30) and a link to file a
  fraud report (FR-32).

---

### 4.6 Notifications

**Description:** Patients are told about everything that changes their booking. The
channel sits behind one interface: SMS from a free quota if one exists, otherwise a
fallback (browser push, or email when the patient gives an address). Sending is
asynchronous; a booking never waits on, or fails because of, a notification. Realizes
UJ-1, UJ-3, UJ-5, UJ-6, UJ-8.

#### FR-27: Notification channel

**Consequences (testable):**
- Which channel is used is configuration, not code: switching between SMS, push and
  email needs no code change.
- A booking, cancellation or check-in commits even if the channel is down. The
  notification is retried and, after the last retry, recorded as failed and visible to
  the owner. `[ASSUMPTION: 5 retries over 30 minutes]`
- Each notification is sent at most once per event: a retry or a redelivered message
  never produces a duplicate SMS.
- A patient can turn on browser push from their bookings page. Push is never required
  to book. `[ASSUMPTION: email collection is optional and appears only if the fallback
  channel is email]`

#### FR-28: Booking notifications

**Consequences (testable):**
- A notification goes out for each of these events: a booking is confirmed (patient or
  practitioner booking), the patient cancels, the clinic cancels (with the reason), and
  a reminder.
- Each notification contains the practitioner, the date, the slot time and the ticket
  code. Cancellations also contain the reason, if one was given.
- Notifications are in the patient's last-used language.

#### FR-29: Reminder

**Consequences (testable):**
- A reminder goes out for each active booking at a time the owner sets before the slot
  starts (default 24 hours; off is allowed).
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
- Each entry has a kind (phone, payment number, messaging, social, web), a label and a
  value. An entry that is a link opens in a new tab.
- The page shows the date the owner last confirmed the list, and the owner can
  re-confirm it without changes. The page states that no number or account outside
  the list belongs to the clinic.
- Only owners can change the list. Every change is an audit entry.

#### FR-31: Home warning

**Consequences (testable):**
- The tenant's home page and the ticket page always show a warning, written by the
  owner with a default text, that the clinic takes money only through the official
  contacts, with a link to them. It can't be dismissed.

#### FR-32: Fraud report

A signed-in patient can report an impersonation attempt.

**Consequences (testable):**
- The report form has: what happened (required, up to 1,000 characters), the number or
  link the impersonator used (optional), and the date (default today).
  `[ASSUMPTION: no file uploads in v1]`
- Reports require a patient session (OTP), and each number can file at most 3 a day.
  `[ASSUMPTION]`
- Owners see the reports in a list, newest first, and can mark each one *reviewed*.
  The patient sees their own reports and their status.

---

### 4.8 Announcements

**Description:** The owner posts notices such as closures, new rules or a change of
hours. They appear on the tenant's home page, and they can be pushed to patients who
have active bookings in the affected days. Realizes UJ-1, UJ-3.

#### FR-33: Post an announcement

An owner can write, publish, edit and archive announcements.

**Consequences (testable):**
- An announcement has a title (up to 100 characters), a body (up to 2,000 characters),
  optional text per language, an optional single image (up to 1 MB, JPEG/PNG/WebP), an
  optional *show until* date, and a pinned flag.
- Published announcements appear on the home page, pinned first and then newest
  first. Archived or expired ones don't appear.
- An owner can choose to **notify** when publishing: either all patients with an active
  booking, or those with an active booking on chosen dates. Each patient receives at
  most one notification per announcement.
- Without the notify option, no notification is sent.

---

### 4.9 Chat

**Description:** Each patient has one conversation with the clinic. Practitioners and
the owner answer from a shared clinic inbox, with canned replies for common
questions. It is asynchronous messaging, not a live-support promise: the patient sees
an expected response time. Realizes UJ-8.

#### FR-34: Patient conversation

A signed-in patient can send messages to the clinic and read the replies.

**Consequences (testable):**
- Messages are text only, up to 1,000 characters each. `[ASSUMPTION: no attachments in
  v1]`
- The patient sees all their messages and the clinic's replies in order, with times,
  and which practitioner or owner sent each reply.
- The conversation page shows the owner-set expected response time (for example *"We
  usually reply within 24 hours"*) and a note to use the official phone number in an
  emergency.
- A patient can send at most 20 messages an hour. `[ASSUMPTION]`
- A new reply appears without a page reload while the conversation is open.

#### FR-35: Clinic inbox

Practitioners and owners can read and answer all patients' conversations.

**Consequences (testable):**
- The inbox lists conversations with unanswered patient messages first, then the rest
  by latest activity, with an unread count.
- Opening a conversation shows the patient's name, masked number and active bookings.
- A user can mark a conversation *done*. A new patient message reopens it.
- New patient messages appear in the inbox without a page reload.

#### FR-36: Canned replies

An owner can manage canned replies, and practitioners and owners can insert one into a
reply and edit it before sending.

**Consequences (testable):**
- A canned reply has a title and a body, with text per language.

#### FR-37: Reply notification

**Consequences (testable):**
- When the clinic replies and the patient isn't viewing the conversation, the patient
  gets a notification through push or email if available, otherwise none.
  `[ASSUMPTION: chat replies never go by SMS, to protect the SMS quota]`
- The tenant's home page shows a badge when there's an unread reply.

---

### 4.10 Clinic administration

**Description:** The owner sets up practitioners and the tenant's rules, and sees
simple booking lists. Every significant action is recorded in an immutable audit log,
because investigating a fraud claim afterwards is one of the product's reasons to
exist. Realizes UJ-1, UJ-4.

#### FR-38: Staff accounts

An owner can create, disable and re-enable practitioner and owner accounts.

**Consequences (testable):**
- Practitioners and owners sign in with credentials separate from patient sign-in.
  `[ASSUMPTION: the method is decided in architecture; it must support MFA for owners]`
- A disabled account can't sign in, and its open sessions stop working within 1
  minute.
- A practitioner can't manage accounts, settings, official contacts, announcements or
  canned replies. An attempt is refused and recorded.
- The last enabled owner of a tenant can't be disabled.

#### FR-39: Tenant settings

An owner can set the tenant's rules.

**Consequences (testable):**
- Settings: the reason category list (FR-16; at least one entry, and a category in use
  can be retired but not deleted), the booking limit (FR-17), the cancellation cut-off (FR-18), the reminder
  lead time (FR-29), the publication day and time shown to patients (FR-8), the ticket
  code prefix (FR-22), the expected chat response time (FR-34), the home warning text
  (FR-31), the time zone, and the default language.
- A changed limit or cut-off applies to new actions from the moment it is saved. It
  never cancels or changes existing bookings.

#### FR-40: Booking lists

Practitioners and owners can see bookings.

**Consequences (testable):**
- A day view per practitioner lists each slot with its bookings: sequence number,
  attendee name and age, reason category, masked number, state, and whether it is a
  practitioner booking. The
  default is today, for the signed-in practitioner.
- An owner can see every practitioner's day, and can export one week's bookings as
  CSV.
- Counts per slot: capacity, taken, reserved and free.
- *Not in v1:* any other reporting or analytics.

#### FR-41: Patient lookup

Practitioners and owners can find a patient by mobile number or name.

**Consequences (testable):**
- The result shows the patient's name, the masked number and their bookings (all
  states). The full number is shown only on the patient's detail view, and opening it
  is an audit entry.
- An owner can revoke a patient's sessions and known devices.

#### FR-42: Audit log

**Consequences (testable):**
- An audit entry is written for: publication, every slot change after publication,
  closing and reopening, reserving and releasing, practitioner bookings, every
  cancellation (with the reason), check-ins, viewing a full mobile number,
  official-contact changes, setting changes, account changes, door-check throttling
  and session revocations.
- Each entry records the tenant, the actor (user and role), the action, the affected
  record, the time (UTC) and a details payload.
- An entry refers to patients only by internal ID. It never contains a name, age,
  mobile number, note or message text, so erasure (FR-44) never needs to change one.
- No role can edit or delete an entry, the owner included. The application's own
  database access must not allow it, and an automated test proves it.
- An owner can browse the log newest first and filter it by actor, action and date
  range.

---

### 4.11 Languages and layout

**Description:** Every page works in Bangla, English and Arabic, with a full
right-to-left layout for Arabic. Realizes the *Demo-ready* criterion.

#### FR-43: Languages

**Consequences (testable):**
- Every patient-facing and staff-facing string is available in `bn`, `en` and `ar`.
  A missing translation fails the build.
- In `ar` the layout mirrors right-to-left. Ticket codes, mobile numbers and times
  stay left-to-right in every language.
- The patient can switch language on any page, and the choice is remembered. The
  first visit uses the tenant's default language.
- Text that users enter (names, notes, announcements, chat) is stored and shown in any
  script without corruption, including mixed Bangla and English.
- Dates and times appear in the tenant's time zone and are formatted for the active
  language.

---

### 4.12 Patient data rights

#### FR-44: Delete my data

A signed-in patient can delete their data.

**Consequences (testable):**
- Deleting first cancels all active bookings (the cut-off doesn't apply), then removes
  the patient's number, attendee names and ages, notes, conversation and fraud-report
  text.
- Bookings are kept for the clinic's records but de-identified: they keep the
  booking's facts (slot, state, timestamps, reason category) and lose every name,
  age, number and note.
- Audit entries are untouched, because they never hold personal data (FR-42).
  Deletion is itself an audit entry.
- After deletion the same number can sign up again as a new patient with no history.

## 5. Cross-Cutting NFRs

### 5.1 Correctness under concurrency

- **NFR-1:** Capacity, the booking limit, reserving and check-in are enforced by the
  data store at commit time, never by a read-then-write in application code. Each one
  has an automated concurrency test: 200 → 10 places (FR-15), 20 parallel bookings
  from one number (FR-17), a reserve racing a booking (FR-12), and a double check-in
  (FR-25).
- **NFR-2:** Each slot's taken-place count can be reconciled against its actual
  bookings. A scheduled check reports any drift to the owner and the logs.

### 5.2 Security

- **NFR-3:** Tenant isolation. Every read and write is scoped to one tenant. Automated
  tests prove that a user, session or API key of tenant A can't read or change any
  data of tenant B, using a second test tenant, although v1 has only one live tenant.
- **NFR-4:** Patient, practitioner and owner sessions are distinct audiences; one can
  never satisfy another's endpoint.
- **NFR-5:** HTTPS only, HSTS, and secure, HttpOnly, SameSite cookies or equivalent
  tokens. No secret is in the repo.
- **NFR-6:** Every public endpoint (OTP request, OTP verify, public ticket check, fraud
  report, availability) has a rate limit per IP and, where it applies, per mobile
  number. Every limit returns HTTP 429 with a retry hint.
- **NFR-7:** Free text that users enter is never rendered as HTML.
- **NFR-8:** A threat model covering OTP abuse, ticket forgery and enumeration,
  cross-patient and cross-tenant access, and staff-account takeover is written before
  the security-sensitive epics start. `[NOTE FOR PM: the architecture step owns it]`

### 5.3 Privacy

- **NFR-9:** Full mobile numbers appear only where a feature needs them (the patient's
  own pages, the patient detail view). Lists, door checks and exports mask them, except
  that the owner's CSV export contains full numbers and is an audit entry.
  `[ASSUMPTION]`
- **NFR-10:** Data is encrypted in transit and at rest.
- **NFR-11:** A retention period for past bookings, conversations and audit entries is
  set before launch. *(Open question Q3.)*
- **NFR-11a:** The reason category, the age and the note are health-adjacent. They're
  stored encrypted at rest along with the rest of the data, shown only as FR-16 allows,
  and excluded from logs.

### 5.4 Performance and usability

- **NFR-12:** A first-time patient on a mid-range phone on a 4G connection can go from
  opening the site to holding a ticket in under 2 minutes, excluding SMS delivery
  time.
- **NFR-13:** At the first tenant's peak (about 100 patients at once just after
  publication), 95% of availability reads complete in under 500 ms and 95% of booking
  commits in under 1 s, server time.
- **NFR-14:** An owner can publish a typical week (a copied previous week with
  edits) in under 10 minutes.
- **NFR-15:** Every patient page is usable at 360 px width and meets WCAG 2.1 AA for
  contrast, labels and keyboard use. `[ASSUMPTION: AA is the target]`

### 5.5 Cost and operations

- **NFR-16:** The running cost before the first sale is $0: hosting, database,
  messaging, SMS, email and push all stay within free tiers. Any component without a
  free tier needs a free alternative before it is adopted.
- **NFR-17:** An exhausted free quota (SMS especially) degrades the product but never
  breaks booking. The OTP falls back to the fallback channel, or the owner is alerted
  that sign-in for new devices is unavailable.
- **NFR-18:** Health and readiness endpoints exist, and errors are logged with a
  correlation ID per request. `[ASSUMPTION: an uptime target is not set for v1; see
  Q5]`
- **NFR-19:** The data is backed up daily, and a restore has been tested at least once
  before launch.

## 6. Non-Goals (Explicit)

- **Native mobile apps.** Web only, for the phone browser (ADR-0005).
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
- **Reporting and analytics** beyond the booking lists and the CSV export in FR-40.
- **Self-service clinic onboarding**, a platform-operator console, or a second live
  tenant.
- **A front-desk role.**
- **Rescheduling** as its own action: cancel and book again.

## 7. MVP Scope

### 7.1 In Scope

Everything in §4, FR-1 to FR-44, for one live tenant, plus the NFRs in §5.

### 7.2 Suggested slicing for the epic breakdown

These are not requirements, only a hint for `bmad-ticket`. Each epic leaves something
demonstrable:

1. **Tenant and staff foundation:** tenancy, staff accounts, settings, the audit log
   (FR-38, FR-39, FR-42, NFR-3)
2. **Schedule and publication:** draft week, publication, availability and closing
   (FR-7 to FR-11)
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
- **SM-2: Forged tickets fail.** Forged, reused, wrong-day, cancelled and
  wrong-number tickets are all rejected at the door check and the public check: 100%
  of the cases in the test suite. Validates FR-22, FR-24 to FR-26.
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
  a reserved place all run end to end in `bn`, `en` and `ar`. Validates the whole of
  §4.
- **SM-10: Phone traffic drops.** After launch, the clinic's booking calls drop by at
  least half within 4 weeks, as reported by the clinic. `[ASSUMPTION: there is no
  baseline yet]`

**Counter-metrics (do not optimize)**
- **SM-C1: OTP skip rate.** Don't raise it by lengthening the known-device lifetime or
  loosening its matching. Saving SMS must not weaken identity. Counterbalances SM-8.
- **SM-C2: Booking volume per number.** Higher is not better: a rise suggests
  hoarding. Counterbalances SM-6.
- **SM-C3: Chat response time.** Don't promise live support. The expected response
  time is a setting, not a target to push down. Counterbalances FR-34.
- **SM-C4: Free places at publication.** Reserving many places isn't a failure, but
  it leaves patients without slots, so watch it rather than maximise it.
  Counterbalances FR-12.

## 9. Open Questions

1. **Q1 — SMS free quota.** Is there a free SMS quota for local numbers, and how large
   is it? This decides the OTP and notification channels. *(architecture)*
2. ~~**Q2 — Booking details.**~~ *Resolved 2026-10-09:* attendee name, age and reason
   category are required; a note is optional (FR-16).
3. **Q3 — Retention.** How long are past bookings, conversations and audit entries
   kept? *(before launch; privacy policy)*
4. **Q4 — Staff authentication.** Which sign-in method do staff use, and is MFA
   required for owners? *(architecture)*
5. **Q5 — Uptime.** What availability does the clinic expect, and who responds out of
   hours? *(before launch)*
6. **Q6 — Multiple owners.** Does the first tenant need more than one owner account?
   *(the developer)*
7. ~~**Q7 — Booking for someone else.**~~ *Resolved 2026-10-09:* allowed; the limit
   counts per number (FR-16, FR-17).

## 10. Assumptions Index

Each item needs explicit confirmation. **(B)** marks a likely phase-blocker for UX or
architecture.

| # | Where | Assumption |
|---|---|---|
| A1 | §3 Owner, Q6 | A tenant may have more than one owner account |
| A2 | §3 Week | Weeks run Monday–Sunday in the tenant's time zone; the start day is a setting |
| A3 | FR-1 | The country is a tenant setting; v1 validates one country's numbers |
| A4 | FR-2 | The OTP expires after 5 minutes (the prior plan said 60 seconds) |
| A5 | FR-3 | OTP sends are limited to 20 per IP per hour |
| A6 | FR-4 | A known-device marker lasts 90 days from its last use |
| A7 | FR-5 | A patient session lasts 30 days |
| A8 | FR-8 | Scheduled publication is in v1 |
| A9 | FR-10 | Practitioners can't edit published slots; they manage reserved places only |
| A10 | FR-15 | A `slot_full` failure suggests up to 3 nearest free slots |
| ~~A11~~ | FR-16, Q2 | **Resolved:** attendee name, age and reason category are required; the note is optional |
| ~~A12~~ | FR-16, Q7 | **Confirmed:** one number may book for a family member; the limit applies per number |
| A13 | FR-17 | The default weekly limit is 2 patient bookings per number |
| A14 | FR-17 | Cancellations after the cut-off count towards the weekly limit |
| A15 | FR-24 | Any practitioner can door-check any practitioner's tickets |
| A16 | FR-24 | Door-check lookups are limited to 60 per minute per account |
| A17 | FR-26 | The public check is limited to 10 per IP and 5 per number per hour |
| A18 | FR-27 | A notification is retried 5 times over 30 minutes |
| A19 **(B)** | FR-27 | Email is collected only if the fallback channel is email |
| A20 | FR-32 | Fraud reports have no file uploads in v1 |
| A21 | FR-32 | Fraud reports need an OTP session and are limited to 3 per number per day |
| A22 | FR-34 | Chat is text only, with no attachments |
| A23 | FR-34 | A patient can send at most 20 chat messages an hour |
| A24 | FR-37 | Chat replies are never sent by SMS |
| A25 **(B)** | FR-38, Q4 | The staff sign-in method is decided in architecture, with MFA for owners |
| ~~A26~~ | FR-44 | **Confirmed:** erasure de-identifies. Audit entries reference patient IDs and hold no personal data, so they stay append-only |
| A27 | NFR-9 | The owner's CSV export contains full numbers, and exporting it is audited |
| A28 | NFR-15 | WCAG 2.1 AA is the accessibility target |
| A29 | NFR-18, Q5 | No uptime target for v1 |
| A30 | SM-10 | The phone-traffic baseline is reported by the clinic; none exists yet |
