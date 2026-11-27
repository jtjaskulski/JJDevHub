#!/usr/bin/env bash
set -euo pipefail

root=$(cd -- "$(dirname -- "$0")/../.." && pwd)
component_sh=$root/.github/scripts/deploy-component.sh
version_sh=$root/.github/scripts/set-service-version.sh
verify_sh=$root/.github/scripts/verify-attestation.sh
workflow=$root/.github/workflows/deploy.yml
versions=$root/.github/workflows/versions.yml

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
  log=$(bash "$@" 2>&1)
  code=$?
  set -e
  if [[ $code -ne $expected ]]; then
    fail "oczekiwano ${expected}, jest ${code}" "$log"
  fi
}

expect_out() {
  local expected=$1
  shift
  local actual
  actual=$(bash "$@")
  if [[ $actual != "$expected" ]]; then
    fail "oczekiwano [${expected}], jest [${actual}]"
  fi
}

expect_out 'component has no image yet' "$component_sh" web-v0.1.0 "$root"
expect_out 'gateway JJDevHub.Gateway JJDevHub.Gateway.dll' "$component_sh" gateway-v0.1.0 "$root"
run_script 1 "$component_sh" nope-v1.0.0 "$root"
run_script 1 "$component_sh" gateway-v1 "$root"
run_script 1 "$component_sh" gateway-latest "$root"

empty=$(mktemp -d)
dirs+=("$empty")
mkdir -p "$empty/.github"
: >"$empty/.github/deploy-components.txt"
expect_out 'component has no image yet' "$component_sh" gateway-v0.1.0 "$empty"

bad_line=$(mktemp -d)
dirs+=("$bad_line")
mkdir -p "$bad_line/.github"
printf '%s\n' 'gateway JJDevHub.Gateway' >"$bad_line/.github/deploy-components.txt"
run_script 1 "$component_sh" gateway-v0.1.0 "$bad_line"

duplicate=$(mktemp -d)
dirs+=("$duplicate")
mkdir -p "$duplicate/.github"
printf '%s\n' 'gateway JJDevHub.Gateway JJDevHub.Gateway.dll' 'gateway JJDevHub.Gateway JJDevHub.Gateway.dll' >"$duplicate/.github/deploy-components.txt"
run_script 1 "$component_sh" gateway-v0.1.0 "$duplicate"

env_file=$(mktemp)
printf '%s\n' 'OTHER=kept' 'SERVICE_VERSION=old' >"$env_file"
stdout=$(bash "$version_sh" "$env_file" gateway-v0.2.0 2>"${env_file}.err")
if [[ -n $stdout ]]; then
  fail "skrypt wersji wypisał plik" "$stdout"
fi
if [[ -s ${env_file}.err ]]; then
  fail "skrypt wersji wypisał błąd" "$(cat "${env_file}.err")"
fi
if ! grep -qx 'OTHER=kept' "$env_file"; then
  fail "skrypt wersji usunął inną zmienną"
fi
if ! grep -qx 'SERVICE_VERSION=gateway-v0.2.0' "$env_file"; then
  fail "skrypt wersji nie zapisał tagu"
fi
if grep -q 'old' "$env_file"; then
  fail "skrypt wersji zostawił poprzedni tag"
fi
mode=$(stat -c '%a' "$env_file")
if [[ $mode != 600 ]]; then
  fail "plik env ma tryb ${mode}"
fi
run_script 1 "$version_sh" "$env_file" gateway-latest
rm -f "$env_file" "${env_file}.err"

python3 - "$workflow" "$versions" <<'PY'
import pathlib
import sys

workflow = pathlib.Path(sys.argv[1]).read_text(encoding="utf-8")
versions = pathlib.Path(sys.argv[2]).read_text(encoding="utf-8")

def die(message):
    raise SystemExit(message)

if "pull_request" in workflow:
    die("deploy.yml ma pull_request")
if ":latest" in workflow:
    die("deploy.yml ma latest")
if "workflow_run" in workflow:
    die("deploy.yml ma workflow_run")
if "cancel-in-progress: false" not in workflow:
    die("cancel-in-progress nie jest wyłączone")
if "cancel-in-progress: true" in workflow:
    die("cancel-in-progress jest włączone")
if "environment: production" not in workflow:
    die("brak environment production")
if "push-to-registry: true" not in workflow:
    die("atestacja nie jedzie do rejestru")
if "docker push" not in workflow:
    die("brak docker push")
if "--no-build" not in workflow:
    die("deploy buduje obraz")
if "cat /etc/jjdevhub/compose.env" in workflow or "compose.env |" in workflow:
    die("workflow drukuje compose.env")

publish = workflow.split("  deploy:\n", 1)[0]
deploy = workflow.split("  deploy:\n", 1)[1]
if "self-hosted" in publish:
    die("publish stoi na runnerze maszyny")
if "runs-on: ubuntu-latest" not in publish:
    die("publish nie jest na ubuntu-latest")
if "runs-on: [self-hosted, jjdevhub]" not in deploy:
    die("deploy nie jest na runnerze jjdevhub")
if "needs: publish" not in deploy:
    die("deploy nie czeka na publish")
for permission in ("contents: read", "packages: write", "id-token: write", "attestations: write"):
    if permission not in publish:
        die(f"publish nie ma uprawnienia {permission}")
for permission in ("contents: read", "packages: read", "id-token: write", "attestations: read"):
    if permission not in deploy:
        die(f"deploy nie ma uprawnienia {permission}")
if "packages: write" in deploy.split("permissions:", 1)[1].split("env:", 1)[0]:
    die("deploy ma zapis pakietów")

if "uses: ./.github/workflows/deploy.yml" not in versions:
    die("versions.yml nie woła deployu")
if "workflow_run" in versions:
    die("versions.yml ma workflow_run")
if "bash tests/ci/deploy.sh" not in versions:
    die("suchy przebieg deployu nie jest w release-tags")
PY

zero=$(printf 'sha256:%064d' 0)
run_script 1 "$verify_sh" jtjaskulski gateway "$zero"
if [[ $log == *'brak gh'* || $log == *'użycie:'* || -z $log ]]; then
  fail "weryfikacja nie użyła narzędzia atestacji" "$log"
fi
run_script 2 "$verify_sh" jtjaskulski gateway not-a-digest
