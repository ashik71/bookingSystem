#!/usr/bin/env bash
# Run one sandbox job on the Mac host.
#
#   sandbox/run-job.sh <issue> <plan|implement|fix>
#
# Starts a fresh, disposable container (sandbox/Dockerfile), feeds it the prompt
# template for the mode, and saves the run to $SLOTBOOK_RUNS:
#   <issue>-<mode>-<time>.jsonl      Claude's stream-json output
#   <issue>-<mode>-<time>.log        the job script's log
#   <issue>-<mode>-<time>.meta.json  outcome, session id, turns, cost
#
# Exit: 0 done · 10 paused at usage limit · 11 hit max turns · 12 agent blocked · 1 failed
#
# Env overrides: SLOTBOOK_REPO (owner/name), SLOTBOOK_MAX_TURNS, SLOTBOOK_MODEL,
#                SLOTBOOK_TRUSTED (comma-separated logins), SLOTBOOK_IMAGE
#
# Must stay compatible with macOS /bin/bash 3.2.
set -euo pipefail

die() { printf 'run-job: %s\n' "$*" >&2; exit 1; }

[ $# -eq 2 ] || die "usage: $0 <issue> <plan|implement|fix>"
ISSUE=$1 MODE=$2
[[ $ISSUE =~ ^[0-9]+$ ]] || die "issue must be a number"
case $MODE in
  plan)      default_turns=40 ;;
  implement) default_turns=150 ;;
  fix)       default_turns=80 ;;
  *) die "mode must be plan, implement or fix" ;;
esac

SANDBOX_DIR=$(cd "$(dirname "$0")" && pwd)
IMAGE=${SLOTBOOK_IMAGE:-slotbook-sandbox}
PROMPT="$SANDBOX_DIR/prompts/$MODE.md"
[ -f "$PROMPT" ] || die "missing prompt template $PROMPT"

: "${SLOTBOOK_RUNS:?not set; see docs/process/SANDBOX-WORKFLOW.md (one-time setup)}"
[ -d "$SLOTBOOK_RUNS" ] || die "$SLOTBOOK_RUNS not found. Is the external drive mounted?"

if [ -z "${SLOTBOOK_REPO:-}" ]; then
  origin=$(git -C "$SANDBOX_DIR/.." remote get-url origin)
  SLOTBOOK_REPO=$(printf '%s\n' "$origin" | sed -E 's#^(https://github\.com/|git@github\.com:)##; s#\.git$##')
fi
[[ $SLOTBOOK_REPO =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]] || die "can't work out the repo (got '$SLOTBOOK_REPO')"

docker info >/dev/null 2>&1 || die "Docker isn't reachable. Start the VM: colima start"

# Rebuild the image whenever the files baked into it change.
src_hash=$(cat "$SANDBOX_DIR/Dockerfile" "$SANDBOX_DIR/entrypoint.sh" | shasum -a 256 | cut -c1-16)
image_hash=$(docker image inspect "$IMAGE" --format '{{ index .Config.Labels "slotbook.src" }}' 2>/dev/null || true)
if [ "$image_hash" != "$src_hash" ]; then
  echo "run-job: building $IMAGE ($src_hash)" >&2
  docker build --platform linux/arm64 --label "slotbook.src=$src_hash" -t "$IMAGE" "$SANDBOX_DIR" >&2
fi

# The only two secrets the sandbox gets. Read from the Keychain, passed by name
# (docker -e VAR) so they never appear on a command line.
CLAUDE_CODE_OAUTH_TOKEN=$(security find-generic-password -a "$USER" -s slotbook-sandbox-claude -w) \
  || die "Keychain item slotbook-sandbox-claude not found"
GH_TOKEN=$(security find-generic-password -a "$USER" -s slotbook-sandbox-github -w) \
  || die "Keychain item slotbook-sandbox-github not found"
MAX_TURNS=${SLOTBOOK_MAX_TURNS:-$default_turns}
export CLAUDE_CODE_OAUTH_TOKEN GH_TOKEN SLOTBOOK_REPO ISSUE MODE MAX_TURNS

optional_env=""
for v in SLOTBOOK_MODEL SLOTBOOK_TRUSTED; do
  if [ -n "${!v:-}" ]; then export "$v"; optional_env="$optional_env -e $v"; fi
done

stamp=$(date +%Y%m%d-%H%M%S)
base="$SLOTBOOK_RUNS/$ISSUE-$MODE-$stamp"
echo "run-job: #$ISSUE $MODE on $SLOTBOOK_REPO, max-turns $MAX_TURNS → $base.*" >&2

set +e
# shellcheck disable=SC2086 # optional_env is a list of flags
docker run --rm -i --init --name "slotbook-$ISSUE-$MODE-$stamp" --platform linux/arm64 \
  --memory 3g --memory-swap 3g --cpus 3 --pids-limit 1024 \
  --cap-drop ALL --cap-add SETUID --cap-add SETGID --security-opt no-new-privileges \
  -e GH_TOKEN -e CLAUDE_CODE_OAUTH_TOKEN -e SLOTBOOK_REPO -e ISSUE -e MODE -e MAX_TURNS $optional_env \
  -v slotbook-nuget:/home/agent/.nuget/packages -v slotbook-npm:/home/agent/.npm \
  "$IMAGE" < "$PROMPT" 2>&1 >"$base.jsonl" | tee "$base.log" >&2
rc=${PIPESTATUS[0]}
set -e

case $rc in
  0) outcome=done ;; 10) outcome=paused-limit ;; 11) outcome=max-turns ;; 12) outcome=blocked ;;
  *) outcome=failed ;;
esac

jq -s --arg issue "$ISSUE" --arg mode "$MODE" --arg outcome "$outcome" --argjson exit "$rc" \
  --arg repo "$SLOTBOOK_REPO" --arg started "$stamp" '
  (map(select(.type == "result")) | last) as $r
  | { issue: ($issue | tonumber), mode: $mode, repo: $repo, started: $started,
      exit: $exit, outcome: $outcome,
      session_id: $r.session_id, num_turns: $r.num_turns,
      duration_ms: $r.duration_ms, total_cost_usd: $r.total_cost_usd }
' "$base.jsonl" > "$base.meta.json" 2>/dev/null \
  || printf '{"issue":%s,"mode":"%s","exit":%s,"outcome":"%s"}\n' "$ISSUE" "$MODE" "$rc" "$outcome" > "$base.meta.json"

echo "run-job: $outcome (exit $rc). $(jq -c '{session_id, num_turns, total_cost_usd}' "$base.meta.json")" >&2
exit "$rc"
