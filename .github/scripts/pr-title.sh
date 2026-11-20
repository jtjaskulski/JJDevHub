#!/usr/bin/env bash
set -euo pipefail

export LC_ALL=C.UTF-8

if [[ $# -ne 1 ]]; then
  echo "użycie: pr-title.sh <tytuł>" >&2
  exit 1
fi

title=$1
pattern='^(feat|fix|docs|chore|ci|refactor|test|perf|build)(\(([^)]+)\))?(!)?: (.+)$'

if [[ ! $title =~ $pattern ]]; then
  echo "tytuł nie pasuje do Conventional Commits" >&2
  exit 1
fi

type=${BASH_REMATCH[1]}
scope=${BASH_REMATCH[3]:-}
description=${BASH_REMATCH[5]}

scope_allowed() {
  case $1 in
    identity|news|learning|cv|contact|gateway|web|mobile|infra|shared)
      return 0
      ;;
    *)
      return 1
      ;;
  esac
}

if [[ -n $scope ]] && ! scope_allowed "$scope"; then
  echo "zakres spoza listy: ${scope}" >&2
  exit 1
fi

if [[ $type == feat || $type == fix ]] && [[ -z $scope ]]; then
  echo "feat i fix wymagają zakresu" >&2
  exit 1
fi

if [[ ${#description} -lt 3 ]]; then
  echo "opis krótszy niż trzy znaki" >&2
  exit 1
fi
