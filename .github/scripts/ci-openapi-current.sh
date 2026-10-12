#!/usr/bin/env bash
# Fails when the committed OpenAPI document is stale: run it after `dotnet build`, which
# regenerates the file (AD-13). Usage: ci-openapi-current.sh [path]; run from the repo root.
set -euo pipefail

file=${1:-src/backend/SlotBook.Api/openapi.json}

if ! git ls-files --error-unmatch -- "$file" >/dev/null 2>&1; then
  echo "::error file=$file::$file is missing or not committed. Run 'dotnet build' and commit it." >&2
  exit 1
fi
if [[ ! -f $file ]] || ! git diff --quiet HEAD -- "$file"; then
  echo "::error file=$file::$file is stale. Run 'dotnet build' and commit the regenerated file." >&2
  git diff HEAD -- "$file" >&2 || true
  exit 1
fi
echo "ok   $file is current"
