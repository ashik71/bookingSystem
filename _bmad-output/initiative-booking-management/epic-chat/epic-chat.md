---
type: epic
title: "Patients and the clinic talk in one conversation"
parent: initiative-booking-management
covers: [FR-34, FR-35, FR-36, FR-37]
after: [epic-launch]
assignee: ""
risk: medium
---

# Patients and the clinic talk in one conversation

## Description

Fast-follow after launch. Each patient has one text conversation with the clinic.
Practitioners and owners answer from a shared inbox, with canned replies. This is
asynchronous messaging, not a promise of live support: the patient sees the owner's
expected response time. New messages appear without a page reload. A reply reaches a
patient who isn't looking by push only, never by SMS.

## Outcome

UJ-8 works: a patient asks a question and gets an answer without phoning. The expected
response time is a setting, not a target (SM-C3).

## Done when

1. A signed-in patient sends text messages of up to 1,000 characters, at most 20 an hour.
   They see the whole conversation in order, with times and who replied. The page shows
   the expected response time and a note to phone in an emergency (FR-34).
2. While the conversation is open, a new reply appears within 10 seconds without a
   reload. In the inbox, new patient messages also appear within 10 seconds (FR-34,
   FR-35).
3. The inbox lists unanswered conversations first, then the rest by latest activity,
   with an unanswered count shared by all staff. Opening a conversation shows:
   - the masked number;
   - the attendee name from the latest booking;
   - the patient's active bookings, with reason category and note only as FR-16 allows.

   Done and reopen work (FR-35).
4. Owners manage canned replies per language, and staff insert and edit one before
   sending (FR-36). When the patient isn't viewing, a reply goes by push if it's on,
   otherwise not at all, and never by SMS. The home page shows an unread badge (FR-37).
   Delete my data now also removes conversation text (FR-44).
5. The standard epic checks pass, in production (initiative, *Standard epic checks*).
   SC-7 covers the real-time transport, and SC-4 refuses practitioners on canned-reply
   management.

## Boundaries

The boundary is the patient conversation, the clinic inbox and canned replies. It is not
live support, and has no attachments, voice or video (PRD §6). Touch point: the real-time
transport, configured here. Shared with epic 10: FR-44 is extended here to conversation
text.

## References

- parent — _bmad-output/initiative-booking-management/prd-slotbook/prd-slotbook.md §4.9 (FR-34 to FR-37), §2.3 UJ-8, §4.12 FR-44
- constraint — the same PRD §5.2 NFR-6, NFR-7, §5.5 NFR-16
- addendum — _bmad-output/initiative-booking-management/prd-slotbook/addendum.md, section Chat at scale
- architecture — not yet written; sections for the real-time transport and messaging
- ux — not yet written; the conversation page and the inbox

## Notes

- Decision: fast-follow, shipped after the go-live line (developer, 2026-10-10).
- Unknown: the real-time transport must run on a free tier at about 125 chats a day; the
  architecture step chooses it.
- Waits on epic-launch because: fast-follow epics ship to production after the first real
  booking, and FR-44 deletion must exist to be extended.
- Waits on epic-notifications because: replies go by push.
- Waits on epic-schedule-and-publication because: it needs the rate limiter and the home
  page (the badge).
- Waits on epic-announcements because: both change the patient home page, so the changes
  are made one after the other.
