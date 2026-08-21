# Vault bootstrap (Windows PowerShell). Wymaga uruchomionego kontenera jjdevhub-vault.
$token = if ($env:VAULT_TOKEN) { $env:VAULT_TOKEN } else { "jjdevhub-root-token" }
$container = if ($env:VAULT_CONTAINER) { $env:VAULT_CONTAINER } else { "jjdevhub-vault" }
$vaultEnv = @("-e", "VAULT_TOKEN=$token", "-e", "VAULT_ADDR=http://127.0.0.1:8200")

Write-Host "--- Initializing JJDevHub Vault Secrets ---"

$null = & docker exec @vaultEnv $container vault secrets enable -path=secret kv-v2 2>&1
if ($LASTEXITCODE -ne 0) { Write-Host "(Mount 'secret' juz istnieje OK)" }

& docker exec @vaultEnv $container vault kv put secret/database/postgres `
    ConnectionStrings__ContentDb="Host=jjdevhub-db;Port=5432;Database=jjdevhub_content;Username=postgres;Password=password"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& docker exec @vaultEnv $container vault kv put secret/database/mongodb `
    MongoDb__ConnectionString="mongodb://jjdevhub-mongo:27017" `
    MongoDb__DatabaseName="jjdevhub_content_read"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$recruiterKey = if ($env:RECRUITER_ACCESS_KEY) { $env:RECRUITER_ACCESS_KEY } else { "CHANGE_ME_BEFORE_USE" }
& docker exec @vaultEnv $container vault kv put secret/identity/tokens `
    recruiter-access-key="$recruiterKey"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "--- Vault Configuration Complete ---"
