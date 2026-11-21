#!/usr/bin/env bash
set -euo pipefail

root=$(cd "$(dirname "$0")/../.." && pwd)
branch_sh=$root/.github/scripts/branch-name.sh
title_sh=$root/.github/scripts/pr-title.sh
size_sh=$root/.github/scripts/pr-size.sh

expect_code() {
  local expected=$1
  shift
  set +e
  "$@"
  local code=$?
  set -e
  if [[ $code -ne $expected ]]; then
    echo "oczekiwano ${expected}, jest ${code}: $*" >&2
    exit 1
  fi
}

write_lines() {
  local path=$1
  local lines=$2
  local text=$3
  mkdir -p "$(dirname "$path")"
  python3 -c 'import pathlib,sys; pathlib.Path(sys.argv[1]).write_text((sys.argv[3] + "\n") * int(sys.argv[2]))' "$path" "$lines" "$text"
}

expect_size() {
  local expected=$1
  shift
  local code
  set +e
  (
    set -euo pipefail
    dir=$(mktemp -d)
    trap 'rm -rf "$dir"' EXIT
    git -C "$dir" init -q -b main
    git -C "$dir" config user.email "ci@example.com"
    git -C "$dir" config user.name "ci"
    git -C "$dir" config commit.gpgsign false
    git -C "$dir" commit -q --allow-empty -m base
    base=$(git -C "$dir" rev-parse HEAD)
    local spec path lines
    for spec in "$@"; do
      path=${spec%%:*}
      lines=${spec##*:}
      write_lines "$dir/$path" "$lines" x
      git -C "$dir" add -- "$path"
    done
    git -C "$dir" commit -q -m head
    head=$(git -C "$dir" rev-parse HEAD)
    cd "$dir"
    unset GIT_DIR GIT_WORK_TREE
    bash "$size_sh" "$base" "$head"
  )
  code=$?
  set -e
  if [[ $code -ne $expected ]]; then
    echo "pr-size $* : oczekiwano ${expected}, jest ${code}" >&2
    exit 1
  fi
}

message=$(bash "$branch_sh" feature/lesson_markdown develop 2>&1 >/dev/null || true)
[[ $message == *"nazwa głowy nie pasuje do wzorca: feature/lesson_markdown"* ]]
expect_code 1 bash "$branch_sh" feature/lesson_markdown develop
expect_code 0 bash "$branch_sh" feature/lesson-markdown develop
expect_code 0 bash "$branch_sh" develop release
expect_code 0 bash "$branch_sh" release main
expect_code 1 bash "$branch_sh" feature/lesson-markdown release
expect_code 1 bash "$branch_sh" release develop
expect_code 0 bash "$branch_sh" release-please--branches--main--components--news main
expect_code 1 bash "$branch_sh" release-please--branches--main--components--news develop
expect_code 1 bash "$branch_sh" release-please--branches--develop--components--news develop

expect_code 1 bash "$title_sh" "feat: add lesson"
expect_code 0 bash "$title_sh" "feat(learning): add lesson markdown"
expect_code 0 bash "$title_sh" "feat(news): publish article revision"
expect_code 0 bash "$title_sh" "feat(learning)!: add lesson markdown"
expect_code 0 bash "$title_sh" "docs: abc"
expect_code 1 bash "$title_sh" "docs(cicd): update the pipeline"
expect_code 1 bash "$title_sh" "docs: ab"

expect_size 1 "src/file.cs:801"
expect_size 1 "src/services/Learning/File.cs:801"
expect_size 0 "src/file.cs:800"
expect_size 0 "src/services/Learning/Infrastructure/Persistence/Migrations/001_Init.cs:801"
expect_size 0 "pnpm-lock.yaml:801"
expect_size 0 "src/App.Designer.cs:801"
expect_size 0 "src/clients/shared/api-client/generated/client.ts:801"
expect_size 0 "aspire-output/run.log:801"

set +e
(
  set -euo pipefail
  dir=$(mktemp -d)
  trap 'rm -rf "$dir"' EXIT
  git -C "$dir" init -q -b main
  git -C "$dir" config user.email "ci@example.com"
  git -C "$dir" config user.name "ci"
  git -C "$dir" config commit.gpgsign false
  write_lines "$dir/src/file.cs" 400 a
  git -C "$dir" add -- src/file.cs
  git -C "$dir" commit -q -m base
  base=$(git -C "$dir" rev-parse HEAD)
  write_lines "$dir/src/file.cs" 401 b
  git -C "$dir" add -- src/file.cs
  git -C "$dir" commit -q -m head
  head=$(git -C "$dir" rev-parse HEAD)
  cd "$dir"
  unset GIT_DIR GIT_WORK_TREE
  bash "$size_sh" "$base" "$head"
)
code=$?
set -e
if [[ $code -ne 1 ]]; then
  echo "suma dodanych i usuniętych: oczekiwano 1, jest ${code}" >&2
  exit 1
fi

echo "branch-rules: ok"
