---
title: "Product Brief: SlotBook"
status: final
created: 2026-10-09
updated: 2026-10-09
---

# Product Brief: SlotBook

## Executive Summary

SlotBook is a web booking system for specialist treatment clinics that today take
appointments by phone. Patients book a slot from their phone's browser, get a
ticket that can be checked at the door, and receive reminders. The clinic owner
publishes each week's slots in one batch and controls who may book and how often.

It is built for one real clinic first, and designed so that more clinics can be
added later as separate tenants. Until a sale it must cost nothing to run. It is
also the developer's vehicle for learning cloud, messaging, multi-tenancy, security
and DDD.

## The Problem

Bookings arrive by phone and staff write them down by hand. That causes five
problems:

| Problem | What happens today |
|---|---|
| Missed bookings | A patient who misses their slot can't be fitted in again because the week is already full |
| Hoarding and resale | One person books many slots under different names, sometimes to resell them |
| False claims at the door | Someone arrives claiming a booking they don't have; staff can't check it |
| Clinic impersonation | Someone pretends to be the clinic and takes bookings or money |
| Phone bottleneck | Booking depends on getting through on the phone during opening hours |

Staff time goes on the phone and on arguments at the door. Genuine patients lose
slots to people gaming the system.

## Who This Serves

- **Patient:** books, cancels and shows a ticket, usually from a phone browser.
  Identified by a mobile number, not an email address.
- **Practitioner:** runs the clinic day to day. Manages their own availability, sees
  their bookings, checks tickets at the door and manages their reserved slots.
  There is no separate front-desk role in v1.
- **Clinic owner (super admin):** manages the clinic's practitioners, publishes the
  weekly schedule and sets the clinic's booking rules and limits.
- **Platform operator (later):** onboards new clinics. Not a v1 role. The first
  clinic is set up by hand.

## The Solution — v1

1. **Weekly publication.** On a fixed weekday the owner publishes next week's
   slots. Booking stays open until each slot is taken.
2. **Phone identity with OTP.** The patient signs in with a mobile number and a
   one-time code sent by SMS.
3. **Booking.** Choose a practitioner and a free slot, then book. A slot can never
   be double-booked, even when many people try for it at the same moment.
   Cancelling is supported; rescheduling means cancel and rebook.
4. **Verifiable ticket.** Each booking gets a QR code and a short verification
   code. At the door, the practitioner scans or types it, and the system confirms that the ticket
   is genuine, belongs to that mobile number, is for today, and hasn't been used.
5. **Reserved slots.** Each week the practitioner decides how many of their slots
   to hold back. Patients see reserved slots but can't book them. The practitioner
   books a reserved slot for a patient who missed their booking, by entering the
   patient's mobile number; the patient gets the normal ticket and notification.
   The practitioner can open any unused reserved slots to everyone (for example,
   5 reserved, 2 used, the other 3 opened).
6. **Abuse limits.** The owner sets limits per mobile number, such as active
   bookings per week. There is no no-show tracking or penalty in v1.
7. **Anti-impersonation.** There is one official booking address. A public page
   lets anyone check whether a ticket code is genuine. The product can't stop
   someone faking the clinic outside the system, but it gives patients a way to
   check.
8. **Notifications.** Booking confirmations, reminders and cancellations go out by
   SMS, or by email or browser push if no free SMS quota is available (see
   Constraints).
9. **Languages.** Bangla, English and Arabic, with full right-to-left layout for
   Arabic.

## Constraints

- **Zero running cost before a sale.** Hosting, database, SMS and email must stay
  within free tiers. Anything that would cost money needs a free alternative.
  SMS for OTP and notifications uses a provider's free quota for local numbers. If
  no such quota exists (to be checked in the architecture step), both fall back to
  email or browser push.
- **Web only.** No mobile app. Pages must work well in a phone browser.
- **No payment.** Booking is free. The booking states leave room for a payment step
  later.
- **Generic by design.** This repo may become public, so it contains no client
  names, branding or domain content. The first clinic is configured privately.
- **Built by agents.** Code is written by Claude Code in a sandbox, one GitHub Issue
  per PR. Requirements have to be precise enough that an agent can't misread them.

## What Makes This Different

Generic booking tools assume email sign-up, booking any future date, and payment
up front to stop abuse. Neither they nor a phone line combine the features in
*The Solution — v1*.

There is no technical moat. The advantage is fitting this niche closely and having a
real first client to shape it.

## Success Criteria

- **No double bookings:** a load test of 200 concurrent users competing for 10 slots
  ends with exactly 10 bookings
- **Forged tickets fail:** forged, reused, wrong-day and wrong-number tickets are all
  rejected at the door check
- **Limits hold:** one mobile number can't exceed the clinic's booking limit, even
  with requests fired in parallel
- **Reserved means reserved:** no patient can book a reserved slot until the
  practitioner opens it, including by calling the API directly
- **Fast booking:** a patient on a phone books a slot in under 2 minutes from
  opening the site
- **Fast publishing:** the owner publishes a week's schedule in under 10 minutes
- **Zero cost:** $0 spent on running the system before the first sale
- **Tenant isolation:** automated tests prove one clinic can't read or change
  another clinic's data, even though v1 has only one clinic
- **Demo-ready:** the full flow (publish, reserve, book, ticket, door check,
  rebook into a reserved slot) runs end to end in all three languages

## Scope

**In v1:** everything under *The Solution — v1*, for a single clinic.

**Out of v1:** mobile app · payment · video consultations · reviews and ratings ·
self-service onboarding for new clinics · a second live clinic · a platform-operator
console · reporting and analytics beyond simple booking lists.

## Open Questions

- Does a free SMS quota for local numbers exist, and is it large enough?
  *(architecture)*
- Exact booking limits per number *(PRD)*
- Reserved slots: does the practitioner set them before the owner publishes the
  week, or can they reserve slots after publication? Does a booking made by the
  practitioner count towards the patient's limit? *(PRD)*
- How far ahead can a patient book, and when do the cancellation cut-offs fall? *(PRD)*
- Does the door check work offline if the clinic's internet drops? *(PRD)*

## Vision

The near-term goal is one clinic running well on SlotBook.

*Note, not a plan:* selling it later, possibly to similar clinics as a multi-tenant
service, is not decided and must not drive v1 scope. The seams for it
(tenant-aware data, a payment step, paid SMS) are designed in, not built.
