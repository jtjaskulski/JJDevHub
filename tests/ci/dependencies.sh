#!/usr/bin/env bash
set -euo pipefail

root=$(cd -- "$(dirname -- "$0")/../.." && pwd)
script=$root/.github/scripts/vulnerable.sh

fail() {
  echo "$1" >&2
  if [[ $# -gt 1 ]]; then
    printf '%s\n' "$2" >&2
  fi
  exit 1
}

empty=$(mktemp -d)
lock=$(mktemp -d)
star=$(mktemp -d)
allowed=$(mktemp -d)
cleanup() {
  rm -rf -- "$empty" "$lock" "$star" "$allowed"
}
trap cleanup EXIT

run_script() {
  local expected=$1
  local dir=$2
  set +e
  log=$(bash "$script" "$dir" 2>&1)
  code=$?
  set -e
  if [[ $code -ne $expected ]]; then
    fail "oczekiwano ${expected}, jest ${code} dla ${dir}" "$log"
  fi
}

run_script 0 "$empty"
if [[ $log != *'no material'* ]]; then
  fail "brak logu no material" "$log"
fi

printf '{}\n' >"$lock/package.json"
run_script 1 "$lock"
if [[ $log != *'pnpm-lock.yaml'* ]]; then
  fail "brak informacji o lockfile" "$log"
fi

mkdir -p "$star/.github"
printf '*\n' >"$star/.github/advisories-allow.txt"
run_script 1 "$star"
if [[ $log != *'gwiazdk'* ]]; then
  fail "brak informacji o gwiazdce" "$log"
fi

mkdir -p "$allowed/.github"
printf 'GHSA-xxxx-xxxx-xxxx 2026-11-20 powód tłumienia.\n' >"$allowed/.github/advisories-allow.txt"
run_script 0 "$allowed"
if [[ $log != *'no material'* ]]; then
  fail "poprawne tłumienie na pustym drzewie nie jest no material" "$log"
fi
