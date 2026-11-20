#!/usr/bin/env bash
set -euo pipefail

if [[ $# -gt 1 ]]; then
  echo "użycie: vulnerable.sh [korzeń]" >&2
  exit 2
fi

if [[ $# -eq 1 ]]; then
  root=$(cd -- "$1" && pwd)
else
  root=$(cd -- "$(dirname -- "$0")/../.." && pwd)
fi
cd -- "$root"

tmps=()
cleanup() {
  if [[ ${#tmps[@]} -gt 0 ]]; then
    rm -f -- "${tmps[@]}"
  fi
}
trap cleanup EXIT

blocking_severity() {
  case "$1" in
    High|Critical|high|critical) return 0 ;;
    *) return 1 ;;
  esac
}

load_allow() {
  local file=$1
  local line id date reason
  allow_ids=()
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
    allow_ids+=("$id")
  done <"$file"
}

allowed() {
  local id=$1
  local known
  [[ -n $id ]] || return 1
  [[ ${#allow_ids[@]} -gt 0 ]] || return 1
  for known in "${allow_ids[@]}"; do
    if [[ $known == "$id" ]]; then
      return 0
    fi
  done
  return 1
}

gate_report() {
  local kind=$1
  local file=$2
  local report sev ident name status
  report_blocking=0
  report=$(mktemp)
  tmps+=("$report")
  if ! python3 - "$kind" "$file" >"$report" <<'PY'
import json
import sys

kind, path = sys.argv[1], sys.argv[2]
with open(path, encoding="utf-8") as handle:
    data = json.load(handle)

def advisory_id(url, explicit=None):
    if explicit:
        return str(explicit)
    if not url:
        return ""
    return str(url).rstrip("/").rsplit("/", 1)[-1]

def emit(severity, ident, name):
    print(f"{severity}\t{ident}\t{name}")

if kind == "nuget":
    for project in data.get("projects") or []:
        for framework in project.get("frameworks") or []:
            packages = []
            packages.extend(framework.get("topLevelPackages") or [])
            packages.extend(framework.get("transitivePackages") or [])
            for package in packages:
                for vuln in package.get("vulnerabilities") or []:
                    url = vuln.get("advisoryurl") or vuln.get("advisoryUrl") or ""
                    emit(vuln.get("severity") or "", advisory_id(url), package.get("id") or "")
elif kind == "npm":
    vulns = data.get("vulnerabilities") or {}
    if isinstance(vulns, dict) and vulns:
        for name, item in vulns.items():
            if not isinstance(item, dict):
                continue
            detailed = False
            for via in item.get("via") or []:
                if not isinstance(via, dict):
                    continue
                detailed = True
                url = via.get("url") or ""
                emit(
                    via.get("severity") or item.get("severity") or "",
                    advisory_id(url, via.get("github_advisory_id")),
                    via.get("name") or name,
                )
            if not detailed:
                emit(item.get("severity") or "", "", name)
    else:
        advisories = data.get("advisories") or {}
        if isinstance(advisories, dict):
            for item in advisories.values():
                if not isinstance(item, dict):
                    continue
                url = item.get("url") or ""
                emit(
                    item.get("severity") or "",
                    advisory_id(url, item.get("github_advisory_id")),
                    item.get("module_name") or "",
                )
else:
    sys.exit(2)
PY
  then
    echo "zły raport ${kind}" >&2
    exit 1
  fi
  status=0
  while IFS=$'\t' read -r sev ident name || [[ -n ${sev:-} ]]; do
    [[ -z ${sev:-} ]] && continue
    if blocking_severity "$sev"; then
      report_blocking=$((report_blocking + 1))
      if allowed "$ident"; then
        printf 'tłumienie %s %s %s\n' "$name" "$sev" "$ident"
      else
        printf '%s %s %s\n' "$name" "$sev" "$ident" >&2
        status=1
      fi
    else
      printf '%s %s %s\n' "$name" "$sev" "$ident"
    fi
  done <"$report"
  return "$status"
}

collect() {
  find "$root" \
    \( -path "$root/.git" -o -path '*/bin' -o -path '*/obj' -o -path '*/node_modules' \) -prune \
    -o \( -name '*.csproj' -o -name 'package.json' \) -print
}

allow_ids=()
load_allow "$root/.github/advisories-allow.txt"

csprojs=()
packages=()
while IFS= read -r path; do
  [[ -z $path ]] && continue
  case "$path" in
    *.csproj) csprojs+=("$path") ;;
    *) packages+=("$path") ;;
  esac
done < <(collect)

if [[ ${#csprojs[@]} -eq 0 && ${#packages[@]} -eq 0 ]]; then
  printf 'no material\n'
  exit 0
fi

if [[ ${#csprojs[@]} -eq 0 ]]; then
  printf 'no material: nuget\n'
fi
if [[ ${#packages[@]} -eq 0 ]]; then
  printf 'no material: npm\n'
fi

if [[ ${#csprojs[@]} -gt 0 && ! -f $root/global.json ]]; then
  echo "brak global.json przy projekcie" >&2
  exit 1
fi

status=0
for package_json in "${packages[@]}"; do
  dir=$(dirname -- "$package_json")
  if [[ ! -f $dir/pnpm-lock.yaml ]]; then
    echo "brak pnpm-lock.yaml przy ${package_json}" >&2
    status=1
  fi
done
if [[ $status -ne 0 ]]; then
  exit 1
fi

if [[ ${#csprojs[@]} -gt 0 ]]; then
  slnx=$root/JJDevHub.slnx
  if [[ ! -f $slnx ]]; then
    echo "brak JJDevHub.slnx" >&2
    exit 1
  fi
  dotnet restore "$slnx"
  nuget_json=$(mktemp)
  tmps+=("$nuget_json")
  dotnet list "$slnx" package --vulnerable --include-transitive --format json >"$nuget_json"
  if ! gate_report nuget "$nuget_json"; then
    status=1
  fi
fi

for package_json in "${packages[@]}"; do
  dir=$(dirname -- "$package_json")
  if ! command -v pnpm >/dev/null 2>&1; then
    echo "brak pnpm" >&2
    exit 1
  fi
  npm_json=$(mktemp)
  npm_err=$(mktemp)
  tmps+=("$npm_json" "$npm_err")
  set +e
  (
    cd -- "$dir"
    pnpm audit --audit-level high --json
  ) >"$npm_json" 2>"$npm_err"
  npm_code=$?
  set -e
  if [[ $npm_code -gt 1 ]]; then
    echo "pnpm audit nie doszedł w ${dir}" >&2
    cat "$npm_err" >&2
    exit 1
  fi
  if [[ ! -s $npm_json ]]; then
    if [[ $npm_code -ne 0 ]]; then
      echo "pnpm audit bez raportu w ${dir}" >&2
      exit 1
    fi
    continue
  fi
  if ! gate_report npm "$npm_json"; then
    status=1
  fi
  if [[ $npm_code -ne 0 && $report_blocking -eq 0 ]]; then
    echo "pnpm audit zwrócił ${npm_code} bez progu w raporcie: ${dir}" >&2
    status=1
  fi
done

exit "$status"
