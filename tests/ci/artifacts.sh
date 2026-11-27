#!/usr/bin/env bash
set -euo pipefail

root=$(cd -- "$(dirname -- "$0")/../.." && pwd)
script=$root/.github/scripts/artifacts-material.sh
workflow=$root/.github/workflows/artifacts.yml

fail() {
  echo "$1" >&2
  if [[ $# -gt 1 ]]; then
    printf '%s\n' "$2" >&2
  fi
  exit 1
}

dirs=()
cleanup() {
  if [[ ${#dirs[@]} -gt 0 ]]; then
    rm -rf -- "${dirs[@]}"
  fi
}
trap cleanup EXIT

run_script() {
  local expected=$1
  shift
  set +e
  log=$(bash "$script" "$@" 2>&1)
  code=$?
  set -e
  if [[ $code -ne $expected ]]; then
    fail "oczekiwano ${expected}, jest ${code}" "$log"
  fi
}

if [[ -f $root/deploy/docker/Dockerfile && -f $root/src/gateway/JJDevHub.Gateway/JJDevHub.Gateway.csproj ]]; then
  run_script 1 "$root"
  if [[ $log != *'brak BASE_REF'* ]]; then
    fail "drzewo z Dockerfile bez BASE_REF" "$log"
  fi
else
  run_script 0 "$root"
  if [[ $log != 'no material' ]]; then
    fail "brak logu no material na drzewie repozytorium" "$log"
  fi
fi

empty=$(mktemp -d)
dirs+=("$empty")
run_script 0 "$empty"
if [[ $log != 'no material' ]]; then
  fail "brak logu no material" "$log"
fi

dockerfile_only=$(mktemp -d)
dirs+=("$dockerfile_only")
mkdir -p "$dockerfile_only/deploy/docker"
printf 'FROM scratch\n' >"$dockerfile_only/deploy/docker/Dockerfile"
run_script 0 "$dockerfile_only"
if [[ $log != 'no material' ]]; then
  fail "Dockerfile bez projektu nie jest no material" "$log"
fi

with_project=$(mktemp -d)
dirs+=("$with_project")
mkdir -p "$with_project/deploy/docker" "$with_project/src/gateway/JJDevHub.Gateway"
printf 'FROM scratch\n' >"$with_project/deploy/docker/Dockerfile"
printf '<Project Sdk="Microsoft.NET.Sdk"></Project>\n' >"$with_project/src/gateway/JJDevHub.Gateway/JJDevHub.Gateway.csproj"
run_script 1 "$with_project"
if [[ $log != *'brak BASE_REF'* ]]; then
  fail "brak informacji o BASE_REF" "$log"
fi

seed_repo() {
  local dir=$1
  mkdir -p "$dir/deploy/docker" "$dir/src/gateway/JJDevHub.Gateway" "$dir/src/services/News"
  printf 'FROM scratch\n' >"$dir/deploy/docker/Dockerfile"
  printf '<Project Sdk="Microsoft.NET.Sdk"></Project>\n' >"$dir/src/gateway/JJDevHub.Gateway/JJDevHub.Gateway.csproj"
  printf '<Project Sdk="Microsoft.NET.Sdk"></Project>\n' >"$dir/src/services/News/News.csproj"
  git -C "$dir" init -b develop >/dev/null
  git -C "$dir" config user.email test@example.com
  git -C "$dir" config user.name test
  git -C "$dir" config commit.gpgsign false
  git -C "$dir" add -A
  git -C "$dir" commit -m init >/dev/null
  git -C "$dir" update-ref refs/remotes/origin/develop HEAD
}

expect_list() {
  local dir=$1
  local expected=$2
  run_script 0 "$dir"
  if [[ $log != "$expected" ]]; then
    fail "oczekiwano listy: ${expected}" "$log"
  fi
}

news=$(mktemp -d)
dirs+=("$news")
seed_repo "$news"
git -C "$news" checkout -b feature/news >/dev/null 2>&1
printf '\n' >>"$news/src/services/News/News.csproj"
git -C "$news" add -A
git -C "$news" commit -m news >/dev/null
BASE_REF=develop run_script 0 "$news"
if [[ $log != 'no material' ]]; then
  fail "zmiana News buduje obraz" "$log"
fi

gateway=$(mktemp -d)
dirs+=("$gateway")
seed_repo "$gateway"
git -C "$gateway" checkout -b feature/gateway >/dev/null 2>&1
printf '\n' >>"$gateway/src/gateway/JJDevHub.Gateway/JJDevHub.Gateway.csproj"
git -C "$gateway" add -A
git -C "$gateway" commit -m gateway >/dev/null
BASE_REF=develop expect_list "$gateway" 'gateway JJDevHub.Gateway JJDevHub.Gateway.dll'

docker_change=$(mktemp -d)
dirs+=("$docker_change")
seed_repo "$docker_change"
git -C "$docker_change" checkout -b feature/docker >/dev/null 2>&1
printf '\n' >>"$docker_change/deploy/docker/Dockerfile"
git -C "$docker_change" add -A
git -C "$docker_change" commit -m docker >/dev/null
BASE_REF=develop expect_list "$docker_change" 'gateway JJDevHub.Gateway JJDevHub.Gateway.dll'

ignore_change=$(mktemp -d)
dirs+=("$ignore_change")
seed_repo "$ignore_change"
git -C "$ignore_change" checkout -b feature/ignore >/dev/null 2>&1
printf '.git\n' >"$ignore_change/.dockerignore"
git -C "$ignore_change" add -A
git -C "$ignore_change" commit -m ignore >/dev/null
BASE_REF=develop expect_list "$ignore_change" 'gateway JJDevHub.Gateway JJDevHub.Gateway.dll'

absent=$(mktemp -d)
dirs+=("$absent")
run_script 0 --ignore "$absent"
if [[ -n $log ]]; then
  fail "brak pliku tłumienia drukuje identyfikatory" "$log"
fi

star=$(mktemp -d)
dirs+=("$star")
mkdir -p "$star/.github"
printf '* 2026-11-20 powód tłumienia.\n' >"$star/.github/trivy-allow.txt"
run_script 1 --ignore "$star"
if [[ $log != *'gwiazdk'* ]]; then
  fail "brak informacji o gwiazdce" "$log"
fi

short=$(mktemp -d)
dirs+=("$short")
mkdir -p "$short/.github"
printf 'CVE-2024-0001 2026-11-20\n' >"$short/.github/trivy-allow.txt"
run_script 1 --ignore "$short"
if [[ $log != *'zdania'* ]]; then
  fail "brak informacji o zdaniu" "$log"
fi

allowed=$(mktemp -d)
dirs+=("$allowed")
mkdir -p "$allowed/.github"
printf 'CVE-2024-0001 2026-11-20 powód tłumienia.\n' >"$allowed/.github/trivy-allow.txt"
run_script 0 --ignore "$allowed"
if [[ $log != 'CVE-2024-0001' ]]; then
  fail "tłumienie nie oddało identyfikatora" "$log"
fi

if ! grep -q -- '--severity HIGH,CRITICAL' "$workflow"; then
  fail "brak progu HIGH,CRITICAL w workflow"
fi
if ! grep -q -- '--exit-code 1' "$workflow"; then
  fail "brak kodu wyjścia Trivy w workflow"
fi
if ! grep -q -- '--ignore-unfixed=false' "$workflow"; then
  fail "brak ignore-unfixed=false w workflow"
fi
if grep -q 'docker push' "$workflow"; then
  fail "workflow zawiera docker push"
fi
if grep -Eq 'cosign|COSIGN' "$workflow"; then
  fail "workflow zawiera klucz podpisujący"
fi
if ! grep -q 'push-to-registry: false' "$workflow"; then
  fail "atestacja nie ma push-to-registry: false"
fi
if ! grep -q 'actions/attest-build-provenance@' "$workflow"; then
  fail "brak atestacji w workflow"
fi
if ! grep -q 'AUTHORIZATION: basic' "$workflow"; then
  fail "brak nagłówka basic przy fetchu"
fi
if grep -q 'AUTHORIZATION: bearer' "$workflow"; then
  fail "fetch używa bearer"
fi

for name in image-scan sbom attestations; do
  if ! grep -q "name: ${name}" "$workflow"; then
    fail "brak joba ${name}"
  fi
  for ruleset in develop release main; do
    if ! grep -q "\"context\": \"${name}\"" "$root/.github/rulesets/${ruleset}.json"; then
      fail "brak ${name} w ${ruleset}"
    fi
  done
done
