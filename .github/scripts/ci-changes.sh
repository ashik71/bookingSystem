#!/usr/bin/env bash
# Classifies changed paths (one per line on stdin) into the sides CI must run.
# Prints "backend=true|false" and "frontend=true|false" (GITHUB_OUTPUT format).
# Single source of truth for the path rules; tested by ci-changes.test.sh.
set -euo pipefail

backend=false
frontend=false

while IFS= read -r path || [[ -n $path ]]; do
  case $path in
    src/frontend/*)
      frontend=true ;;
    src/backend/* | SlotBook.slnx | global.json | Directory.Build.props | Directory.Packages.props \
      | .editorconfig | stylecop.json | nuget.config)
      backend=true ;;
    .github/workflows/ci.yml | .github/scripts/ci-*.sh)
      backend=true
      frontend=true ;;
  esac
done

echo "backend=$backend"
echo "frontend=$frontend"
