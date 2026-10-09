---
title: "PRD input: prior private plan (generic extract)"
created: 2026-10-09
source: the developer's private planning folder for the first tenant (outside this repo)
---

# PRD input: prior private plan (generic extract)

The developer pointed the PRD at an earlier, private plan for the first tenant:
a proposal, five capability specs, design notes, a project state document and UI
mockups. That material names the client, so it stays outside the repo. This file
keeps **only the generic product requirements** found in it, rewritten in SlotBook
terms. No names, branding, colours, fonts, pricing, payment providers or
domain-specific content were carried over.

Where the prior plan disagrees with the final brief (`brief-slotbook.md`), the
conflict is listed in *Conflicts with the brief* below. The brief stays the source of
truth until the developer decides otherwise.

## Sizing for the first tenant

- About 1,000 patients a week at the busiest, about 140 a day on average and about
  100 at the same moment at peak
- About 300 tickets a day; the load is highest on publication day and the day after
- 2–3 staff accounts working the admin side
- Single-server workload. Overbooking is still treated as a correctness bug at any
  traffic level

## Requirements found in the prior plan

### Patient identity and OTP

- The patient's mobile number is their only identity: no username or password. The
  number is normalised to one canonical stored form, so two spellings of the same
  number cannot become two patients
- 6-digit numeric OTP, single-use, expiring **60 seconds** after it is sent, stored
  hashed
- Resend limit: none within 60 seconds of the last send, and at most **5 sends per
  number per hour**, after which the patient is told to contact the clinic
- **The OTP is skipped for a known device and number:** a booking from a device that
  has already verified that same number needs no new code. Both must match. The
  reason is SMS cost. It is accepted that a spoofed device ID only skips an SMS; it
  cannot forge a ticket
- **The clinic never asks for the code:** no staff screen shows a patient's OTP. The
  OTP screen warns that the clinic never asks for the code by phone, because
  impersonators phone patients to harvest codes
- A patient session can read only that patient's own data, and an admin can revoke it

### Weekly publication and capacity

- On a fixed weekday the clinic publishes the next week's bookable days and **time
  windows**, each with a **capacity** (for example, 08:00–10:00, 12 patients).
  Booking stays open across the whole week
- A day without windows is closed. Capacity must be a positive integer
- The calendar shows full days as unselectable. The window list shows the number of
  places left, and full windows can't be selected
- Capacity can be raised after publication, but never lowered below the number of
  bookings already confirmed
- **Closing a window** (for a holiday, say) stops new bookings and reports the
  affected bookings. It does **not** cancel them silently; the admin cancels them
  deliberately
- Past dates are never offered and never accepted
- The open question in the prior plan, *weekly template or per-date windows?*: build
  per-date windows as the primitive, with a template as a generator on top

### Booking model in the prior plan: request, then triage

- The patient fills in a short form: name, mobile, age, a **reason category** chosen
  from a fixed list set by the clinic, an optional free-text description, and a date
  and window
- Submitting creates a **request** in `pending`. A pending request holds no capacity,
  has no verification code and must never look confirmed to the patient
- Staff triage each request with a binary outcome:
  - **needs a visit:** a ticket is issued into a chosen window and capacity is used;
  - **can be handled remotely:** the patient is sent to chat, no ticket is issued and
    no capacity is used.
- Bulk approval goes request by request. If capacity runs out partway, the requests
  that fit are approved, the rest stay pending, and the result is reported per
  request. A partial result is never reported as a full success
- Approving into a window that filled up after the queue was loaded is refused, and
  the request stays pending
- The queue is ordered oldest first and can be filtered to today. The request detail
  shows whether this is the patient's first visit and links to their earlier bookings

### Ticket lifecycle

- States: `pending → confirmed → served`, plus `routed_to_chat` and `cancelled`.
  Transitions go one way only, and `served` and `cancelled` are final
- A reserved `awaiting_payment` state stays unused, so a payment step can be added
  later without rewriting the lifecycle
- The patient sees a progress timeline: each reached stage with its timestamp, and
  stages not yet reached
- The patient can cancel their own pending or confirmed booking. An admin can cancel
  any booking, but must give a reason. Cancelling releases the capacity at once
- **Abuse limit assumed in the prior plan:** one active (pending or confirmed) booking
  per verified mobile number. The prior plan calls this "load-bearing" because
  nothing else stops one number from taking every slot

### Verification code and door check

- Exactly one code is issued, at confirmation, and no code exists in any other state.
  The system generates it; no human input can choose it
- Format: a short fixed prefix plus **4 characters**. The alphabet leaves out the
  look-alikes `0 O 1 I L`, which leaves 27 symbols and about 531k combinations. Codes
  are shown in a fixed-width font so they can be read aloud
- Codes are unique **among active tickets** and may be reused after a ticket is served
  or cancelled
- The ticket shows the patient name, date, window, sequence number within the window,
  the code and a QR code of the code. It also states that a ticket without a code is
  not valid
- The door check in the prior plan is **for staff only, with authentication and a
  per-account rate limit**, so the small code space can't be enumerated from outside.
  A genuine result shows the name, date, window and sequence number. An unknown code
  gives an explicit *not genuine* result. A cancelled code reports *cancelled*
- The door check is a single text field: the code is typed, or entered by a
  keyboard-wedge scanner. No dedicated scanning hardware or app is needed

### Anti-impersonation, beyond the ticket

- An official-contacts page lists the clinic's real payment and contact numbers. It
  says that no other number belongs to the clinic, and shows the date the clinic last
  confirmed the list. Only the owner can edit it
- A permanent warning on the home screen says the clinic takes money only through the
  listed numbers
- Links to the clinic's verified social and messaging channels
- **Fraud report:** the patient reports an impersonation attempt in one tap, and the
  reports go into a separate admin list

### Staff side

- Roles: **admin** (staff) and **super admin** (head of clinic). Only the super admin
  manages staff accounts, the official numbers and settings. A disabled account loses
  access at once, including any open sessions
- Dashboard: today's tickets, requests waiting for triage, unread chats
- Patient list, searchable by name or mobile number, with each patient's history
- Mobile numbers are masked in list views and shown in full only on the (audited)
  detail screen
- **Immutable audit log:** every triage decision, state change, code issued,
  cancellation, capacity change and staff-account change. It records who, what and
  when. Nobody can edit or delete an entry, the super admin included; this is
  enforced by database permissions, not by convention

### Other features in the prior plan (outside the booking core)

- Chat between patient and clinic, with canned replies, for cases triaged as remote
- Announcements, such as closures and new rules, with images and a push notification
- A content library curated by the clinic and maintained in a small CMS, with RTL
  text, audio and draft/publish states
- A treatment history for registered patients, with staff notes on each past booking
- Reports: daily and monthly counts and the approval rate, with CSV export
- Sponsor banners, as a later phase
- Guest patients book with name and mobile only; registration is optional and lets
  history follow the patient to a new phone

### Security and privacy notes

- The patient's description of their problem is sensitive health-adjacent data.
  Access to it is audited
- HTTPS only; rate limits per device; input validation
- **Right to erasure:** a patient can delete all their data, which app-store rules
  require
- Daily backups
- The retention period for served bookings and audit entries is still open, and it is
  needed before the privacy policy is written

### Open items the prior plan left for the client

- The booking limit per number (one active booking was assumed)
- **No-show handling:** with free booking, no-shows will rise. Should repeat no-shows
  be blocked?
- Should staff mark a booking as paid or unpaid at the clinic? (A record only, not
  payment processing)
- How many slots are published each week
- How many staff accounts

## Conflicts with the brief

These need the developer's decision before the PRD can settle the affected features.

| # | Topic | Brief (final) | Prior plan |
|---|---|---|---|
| C1 | Booking model | The patient books a free slot directly; there is no approval step | The patient sends a request; staff triage it (ticket or chat) |
| C2 | Capacity unit | A slot per practitioner; the patient chooses the practitioner | A time window with capacity N and a sequence number in it; no practitioner choice |
| C3 | Reserved slots | The practitioner holds slots back and rebooks patients who missed theirs | Not present |
| C4 | Limits | Limits per number set by the owner (for example, active bookings per week) | A fixed limit of one active booking per number |
| C5 | Public ticket check | A public page lets anyone check whether a ticket code is genuine | The lookup is for staff only and authenticated, so the 4-character code space can't be probed |
| C6 | Platform | Web only, phone browser | A native mobile app (settled for SlotBook by the brief and ADR-0005: web, Angular) |
| C7 | Languages | Bangla, English and Arabic, with RTL | Bangla and English (Arabic only inside content) |
| C8 | Feature breadth | No chat, announcements, CMS, fraud report, official-numbers page or reports beyond lists | All of these are in scope |
| C9 | Roles | Patient, practitioner and owner; no front desk | Patient (guest or registered), admin (staff) and super admin; no practitioner role |
| C10 | Notifications | SMS, else email or browser push | Native push notifications |
