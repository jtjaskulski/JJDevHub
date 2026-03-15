#!/bin/bash

export VAULT_ADDR='http://localhost:8201'
export VAULT_TOKEN='jjdevhub-root-token'

echo "--- Initializing JJDevHub Vault Secrets ---"

# 1. Włączenie silnika KV (Key-Value)
docker exec jjdevhub-vault vault secrets enable -path=secret kv-v2

# 2. Dodanie sekretów dla Bazy Danych (Postgres)
docker exec jjdevhub-vault vault kv put secret/database/postgres \
    username="postgres" \
    password="password" \
    connectionString="Host=jjdevhub-db;Port=5432;Database=jjdevhub_content;Username=postgres;Password=password"

# 3. Dodanie sekretów dla MongoDB
docker exec jjdevhub-vault vault kv put secret/database/mongodb \
    connectionString="mongodb://jjdevhub-mongo:27017"

# 4. Dodanie sekretów dla profilu profesjonalnego (Twoje ukryte CV)
docker exec jjdevhub-vault vault kv put secret/identity/tokens \
    recruiter-access-key="antigravity-secret-2026"

echo "--- Vault Configuration Complete ---"
