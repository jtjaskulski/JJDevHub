#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${JJDEVHUB_ENV_FILE:-/etc/jjdevhub/api.env}"
STATE_FILE="${JJDEVHUB_STATE_FILE:-/var/lib/jjdevhub/last-release-sha}"
PUSH_RELEASE="${JJDEVHUB_PUSH_RELEASE:-0}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "missing env file: $ENV_FILE" >&2
  echo "copy infra/docker/.env.example and fill secrets; on the server use /etc/jjdevhub/api.env (chmod 600)" >&2
  exit 1
fi

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

git fetch origin main
remote_sha="$(git rev-parse origin/main)"

if [[ -f "$STATE_FILE" && "$(cat "$STATE_FILE")" == "$remote_sha" ]]; then
  echo "already deployed $remote_sha"
  exit 0
fi

date_utc="$(date -u +%Y-%m-%d)"
n=1
while git show-ref --verify --quiet "refs/heads/release/${date_utc}.${n}" \
  || git show-ref --verify --quiet "refs/remotes/origin/release/${date_utc}.${n}"; do
  n=$((n + 1))
done

branch="release/${date_utc}.${n}"

git checkout --detach "$remote_sha"
git branch "$branch" "$remote_sha"
git checkout "$branch"

if [[ "$PUSH_RELEASE" == "1" ]]; then
  git push origin "$branch"
fi

docker compose --env-file "$ENV_FILE" -f "$REPO_ROOT/infra/docker/docker-compose.yml" up -d --build

state_dir="$(dirname "$STATE_FILE")"
mkdir -p "$state_dir"
printf '%s\n' "$remote_sha" > "$STATE_FILE"
echo "deployed $remote_sha as $branch"
