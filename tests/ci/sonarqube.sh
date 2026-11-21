#!/usr/bin/env bash
set -euo pipefail

root=$(cd -- "$(dirname -- "$0")/../.." && pwd)
material=$root/.github/scripts/sonar-material.sh
coverage=$root/.github/scripts/sonar-coverage.sh
cd -- "$root"

fail() {
  echo "$1" >&2
  if [[ $# -gt 1 ]]; then
    printf '%s\n' "$2" >&2
  fi
  exit 1
}

python3 - <<'PY'
import json
gate = json.load(open("deploy/sonar/quality-gate.json", encoding="utf-8"))
expected = {
    "new_blocker": 0,
    "new_critical": 0,
    "new_hotspots_reviewed": 100,
    "new_duplicated_lines_density": 3,
    "new_coverage": 80,
}
if gate != expected:
    raise SystemExit(f"zła bramka: {gate}")
PY

props=$(cat sonar-project.properties)
for needle in 'sonar.projectKey=jjdevhub' '**/Migrations/**' 'src/clients/shared/api-client/generated/**' 'sonar.qualitygate.wait=true'; do
  if [[ $props != *"$needle"* ]]; then
    fail "brak ${needle} w sonar-project.properties"
  fi
done

for metric in new_software_quality_blocker_issues new_software_quality_high_issues new_security_hotspots_reviewed new_duplicated_lines_density new_coverage; do
  if ! grep -q "$metric" .github/scripts/sonar-bootstrap.sh; then
    fail "brak metryki ${metric}"
  fi
done

run_material() {
  local expected=$1
  local dir=$2
  set +e
  log=$(bash "$material" "$dir" 2>&1)
  code=$?
  set -e
  if [[ $code -ne 0 ]]; then
    fail "oczekiwano 0, jest ${code}" "$log"
  fi
  if [[ $log != "$expected" ]]; then
    fail "oczekiwano ${expected}" "$log"
  fi
}

empty=$(mktemp -d)
tests_only=$(mktemp -d)
sources=$(mktemp -d)
cov=$(mktemp -d)
cleanup() {
  rm -rf -- "$empty" "$tests_only" "$sources" "$cov"
}
trap cleanup EXIT

run_material 'no material' "$empty"
mkdir -p "$tests_only/tests" "$tests_only/src/App.Tests"
printf 'class A {}\n' >"$tests_only/tests/A.cs"
printf 'class B {}\n' >"$tests_only/src/App.Tests/B.cs"
printf 'export const a = 1\n' >"$tests_only/src/app.test.ts"
run_material 'no material: brak produkcji' "$tests_only"
mkdir -p "$sources/src"
printf 'class A {}\n' >"$sources/src/A.cs"
run_material 'material' "$sources"

set +e
cov_log=$(env -u GITHUB_ACTIONS bash "$coverage" "$cov" 2>&1)
cov_code=$?
set -e
if [[ $cov_code -ne 0 ]]; then
  fail "oczekiwano 0 dla pustego pokrycia" "$cov_log"
fi
if [[ $cov_log != *'pokrycie puste'* ]]; then
  fail "brak logu pokrycie puste" "$cov_log"
fi
