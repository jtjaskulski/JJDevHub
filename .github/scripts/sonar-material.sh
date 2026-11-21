#!/usr/bin/env bash
set -euo pipefail

if [[ $# -gt 1 ]]; then
  echo "użycie: sonar-material.sh [korzeń]" >&2
  exit 2
fi

if [[ $# -eq 1 ]]; then
  root=$(cd -- "$1" && pwd)
else
  root=$(cd -- "$(dirname -- "$0")/../.." && pwd)
fi

is_test() {
  local path=$1
  local base=${path##*/}
  case "$path" in
    tests/* | */tests/* | */Tests/* | *.Tests/*) return 0 ;;
  esac
  case "$base" in
    *.test.ts | *.test.tsx | *.spec.ts | *.spec.tsx) return 0 ;;
  esac
  return 1
}

found=0
production=0
while IFS= read -r path; do
  [[ -z $path ]] && continue
  rel=${path#"$root"/}
  case "$rel" in
    *.cs | *.ts | *.tsx) ;;
    *) continue ;;
  esac
  found=1
  if ! is_test "$rel"; then
    production=1
    break
  fi
done < <(find "$root" \
  \( -path "$root/.git" -o -path '*/bin' -o -path '*/obj' -o -path '*/node_modules' -o -path '*/dist' \) -prune \
  -o \( -name '*.cs' -o -name '*.ts' -o -name '*.tsx' \) -print)

if [[ $production -eq 1 ]]; then
  printf 'material\n'
elif [[ $found -eq 1 ]]; then
  printf 'no material: brak produkcji\n'
else
  printf 'no material\n'
fi
