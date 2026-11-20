#!/usr/bin/env bash
set -euo pipefail

root=$(cd "$(dirname "$0")/../.." && pwd)
cd "$root"

lang=$root/.github/scripts/codeql-languages.sh
gate=$root/.github/scripts/codeql-gate.sh
fix=$root/tests/ci/codeql

fail() {
  echo "$1" >&2
  if [[ $# -gt 1 ]]; then
    printf '%s\n' "$2" >&2
  fi
  exit 1
}

set +e
log=$(bash "$lang" 2>&1)
code=$?
set -e
if [[ $code -ne 0 ]]; then
  fail "oczekiwano 0, jest ${code} dla języków" "$log"
fi
if [[ $log != 'no material' ]]; then
  fail "brak logu no material" "$log"
fi

run_gate() {
  local expected=$1
  local sarif=$2
  shift 2
  set +e
  gate_log=$(bash "$gate" --sarif "$sarif" "$@" 2>&1)
  gate_code=$?
  set -e
  if [[ $gate_code -ne $expected ]]; then
    fail "oczekiwano ${expected}, jest ${gate_code}: ${sarif}" "$gate_log"
  fi
}

run_gate 1 "$fix/high.sarif"
if [[ $gate_log != *'fixture/high fixture.txt 1'* ]]; then
  fail "brak reguły, pliku i linii dla 7.0" "$gate_log"
fi

run_gate 1 "$fix/unscored.sarif"
if [[ $gate_log != *'fixture/unscored fixture.txt 4'* ]]; then
  fail "brak reguły, pliku i linii dla braku wagi" "$gate_log"
fi

run_gate 0 "$fix/low.sarif"
if [[ $gate_log != *'fixture/low fixture.txt 1'* ]]; then
  fail "brak wpisu dla wagi 3.0" "$gate_log"
fi

base=$(mktemp)
trap 'rm -f "$base"' EXIT

cat >"$base" <<'EOF'
- rule: fixture/high
  reason: false positive reviewed in this pull request
EOF
run_gate 0 "$fix/high.sarif" --baseline "$base"
if [[ $gate_log != *'baseline fixture/high fixture.txt 1'* ]]; then
  fail "baseline nie gasi wagi 7.0" "$gate_log"
fi

cat >"$base" <<'EOF'
- rule: fixture/high
  reason:
EOF
run_gate 1 "$fix/high.sarif" --baseline "$base"
if [[ $gate_log != *'baseline bez powodu'* ]]; then
  fail "pusty powód nie jest w logu" "$gate_log"
fi
