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
# Model: per mode from sandbox/models.conf (lower model writes, higher model reviews).
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
SLOTBOOK_MODEL=${SLOTBOOK_MODEL:-$(sed -n "s/^$MODE=//p" "$SANDBOX_DIR/models.conf" | tr -d '[:space:]')}
[ -n "$SLOTBOOK_MODEL" ] || die "no model for mode '$MODE' in sandbox/models.conf"
export CLAUDE_CODE_OAUTH_TOKEN GH_TOKEN SLOTBOOK_REPO ISSUE MODE MAX_TURNS

optional_env=""
for v in SLOTBOOK_MODEL SLOTBOOK_TRUSTED; do
  if [ -n "${!v:-}" ]; then export "$v"; optional_env="$optional_env -e $v"; fi
done

stamp=$(date +%Y%m%d-%H%M%S)
base="$SLOTBOOK_RUNS/$ISSUE-$MODE-$stamp"
echo "run-job: #$ISSUE $MODE on $SLOTBOOK_REPO, model $SLOTBOOK_MODEL, max-turns $MAX_TURNS → $base.*" >&2

# ---------------------------------------------------------------- sidecars (spine AD-19)
# Each run gets its own network with a single-node MongoDB replica set and a RabbitMQ,
# reachable from the sandbox as `mongo` and `rabbitmq`. The agent gets connection strings
# only; it has no Docker access. Everything is removed when the run ends.
MONGO_IMAGE=${SLOTBOOK_MONGO_IMAGE:-mongo:8.0}
RABBIT_IMAGE=${SLOTBOOK_RABBIT_IMAGE:-rabbitmq:4-alpine}
net="slotbook-$ISSUE-$MODE-$stamp"
mongo_ctr="$net-mongo" rabbit_ctr="$net-rabbitmq"
cleanup() {
  docker rm -f "$mongo_ctr" "$rabbit_ctr" >/dev/null 2>&1 || true
  docker network rm "$net" >/dev/null 2>&1 || true
}
trap cleanup EXIT

docker network create "$net" >/dev/null
# The data lives in tmpfs, inside the 1 GB limit. Cap the oplog (default minimum 990 MB, never shrunk by
# dropping test databases) and the WiredTiger cache, or repeated test runs fill memory, stall MongoDB and
# get it OOM-killed mid-run. MongoDB holds a file per collection and index plus one per connection; the
# image's soft limit of 1024 is less than a full integration-suite run needs (it dies "Too many open files").
docker run -d --name "$mongo_ctr" --network "$net" --network-alias mongo --platform linux/arm64 \
  --memory 1g --ulimit nofile=64000:64000 --tmpfs /data/db "$MONGO_IMAGE" --replSet rs0 --bind_ip_all \
  --oplogSize 128 --wiredTigerCacheSizeGB 0.25 >/dev/null
docker run -d --name "$rabbit_ctr" --network "$net" --network-alias rabbitmq --platform linux/arm64 \
  --memory 512m "$RABBIT_IMAGE" >/dev/null

ready=""
for _ in $(seq 1 60); do
  if docker exec -e HOME=/tmp "$mongo_ctr" mongosh --quiet --eval \
      'try { rs.status().ok } catch (e) { rs.initiate({_id: "rs0", members: [{_id: 0, host: "mongo:27017"}]}).ok }' \
      >/dev/null 2>&1 \
    && docker exec -e HOME=/tmp "$mongo_ctr" mongosh --quiet --eval 'db.hello().isWritablePrimary' 2>/dev/null | grep -q true \
    && docker exec "$rabbit_ctr" rabbitmq-diagnostics -q ping >/dev/null 2>&1; then
    ready=yes; break
  fi
  sleep 2
done
[ -n "$ready" ] || die "sidecars didn't become ready (MongoDB replica set and RabbitMQ)"
echo "run-job: sidecars ready on network $net (mongo, rabbitmq)" >&2

ConnectionStrings__mongodb="mongodb://mongo:27017/?replicaSet=rs0"
ConnectionStrings__rabbitmq="amqp://guest:guest@rabbitmq:5672/"
export ConnectionStrings__mongodb ConnectionStrings__rabbitmq

set +e
# shellcheck disable=SC2086 # optional_env is a list of flags
docker run --rm -i --init --name "slotbook-$ISSUE-$MODE-$stamp" --platform linux/arm64 \
  --network "$net" \
  --memory 3g --memory-swap 3g --cpus 3 --pids-limit 1024 \
  --cap-drop ALL --cap-add SETUID --cap-add SETGID --security-opt no-new-privileges \
  -e GH_TOKEN -e CLAUDE_CODE_OAUTH_TOKEN -e SLOTBOOK_REPO -e ISSUE -e MODE -e MAX_TURNS $optional_env \
  -e ConnectionStrings__mongodb -e ConnectionStrings__rabbitmq \
  -v slotbook-nuget:/home/agent/.nuget/packages -v slotbook-npm:/home/agent/.npm \
  "$IMAGE" < "$PROMPT" 2>&1 >"$base.jsonl" | tee "$base.log" >&2
rc=${PIPESTATUS[0]}
set -e

case $rc in
  0) outcome=done ;; 10) outcome=paused-limit ;; 11) outcome=max-turns ;; 12) outcome=blocked ;;
  *) outcome=failed ;;
esac

jq -s --arg issue "$ISSUE" --arg mode "$MODE" --arg outcome "$outcome" --argjson exit "$rc" \
  --arg repo "$SLOTBOOK_REPO" --arg started "$stamp" --arg model "$SLOTBOOK_MODEL" '
  (map(select(.type == "result")) | last) as $r
  | { issue: ($issue | tonumber), mode: $mode, model: $model, repo: $repo, started: $started,
      exit: $exit, outcome: $outcome,
      session_id: $r.session_id, num_turns: $r.num_turns,
      duration_ms: $r.duration_ms, total_cost_usd: $r.total_cost_usd }
' "$base.jsonl" > "$base.meta.json" 2>/dev/null \
  || printf '{"issue":%s,"mode":"%s","exit":%s,"outcome":"%s"}\n' "$ISSUE" "$MODE" "$rc" "$outcome" > "$base.meta.json"

echo "run-job: $outcome (exit $rc). $(jq -c '{session_id, num_turns, total_cost_usd}' "$base.meta.json")" >&2
exit "$rc"
