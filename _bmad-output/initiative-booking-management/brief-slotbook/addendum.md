---
title: "Product Brief Addendum: SlotBook"
created: 2026-10-09
updated: 2026-10-09
---

# Addendum — SlotBook brief

Detail for the PRD and architecture steps that does not belong in the brief itself.

## OTP channel: options considered

**Decision: option (c), a free SMS quota, with (a) as the fallback.** Phone-and-OTP
sign-in clashes with the zero-cost constraint, because SMS is never free per
message.

| Option | What it is | Verdict |
|---|---|---|
| a. Free channel first, SMS later | Mobile number as the ID; code sent by email or browser push; SMS added after a sale | Fallback if no free SMS quota exists |
| b. No OTP in v1 | Mobile number not verified; the door ticket does all the fraud prevention | Rejected: too weak against hoarding and resale |
| c. Free SMS quota during build | Use a provider's free SMS allowance until a sale | **Chosen.** Acceptable because there are no live patients before a sale |

**Note for architecture:** put SMS behind a channel interface so that switching
between (c) and (a), or moving to a paid provider, is a configuration change.
Free-tier quotas change without notice.

## Problem → feature map

| Problem | Main control | Backup control |
|---|---|---|
| Missed bookings | Reserved slots the practitioner can rebook patients into | Practitioner opens unused reserved slots to everyone |
| Hoarding and resale | Per-number limits backed by OTP | Ticket tied to the mobile number and checked at the door |
| False claims at the door | QR code plus verification code | Single-use tickets, valid only on the slot's date |
| Clinic impersonation | Public ticket-check page and one official address | Outside the product's control; patient education |

## Learning focus areas this product must exercise

The product exists partly to build the developer's skills. Architecture choices
should give each focus area a genuine job, not a token one:

- **Multi-tenancy:** data that is tenant-aware from day one, with one tenant live
- **Security:** OTP abuse, the ticket forgery model, and access checks across users
  and tenants
- **Messaging:** notifications and reminders are natural asynchronous work
- **Azure:** must fit within free tiers; the zero-cost constraint is itself a design
  input
- **DDD / Clean Architecture:** booking, slot and limit rules are the core domain
- **System design:** the concurrent double-booking problem is the signature piece
