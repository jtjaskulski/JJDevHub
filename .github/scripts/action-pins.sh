#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "$1" >&2
  exit 2
}

trim() {
  local value=$1
  value=${value#"${value%%[![:space:]]*}"}
  value=${value%"${value##*[![:space:]]}"}
  printf '%s' "$value"
}

script_dir=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
repo_root=$(CDPATH='' cd -- "${script_dir}/../.." && pwd)

if [[ $# -gt 1 ]]; then
  usage "podaj plik albo katalog"
fi

if [[ $# -eq 0 ]]; then
  target="${repo_root}/.github/workflows"
else
  target=$1
fi

if [[ ! -e $target ]]; then
  usage "brak ścieżki"
fi

files=()
if [[ -f $target ]]; then
  files+=("$target")
elif [[ -d $target ]]; then
  while IFS= read -r file; do
    [[ -z $file ]] && continue
    files+=("$file")
  done < <(find "$target" -maxdepth 1 -type f \( -name '*.yml' -o -name '*.yaml' \) | sort)
else
  usage "brak ścieżki"
fi

status=0
for file in "${files[@]}"; do
  line_no=0
  while IFS= read -r line || [[ -n $line ]]; do
    line_no=$((line_no + 1))
    [[ $line =~ ^[[:space:]]*-?[[:space:]]*uses:[[:space:]]*(.*)$ ]] || continue
    raw=$(trim "${BASH_REMATCH[1]}")
    if [[ $raw == \"*\" ]]; then
      raw=${raw#\"}
      raw=${raw%\"}
    elif [[ $raw == \'*\' ]]; then
      raw=${raw#\'}
      raw=${raw%\'}
    fi
    code=$raw
    comment=""
    if [[ $raw == *#* ]]; then
      code=$(trim "${raw%%#*}")
      comment=$(trim "${raw#*#}")
    fi
    if [[ -z $code ]]; then
      printf '%s:%s brak uses\n' "$file" "$line_no" >&2
      status=1
      continue
    fi
    if [[ $code == ./* ]]; then
      continue
    fi
    sha=${code##*@}
    if [[ $code != *@* || ! $sha =~ ^[0-9a-fA-F]{40}$ ]]; then
      printf '%s:%s brak pełnego SHA\n' "$file" "$line_no" >&2
      status=1
      continue
    fi
    if [[ ! $comment =~ ^v ]]; then
      printf '%s:%s brak komentarza wersji\n' "$file" "$line_no" >&2
      status=1
    fi
  done < "$file"
done

exit "$status"
