# Task 1.2: Konfiguracja HashiCorp Vault

| Pole | Wartosc |
|------|---------|
| Sprint | 1 - Identity & Foundation |
| Status | IN PROGRESS |
| Priorytet | High |
| Estymacja | 5 story points |
| Powiazane pliki | `infra/docker/docker-compose.yml`, `infra/docker/bootstrap-vault.sh`, `src/Services/JJDevHub.Content/JJDevHub.Content.Api/Program.cs` |

## Opis

HashiCorp Vault sluzy do bezpiecznego zarzadzania sekretami (connection stringi, klucze API, tokeny). Zamiast trzymac sekrety w `appsettings.json` lub zmiennych srodowiskowych, aplikacja pobiera je dynamicznie z Vault przy starcie. Vault jest juz obecny w docker-compose i ma skrypt bootstrap, ale brakuje integracji po stronie .NET.

### Co juz jest zrobione

- Vault w docker-compose: `hashicorp/vault:latest` na porcie `8201`
- Skrypt `bootstrap-vault.sh` ktory:
  - Wlacza KV v2 engine na sciezce `secret`
  - Zapisuje sekrety PostgreSQL: `secret/database/postgres` (username, password, connectionString)
  - Zapisuje sekrety MongoDB: `secret/database/mongodb` (connectionString)
  - Zapisuje tokeny: `secret/identity/tokens` (recruiter-access-key)
- Dev token: `jjdevhub-root-token`

### Co pozostalo

- Integracja `VaultSharp` w serwisach .NET do dynamicznego pobierania sekretow
- Zamiana hardcoded connection stringow w appsettings na Vault lookups
- Konfiguracja Vault policies i AppRole auth dla produkcji

## Kryteria akceptacji

- [ ] Pakiet `VaultSharp` zainstalowany w Content.Api
- [ ] Connection string PostgreSQL pobierany z Vault zamiast z appsettings
- [ ] Connection string MongoDB pobierany z Vault zamiast z appsettings
- [ ] Aplikacja uruchamia sie poprawnie z sekretami z Vault
- [ ] Fallback na appsettings gdy Vault jest niedostepny (development)
- [ ] Vault health check dodany do `/health` endpoint

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `VaultSharp` | latest | JJDevHub.Content.Api | Klient HashiCorp Vault - pobieranie sekretow z KV v2 engine |
| `VaultSharp.Extensions.Configuration` | latest | JJDevHub.Content.Api | Integracja Vault z `IConfiguration` (.NET Configuration Provider) |

## Kroki implementacji

1. **Zainstaluj pakiety NuGet:**
   ```
   dotnet add src/Services/JJDevHub.Content/JJDevHub.Content.Api package VaultSharp
   dotnet add src/Services/JJDevHub.Content/JJDevHub.Content.Api package VaultSharp.Extensions.Configuration
   ```

2. **Stworz Vault Configuration Provider:**
   - W `Program.cs` dodaj Vault jako configuration source:
     ```csharp
     builder.Configuration.AddVaultConfiguration(
         () => new VaultOptions("http://jjdevhub-vault:8200", "jjdevhub-root-token"),
         "secret/database/postgres",
         "secret/database/mongodb"
     );
     ```

3. **Zaktualizuj DependencyInjection w Persistence:**
   - Zamien hardcoded connection string na `IConfiguration["ConnectionStrings:ContentDb"]`
   - Vault provider automatycznie mapuje `secret/database/postgres/connectionString` na klucz konfiguracji

4. **Zaktualizuj MongoDbSettings w Infrastructure:**
   - Analogicznie dla MongoDB connection string z `secret/database/mongodb`

5. **Dodaj fallback w appsettings.Development.json:**
   ```json
   {
     "Vault": {
       "Enabled": false
     },
     "ConnectionStrings": {
       "ContentDb": "Host=localhost;Port=5433;Database=jjdevhub_content;..."
     },
     "MongoDb": {
       "ConnectionString": "mongodb://localhost:27018"
     }
   }
   ```

6. **Dodaj Vault health check:**
   - `builder.Services.AddHealthChecks().AddVault(...)` lub custom health check

7. **Rozszerz bootstrap-vault.sh o dodatkowe sekrety:**
   - Sekrety Keycloak (client secret)
   - Sekrety Kafka (jesli SASL)

## Zaleznosci

- **Wymaga:** Task 1.4 (infrastruktura bazodanowa)
- **Blokuje:** Wszystkie serwisy korzystajace z sekretow (bezposrednio nie blokuje, bo fallback na appsettings)

## Notatki techniczne

- W dev mode Vault uzywa in-memory storage i traci dane po restarcie. Skrypt `bootstrap-vault.sh` nalezy uruchomic po kazdym `docker-compose up`.
- Na produkcji nalezy uzyc auth method `AppRole` zamiast root tokena. Kazdy serwis dostanie wlasny `role_id` i `secret_id`.
- Vault KV v2 wspiera wersjonowanie sekretow - mozna przywrocic poprzednie wersje connection stringow.
- `VaultSharp.Extensions.Configuration` pozwala na automatyczny reload sekretow bez restartu aplikacji.
- Dokumentacja: https://developer.hashicorp.com/vault/docs, https://github.com/rajanadar/VaultSharp
