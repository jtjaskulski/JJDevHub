#!/usr/bin/env bash
set -euo pipefail

root=""
paths=()

usage() {
  echo "$1" >&2
  exit 2
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --root)
      if [[ $# -lt 2 || $2 == --* ]]; then
        usage "brak --root"
      fi
      root=$2
      shift 2
      ;;
    --paths)
      if [[ $# -lt 2 || $2 == --* ]]; then
        usage "brak --paths"
      fi
      paths+=("$2")
      shift 2
      ;;
    *)
      usage "nieznany argument: $1"
      ;;
  esac
done

if [[ -z $root ]]; then
  script_dir=$(CDPATH='' cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
  root=$(CDPATH='' cd -- "${script_dir}/../.." && pwd)
fi

if [[ ! -d $root ]]; then
  usage "brak katalogu: ${root}"
fi

cd -- "$root"

component_names=(identity news learning cv contact gateway web mobile)
kernel_root=src/shared/JJDevHub.Shared.Kernel

component_root() {
  case "$1" in
    identity) printf '%s' 'src/services/Identity' ;;
    news) printf '%s' 'src/services/News' ;;
    learning) printf '%s' 'src/services/Learning' ;;
    cv) printf '%s' 'src/services/Cv' ;;
    contact) printf '%s' 'src/services/Contact' ;;
    gateway) printf '%s' 'src/gateway' ;;
    web) printf '%s' 'src/clients/web' ;;
    mobile) printf '%s' 'src/clients/mobile' ;;
    *) return 1 ;;
  esac
}

component_of_path() {
  local path=$1
  local name
  local component_path
  for name in "${component_names[@]}"; do
    component_path=$(component_root "$name")
    if [[ $path == "$component_path" || $path == "$component_path"/* ]]; then
      printf '%s' "$name"
      return 0
    fi
  done
  return 1
}

kernel=0
path=""
for path in "${paths[@]+"${paths[@]}"}"; do
  path=${path%$'\r'}
  path=${path#./}
  if [[ $path == "$kernel_root" || $path == "$kernel_root"/* ]]; then
    kernel=1
  fi
done

if [[ $kernel -eq 0 ]]; then
  exit 0
fi

references_kernel() {
  local flat
  flat=$(tr '\n' ' ' < "$1" | tr -d '\r')
  grep -Eq 'ProjectReference[^<]*JJDevHub\.Shared\.Kernel' <<< "$flat"
}

found=' '
mark() {
  case "$found" in
    *" $1 "*) ;;
    *) found="${found}$1 " ;;
  esac
}

has() {
  case "$found" in
    *" $1 "*) return 0 ;;
    *) return 1 ;;
  esac
}

if [[ -d . ]]; then
  while IFS= read -r csproj; do
    [[ -z $csproj ]] && continue
    if ! references_kernel "$csproj"; then
      continue
    fi
    relative=${csproj#./}
    if name=$(component_of_path "$relative"); then
      mark "$name"
    fi
  done < <(find . \( -path './.git' -o -path '*/bin' -o -path '*/obj' -o -path '*/node_modules' \) -prune -o -name '*.csproj' -print)
fi

any=0
name=""
for name in "${component_names[@]}"; do
  if has "$name"; then
    printf '%s\n' "$name"
    any=1
  fi
done

if [[ $any -eq 0 ]]; then
  printf '%s\n' 'nie ma kogo podbijać'
fi
