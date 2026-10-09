#!/usr/bin/env bash
# In-container job runner. Started by sandbox/run-job.sh; never run on the Mac.
#
# Runs as root (with almost every capability dropped) and is the only process
# that holds GH_TOKEN. Claude runs as the unprivileged `agent` user with the
# Claude token only. The agent writes its outputs to /work/out; this script
# validates them, pushes the ai/<issue>-* branch (never forced) and posts them.
#
# In:   env GH_TOKEN CLAUDE_CODE_OAUTH_TOKEN SLOTBOOK_REPO ISSUE MODE MAX_TURNS
#       optional SLOTBOOK_MODEL, SLOTBOOK_TRUSTED (comma-separated logins)
#       stdin: the prompt template (sandbox/prompts/<mode>.md)
# Out:  stdout: Claude's stream-json; stderr: this script's log
# Exit: 0 done · 10 paused at usage limit · 11 hit max turns · 12 agent blocked · 1 failed
set -euo pipefail

log() { printf '[job %s] %s\n' "$(date -u +%H:%M:%S)" "$*" >&2; }
die() { log "ERROR: $*"; exit 1; }

: "${GH_TOKEN:?GH_TOKEN not set}"
: "${CLAUDE_CODE_OAUTH_TOKEN:?CLAUDE_CODE_OAUTH_TOKEN not set}"
: "${SLOTBOOK_REPO:?SLOTBOOK_REPO not set}"
: "${MAX_TURNS:?MAX_TURNS not set}"
[[ ${ISSUE:-} =~ ^[0-9]+$ ]] || die "ISSUE must be a number"
case ${MODE:-} in plan|implement|fix) ;; *) die "MODE must be plan, implement or fix" ;; esac

REPO_URL="https://github.com/$SLOTBOOK_REPO.git"
CTX=/work/context
OUT=/work/out
REPO=/work/agent/repo
export GH_REPO=$SLOTBOOK_REPO

cat > /work/prompt.tmpl
[[ -s /work/prompt.tmpl ]] || die "empty prompt template on stdin"

# ---------------------------------------------------------------- identities
BOT_LOGIN=$(gh api user --jq .login)
BOT_ID=$(gh api user --jq .id)
TRUSTED=${SLOTBOOK_TRUSTED:-${SLOTBOOK_REPO%%/*},$BOT_LOGIN}
log "repo=$SLOTBOOK_REPO issue=#$ISSUE mode=$MODE bot=$BOT_LOGIN trusted=$TRUSTED"

AGENT_ENV=(
  PATH=/home/agent/.local/bin:/usr/local/bin:/usr/bin:/bin
  DISABLE_AUTOUPDATER=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
  GIT_AUTHOR_NAME="$BOT_LOGIN" GIT_COMMITTER_NAME="$BOT_LOGIN"
  GIT_AUTHOR_EMAIL="$BOT_ID+$BOT_LOGIN@users.noreply.github.com"
  GIT_COMMITTER_EMAIL="$BOT_ID+$BOT_LOGIN@users.noreply.github.com"
)
# Everything that touches the agent's working copy runs as agent, so root never
# executes config or hooks the agent could have planted in it.
as_agent() { setpriv --reuid=agent --regid=agent --init-groups --reset-env env "${AGENT_ENV[@]}" "$@"; }
agit() { as_agent git -C "$REPO" "$@"; }

# ---------------------------------------------------------------- the issue
gh issue view "$ISSUE" --json number,title,body,author,state,labels,comments > /work/issue.json
ISSUE_AUTHOR=$(jq -r .author.login /work/issue.json)
ISSUE_TITLE=$(jq -r .title /work/issue.json)
[[ $(jq -r .state /work/issue.json) == OPEN ]] || die "issue #$ISSUE is not open"
[[ ",$TRUSTED," == *",$ISSUE_AUTHOR,"* ]] || die "issue author '$ISSUE_AUTHOR' is not trusted"

jq -r --arg trusted "$TRUSTED" '
  ($trusted | split(",")) as $t
  | "# Issue #\(.number): \(.title)\n\nLabels: \([.labels[].name] | join(", "))\n\n## Body\n\n\(.body)\n\n## Comments (trusted authors only, oldest first)\n",
    (.comments[] | select(.author.login as $a | any($t[]; . == $a))
     | "\n### \(.author.login) at \(.createdAt)\n\n\(.body)\n")
' /work/issue.json > "$CTX/issue.md"
dropped=$(jq --arg trusted "$TRUSTED" '($trusted | split(",")) as $t
  | [.comments[] | select(.author.login as $a | any($t[]; . == $a) | not)] | length' /work/issue.json)
log "issue: \"$ISSUE_TITLE\" ($dropped untrusted comment(s) withheld from the agent)"

# ---------------------------------------------------------------- the branch
as_agent git clone --quiet "$REPO_URL" "$REPO"
mapfile -t existing < <(agit ls-remote --heads origin | awk '{print $2}' \
  | sed -n "s#^refs/heads/\(ai/$ISSUE-.*\)#\1#p")
(( ${#existing[@]} <= 1 )) || die "several branches for #$ISSUE: ${existing[*]}"

BRANCH="" PR=""
case $MODE in
  plan) ;;
  implement)
    if (( ${#existing[@]} == 1 )); then
      BRANCH=${existing[0]}
      agit checkout --quiet -B "$BRANCH" "origin/$BRANCH"
      log "resuming existing branch $BRANCH"
    else
      slug=$(tr '[:upper:]' '[:lower:]' <<<"$ISSUE_TITLE" | sed -E 's/[^a-z0-9]+/-/g; s/^-+//' | cut -c1-40 | sed -E 's/-+$//')
      BRANCH="ai/$ISSUE-${slug:-story}"
      agit checkout --quiet -b "$BRANCH"
      log "new branch $BRANCH"
    fi ;;
  fix)
    PR=$(gh pr list --state open --json number,headRefName \
      --jq "[.[] | select(.headRefName | startswith(\"ai/$ISSUE-\"))] | if length == 1 then .[0].number else empty end")
    [[ -n $PR ]] || die "no single open PR for an ai/$ISSUE-* branch"
    BRANCH=$(gh pr view "$PR" --json headRefName --jq .headRefName)
    agit checkout --quiet -B "$BRANCH" "origin/$BRANCH"
    log "fixing PR #$PR on $BRANCH" ;;
esac
[[ -z $BRANCH || $BRANCH =~ ^ai/$ISSUE-[a-z0-9-]+$ ]] || die "refusing branch name '$BRANCH'"
BASE_SHA=$(agit rev-parse HEAD)

# ---------------------------------------------------------------- review context (fix)
if [[ $MODE == fix ]]; then
  gh api graphql -F owner="${SLOTBOOK_REPO%%/*}" -F name="${SLOTBOOK_REPO##*/}" -F pr="$PR" -f query='
    query($owner: String!, $name: String!, $pr: Int!) {
      repository(owner: $owner, name: $name) { pullRequest(number: $pr) {
        reviewThreads(first: 100) { nodes { isResolved isOutdated path line originalLine
          comments(first: 50) { nodes { databaseId author { login } body createdAt diffHunk } } } }
        reviews(first: 50) { nodes { author { login } state body submittedAt } }
        comments(first: 100) { nodes { author { login } body createdAt } }
      } } }' > /work/pr.json
  jq --arg trusted "$TRUSTED" '($trusted | split(",")) as $t
    | def ok: .author.login as $a | any($t[]; . == $a);
    .data.repository.pullRequest
    | { threads: [ .reviewThreads.nodes[]
          | select(.isResolved | not) | select(.comments.nodes[0] | ok)
          | { comment_id: .comments.nodes[0].databaseId, path, line: (.line // .originalLine),
              outdated: .isOutdated, diff_hunk: .comments.nodes[0].diffHunk,
              comments: [ .comments.nodes[] | select(ok) | { author: .author.login, body, at: .createdAt } ] } ],
        reviews: [ .reviews.nodes[] | select(ok) | select(.body != "") | { author: .author.login, state, body } ],
        conversation: [ .comments.nodes[] | select(ok) | { author: .author.login, body, at: .createdAt } ] }
  ' /work/pr.json > "$CTX/review.json"
  jq -r '
    "# Unresolved review threads\n",
    (.threads[] | "## comment_id \(.comment_id): `\(.path)` line \(.line)\(if .outdated then " (outdated)" else "" end)\n\n```diff\n\(.diff_hunk)\n```\n",
       (.comments[] | "**\(.author)** at \(.at):\n\n\(.body)\n")),
    "\n# Review summaries\n", (.reviews[] | "**\(.author)** (\(.state)):\n\n\(.body)\n"),
    "\n# PR conversation\n", (.conversation[] | "**\(.author)** at \(.at):\n\n\(.body)\n")
  ' "$CTX/review.json" > "$CTX/review.md"
  log "review: $(jq '.threads | length' "$CTX/review.json") unresolved thread(s)"
  (( $(jq '(.threads | length) + (.reviews | length) + (.conversation | length)' "$CTX/review.json") > 0 )) \
    || die "PR #$PR has no unresolved trusted review comments"
fi
chmod -R a+rX "$CTX"

# ---------------------------------------------------------------- run Claude
prompt=$(< /work/prompt.tmpl)
for kv in "ISSUE=$ISSUE" "REPO=$SLOTBOOK_REPO" "MODE=$MODE" "BRANCH=$BRANCH" "PR=$PR" "CONTEXT=$CTX" "OUT=$OUT"; do
  prompt=${prompt//"{{${kv%%=*}}}"/${kv#*=}}
done
printf '%s\n' "$prompt" > /work/prompt.md
chmod a+r /work/prompt.md

CLAUDE_ARGS=(-p --output-format stream-json --verbose --max-turns "$MAX_TURNS" --dangerously-skip-permissions)
[[ -n ${SLOTBOOK_MODEL:-} ]] && CLAUDE_ARGS+=(--model "$SLOTBOOK_MODEL")
log "claude: max-turns=$MAX_TURNS model=${SLOTBOOK_MODEL:-default}"

set +e
as_agent CLAUDE_CODE_OAUTH_TOKEN="$CLAUDE_CODE_OAUTH_TOKEN" \
  bash -c 'cd /work/agent/repo && exec claude "$@" < /work/prompt.md' _ "${CLAUDE_ARGS[@]}" \
  | tee /work/claude.jsonl
claude_rc=${PIPESTATUS[0]}
set -e

result=$(jq -c 'select(.type == "result")' /work/claude.jsonl 2>/dev/null | tail -n 1)
outcome=failed
if [[ -z $result ]]; then
  log "claude exited $claude_rc with no result event"
elif [[ $(jq -r .subtype <<<"$result") == error_max_turns ]]; then
  outcome=max-turns
elif [[ $(jq -r .is_error <<<"$result") == true ]]; then
  jq -r '.result // ""' <<<"$result" | grep -qi 'limit' && outcome=paused-limit
elif [[ $(jq -r .subtype <<<"$result") == success && $claude_rc == 0 ]]; then
  outcome=done
fi
[[ -f $OUT/blocked.md && $outcome == done ]] && outcome=blocked
log "claude: exit=$claude_rc outcome=$outcome session=$(jq -r '.session_id // "-"' <<<"${result:-null}")"

# ---------------------------------------------------------------- save and push (implement, fix)
pushed=false
if [[ -n $BRANCH ]]; then
  if [[ -n $(agit status --porcelain) ]]; then
    case $outcome in
      paused-limit) msg="WIP: paused at limit" ;;
      max-turns)    msg="WIP: stopped at max turns" ;;
      done)         msg="WIP: changes left uncommitted at end of run" ;;
      *)            msg="WIP: run $outcome" ;;
    esac
    agit add -A && agit commit --quiet -m "$msg"
    log "committed leftover changes: $msg"
  fi
  if [[ $(agit rev-parse "refs/heads/$BRANCH") != "$BASE_SHA" ]]; then
    git init --quiet --bare /work/push.git
    # Git drops -c config for a local upload-pack, so safe.directory goes in its command.
    git -C /work/push.git -c protocol.file.allow=always fetch --quiet --no-tags \
      --upload-pack="git -c safe.directory=$REPO/.git upload-pack" \
      "file://$REPO" "refs/heads/$BRANCH:refs/heads/$BRANCH"
    # No '+' and no --force: a non-fast-forward is rejected by design.
    git -C /work/push.git -c credential.helper= -c 'credential.helper=!gh auth git-credential' \
      push --quiet "$REPO_URL" "refs/heads/$BRANCH:refs/heads/$BRANCH"
    pushed=true
    log "pushed $BRANCH ($(agit rev-list --count "$BASE_SHA..refs/heads/$BRANCH") new commit(s))"
  else
    log "no new commits on $BRANCH"
  fi
fi

# ---------------------------------------------------------------- post outputs
marker="<!-- slotbook:$MODE issue=$ISSUE -->"
post_file() { # <kind> <number> <file>
  { printf '%s\n' "$marker"; cat "$3"; } > /work/post.md
  gh "$1" comment "$2" --body-file /work/post.md >/dev/null
}

case $outcome in
  blocked)
    post_file issue "$ISSUE" "$OUT/blocked.md"
    log "agent blocked; posted $OUT/blocked.md on #$ISSUE"
    exit 12 ;;
  paused-limit) exit 10 ;;
  max-turns)    exit 11 ;;
  done) ;;
  *) exit 1 ;;
esac

case $MODE in
  plan)
    [[ -s $OUT/plan.md ]] || die "agent wrote no $OUT/plan.md"
    [[ -z $(agit status --porcelain) ]] || log "WARNING: plan run left changes in the working tree (discarded)"
    post_file issue "$ISSUE" "$OUT/plan.md"
    log "posted plan on #$ISSUE" ;;
  implement)
    [[ -s $OUT/pr-body.md ]] || die "agent wrote no $OUT/pr-body.md"
    $pushed || [[ ${#existing[@]} == 1 ]] || die "nothing was committed"
    grep -qiE "closes #$ISSUE\b" "$OUT/pr-body.md" || printf '\nCloses #%s\n' "$ISSUE" >> "$OUT/pr-body.md"
    title=$(head -n 1 "$OUT/pr-title.txt" 2>/dev/null || true)
    PR=$(gh pr list --state open --head "$BRANCH" --json number --jq '.[0].number // empty')
    if [[ -n $PR ]]; then
      post_file pr "$PR" "$OUT/pr-body.md"
      log "PR #$PR already open; posted the run summary as a comment"
    else
      gh pr create --base main --head "$BRANCH" --title "${title:-#$ISSUE: $ISSUE_TITLE}" \
        --body-file "$OUT/pr-body.md" >&2
    fi ;;
  fix)
    [[ -s $OUT/replies.json ]] || die "agent wrote no $OUT/replies.json"
    jq -e 'type == "array"' "$OUT/replies.json" >/dev/null || die "replies.json is not a JSON array"
    while IFS= read -r reply; do
      id=$(jq -r .comment_id <<<"$reply")
      jq -e --argjson id "$id" 'any(.threads[]; .comment_id == $id)' "$CTX/review.json" >/dev/null \
        || { log "WARNING: skipping reply to unknown comment_id $id"; continue; }
      jq -r .body <<<"$reply" > /work/reply.md
      gh api -X POST "repos/$SLOTBOOK_REPO/pulls/$PR/comments/$id/replies" -F body=@/work/reply.md >/dev/null
      log "replied to comment $id"
    done < <(jq -c '.[]' "$OUT/replies.json")
    missing=$(jq -n --slurpfile r "$OUT/replies.json" --slurpfile v "$CTX/review.json" \
      '[$v[0].threads[].comment_id] - [$r[0][].comment_id] | length')
    (( missing == 0 )) || log "WARNING: $missing thread(s) got no reply"
    [[ -s $OUT/summary.md ]] && post_file pr "$PR" "$OUT/summary.md" && log "posted summary on PR #$PR"
    ;;
esac
log "done"
