#!/usr/bin/env bash
# Test for ci-openapi-current.sh against throwaway git repos. Plain bash, no dependencies.
set -euo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
script=$here/ci-openapi-current.sh
failed=0
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

new_repo() {
  rm -rf "$tmp/repo"
  mkdir "$tmp/repo"
  git -C "$tmp/repo" init -q
  git -C "$tmp/repo" config user.email t@example.com
  git -C "$tmp/repo" config user.name t
  printf '{ "paths": { } }' > "$tmp/repo/openapi.json"
  git -C "$tmp/repo" add openapi.json
  git -C "$tmp/repo" commit -q -m init
}

# expect <0|1> <description>
expect() {
  local want=$1 name=$2 got=0
  (cd "$tmp/repo" && "$script" openapi.json >/dev/null 2>&1) || got=$?
  if [[ $got -eq $want ]]; then
    echo "ok   $name"
  else
    echo "FAIL $name: expected exit $want, got $got"
    failed=1
  fi
}

new_repo
expect 0 "committed and unchanged passes"

new_repo
printf '{ "paths": { "/api/x": { } } }' > "$tmp/repo/openapi.json"
expect 1 "modified file fails"

new_repo
rm "$tmp/repo/openapi.json"
expect 1 "deleted file fails"

new_repo
git -C "$tmp/repo" rm -q --cached openapi.json
expect 1 "untracked file fails"

new_repo
printf '{ "paths": { "/api/x": { } } }' > "$tmp/repo/openapi.json"
git -C "$tmp/repo" add openapi.json
expect 1 "staged but uncommitted change fails"

exit $failed
