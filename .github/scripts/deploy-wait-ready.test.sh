#!/usr/bin/env bash
# Test for deploy-wait-ready.sh with a fake fetch command and a throwaway git repo.
# Plain bash (plus jq and git), no network.
set -euo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
script=$here/deploy-wait-ready.sh
failed=0
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

git -C "$tmp" init -q repo
cd "$tmp/repo"
git config user.email t@example.com
git config user.name t
git commit -q --allow-empty -m old
old=$(git rev-parse HEAD)
git commit -q --allow-empty -m target
target=$(git rev-parse HEAD)
git commit -q --allow-empty -m newer
newer=$(git rev-parse HEAD)
git checkout -q -b side "$old"
git commit -q --allow-empty -m unrelated
unrelated=$(git rev-parse HEAD)

# Fake fetch: answers come from $tmp/responses, one per call; the last one repeats.
cat > "$tmp/fetch.sh" <<'F'
#!/usr/bin/env bash
n=$(( $(cat "$RESP_DIR/n" 2>/dev/null || echo 0) + 1 ))
echo "$n" > "$RESP_DIR/n"
total=$(wc -l < "$RESP_DIR/responses")
((n > total)) && n=$total
line=$(sed -n "${n}p" "$RESP_DIR/responses")
[[ $line == FAIL ]] && { echo "connection refused" >&2; exit 7; }
code=${line%% *}
printf '%s\n%s\n' "${line#* }" "$code"
F
chmod +x "$tmp/fetch.sh"

# run_case <expected exit> <description> <expected sha> <response>...
run_case() {
  local want=$1 name=$2 sha=$3 got=0
  shift 3
  printf '%s\n' "$@" > "$tmp/responses"
  rm -f "$tmp/n"
  RESP_DIR=$tmp DEPLOY_FETCH="$tmp/fetch.sh" DEPLOY_POLL_INTERVAL=0 DEPLOY_TIMEOUT=1 \
    "$script" "https://api.example.test/" "$sha" >/dev/null 2>&1 || got=$?
  if [[ $got -eq $want ]]; then
    echo "ok   $name"
  else
    echo "FAIL $name: expected exit $want, got $got"
    failed=1
  fi
}

ok() { echo "200 {\"status\":\"Healthy\",\"commit\":\"$1\",\"checks\":{\"mongodb\":\"Healthy\"}}"; }

run_case 0 "matching sha" "$target" "$(ok "$target")"
run_case 0 "abbreviated matching sha" "$target" "$(ok "${target:0:7}")"
run_case 0 "descendant sha" "$target" "$(ok "$newer")"
run_case 0 "succeeds after 503, old sha, connection error" "$target" \
  "503 {\"status\":\"Unhealthy\",\"commit\":\"$target\"}" FAIL "$(ok "$old")" "$(ok "$target")"
run_case 1 "old sha forever" "$target" "$(ok "$old")"
run_case 1 "unrelated sha forever" "$target" "$(ok "$unrelated")"
run_case 1 "503 forever" "$target" "503 {\"status\":\"Unhealthy\",\"commit\":\"$target\"}"
run_case 1 "connection refused forever" "$target" FAIL
run_case 1 "healthy without a commit field" "$target" '200 {"status":"Healthy"}'
run_case 1 "degraded with matching sha" "$target" "200 {\"status\":\"Degraded\",\"commit\":\"$target\"}"
run_case 1 "non-json body" "$target" "200 <html>"
run_case 1 "unknown sha" "$target" "$(ok "ffffffffffffffffffffffffffffffffffffffff")"

exit $failed
