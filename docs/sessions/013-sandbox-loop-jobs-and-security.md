# Session 013 — sandbox-loop-jobs-and-security: inbox, timed jobs and the browser-security baseline

**Date:** 2026-10-12 · **Duration:** ~6h · **Stage:** build review

## What was covered

- Claude drove the sandbox loop all session. **The developer authorised Claude to
  approve and merge PRs as them, for this session only.** The standing rule is unchanged:
  from the next session Claude stops at `ai:pr-ready`.
- Built and merged 6b (#37), story 7 split into 7a (#28) and 7b (#40), and story 8
  split into 8 (#29), 8a (#43) and 8b (#44). See PRs below.
- The developer finished both pieces of homework: the Render deploy hooks are GitHub
  secrets (`RENDER_DEPLOY_HOOK_API`, `RENDER_DEPLOY_HOOK_WEB`), and the Grafana Cloud free
  account exists with its OTLP variables set on `slotbook-api` in Render (endpoint in
  ap-south-1, header built by Grafana). Claude set the repo variable `DEMO_API_URL`.
  The Grafana Synthetics check is not created yet; story 11 defines it.
- Fixed three pipeline faults found on the way (see Gaps).
- Synced the local ticket tree: story files for 18 (#40), 19 (#43), 20 (#44); stories 7
  and 8 retitled; story 10 now waits on 17–20 as well (also on GitHub #31); split
  decisions in the epic notes.

## Decisions reached

| Decision | Recorded in |
|---|---|
| Inbox cleanup: MassTransit's cleanup service stays off; a TTL index on `Delivered` (30 min, same constant as the duplicate-detection window) removes delivered inbox rows | PR #39; epic notes |
| Story 7 split: lease + runner (7a) and heartbeat + unmapped `/internal/jobs` seam (7b) | #28 approval; epic notes |
| Leases are per run, released at the end, 30 s renewed every 10 s; **each acquisition has its own token** that renew and release match on | #28 approval; PR #41 review |
| Heartbeat: compare-and-set watermark in `leases`, epoch-aligned ticks, cap 1440, off by default (story 10 turns it on) | #28 approval; PR #42 |
| Story 8 split in three: OpenAPI + headers (#29), XSRF (#43), schemes (#44) | #29 approval; epic notes |
| `GET /api/xsrf-token`; cookies `__Host-sb_xsrf` + `XSRF-TOKEN`; XSRF failure 400 `xsrf_invalid`; 401/403 `unauthenticated`/`forbidden` | #29 approval |
| Endpoint fitness test covers every routed endpoint; no fallback deny-all policy yet | #29 approval |
| API sends HSTS outside Development; static-site CSP adds `frame-ancestors 'none'; base-uri 'self'; object-src 'none'; form-action 'self'` | #29 approval; PR #45 |
| The API trusts `X-Forwarded-Proto` only (one hop, any proxy); client IP stays with AD-22 / story 10 | PR #46 review |

## Pressure-test — where the developer was challenged

None. The developer delegated design-level answers to Claude again and spent the
session on the two hitl setup tasks.

## Artifacts produced

- Issues #40, #43, #44 (split children, each with its plan pasted in); #28 and #29 retitled
- PRs merged: #39, #41, #42, #45, #46, #47
- Pipeline commits on main (admin bypass): `d04eb39` sidecar oplog cap, `b6d9212`
  mongosh `HOME=/tmp` in CI and the sandbox, `434a372` push retry
- Repo variable `DEMO_API_URL`; secrets `RENDER_DEPLOY_HOOK_API`, `RENDER_DEPLOY_HOOK_WEB`

## PRs reviewed

| PR | Story | Iterations | What the agent got wrong | Root cause |
|---|---|---|---|---|
| #39 | 6b #37 | 2 | Left inbox rows to grow forever (cleanup disabled, flagged as a follow-up) | story (6b issue didn't carry 6a's "6b turns it on") |
| #41 | 7a #28 | 2 (+1 blocked run) | Lease identity per host, not per acquisition: a stale run could release a newer lease | agent |
| #42 | 7b #40 | 1 (+1 lost run) | First run never saw the plan and guessed the defaults | reviewer (Claude didn't paste the plan into the child issue) |
| #45 | 8 #29 | 1 | nothing | — |
| #46 | 8a #43 | 2 | Secure antiforgery cookies with no forwarded-proto handling: 500 on Render | spec gap (AD-22 defers forwarded headers to story 10) + tests only over https |
| #47 | 8b #44 | 1 | nothing (noted a slow scan host for story 12) | — |

Sandbox spend this session: about $12.65.

## Rules added to `CLAUDE.md`

None. Reviewer rule (for Claude, not the sandbox agent): **every split child issue gets
its plan pasted in before its first run**; the sandbox reads only that issue.

## Gaps exposed

- **Sandbox MongoDB sidecar ran out of memory.** The replica-set oplog defaults to a
  990 MB minimum in tmpfs inside a 1 GB limit; repeated test runs stalled and then killed
  it. Capped at 128 MB.
- **CI MongoDB startup race.** The first `mongosh` probe wrote under `/data/db` while the
  image entrypoint was chowning it, so mongod sometimes never started. Fixed with
  `HOME=/tmp`.
- **A transient GitHub push error lost a whole run** (the container is disposable). The
  entrypoint now retries the push with backoff.
- **Tests over `https://localhost` hide proxy behaviour.** Anything scheme-sensitive needs
  a plain-http test with forwarded headers.
- The Colima VM (4 GiB, no swap) is smaller than the sum of the container limits
  (3 GB + 1 GB + 512 MB). Fine with the oplog cap; watch it.

## Homework set

- [ ] Optional before story 11: Grafana Synthetics HTTP check on `/health/ready` (Claude guides)
- [ ] Carried: brief entry in `docs/learning/LEARNINGS.md`; `colima stop` after sessions; Node ≥ 24.15

## Parked for later

- Correct-course for epics 2–12 and epic 2 inception: still wait for UX.
- The team slide deck.

## Next session

1. Story 9 (#30): deploy on merge. Secrets and `DEMO_API_URL` are ready; needs a plan run.
2. Story 11 (#32): Grafana. OTLP variables are set in Render; needs a plan run.
3. Story 10 (#31): free-tier proofs, after 9 (hitl: heartbeat on in Render).
4. Story 12 (#33): refactor sweep, last.
