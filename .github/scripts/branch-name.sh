#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 2 ]]; then
  echo "użycie: branch-name.sh <głowa> <baza>" >&2
  exit 1
fi

head=$1
base=$2
work='^(feature|fix|docs|chore|ci|refactor|test)/[a-z0-9]+(-[a-z0-9]+)*$'

if [[ $head == develop ]]; then
  if [[ $base == release ]]; then
    exit 0
  fi
  echo "develop wchodzi tylko na release: ${head} → ${base}" >&2
  exit 1
fi

if [[ $head == release ]]; then
  if [[ $base == main ]]; then
    exit 0
  fi
  echo "release wchodzi tylko na main: ${head} → ${base}" >&2
  exit 1
fi

release_please='^release-please--branches--main--components--(identity|news|learning|cv|contact|gateway|web|mobile)$'

if [[ $head =~ $release_please ]]; then
  if [[ $base == main ]]; then
    exit 0
  fi
  echo "pull request wydania wchodzi tylko na main: ${head} → ${base}" >&2
  exit 1
fi

if [[ ! $head =~ $work ]]; then
  echo "nazwa głowy nie pasuje do wzorca: ${head}" >&2
  exit 1
fi

if [[ $base != develop ]]; then
  echo "gałąź robocza wchodzi tylko na develop: ${head} → ${base}" >&2
  exit 1
fi
