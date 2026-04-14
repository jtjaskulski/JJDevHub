# JJDevHub — Docker (lokalny stack)

## Uruchomienie

```bash
cd infra/docker
docker compose up -d
```

Frontend + API przez Nginx: **http://localhost:8081**

## Środowisko ASP.NET w compose

Wszystkie usługi API oraz **sync-worker** (to nadal `WebApplication` z Kestrel — health na `/health`, stąd `ASPNETCORE_ENVIRONMENT`, nie osobny worker host) mają w `docker-compose.yml` ustawione **`Development`**, żeby lokalny stack zachowywał się jak `dotnet run` (m.in. OpenAPI tam, gdzie jest za `IsDevelopment()`, bogatsze logowanie). Na VPS / CI możesz nadpisać na `Production` przez osobny plik compose lub zmienne środowiskowe.

## MongoDB i Kafka: host (`dotnet run`) vs kontener

| Gdzie działa aplikacja | MongoDB | Kafka (bootstrap) | Uwagi |
|------------------------|---------|-------------------|--------|
| Na hoście, bazy w Dockerze (porty zmapowane) | `mongodb://localhost:27018` | `localhost:29092` | `27018` i `29092` to porty **hosta** (mapowanie z `27017` / listener `PLAINTEXT_HOST`). |
| W tej samej sieci co `docker-compose` | `mongodb://mongodb:27017` | `kafka:9092` | Nazwy serwisów z compose; port **wewnętrzny** Mongo to zawsze `27017`. |

W `docker-compose.yml` ustawiane są zmienne `MongoDb__*` i `Kafka__BootstrapServers` dla **content-api** i **sync-worker**, żeby nadpisać domyślne `appsettings.json` (przystosowane do hosta). Lokalne `dotnet run` bez tych zmiennych = wiersz „host”.

## Checklist „działa u mnie”

1. `docker compose up -d` — wszystkie kontenery `Running`.
2. **Vault** — po każdym restarcie kontenera (tryb dev) uruchom bootstrap sekretów:
   - Linux/macOS/Git Bash: `./bootstrap-vault.sh`
   - Windows (PowerShell): `.\bootstrap-vault.ps1`
3. **Kafka** — obrazy `confluentinc/cp-kafka:7.6.1` i `cp-zookeeper:7.6.1` (nie `latest`; `latest` wymaga KRaft). Z hosta (`dotnet run`): `Kafka:BootstrapServers` = `localhost:29092`. Z kontenera w sieci compose: `kafka:9092`.
4. **Keycloak** — realm z importu (jeśli używasz JWT).
5. **Jaeger** — `http://localhost:16686` — UI do distributed tracing (OTLP gRPC na 4317).
6. Dostęp: Vault `8201`, Keycloak admin `8083`, Jenkins w głównym compose `8082`, Jaeger `16686`.

## Jenkins: który plik?

| Plik | Użycie |
|------|--------|
| `docker-compose.yml` | Jenkins **:8082**, jedna sieć z aplikacją; bez `docker.sock`. |
| `jenkins-compose.yml` | Sam Jenkins **:8080** + `docker.sock` — `docker build` w pipeline z hosta. |

Nie uruchamiaj dwóch Jenkinsów na tym samym wolumenie `jenkins_home` bez zmiany nazwy wolumenu.

## Produkcja (VPS + Cloudflare)

Pełna ścieżka wdrożenia: **[docs/backlog/hosting-cloudflare.md](../../docs/backlog/hosting-cloudflare.md)**  
(VPS, Nginx, SSL, Cloudflare DNS/WAF, zasoby, hardening.)

## Auto-migracje EF Core (Development)

Gdy `ASPNETCORE_ENVIRONMENT=Development` (domyślnie w compose), Content API wywołuje `db.Database.Migrate()` przy starcie — tabele PG tworzą się automatycznie bez ręcznego `dotnet ef database update`.

## Pliki

- `docker-compose.yml` — główny stack (20 serwisów).
- `bootstrap-vault.sh` / `bootstrap-vault.ps1` — sekrety KV (token przekazywany w `docker exec`).
- `jenkins-compose.yml` — izolowany Jenkins pod CI z Dockerem.
- `keycloak/jjdevhub-realm.json` — import realmu (mount w compose, `--import-realm`).
- `nginx/nginx.conf` — routing reverse proxy (API v1, rewrite legacy, Keycloak, Jenkins).
- `monitoring/prometheus/prometheus.yml` — scrape targets.
- `monitoring/grafana/provisioning/` — datasources + dashboards auto-provisioning.
