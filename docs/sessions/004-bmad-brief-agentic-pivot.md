# Session 004 — Delivery pivot and BMAD product brief

**Date:** 2026-10-09 · **Duration:** ~2h · **Block:** planning · **Topic:** product brief

## What was covered

- The developer adopted the **sandbox agentic workflow**: Claude Code writes all
  code in a disposable Docker sandbox, one GitHub Issue becomes one PR, and the
  developer plans, reviews and merges. Process reference: `docs/AIAgenticGuideline/`
  (untracked)
- ADR-0004 written; ADR-0001 marked superseded; `CLAUDE.md` contract rewritten
- `src/` and `BookingSystem.sln` wiped so the build starts from scratch
- BMAD initiative `initiative-booking-management` created
- **Product brief finished** with `bmad-product-brief` on the Fast path, then given
  a structure and prose review:
  `_bmad-output/initiative-booking-management/brief-slotbook/` (brief, addendum,
  `.memlog.md`)

## Decisions reached

| Decision | Recorded in |
|---|---|
| Full agentic delivery; BMAD for planning; agent never merges | ADR-0004 |
| SlotBook stays a generic platform; the first real clinic is described only generically | Brief: Constraints |
| Web only, standalone; no mobile app, no payment in v1 | Brief: Constraints, Scope |
| Roles: clinic owner (super admin), practitioner (also does the door check), patient | Brief: Who This Serves |
| Multi-tenancy is planned for later: one tenant in v1, data tenant-aware from day one | Brief + addendum |
| Zero running cost before a sale; SMS on a free quota, falling back to email or browser push | Brief: Constraints; addendum |
| Reserved slots: the practitioner holds some back each week, rebooks patients who missed theirs, and opens unused ones to everyone | Brief: Solution 5 |
| No no-show tracking or penalty in v1 | Brief: Solution 6 |
| All documents in English | `CLAUDE.md` |

## Pressure-test — where the developer was challenged

- **Agent writes everything vs a hybrid.** The AI argued that focus-area code
  (concurrency guard, tenant isolation) is learned better by writing it than by
  reviewing it. The developer held firm on full agentic delivery. This is recorded
  as a negative consequence in ADR-0004, with a revisit trigger. **Held, by
  decision.**
- **Client IP.** The developer's first answers described the client's domain
  directly. The firewall conflict was raised and the developer chose to keep the
  platform generic. **Held.**
- **Zero cost vs SMS OTP.** The developer hadn't seen that SMS always costs money
  per message. They chose a free SMS quota, accepting the risk because there are no
  live patients before a sale. **Held, with a fallback.**
- **"Fraud" was too vague.** The developer was asked to split it into five specific
  abuse types and said all five apply. **Weak:** it took no position on which
  matters most, so every control has the same priority.
- **No-show penalty.** The AI invented a block-after-N-no-shows rule. The developer
  questioned it, and that surfaced the real requirement: reserved slots. A good
  catch, and a warning that Fast path `[ASSUMPTION]` tags need a real review.

## Code the developer wrote

None. Under ADR-0004 the agent writes code; none has been written yet.

## Review findings

The structure and prose review of the brief found one terminology problem that
mattered: "staff" at the door check, when v1 has no staff role, which an agent
might turn into an extra role. Fixed. The other fixes were minor.

## Gaps exposed

- Requirements came in short and were filled in through follow-up questions. The
  PRD step needs firmer answers, especially the numbers
- Whether a free SMS quota exists for local numbers is **unverified**. The whole
  OTP design depends on it
- Open for the PRD: how reserved slots fit with publication timing, whether
  practitioner-made bookings count towards a patient's limit, the per-number limits,
  how far ahead patients can book, cancellation cut-offs, and whether the door check
  works offline

## Homework set

- [ ] Guideline Phase 0: Mac setup, `claude setup-token`, a repo-scoped
      fine-grained PAT, and protection on `main`
- [ ] Scrub the client reference from `docs/AIAgenticGuideline/` before committing
      it

## Parked for later

- Scrubbing the client name and price from ADR-0001 and git history: needed before
  the repo goes public
- OpenSpec vs BMAD spec/ticket for per-change specs: decide before the first epic's
  tickets
- Selling to other clinics: kept as a note only, not a plan

## Next session

`bmad-prd` (create), in a fresh session as the guideline recommends. It reads the
brief. Have numbers ready for the PRD open questions above.
