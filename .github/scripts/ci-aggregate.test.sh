#!/usr/bin/env bash
# Table-driven test for ci-aggregate.sh. Plain bash, no dependencies.
set -euo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
failed=0

# check <pass|fail> <changes> <backend> <frontend>
check() {
  local expected=$1 rc=0
  CHANGES=$2 BACKEND=$3 FRONTEND=$4 "$here/ci-aggregate.sh" >/dev/null 2>&1 || rc=$?
  local actual=pass
  [[ $rc -eq 0 ]] || actual=fail
  if [[ $actual == "$expected" ]]; then
    echo "ok   $expected: changes=$2 backend=$3 frontend=$4"
  else
    echo "FAIL expected $expected, got $actual: changes=$2 backend=$3 frontend=$4"
    failed=1
  fi
}

check pass success success success
check pass success success skipped
check pass success skipped success
check pass success skipped skipped
check fail success failure skipped
check fail success skipped failure
check fail success success failure
check fail success failure success
check fail success cancelled skipped
check fail success skipped cancelled
check fail failure skipped skipped
check fail cancelled skipped skipped
check fail skipped skipped skipped
check fail failure success success

# Missing inputs must not pass.
rc=0
env -u CHANGES -u BACKEND -u FRONTEND "$here/ci-aggregate.sh" >/dev/null 2>&1 || rc=$?
if [[ $rc -ne 0 ]]; then echo "ok   fail: missing inputs"; else echo "FAIL missing inputs passed"; failed=1; fi

exit $failed
