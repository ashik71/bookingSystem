#!/usr/bin/env bash
# Table-driven test for ci-changes.sh. Plain bash, no dependencies.
set -euo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
failed=0

# check <expected "backend frontend"> <description> <path>...
check() {
  local expected=$1 name=$2
  shift 2
  local out actual
  out=$(printf '%s\n' "$@" | "$here/ci-changes.sh")
  actual=$(sed -n 's/^backend=//p;s/^frontend=//p' <<<"$out" | paste -sd' ')
  if [[ $actual == "$expected" ]]; then
    echo "ok   $name"
  else
    echo "FAIL $name: expected '$expected', got '$actual'"
    failed=1
  fi
}

# AC1: frontend only
check "false true" "frontend source" "src/frontend/src/app/app.ts"
check "false true" "frontend lockfile" "src/frontend/package-lock.json"
# AC2: backend only
check "true false" "backend source" "src/backend/SlotBook.Api/Program.cs"
check "true false" "backend tests" "src/backend/tests/SlotBook.Api.Tests/HealthEndpointTests.cs"
for f in SlotBook.slnx global.json Directory.Build.props Directory.Packages.props \
  .editorconfig stylecop.json nuget.config; do
  check "true false" "root build file $f" "$f"
done
# AC3: both sides, or CI itself
check "true true" "both sides" "src/backend/SlotBook.Api/Program.cs" "src/frontend/src/app/app.ts"
check "true true" "ci workflow" ".github/workflows/ci.yml"
check "true true" "ci classifier script" ".github/scripts/ci-changes.sh"
check "true true" "ci classifier test" ".github/scripts/ci-changes.test.sh"
check "true true" "ci aggregate script" ".github/scripts/ci-aggregate.sh"
# AC4: neither side
check "false false" "docs" "docs/STATE.md"
check "false false" "readme" "README.md"
check "false false" "board-sync workflow" ".github/workflows/board-sync.yml"
check "false false" "progress log" ".ai/5.md"
check "false false" "planning output" "_bmad-output/x/y.md"
check "false false" "empty input"
# Edge cases
check "false false" "prefix is not a match (frontend)" "src/frontend-old/x.ts"
check "false false" "prefix is not a match (backend)" "src/backend-old/x.cs"
check "false false" "nested root file name" "docs/global.json"
check "true false" "path with spaces" "src/backend/My Folder/a b.cs"
check "true true" "rename out of backend (old + new path)" "src/backend/A.cs" "src/frontend/A.ts"
check "true false" "rename within backend" "src/backend/A.cs" "src/backend/B.cs"

exit $failed
