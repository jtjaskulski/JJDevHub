#!/usr/bin/env bash
set -euo pipefail

root=$(cd "$(dirname "$0")/../.." && pwd)
gate=$root/.github/scripts/dotnet-gate.sh
affected=$root/.github/scripts/affected-components.sh

rm -rf "$root"/tests/ci/.format.*

expect_no_material() {
  local job=$1
  local log code
  set +e
  log=$(bash "$gate" "$job" 2>&1)
  code=$?
  set -e
  if [[ $code -ne 0 ]]; then
    echo "oczekiwano 0, jest ${code} dla ${job}" >&2
    printf '%s\n' "$log" >&2
    exit 1
  fi
  if [[ $log != *"no material"* ]]; then
    echo "brak logu no material dla ${job}" >&2
    printf '%s\n' "$log" >&2
    exit 1
  fi
}

for job in build unit-tests format integration-tests performance; do
  expect_no_material "$job"
done

list=$(bash "$affected")
if [[ $list != '{"components":[],"kernel":false}' ]]; then
  echo "oczekiwano pustej listy komponentów, jest: ${list}" >&2
  exit 1
fi

work=$(mktemp -d "${root}/tests/ci/.format.XXXXXX")
cleanup() {
  rm -rf "$work"
}
trap cleanup EXIT

cat >"$work/Sample.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
</Project>
EOF

printf '%s' 'namespace Sample;

public static class SampleType
{
    public static int Run()
    {
        int  value = 1;
        return value;
    }
}' >"$work/Sample.cs"

log=$(mktemp)
set +e
dotnet format "$work/Sample.csproj" --verify-no-changes >"$log" 2>&1
code=$?
set -e
if [[ $code -ne 2 ]]; then
  echo "oczekiwano 2, jest ${code}: dotnet format --verify-no-changes" >&2
  cat "$log" >&2
  exit 1
fi
if ! grep -q 'WHITESPACE' "$log"; then
  echo "brak diagnostyki WHITESPACE" >&2
  cat "$log" >&2
  exit 1
fi
