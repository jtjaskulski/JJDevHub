#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "użycie: artifacts-material.sh [--ignore] [korzeń]" >&2
  exit 2
}

mode=list
if [[ ${1:-} == --ignore ]]; then
  mode=ignore
  shift
fi

if [[ $# -gt 1 ]]; then
  usage
fi

if [[ $# -eq 1 ]]; then
  root=$(cd -- "$1" && pwd)
else
  root=$(cd -- "$(dirname -- "$0")/../.." && pwd)
fi

script_dir=$(cd -- "$(dirname -- "$0")" && pwd)

ignore_ids() {
  local file=$1
  local line id date reason
  if [[ ! -f $file ]]; then
    return 0
  fi
  while IFS= read -r line || [[ -n $line ]]; do
    [[ -z $line ]] && continue
    if [[ $line == *'*'* ]]; then
      echo "tłumienie gwiazdką jest zakazane" >&2
      exit 1
    fi
    if [[ ! $line =~ ^([^[:space:]]+)[[:space:]]+([0-9]{4}-[0-9]{2}-[0-9]{2})[[:space:]]+(.+)$ ]]; then
      echo "tłumienie bez daty albo zdania" >&2
      exit 1
    fi
    id=${BASH_REMATCH[1]}
    date=${BASH_REMATCH[2]}
    reason=${BASH_REMATCH[3]}
    if ! python3 -c 'import datetime,sys; datetime.date.fromisoformat(sys.argv[1])' "$date" >/dev/null 2>&1; then
      echo "tłumienie ze złą datą: ${id}" >&2
      exit 1
    fi
    read -r -a words <<<"$reason"
    if [[ ${#words[@]} -lt 2 || ! $reason =~ [[:alpha:]] ]]; then
      echo "tłumienie bez zdania: ${id}" >&2
      exit 1
    fi
    printf '%s\n' "$id"
  done <"$file"
}

if [[ $mode == ignore ]]; then
  ignore_ids "$root/.github/trivy-allow.txt"
  exit 0
fi

if [[ $mode != list ]]; then
  usage
fi

images=(
  'gateway JJDevHub.Gateway JJDevHub.Gateway.dll src/gateway/JJDevHub.Gateway/JJDevHub.Gateway.csproj'
)

if [[ ! -f $root/deploy/docker/Dockerfile ]]; then
  printf 'no material\n'
  exit 0
fi

any_project=0
for row in "${images[@]}"; do
  read -r _projekt _project _assembly csproj <<<"$row"
  if [[ -f $root/$csproj ]]; then
    any_project=1
  fi
done

if [[ $any_project -eq 0 ]]; then
  printf 'no material\n'
  exit 0
fi

cd -- "$root"

json=$(bash "$script_dir/affected-components.sh" --from-git)
diff_output=$(git diff --name-only "origin/${BASE_REF}...HEAD")

dockerfile_changed=0
while IFS= read -r path || [[ -n ${path:-} ]]; do
  path=${path%$'\r'}
  path=${path#./}
  if [[ $path == deploy/docker/Dockerfile || $path == .dockerignore ]]; then
    dockerfile_changed=1
  fi
done <<<"$diff_output"

selected() {
  python3 -c 'import json,sys; data=json.loads(sys.argv[1]); raise SystemExit(0 if sys.argv[2] in data.get("components", []) else 1)' "$json" "$1"
}

found=0
for row in "${images[@]}"; do
  read -r projekt project assembly csproj <<<"$row"
  if [[ ! -f $csproj ]]; then
    continue
  fi
  if [[ $dockerfile_changed -eq 1 ]] || selected "$projekt"; then
    printf '%s %s %s\n' "$projekt" "$project" "$assembly"
    found=1
  fi
done

if [[ $found -eq 0 ]]; then
  printf 'no material\n'
fi
