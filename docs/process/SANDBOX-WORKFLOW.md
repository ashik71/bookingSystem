# Sandbox Agentic Workflow

How SlotBook is built: the developer plans and reviews, and Claude Code writes the
frontend and backend inside a disposable Docker sandbox. Every change reaches `main`
through a pull request that the developer has reviewed and merged.

Decision record: [ADR-0004](../adr/0004-sandbox-agentic-delivery-with-bmad.md).

## Roles

| Who | Does | Never does |
|---|---|---|
| **Developer** | Plans with BMAD, approves plans, reviews PRs, comments, merges, moves labels | Writes production code (by choice, not by rule) |
| **Agent in the sandbox** | Plans a story, writes code and tests, opens the PR, replies to review comments and fixes them | Merges, pushes to `main`, edits planning docs, sees secrets beyond its two tokens |
| **Agent in an interactive session** | Runs BMAD planning skills, pressure-tests decisions, keeps docs current | Invents requirements to fill gaps |

## Work hierarchy

| Level | Produced by | Lives in GitHub as | Size |
|---|---|---|---|
| **Initiative** | `bmad` | The repo itself (`initiative-booking-management`) | The product |
| **Epic** | `bmad-ticket` | **Milestone** | A deliverable capability, weeks of work |
| **Feature** | `bmad-ticket` / `bmad-spec` | **Label** `feature:<slug>` | A user-facing function spanning a few stories |
| **Story** | `bmad-ticket` | **Issue** labelled `story` + its feature label + its milestone | One sitting: one PR, about 3–8 files |
| **Task** | `bmad-ticket` | **Checklist** inside the issue | A step the agent ticks off |

**Stories are vertical slices.** Each one carries its API endpoint, its UI, and its
tests. For example, "Patient can cancel a booking" means endpoint + screen + tests.
Separate backend-only and frontend-only stories leave nothing working end to end
until late.

## The planning chain (interactive, on the Mac)

Run one BMAD skill per session and start a new session for the next skill, so that
earlier conversation doesn't eat the usage window.

| Step | Skill | Output |
|---|---|---|
| 1 | `bmad-product-brief` | Brief + addendum ✅ |
| 2 | `bmad-prd` (create, then validate) | PRD: testable functional requirements and NFRs |
| 3 | `bmad-ux` | `DESIGN.md`, `EXPERIENCE.md`: screens, states, layout, RTL |
| 4 | `bmad-architecture` | Architecture: repo layout, frontend framework, API contract, data, auth, conventions |
| 5 | `bmad-spec` per epic | A compact spec for that epic |
| 6 | `bmad-ticket` per epic | Features, stories and tasks in build order |
| 7 | Script: `gh issue create` | Stories become GitHub Issues |

Plan the next epic only when it is about to start. What the previous epic teaches
changes the next one.

Outputs live in `_bmad-output/initiative-booking-management/` and are committed, so
every sandbox run has the full context.

## Labels: the state machine

The worker reacts only to the states marked *worker*. Every other move is the
developer changing a label, which keeps a human gate at each step.

| Label | Set by | Meaning | Next |
|---|---|---|---|
| `ai:queue` | Developer | Build this story next | Worker picks the oldest one |
| `ai:planning` | Worker | Plan run in progress | → `ai:plan-review` |
| `ai:plan-review` | Worker | Plan posted as an issue comment | Developer approves → `ai:implementing`, or comments → `ai:planning` |
| `ai:implementing` | Developer | Plan approved; *worker* runs the code job | → `ai:pr-ready` |
| `ai:pr-ready` | Worker | PR open, tests green | Developer reviews |
| `ai:changes-requested` | Developer | Review comments left on the PR; *worker* runs the fix job | → `ai:pr-ready` |
| `ai:paused-limit` | Worker | Usage limit hit; work saved on the branch | Worker resumes after the reset |
| `ai:failed` | Worker | Run failed for a non-limit reason | Developer reads the log and re-queues |

Merging the PR (developer only) closes the issue through `Closes #<n>`.

## One story, end to end

1. **Queue.** The developer adds `ai:queue` to one story issue.
2. **Plan run.** In a fresh container the agent reads the issue, `CLAUDE.md` and the
   planning docs, then posts a plan comment: files to touch, approach, and tests.
   If the plan touches more than about 10 files, it says so and proposes a split.
3. **Plan review (about 10 minutes).** Right files, right approach, tests included?
   Approve or comment.
4. **Implement run.** The agent works on branch `ai/<issue>-<slug>` and updates
   `.ai/<issue>.md` after each step. It runs `dotnet test` and the frontend tests,
   pushes, and opens a PR with `Closes #<n>`.
5. **PR review (about 15 minutes).** Read the diff and run the app locally once.
   Leave review comments on specific lines, then set `ai:changes-requested`.
6. **Fix run.** The agent reads every unresolved review comment. It replies to each
   one, saying either what it changed and in which commit, or why it disagrees.
   Then it pushes the fixes to the same branch, re-runs the tests and sets
   `ai:pr-ready`. It never resolves threads; the developer does.
7. **Repeat 5–6** until satisfied, then **merge**.
8. **Learn.** If the agent made a mistake that will repeat, add a rule to
   `CLAUDE.md` → *Lessons from PR review*.

## The sandbox

- **Image** (`sandbox/Dockerfile`): the .NET 10 SDK image (ARM64) with Node, git, `gh`,
  jq and a pinned Claude Code installed. `run-job.sh` rebuilds it automatically when
  `Dockerfile` or `entrypoint.sh` change.
- **Job script** (`sandbox/run-job.sh <issue> <plan|implement|fix>`): reads the two
  secrets from the Keychain and starts a fresh container (`--memory 3g --cpus 3`, all
  capabilities dropped except the two needed to switch user). It pipes in
  `sandbox/prompts/<mode>.md` and saves the run to
  `$SLOTBOOK_RUNS/<issue>-<mode>-<time>.{jsonl,log,meta.json}`. The container is
  removed afterwards. Exit codes: `0` done, `10` paused at the usage limit, `11` hit
  max turns, `12` agent blocked, `1` failed. The worker maps these to labels;
  `run-job.sh` never touches labels.
- **Two users inside the container** (`sandbox/entrypoint.sh`). The repo is public, so
  anyone can write an issue comment, and that comment could carry a prompt injection.
  So **the agent never holds the GitHub token**:
  - *root* (no capabilities beyond switching user) holds `GH_TOKEN`. It reads the
    issue, passing on only comments by trusted authors (repo owner + bot). For `fix`
    it also reads the unresolved review threads. It pushes the branch and posts the
    agent's outputs.
  - *agent* (non-root, a different uid, so it can't read root's environment) runs
    `claude -p --dangerously-skip-permissions` with the Claude token only. It commits
    locally and writes its outputs to `/work/out`: `plan.md`; `pr-title.txt` +
    `pr-body.md`; `replies.json` (one reply per `comment_id`) + optional
    `summary.md`; or `blocked.md`.
  - root pushes only `ai/<issue>-*`, with no force, via a separate bare clone, so it
    never runs git inside the agent's working copy. It adds `Closes #<n>` if missing.
    If the run stops mid-way, leftover changes are committed as `WIP: …` and pushed.
- **Worker:** a background service that polls GitHub every 60 seconds and runs at
  most one job at a time. It later becomes the backend of the dashboard.
- **Dashboard** (its own epic, built through this same pipeline): ASP.NET Core +
  SignalR. Status cards, a board by label, the current run with a live log,
  controls, run history and a usage view.

Inside the container the agent may act without permission prompts, because the box
is disposable and holds no secrets beyond two narrow tokens. **Never do that on the
Mac itself.**

## Usage limits and story sizing

- Size each story to one sitting: one feature area, about 3–8 files, with clear
  acceptance criteria.
- Always plan before implementing. A rejected plan costs far less than a rejected
  implementation.
- Cap every run with `--max-turns`.
- **When a usage limit is hit:** the job commits `WIP: paused at limit` and pushes the
  branch. The `session_id` is saved in the run's `.meta.json`, and the worker sets
  `ai:paused-limit`. After the reset, re-run `implement`. The fresh run finds the
  existing branch and continues from `.ai/<issue>.md` and `git log`. `claude --resume`
  isn't used, because the session file is lost when the container is removed.
- Never have more than one half-finished branch. Let a paused story resume before
  starting a new one.
- Measure for two weeks before deciding whether a bigger plan is worth paying for.

## Daily routine

| When | Time | Do |
|---|---|---|
| Morning | 10 min | Review yesterday's PR → comment (`ai:changes-requested`) or merge. Queue the next story |
| Midday | 10 min | Review the plan → approve or comment |
| Evening | 15 min | Review the PR and the agent's replies. Note repeat mistakes in `CLAUDE.md` |

## Safety rules (never break these)

- The sandbox gets exactly two secrets: a Claude token, and the bot account's
  `public_repo` token (see *One-time setup*). Only the job script inside the
  container holds the GitHub token; the agent never does.
- `main` is protected. Only the developer merges.
- Never mount the home folder into the container. The repo is cloned fresh inside.
- No client data, names or credentials ever enter this repo or its test data.

## One-time setup

**Host:** MacBook Air M1, 8 GB. **Container runtime: Colima.** It's free and MIT
licensed, so there's no licence question for a product that will be sold. It's
command-line only, so the worker can start and stop it from scripts.

**All data lives on the external drive**, never the internal disk. `~/.zshrc` sets
`DEVCACHES` and, under it, `COLIMA_HOME` (the VM disk, holding all Docker images and
volumes), `SLOTBOOK_RUNS` (run logs and saved sessions), and the NuGet, npm, uv and
Homebrew caches. Lima's image cache is symlinked there too. If the drive is
unplugged, the sandbox doesn't run.

1. ✅ Tools: `git gh node dotnet jq uv colima` (Homebrew).
2. ✅ VM, started on demand rather than at login:
   ```sh
   colima start --cpu 4 --memory 4 --disk 40 --arch aarch64 \
     --vm-type vz --mount-type virtiofs --mount "$SLOTBOOK_RUNS:w"
   ```
   Only the runs folder is shared into the VM. Your home folder is not visible
   inside it. Stop it with `colima stop` when you're done, to free RAM.
3. ✅ `gh auth login` (browser).
4. ✅ Sandbox secrets, stored in the macOS Keychain and never in a file:
   - Claude: `claude setup-token`, then
     `security add-generic-password -a "$USER" -s slotbook-sandbox-claude -w`
   - GitHub: a classic PAT with only the `public_repo` scope, created on the **bot
     account** (see below), then
     `security add-generic-password -a "$USER" -s slotbook-sandbox-github -w`
5. ✅ Protect `main` with a ruleset: require a PR with 1 approval, block force
   pushes, restrict deletions. Only the repo admin (the developer) can bypass.
6. ✅ Create the labels above with `gh label create`.
7. ✅ Build the sandbox image and test `run-job.sh` by hand on one issue before
   building the worker.

**The agent has its own GitHub identity.** The sandbox uses a separate free bot
account with **Write** access to this repo. That way PRs, plan comments and replies
to review comments appear as the bot, not as the developer. The developer can
*approve* the bot's PRs, and the ruleset's single required approval stops the bot
merging its own work. The developer's admin bypass lets docs go straight to `main`.
The bot is not on the bypass list. The job script also pushes only the
`ai/<issue>-*` refspec, as a second guard.

The repo is **public**, because GitHub Free only offers rulesets on public repos.

Check the CLI flags with `claude --help`; names change between versions.

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Mac freezes or swaps | VM memory too high, or the IDE open during a run | VM at 4 GB, container at 3 GB, close the IDE |
| `claude` asks to log in inside the container | Token not passed, or expired | Re-run `claude setup-token` and pass it again |
| Image fails to build on Apple Silicon | x86 image | Use `--platform linux/arm64` |
| Run stops halfway | Hit `--max-turns` or the usage limit | Check the log; raise turns slightly or wait for the reset |
| PR passes tests but the app is wrong | Weak acceptance criteria | Rewrite the criteria as testable checks, then re-queue |
| Same mistake every run | Missing rule | Add it to `CLAUDE.md` |
| Agent "fixes" a comment by guessing | Vague review comment | Say what is wrong *and* what correct looks like |
