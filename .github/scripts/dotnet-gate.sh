#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "użycie: dotnet-gate.sh <build|unit-tests|format|integration-tests|performance>" >&2
  exit 2
fi

job=$1
root=$(cd "$(dirname "$0")/../.." && pwd)
cd "$root"

no_material() {
  printf 'no material\n'
  exit 0
}

prune() {
  find "$root" \( -path "$root/.git" -o -path '*/bin' -o -path '*/obj' -o -path '*/node_modules' \) -prune -o "$@" -print
}

has_any() {
  local pattern=$1
  local found
  found=$(find "$root" \( -path "$root/.git" -o -path '*/bin' -o -path '*/obj' -o -path '*/node_modules' \) -prune -o -name "$pattern" -print -quit)
  [[ -n $found ]]
}

is_test_project() {
  local file=$1
  local base=${file##*/}
  if [[ $base == *Tests.csproj ]]; then
    return 0
  fi
  grep -Eq 'Include="xunit"|Include="xunit\.|Include="Microsoft\.NET\.Test\.Sdk"|<IsTestProject>[[:space:]]*true[[:space:]]*</IsTestProject>' "$file"
}

has_test_project() {
  local csproj
  while IFS= read -r csproj; do
    [[ -z $csproj ]] && continue
    if is_test_project "$csproj"; then
      return 0
    fi
  done < <(prune -name '*.csproj')
  return 1
}

test_total() {
  local log=$1
  local trx=${2:-}
  local line count
  line=$(grep -E 'Total tests:[[:space:]]*[0-9]+' "$log" | tail -n 1 || true)
  if [[ -z $line ]]; then
    line=$(grep -E 'Total:[[:space:]]*[0-9]+' "$log" | tail -n 1 || true)
  fi
  if [[ -z $line ]]; then
    line=$(grep -E -i '^[[:space:]]*total:[[:space:]]*[0-9]+' "$log" | tail -n 1 || true)
  fi
  if [[ -n $line ]]; then
    count=${line##*[[:space:]]}
    count=${count//[^0-9]/}
    printf '%s' "$count"
    return 0
  fi
  if [[ -n $trx && -f $trx ]]; then
    line=$(grep -m 1 '<Counters total="' "$trx" || true)
    if [[ -n $line ]]; then
      count=${line#*total=\"}
      count=${count%%\"*}
      printf '%s' "$count"
      return 0
    fi
  fi
  if grep -Eq 'No test matches the given testcase filter|Zero tests ran' "$log"; then
    printf '0'
    return 0
  fi
  return 1
}

run_filtered_tests() {
  local filter=$1
  local log code total failed_line failed
  if ! has_test_project; then
    no_material
  fi
  log=$(mktemp)
  set +e
  dotnet test --solution JJDevHub.slnx -c Release --filter "$filter" --ignore-exit-code 8 >"$log" 2>&1
  code=$?
  set -e
  cat "$log"
  total=""
  if total=$(test_total "$log"); then
    :
  else
    total=""
  fi
  failed_line=$(grep -E -i '^[[:space:]]*failed:[[:space:]]*[0-9]+' "$log" | tail -n 1 || true)
  rm -f "$log"
  if [[ $total == 0 || ( -z $total && $code -eq 8 ) ]]; then
    printf 'no material\n'
    exit 0
  fi
  if [[ $code -eq 8 ]]; then
    failed=${failed_line##*[[:space:]]}
    failed=${failed//[^0-9]/}
    if [[ -z $failed || $failed == 0 ]]; then
      exit 0
    fi
  fi
  exit "$code"
}

case $job in
  build)
    if ! has_any '*.csproj'; then
      no_material
    fi
    dotnet build JJDevHub.slnx -c Release --nologo
    ;;
  unit-tests)
    if ! has_test_project; then
      no_material
    fi
    export PATH="${PATH}:${HOME}/.dotnet/tools"
    if ! dotnet tool list --global | grep -Eq 'dotnet-coverage[[:space:]]+18\.12\.0([[:space:]]|$)'; then
      dotnet tool uninstall --global dotnet-coverage >/dev/null 2>&1 || true
      dotnet tool install --global dotnet-coverage --version 18.12.0
    fi
    mkdir -p "$root/artifacts/coverage"
    dotnet-coverage collect \
      --output "$root/artifacts/coverage/coverage.cobertura.xml" \
      --output-format cobertura \
      --nologo \
      -- dotnet test --solution JJDevHub.slnx -c Release --filter 'Category!=Integration&Category!=Performance' --ignore-exit-code 8
    ;;
  format)
    if ! has_any '*.csproj' && ! has_any '*.cs'; then
      no_material
    fi
    dotnet format JJDevHub.slnx --verify-no-changes --severity warn
    ;;
  integration-tests)
    run_filtered_tests 'Category=Integration|Category=Contract'
    ;;
  performance)
    run_filtered_tests 'Category=Performance'
    ;;
  *)
    echo "nieznany job: ${job}" >&2
    exit 2
    ;;
esac
