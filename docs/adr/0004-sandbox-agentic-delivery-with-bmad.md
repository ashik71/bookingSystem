# ADR-0004: Sandbox agentic delivery — developer plans, agent codes, BMAD for planning

- **Status:** Accepted
- **Date:** 2026-10-09
- **Deciders:** MD Ashik Ashrafe
- **Supersedes:** ADR-0001 (deleted from the repo 2026-10-09)

## Context

ADR-0001 rejected BMAD because its engine is AI agents writing the code, and this
project's contract was that the developer writes all production code. ADR-0001 named
its own revisit trigger: *"the decision is made to let AI write production code."*

That decision has now been made. The developer will run the build as a **sandbox
agentic workflow**:

- Claude Code runs headless (`claude -p`) inside a disposable Docker container
- GitHub Issues with `ai:*` labels act as the state machine; every transition that
  matters is a human label change
- One story = one plan run + one implement run (+ fix runs) = one pull request
- The developer reviews the plan and comments on the PR; the agent replies to each
  comment and pushes fixes; the developer is the only one who merges, and `main` is
  protected
- Frontend and backend are both built this way, as vertical-slice stories

The six focus areas (Azure, system design, RabbitMQ & Kafka, multi-tenancy,
security, Clean Architecture/DDD) and the SlotBook product definition are unchanged.
Agentic delivery is added as a seventh.

## Options considered

### Option A — Developer writes all code (status quo, ADR-0001)

**Pros:** maximal hands-on depth in every focus area.
**Cons:** slow; does not exercise agentic delivery, which the developer now wants to
learn and use.

### Option B — Hybrid: agent builds scaffolding, developer hand-writes the core

**Pros:** keeps hand-built depth where the focus areas live (domain model,
double-booking guard, tenant isolation, outbox).
**Cons:** two delivery modes to manage; the developer chose against it.

### Option C — Full agentic delivery (chosen)

**Pros:** much higher throughput; the developer's time goes into planning,
architecture, specification and review, which are senior-level activities; builds
real fluency in running an agentic pipeline safely.
**Cons:** see Consequences.

## Decision

**Full agentic delivery.** The developer owns the brief, PRD, UX, architecture,
specs, stories, plan review and PR review. The agent writes all implementation code
and opens PRs. The developer merges.

**BMAD is adopted for the planning chain:** `bmad-product-brief` → `bmad-prd` →
`bmad-ux` → `bmad-architecture` → per epic `bmad-spec` + `bmad-ticket` → stories
become GitHub Issues → `bmad-build` (interactive, early foundation stories) →
`bmad-build-auto` (sandbox) → `bmad-retrospective` per epic.

**Work hierarchy:** Epic → Feature → Story → Task, mapped to GitHub as Milestone →
`feature:<slug>` label → Issue → checklist in the issue. Each story is a vertical
slice (API + UI + tests) sized to one PR.

**PR review loop:** the developer comments on the PR and sets `ai:changes-requested`.
The agent's `fix` run answers every unresolved comment, either with the commit that
fixed it or with its reasoning, then pushes to the same branch and re-runs the tests.
The developer resolves the threads and merges.

Planning happens in interactive sessions where the developer answers questions;
the sandbox is for execution only. Process detail: `docs/process/SANDBOX-WORKFLOW.md`.

## Consequences

**Positive:**
- Throughput: about one story per day at 20–40 minutes of developer attention
- Planning artifacts become load-bearing — every sandbox run reads them, so they
  stay current
- Agentic delivery itself becomes a demonstrable skill

**Negative:**
- For the core focus-area code (aggregates, concurrency guard, tenant isolation,
  outbox), the developer's depth comes from *specifying and reviewing* rather than
  *writing*. Compensate by writing sharp acceptance criteria and reviewing those PRs
  hard — be able to explain every line
- Usage limits on a Pro subscription cap daily throughput; stories must be sized to
  one sitting (about 3–8 files)

**Neutral / follow-up:**
- The existing `src/` skeleton was removed (2026-10-09) so the pipeline starts from
  an empty solution
- `CLAUDE.md` is rewritten for the new contract: it is also the instruction file every
  sandbox run reads, so lessons from PR review get added there
- OpenSpec's role (per-change `proposal/design/tasks`) overlaps with BMAD spec and
  ticket output; which one owns per-change specs is open
- The sandbox gets exactly two secrets: a Claude token and a repo-scoped GitHub
  token. No client data or credentials ever enter the repo or its test data

## Revisit when

- PR review stops catching design errors in focus-area code — a sign the review is
  rubber-stamping and the hybrid option should be reconsidered
- Usage limits make one story per day unachievable for two consecutive weeks
