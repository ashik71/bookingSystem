#!/usr/bin/env bash
# Waits until <api-origin>/health/ready answers 200 "Healthy" and reports the expected commit
# (or a commit that descends from it). Exit 0 on success, 1 on timeout. Run from the repo.
# Usage: deploy-wait-ready.sh <api-origin> <expected-sha>
# Env:   DEPLOY_POLL_INTERVAL (seconds, default 15), DEPLOY_TIMEOUT (seconds, default 1200),
#        DEPLOY_FETCH (command run as `$DEPLOY_FETCH <url>`; prints the body, then the HTTP
#        status code on its own last line; default is curl; tests replace it).
# Tested by deploy-wait-ready.test.sh.
set -euo pipefail

api=${1:?usage: deploy-wait-ready.sh <api-origin> <expected-sha>}
expected=${2:?usage: deploy-wait-ready.sh <api-origin> <expected-sha>}
api=${api%/}
interval=${DEPLOY_POLL_INTERVAL:-15}
timeout=${DEPLOY_TIMEOUT:-1200}
fetch=${DEPLOY_FETCH:-}
if [[ -z $fetch ]]; then
  fetch="curl -sS --max-time 30 -w \n%{http_code}"
fi
fetched=false

# accepted <reported>: the expected commit itself, or one that descends from it. A deploy hook
# builds the branch's latest commit, so a newer commit is a correct outcome.
accepted() {
  local reported=$1
  [[ $reported =~ ^[0-9a-f]{7,40}$ ]] || return 1
  [[ $expected == "$reported"* ]] && return 0
  if ! git cat-file -e "$reported^{commit}" 2>/dev/null && [[ $fetched == false ]]; then
    fetched=true
    git fetch --quiet origin main 2>/dev/null || true
  fi
  git merge-base --is-ancestor "$expected" "$reported" 2>/dev/null
}

last="no answer yet"
deadline=$((SECONDS + timeout))
while true; do
  # shellcheck disable=SC2086
  if raw=$($fetch "$api/health/ready" 2>&1); then
    code=${raw##*$'\n'}
    body=${raw%$'\n'*}
    last="HTTP $code: $body"
    if [[ $code == 200 ]]; then
      status=$(jq -r '.status // empty' <<<"$body" 2>/dev/null || true)
      reported=$(jq -r '.commit // empty' <<<"$body" 2>/dev/null || true)
      if [[ $status == Healthy ]] && accepted "$reported"; then
        echo "API is ready and reports commit $reported (expected $expected)"
        exit 0
      fi
    fi
  else
    last="request failed: $raw"
  fi
  echo "waiting: $last"
  if ((SECONDS >= deadline)); then
    echo "::error::API did not report a healthy $expected within ${timeout}s. Last answer: $last"
    exit 1
  fi
  sleep "$interval"
done
