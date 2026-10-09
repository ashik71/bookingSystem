#!/usr/bin/env bash
# Decides the result of the `ci-ok` check from the results of the jobs it needs.
# Inputs (env): CHANGES, BACKEND, FRONTEND = success | failure | cancelled | skipped.
# `changes` must succeed: if detection fails, both side jobs are skipped and
# nothing was tested. A side job may be skipped (not changed) but not fail.
# Tested by ci-aggregate.test.sh.
set -euo pipefail

: "${CHANGES:?}" "${BACKEND:?}" "${FRONTEND:?}"
echo "changes:  $CHANGES"
echo "backend:  $BACKEND"
echo "frontend: $FRONTEND"

ok=true
[[ $CHANGES == success ]] || { echo "::error::change detection did not succeed ($CHANGES)"; ok=false; }
for side in backend:"$BACKEND" frontend:"$FRONTEND"; do
  case ${side#*:} in
    success | skipped) ;;
    *) echo "::error::${side%%:*} job is ${side#*:}"; ok=false ;;
  esac
done

$ok
