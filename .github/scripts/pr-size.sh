#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 2 ]]; then
  echo "użycie: pr-size.sh <baza> <głowa>" >&2
  exit 1
fi

base=$1
head=$2
limit=800

excluded() {
  local path=$1
  local name=${path##*/}
  case $path in
    Migrations/* | */Migrations/*) return 0 ;;
    src/clients/shared/api-client/*) return 0 ;;
    pnpm-lock.yaml) return 0 ;;
    aspire-output/*) return 0 ;;
  esac
  case $name in
    *.Designer.cs) return 0 ;;
  esac
  return 1
}

if ! diff_out=$(git diff --numstat "${base}...${head}"); then
  echo "git diff nie policzył linii między ${base} a ${head}" >&2
  exit 1
fi

sum=0
while IFS=$'\t' read -r added deleted path; do
  [[ -z ${path:-} ]] && continue
  if [[ $path == \"*\" ]]; then
    path=${path#\"}
    path=${path%\"}
  fi
  if excluded "$path"; then
    continue
  fi
  if [[ $added == - || $deleted == - ]]; then
    continue
  fi
  if [[ ! $added =~ ^[0-9]+$ || ! $deleted =~ ^[0-9]+$ ]]; then
    echo "nieczytelny numstat: ${added} ${deleted} ${path}" >&2
    exit 1
  fi
  sum=$((sum + added + deleted))
done <<< "$diff_out"

echo "suma ${sum}"
if [[ $sum -gt $limit ]]; then
  exit 1
fi
