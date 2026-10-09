# SlotBook — instructions for AI agents

SlotBook is a multi-tenant appointment booking platform (web frontend + .NET
backend), built through a **sandbox agentic workflow**. The developer plans with
BMAD and reviews; Claude Code writes the code in a disposable Docker sandbox and
delivers it as pull requests. Settled in ADR-0004; full process in
`docs/process/SANDBOX-WORKFLOW.md`.

**Which mode are you in?**

- **Sandbox run** (headless `claude -p`, working on a GitHub Issue) → follow
  *Build runs* below.
- **Interactive session with the developer** (planning, BMAD skills, reviews) →
  follow *Interactive sessions* below.

Both modes follow *Rules for everyone*.

---

## Rules for everyone

- **All documents are written in English**, even when the developer writes in Bengali.
- **Client-IP firewall.** The developer has a separate paying client. This repo may
  become public. Never commit any client's name, branding, colours, screenshots,
  code, pricing, real data or domain specifics. Use generic language only: *tenant,
  clinic, practitioner, patient, slot, booking*.
- **Settled decisions stay settled.** If an ADR exists, the question is closed.
  Reopen it only if the developer asks or new evidence contradicts it.
- **Never put secrets in the repo**: no tokens, connection strings or keys. Use
  local config files (`*.local.json` is gitignored) or environment variables.

## Build runs (sandbox)

**Read first:** the issue (story, acceptance criteria, task checklist), this file,
and the planning docs it points to in `_bmad-output/initiative-booking-management/`
(PRD, UX, architecture, the epic's spec). The architecture document overrides
anything here about stack and layout.

**Modes:**

| Mode | You do | You finish by |
|---|---|---|
| `plan` | Read the story and the code; write a plan: files, approach, tests. More than ~10 files → propose a split instead | Posting the plan as an issue comment |
| `implement` | Work on branch `ai/<issue>-<slug>`; update `.ai/<issue>.md` after each step; write tests with the code | `dotnet test` + frontend tests green, push, open a PR with `Closes #<n>` |
| `fix` | Read **every** unresolved review comment on the PR; fix it or reply with your reasoning | One reply per comment naming the commit that fixed it, or why not; tests green; push to the same branch |

**Hard rules:**

- Never merge, never push to `main`, never force-push a reviewed branch.
- Never resolve review threads; the developer does.
- Stay inside the story. Note anything out of scope in the PR description instead of
  doing it.
- Don't edit planning docs (brief, PRD, UX, architecture, ADRs) or this file.
  Propose changes in a PR or issue comment.
- Every PR states what was tested and how, and anything left undone.
- On a usage-limit stop: commit `WIP: paused at limit`, push, and leave
  `.ai/<issue>.md` accurate.

**Stack and conventions:** .NET 10 (ADR-0003). Everything else (frontend framework,
database, ORM, auth, repo layout, error format, test strategy) is decided in the
BMAD architecture document. Follow it exactly. If it is silent on something, follow
the nearest existing pattern in the code, and flag the gap in the PR.

### Lessons from PR review

Rules added by the developer after the agent repeated a mistake. They override
everything above except *Rules for everyone*.

- *(none yet)*

## Interactive sessions

### Start of session

1. Read `docs/STATE.md`: where the project is, what's next, what's open.
2. Read the newest log in `docs/sessions/`.
3. Check `_bmad-output/initiative-booking-management/` for in-progress BMAD drafts
   and offer to resume them.
4. Confirm the starting point with the developer before starting work.

### End of session

Two writes, non-negotiable: **update `docs/STATE.md`** (short, an index) and **write
`docs/sessions/NNN-<slug>.md`** from `docs/sessions/TEMPLATE.md`. Then commit. If the
session runs long, write them before you run out of room.

### Working with the developer

- **Senior .NET engineer** (6+ years .NET, Angular, MongoDB). Don't explain
  fundamentals; start at the design-decision level. SQL is the weak spot (indexes,
  isolation levels, locking), so be thorough there.
- **Push back on design and requirements:** give 2–4 real alternatives, recommend one,
  and ask *"at what scale is this choice wrong?"*. Once the developer decides,
  record the decision and move on. Don't re-argue it.
- **Interrogate, don't invent.** Ask specific questions instead of filling gaps with
  plausible requirements. Any guess must be tagged `[ASSUMPTION]`.
- **Requirements must be agent-proof.** Acceptance criteria are what the sandbox
  agent builds against. Make them testable, unambiguous, and consistent in their
  terms.

## Focus areas — architecture must give each a real job

1. Azure (primary cloud; AWS as secondary comparison)
2. System design
3. RabbitMQ & Kafka
4. Multi-tenancy
5. Security
6. Code architecture: Clean Architecture, DDD
7. Agentic delivery: the sandbox pipeline itself

If something serves none of these and isn't needed by the product, propose cutting it.

## Where things go

| Path | Contents |
|---|---|
| `docs/STATE.md` | **Where we are.** Read first, write last |
| `docs/sessions/` | One log per interactive session |
| `docs/PROJECT-CONTEXT.md` | What the project is and why |
| `docs/process/SANDBOX-WORKFLOW.md` | The delivery process: hierarchy, labels, PR loop, sandbox |
| `docs/adr/` | Lasting decisions. *If it will still be true after this feature ships, it's an ADR* |
| `_bmad-output/initiative-booking-management/` | BMAD planning output: brief, PRD, UX, architecture, epic specs, tickets |
| `sandbox/` | Sandbox image, job script, prompt templates (to be built) |
| `src/` | Application code: frontend and backend, written by the agent, merged by PR |
| `.ai/<issue>.md` | Per-story progress log: pause and resume during the build, kept after merge as build history |
| `docs/learning/LEARNINGS.md` | Developer's notes per epic: what was hard, what the agent got wrong |
| `docs/archive/` | The old hand-coding curriculum, roadmap and lessons. Reference only |
