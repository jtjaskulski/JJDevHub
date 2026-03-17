# JJDevHub — Docker (lokalny stack)

## Uruchomienie

```bash
cd infra/docker
docker compose up -d
```

Frontend + API przez Nginx: **http://localhost:8081**

## Checklist „działa u mnie”

1. `docker compose up -d` — wszystkie kontenery `Running`.
2. **Vault** — po każdym restarcie kontenera (tryb dev) uruchom bootstrap sekretów:
   - Linux/macOS/Git Bash: `./bootstrap-vault.sh`
   - Windows (PowerShell): `.\bootstrap-vault.ps1`
3. **Kafka** — obrazy `confluentinc/cp-kafka:7.6.1` i `cp-zookeeper:7.6.1` (nie `latest`; `latest` wymaga KRaft).
4. **Keycloak** — realm z importu (jeśli używasz JWT).
5. Dostęp: Vault `8201`, Keycloak `8083`, Jenkins w głównym compose `8082`.

## Jenkins: który plik?

| Plik | Użycie |
|------|--------|
| `docker-compose.yml` | Jenkins **:8082**, jedna sieć z aplikacją; bez `docker.sock`. |
| `jenkins-compose.yml` | Sam Jenkins **:8080** + `docker.sock` — `docker build` w pipeline z hosta. |

Nie uruchamiaj dwóch Jenkinsów na tym samym wolumenie `jenkins_home` bez zmiany nazwy wolumenu.

## Produkcja (VPS + Cloudflare)

Pełna ścieżka wdrożenia: **[docs/backlog/hosting-cloudflare.md](../../docs/backlog/hosting-cloudflare.md)**  
(VPS, Nginx, SSL, Cloudflare DNS/WAF, zasoby, hardening.)

## Pliki

- `docker-compose.yml` — główny stack.
- `bootstrap-vault.sh` / `bootstrap-vault.ps1` — sekrety KV (token przekazywany w `docker exec`).
- `jenkins-compose.yml` — izolowany Jenkins pod CI z Dockerem.
- `keycloak/realm-export.json` — import realmu (jeśli skonfigurowany w compose).
