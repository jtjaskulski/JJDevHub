#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT/src/Clients/web"

corepack enable
pnpm install --frozen-lockfile
pnpm audit --audit-level=high
