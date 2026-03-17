#!/bin/bash
# Uruchamiaj z hosta (Git Bash/WSL), gdy kontener jjdevhub-vault jest włączony.
# Token musi być przekazany do docker exec — export na hoście NIE trafia do kontenera.

set -euo pipefail

TOKEN="${VAULT_TOKEN:-jjdevhub-root-token}"
CONTAINER="${VAULT_CONTAINER:-jjdevhub-vault}"

# Wewnątrz kontenera Vault nasłuchuje na 8200 (nie używaj localhost:8201 — to port mapowany na hoście).
vault_in_container() {
    docker exec \
        -e "VAULT_TOKEN=${TOKEN}" \
        -e "VAULT_ADDR=http://127.0.0.1:8200" \
        "$CONTAINER" \
        "$@"
}

echo "--- Initializing JJDevHub Vault Secrets ---"

# 1. KV v2 (ignoruj błąd jeśli mount już istnieje)
vault_in_container vault secrets enable -path=secret kv-v2 2>/dev/null || \
    echo "(Mount 'secret' już istnieje — OK)"

# 2. PostgreSQL (klucz ContentDb pod integrację .NET ConnectionStrings:ContentDb)
vault_in_container vault kv put secret/database/postgres \
    ContentDb="Host=jjdevhub-db;Port=5432;Database=jjdevhub_content;Username=postgres;Password=password"

# 3. MongoDB
vault_in_container vault kv put secret/database/mongodb \
    ConnectionString="mongodb://jjdevhub-mongo:27017" \
    DatabaseName="jjdevhub_content_read"

# 4. Identity tokens
vault_in_container vault kv put secret/identity/tokens \
    recruiter-access-key="antigravity-secret-2026"

echo "--- Vault Configuration Complete ---"
