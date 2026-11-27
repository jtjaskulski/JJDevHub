#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "użycie: deploy-component.sh <tag> [korzeń]" >&2
  exit 2
}

if [[ $# -lt 1 || $# -gt 2 ]]; then
  usage
fi

tag=$1
if [[ $# -eq 2 ]]; then
  root=$(cd -- "$2" && pwd)
else
  root=$(cd -- "$(dirname -- "$0")/../.." && pwd)
fi

pattern='^(identity|news|learning|cv|contact|gateway|web|mobile)-v[0-9]+\.[0-9]+\.[0-9]+$'
if [[ ! $tag =~ $pattern ]]; then
  echo "zły tag: ${tag}" >&2
  exit 1
fi

component=${tag%%-v*}
file=$root/.github/deploy-components.txt
if [[ ! -f $file ]]; then
  echo "brak .github/deploy-components.txt" >&2
  exit 1
fi

declare -A seen=()
match=""
while IFS= read -r line || [[ -n $line ]]; do
  line=${line%$'\r'}
  [[ -z $line ]] && continue
  read -r name project assembly extra <<<"$line"
  if [[ -z ${name:-} || -z ${project:-} || -z ${assembly:-} || -n ${extra:-} ]]; then
    echo "zła linia komponentu" >&2
    exit 1
  fi
  if [[ ! $name =~ ^(identity|news|learning|cv|contact|gateway|web|mobile)$ ]]; then
    echo "komponent spoza słownika: ${name}" >&2
    exit 1
  fi
  if [[ ! $project =~ ^[A-Za-z0-9.]+$ || ! $assembly =~ ^[A-Za-z0-9.]+\.dll$ ]]; then
    echo "zła nazwa obrazu: ${name}" >&2
    exit 1
  fi
  if [[ -n ${seen[$name]:-} ]]; then
    echo "powtórzony komponent: ${name}" >&2
    exit 1
  fi
  seen[$name]=1
  if [[ $name == "$component" ]]; then
    match=$line
  fi
done <"$file"

if [[ -z $match ]]; then
  printf '%s\n' "component has no image yet"
  exit 0
fi

printf '%s\n' "$match"
