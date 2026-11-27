#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 0 ]]; then
  echo "użycie: sonar-bootstrap.sh" >&2
  exit 2
fi

command -v curl >/dev/null 2>&1 || {
  echo "brak curl" >&2
  exit 2
}
command -v python3 >/dev/null 2>&1 || {
  echo "brak python3" >&2
  exit 2
}

if [[ -z ${SONAR_ADMIN_PASSWORD:-} || ${SONAR_ADMIN_PASSWORD} =~ [[:space:]] ]]; then
  echo "brak hasła administratora" >&2
  exit 2
fi
if [[ ${#SONAR_ADMIN_PASSWORD} -lt 12 || ! ${SONAR_ADMIN_PASSWORD} =~ [[:upper:]] || ! ${SONAR_ADMIN_PASSWORD} =~ [[:lower:]] || ! ${SONAR_ADMIN_PASSWORD} =~ [[:digit:]] || ! ${SONAR_ADMIN_PASSWORD} =~ [^[:alnum:]] ]]; then
  SONAR_ADMIN_PASSWORD="${SONAR_ADMIN_PASSWORD}Aa1!"
fi
if [[ -z ${SONAR_TOKEN_FILE:-} ]]; then
  echo "brak SONAR_TOKEN_FILE" >&2
  exit 2
fi

host=${SONAR_HOST_URL:-http://127.0.0.1:9000}
host=${host%/}
case "$host" in
  http://* | https://*) ;;
  *)
    echo "zły adres serwera" >&2
    exit 2
    ;;
esac

script_dir=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
repo_root=$(CDPATH='' cd -- "${script_dir}/../.." && pwd)
gate_file=${SONAR_GATE_FILE:-${repo_root}/deploy/sonar/quality-gate.json}
if [[ ! -f $gate_file ]]; then
  echo "brak pliku bramki" >&2
  exit 1
fi

conditions=$(mktemp)
netrc=$(mktemp)
prev=$(mktemp)
new=$(mktemp)
body=$(mktemp)
chmod 600 "$netrc" "$prev" "$new" "$body"
cleanup() {
  rm -f -- "$conditions" "$netrc" "$prev" "$new" "$body"
}
trap cleanup EXIT

if ! python3 - "$gate_file" >"$conditions" <<'PY'
import json
import sys

gate = json.load(open(sys.argv[1], encoding="utf-8"))
mapping = {
    "new_blocker": ("new_software_quality_blocker_issues", "GT"),
    "new_critical": ("new_software_quality_high_issues", "GT"),
    "new_hotspots_reviewed": ("new_security_hotspots_reviewed", "LT"),
    "new_duplicated_lines_density": ("new_duplicated_lines_density", "GT"),
    "new_coverage": ("new_coverage", "LT"),
}
if set(gate) != set(mapping):
    sys.exit(1)
for key, (metric, op) in mapping.items():
    value = gate[key]
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        sys.exit(1)
    print(f"{metric}\t{op}\t{value}")
PY
then
  echo "bramka w pliku jest niekompletna" >&2
  exit 1
fi

printf '%s' 'admin' >"$prev"
printf '%s' "$SONAR_ADMIN_PASSWORD" >"$new"

write_netrc() {
  local password=$1
  local name=${host#*://}
  name=${name%%/*}
  name=${name%%:*}
  printf 'machine %s\nlogin admin\npassword ' "$name" >"$netrc"
  printf '%s\n' "$password" >>"$netrc"
  chmod 600 "$netrc"
}

api_code=""
call_api() {
  local method=$1
  local path=$2
  shift 2
  if ! api_code=$(curl --silent --show-error --netrc-file "$netrc" \
    --output "$body" --write-out '%{http_code}' \
    --request "$method" "$@" \
    --url "${host}${path}"); then
    echo "błąd wywołania ${path}" >&2
    exit 1
  fi
}

deadline=$(( $(date +%s) + 300 ))
ready=0
while [[ $(date +%s) -lt $deadline ]]; do
  status_body=$(curl --silent --max-time 5 "${host}/api/system/status" || true)
  if printf '%s' "$status_body" | python3 -c 'import json,sys
try:
    data=json.load(sys.stdin)
except Exception:
    raise SystemExit(1)
raise SystemExit(0 if data.get("status")=="UP" else 1)' >/dev/null 2>&1; then
    ready=1
    break
  fi
  sleep 2
done
if [[ $ready -ne 1 ]]; then
  echo "kontener nie wstał" >&2
  exit 1
fi

write_netrc admin
call_api POST /api/users/change_password \
  --data-urlencode "login=admin" \
  --data-urlencode "previousPassword@${prev}" \
  --data-urlencode "password@${new}"
if [[ $api_code != 200 && $api_code != 204 ]]; then
  message=$(python3 -c 'import json,sys
try:
    data=json.load(open(sys.argv[1], encoding="utf-8"))
except Exception:
    print("")
    raise SystemExit(0)
print(data.get("result") or "")
' "$body")
  echo "zmiana hasła nie powiodła się: HTTP ${api_code} ${message}" >&2
  exit 1
fi

write_netrc "$SONAR_ADMIN_PASSWORD"
call_api GET /api/authentication/validate
if [[ $api_code != 200 ]] || ! python3 -c 'import json,sys; raise SystemExit(0 if json.load(open(sys.argv[1])).get("valid") is True else 1)' "$body"; then
  echo "logowanie nie powiodło się" >&2
  exit 1
fi

call_api POST /api/projects/create \
  --data-urlencode "project=jjdevhub" \
  --data-urlencode "name=jjdevhub"
if [[ $api_code != 200 && $api_code != 204 ]]; then
  echo "utworzenie projektu nie powiodło się" >&2
  exit 1
fi

call_api POST /api/qualitygates/create \
  --data-urlencode "name=jjdevhub"
if [[ $api_code != 200 && $api_code != 204 ]]; then
  echo "utworzenie bramki nie powiodło się" >&2
  exit 1
fi

while IFS=$'\t' read -r metric op error; do
  [[ -z $metric ]] && continue
  call_api POST /api/qualitygates/create_condition \
    --data-urlencode "gateName=jjdevhub" \
    --data-urlencode "metric=${metric}" \
    --data-urlencode "op=${op}" \
    --data-urlencode "error=${error}"
  if [[ $api_code != 200 && $api_code != 204 ]]; then
    echo "warunek bramki nie wszedł: ${metric}" >&2
    exit 1
  fi
done <"$conditions"

call_api POST /api/qualitygates/select \
  --data-urlencode "gateName=jjdevhub" \
  --data-urlencode "projectKey=jjdevhub"
if [[ $api_code != 200 && $api_code != 204 ]]; then
  echo "przypięcie bramki nie powiodło się" >&2
  exit 1
fi

set +x
call_api POST /api/user_tokens/generate \
  --data-urlencode "name=jjdevhub-${GITHUB_RUN_ID:-local}-$$" \
  --data-urlencode "type=PROJECT_ANALYSIS_TOKEN" \
  --data-urlencode "projectKey=jjdevhub"
if [[ $api_code != 200 ]]; then
  echo "brak tokenu skanera" >&2
  exit 1
fi
token_dir=$(dirname -- "$SONAR_TOKEN_FILE")
mkdir -p -- "$token_dir"
umask 077
if ! python3 -c 'import json,sys; data=json.load(open(sys.argv[1])); token=data.get("token") or ""; open(sys.argv[2],"w").write(token); raise SystemExit(0 if token else 1)' "$body" "$SONAR_TOKEN_FILE"; then
  echo "brak tokenu skanera" >&2
  exit 1
fi
chmod 600 "$SONAR_TOKEN_FILE"
