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

# 2. PostgreSQL — flat keys dla IConfiguration (ConnectionStrings__ContentDb)
vault_in_container vault kv put secret/database/postgres \
    ConnectionStrings__ContentDb="Host=jjdevhub-db;Port=5432;Database=jjdevhub_content;Username=postgres;Password=password"

# 3. MongoDB
vault_in_container vault kv put secret/database/mongodb \
    MongoDb__ConnectionString="mongodb://jjdevhub-mongo:27017" \
    MongoDb__DatabaseName="jjdevhub_content_read"

# 4. Keycloak (client secret dla jjdevhub-api — opcjonalnie)
vault_in_container vault kv put secret/keycloak/clients \
    jjdevhub-api-secret="jjdevhub-api-dev-secret" || true

# 5. Identity tokens
RECRUITER_KEY="${RECRUITER_ACCESS_KEY:-CHANGE_ME_BEFORE_USE}"
vault_in_container vault kv put secret/identity/tokens \
    recruiter-access-key="$RECRUITER_KEY"

echo "--- Vault Configuration Complete ---"
