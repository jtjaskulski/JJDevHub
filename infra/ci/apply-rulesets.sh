#!/usr/bin/env bash
set -euo pipefail

repo="${1:?usage: apply-rulesets.sh owner/name}"
root="$(git rev-parse --show-toplevel)"

gh api -X PATCH "repos/$repo" \
  -F allow_squash_merge=true \
  -F allow_merge_commit=false \
  -F allow_rebase_merge=false \
  -f squash_merge_commit_title=PR_TITLE \
  -f squash_merge_commit_message=PR_BODY \
  -F delete_branch_on_merge=true \
  -F allow_auto_merge=true \
  -F allow_update_branch=true >/dev/null

gh api -X PUT "repos/$repo/actions/permissions/workflow" \
  -f default_workflow_permissions=read \
  -F can_approve_pull_request_reviews=true >/dev/null

for file in "$root"/.github/rulesets/*.json; do
  name="$(jq -r .name "$file")"
  id="$(gh api "repos/$repo/rulesets" --paginate --jq ".[] | select(.name == \"$name\") | .id")"
  if [[ -n "$id" ]]; then
    gh api -X PUT "repos/$repo/rulesets/$id" --input "$file" >/dev/null
    echo "updated ruleset $name ($id)"
  else
    gh api -X POST "repos/$repo/rulesets" --input "$file" >/dev/null
    echo "created ruleset $name"
  fi
done

gh api -X PUT "repos/$repo/environments/production" \
  --input - >/dev/null <<'JSON'
{ "deployment_branch_policy": { "protected_branches": false, "custom_branch_policies": true } }
JSON
gh api "repos/$repo/environments/production/deployment-branch-policies" --jq '.branch_policies[].name' \
  | grep -qx main \
  || gh api -X POST "repos/$repo/environments/production/deployment-branch-policies" \
       -f name=main -f type=branch >/dev/null
