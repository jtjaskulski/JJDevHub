#!/usr/bin/env bash
set -euo pipefail

dest=${1:-artifacts/coverage}
mkdir -p -- "$dest"

if [[ -z ${GITHUB_ACTIONS:-} ]]; then
  printf 'pokrycie puste\n'
  exit 0
fi

if [[ -z ${GITHUB_TOKEN:-} || -z ${GITHUB_REPOSITORY:-} || -z ${SONAR_HEAD_SHA:-} ]]; then
  echo "brak tokenu, repozytorium albo SHA" >&2
  exit 1
fi

export GH_TOKEN="$GITHUB_TOKEN"
deadline=$(( $(date +%s) + 1200 ))
run_id=""
conclusion=""
while [[ $(date +%s) -lt $deadline ]]; do
  if ! json=$(gh run list --repo "$GITHUB_REPOSITORY" --workflow unit-tests.yml --commit "$SONAR_HEAD_SHA" --limit 5 --json databaseId,status,conclusion); then
    echo "nie odczytano przebiegu unit-tests" >&2
    exit 1
  fi
  parsed=$(printf '%s' "$json" | python3 -c '
import json
import sys
runs = json.load(sys.stdin)
if not runs:
    print("none")
    raise SystemExit(0)
run = runs[0]
ident = run.get("databaseId") or ""
status = run.get("status") or ""
conclusion = run.get("conclusion") or ""
print(f"{ident}\t{status}\t{conclusion}")
')
  if [[ $parsed == none ]]; then
    sleep 15
    continue
  fi
  IFS=$'\t' read -r run_id status conclusion <<<"$parsed"
  if [[ $status == completed ]]; then
    break
  fi
  sleep 15
done

if [[ -z $run_id || ${status:-} != completed ]]; then
  echo "unit-tests nie zakończył przebiegu" >&2
  exit 1
fi

case "$conclusion" in
  success | failure | skipped | neutral) ;;
  *)
    echo "unit-tests zakończył się: ${conclusion}" >&2
    exit 1
    ;;
esac

err=$(mktemp)
trap 'rm -f -- "$err"' EXIT
set +e
gh run download "$run_id" --repo "$GITHUB_REPOSITORY" --name coverage --dir "$dest" 2>"$err"
code=$?
set -e
if [[ $code -ne 0 ]]; then
  if grep -Eiq 'not found|no artifact|no valid artifacts' "$err"; then
    printf 'pokrycie puste\n'
    exit 0
  fi
  cat "$err" >&2
  exit 1
fi

found=$(find "$dest" \( -name 'coverage.cobertura.xml' -o -name 'lcov.info' \) -print -quit || true)
if [[ -z $found ]]; then
  printf 'pokrycie puste\n'
fi
