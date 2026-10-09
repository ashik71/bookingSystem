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
echo "═══ BMAD PLANNING OUTPUT ═══"
find _bmad-output -mindepth 2 -maxdepth 3 -name '*.md' ! -name '.memlog.md' 2>/dev/null \
  | while read -r f; do
      status=$(grep -m1 '^status:' "$f" 2>/dev/null | sed 's/status: *//' || true)
      printf '  %-80s %s\n' "$f" "${status:-}"
    done

echo
echo "═══ PIPELINE (GitHub) ═══"
if command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then
  echo "Stories in flight:"
  gh issue list --state open --search 'label:ai:queue,ai:planning,ai:plan-review,ai:implementing,ai:pr-ready,ai:changes-requested,ai:paused-limit,ai:failed' \
    --json number,title,labels \
    --template '{{range .}}  #{{.number}} {{.title}} [{{range .labels}}{{.name}} {{end}}]{{"\n"}}{{end}}' 2>/dev/null || echo "  (none)"
  echo "Open PRs:"
  gh pr list --state open --template '{{range .}}  #{{.number}} {{.title}} ({{.headRefName}}){{"\n"}}{{end}}' 2>/dev/null || echo "  (none)"
else
  echo "  (gh not installed or not logged in)"
fi

echo
echo "═══ RECENT COMMITS ═══"
git log --oneline -8 2>/dev/null || true
