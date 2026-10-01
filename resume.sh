#!/usr/bin/env bash
# Print the project's current position. Run this, or paste its output into a new
# AI session, to resume where the last session left off.
set -euo pipefail
cd "$(dirname "$0")"

echo "═══ STATE ═══"
cat docs/STATE.md

echo
echo "═══ LAST SESSION LOG ═══"
last=$(ls docs/sessions/[0-9]*.md 2>/dev/null | sort | tail -1 || true)
if [ -n "$last" ]; then echo "($last)"; echo; cat "$last"; else echo "(none yet)"; fi

echo
echo "═══ DECISIONS ON RECORD ═══"
for f in docs/adr/[0-9]*.md; do
  case "$f" in docs/adr/0000-*) continue;; esac
  [ -e "$f" ] || continue
  title=$(grep -m1 '^# ' "$f" | sed 's/^# //')
  status=$(grep -m1 '^- \*\*Status:\*\*' "$f" | sed 's/.*Status:\*\* //')
  printf '  %-72s %s\n' "$title" "$status"
done

echo
echo "═══ RECENT COMMITS ═══"
git log --oneline -8 2>/dev/null || true
