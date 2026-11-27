#!/usr/bin/env bash
set -euo pipefail

root=$(cd -- "$(dirname -- "$0")/../.." && pwd)
cd -- "$root"

if [[ -z ${SONAR_TOKEN_FILE:-} || ! -s $SONAR_TOKEN_FILE ]]; then
  echo "brak tokenu skanera" >&2
  exit 1
fi
if [[ -z ${SONAR_HOST_URL:-} ]]; then
  echo "brak adresu serwera" >&2
  exit 1
fi
if [[ ! -d $root/src ]]; then
  echo "brak src" >&2
  exit 1
fi

host=${SONAR_HOST_URL%/}
coverage_dir=${COVERAGE_DIR:-$root/artifacts/coverage}
scanner_version=12.1.0
cli_version=8.5.0.16571
cli_sha=62a200079f5ed38682e59ce5fb507f75b7a3c8dd672cfaf647be15a2406422cd

join_by_comma() {
  local IFS=,
  printf '%s' "$*"
}

collect_reports() {
  local name=$1
  shift
  local path
  reports=()
  [[ $# -eq 0 ]] && return 0
  while IFS= read -r path; do
    [[ -z $path ]] && continue
    reports+=("$path")
  done < <(find "$@" -name "$name" -print 2>/dev/null || true)
}

search=()
[[ -d $coverage_dir ]] && search+=("$coverage_dir")
[[ -d $root/src ]] && search+=("$root/src")
reports=()
collect_reports 'coverage.cobertura.xml' "${search[@]+"${search[@]}"}"
cobertura=()
if [[ ${#reports[@]} -gt 0 ]]; then
  cobertura=("${reports[@]}")
fi
collect_reports 'lcov.info' "${search[@]+"${search[@]}"}"
lcov=()
if [[ ${#reports[@]} -gt 0 ]]; then
  lcov=("${reports[@]}")
fi

if [[ ${#cobertura[@]} -eq 0 && ${#lcov[@]} -eq 0 ]]; then
  printf 'pokrycie puste\n'
fi

has_csproj=0
csproj=$(find "$root" \( -path '*/bin' -o -path '*/obj' -o -path '*/node_modules' \) -prune -o -name '*.csproj' -print -quit || true)
if [[ -n $csproj ]]; then
  has_csproj=1
fi

set +x
SONAR_TOKEN=$(<"$SONAR_TOKEN_FILE")
export SONAR_TOKEN

extra=()
if [[ -n ${SONAR_PR_KEY:-} ]]; then
  printf 'community build nie przyjmuje parametrów pull requestu\n'
fi
if [[ ${#cobertura[@]} -gt 0 ]]; then
  extra+=("sonar.cs.cobertura.reportsPaths=$(join_by_comma "${cobertura[@]}")")
fi
if [[ ${#lcov[@]} -gt 0 ]]; then
  extra+=("sonar.javascript.lcov.reportPaths=$(join_by_comma "${lcov[@]}")")
fi

if [[ $has_csproj -eq 1 ]]; then
  if [[ ! -f $root/global.json ]]; then
    echo "brak global.json przy projekcie" >&2
    exit 1
  fi
  export PATH="${PATH}:${HOME}/.dotnet/tools"
  dotnet tool install --global dotnet-sonarscanner --version "$scanner_version"
  begin=(/k:jjdevhub "/d:sonar.host.url=${host}" "/d:sonar.token=${SONAR_TOKEN}")
  while IFS= read -r line || [[ -n ${line:-} ]]; do
    line=${line%%$'\r'}
    [[ $line =~ ^[[:space:]]*# || $line != *=* ]] && continue
    key=${line%%=*}
    value=${line#*=}
    case $key in
      sonar.projectKey | sonar.sources | sonar.tests | sonar.test.inclusions)
        continue
        ;;
    esac
    begin+=("/d:${key}=${value}")
  done <"$root/sonar-project.properties"
  if [[ ${#extra[@]} -gt 0 ]]; then
    for item in "${extra[@]}"; do
      begin+=("/d:${item}")
    done
  fi
  dotnet sonarscanner begin "${begin[@]}"
  dotnet build "$root/JJDevHub.slnx" -c Release --nologo
  dotnet sonarscanner end "/d:sonar.token=${SONAR_TOKEN}"
else
  archive=$(mktemp)
  unpacked=$(mktemp -d)
  url="https://binaries.sonarsource.com/Distribution/sonar-scanner-cli/sonar-scanner-cli-${cli_version}-linux-x64.zip"
  curl --silent --show-error --fail --location --output "$archive" "$url"
  echo "${cli_sha}  ${archive}" | sha256sum -c -
  unzip -q "$archive" -d "$unpacked"
  scanner=$(find "$unpacked" -type f -name sonar-scanner -print -quit)
  if [[ -z $scanner ]]; then
    echo "brak skanera CLI" >&2
    exit 1
  fi
  chmod +x "$scanner"
  args=("-Dsonar.host.url=${host}" "-Dproject.settings=${root}/sonar-project.properties")
  if [[ ${#extra[@]} -gt 0 ]]; then
    for item in "${extra[@]}"; do
      args+=("-D${item}")
    done
  fi
  "$scanner" "${args[@]}"
fi

unset SONAR_TOKEN
