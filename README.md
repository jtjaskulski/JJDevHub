# JJDevHub

Minimal stack: one .NET 11 (Preview 7) API (Identity + JWT + PostgreSQL), React 19 web client (Vite), React Native scaffold.

Kafka, Keycloak, CQRS, Vault, Jenkins and the old microservices are **not** in this tree.

## Secrets

Nothing sensitive belongs in git. Compose only interpolates variables.

| File | Purpose |
|------|---------|
| [`infra/docker/.env.example`](infra/docker/.env.example) | Placeholders (committed) |
| `infra/docker/.env` | Local laptop copy (gitignored). `cp .env.example .env` then edit |
| `/etc/jjdevhub/api.env` | Server (`root:docker`, mode `640`). `chmod 600` drops the deploy user's read and breaks Compose/cron. Survives `git checkout` of `release/*` |

Generate a JWT key: `openssl rand -base64 48`

## Run (laptop)

```bash
cd infra/docker
cp .env.example .env   # once
docker compose up --build
```

- Web: http://localhost:4200 (nginx proxies `/api`, `/health`, `/openapi`, `/scalar` to the API)
- API: http://localhost:5080
- Health: http://localhost:5080/health
- OpenAPI JSON: http://localhost:5080/openapi/v1.json (`ASPNETCORE_ENVIRONMENT=Development`)
- Scalar UI: http://localhost:5080/scalar
- Postgres: localhost:5433 (user/db from `.env`)

API without the API container (Postgres still in Compose):

```bash
dotnet run --project src/api
```

Register / login:

```bash
curl -s -X POST http://localhost:5080/api/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"email":"owner@test.com","password":"Password1"}'

TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"owner@test.com","password":"Password1"}' | jq -r .token)

curl -s http://localhost:5080/api/auth/me -H "Authorization: Bearer $TOKEN"
```

Web (pnpm, without the web container):

```bash
cd src/Clients/web
pnpm install
pnpm start
```

http://localhost:4200 — `/api`, `/health`, `/openapi` and `/scalar` are proxied to `:5080`. Stop the Compose `web` service first if port 4200 is already taken.

Login and register call `/api/auth` with `fetch`. Content pages do not.

Mobile (pnpm):

```bash
cd src/Clients/mobile/JJDevHubMobile
pnpm install
pnpm start
# other terminal:
pnpm android
```

## CI / CD

**GitHub Actions** on PR and `main`: [`.github/workflows/api.yml`](.github/workflows/api.yml) restores, builds, tests, and `docker build`s the API (Testcontainers starts its own Postgres). [`.github/workflows/web.yml`](.github/workflows/web.yml) runs `pnpm` test/build and builds the React image. No production secrets in the workflows.

Enable **branch protection** on `main` (GitHub → Settings → Branches): require the `api` check before merge.

**Self-hosted release** (hourly cron): if `origin/main` moved, create `release/YYYY-MM-DD.N` and `docker compose up --build` with `--env-file /etc/jjdevhub/api.env`. See [`infra/ci/jjdevhub-release.cron`](infra/ci/jjdevhub-release.cron) and [`infra/ci/release-and-deploy.sh`](infra/ci/release-and-deploy.sh).

A green `api` or `web` run on `main` deploys that commit from [`.github/workflows/deploy.yml`](.github/workflows/deploy.yml) only after every workflow required by the same SHA has succeeded. Path filters still skip the workflow the commit does not touch. The hourly cron stays the fallback when the runner is offline.

Step-by-step (PL): [Proxmox](docs/CICD/01-proxmox.md), [Cloudflare Tunnel](docs/CICD/02-cloudflare-tunnel.md), [GitHub runner](docs/CICD/03-github.md).

Old Jenkinsfile: only on `archive/pre-rewrite`. Next steps for a CV/enterprise path: Vault or OIDC instead of the env file, then Compose → k3s/k8s.

## Layout

```
src/JJDevHub.Api/                 Minimal API (.NET 11 preview), EF Core Identity, JWT
src/JJDevHub.Api/openapi/         OpenAPI document generated at Debug build
src/Clients/web/                  React 19 + Vite (pnpm) + Dockerfile/nginx
src/Clients/shared/theme/         shared color and type tokens
src/Clients/mobile/JJDevHubMobile React Native 0.84
tests/JJDevHub.Api.Tests/         API tests (Testcontainers)
infra/docker/                     Postgres 16 + API + web
infra/ci/                         release cron + deploy script
.github/workflows/api.yml         GitHub Actions (API)
.github/workflows/web.yml         GitHub Actions (React / pnpm)
```

---

# JJDevHub

Minimalny stos: jedno API .NET 11 (Preview 7, Identity + JWT + PostgreSQL), klient webowy React 19 (Vite), szkielet React Native.

Kafka, Keycloak, CQRS, Vault, Jenkins i stare mikroserwisy są poza tym drzewem.

## Przywracanie poprzedniego kodu

Drzewo sprzed przepisania jest na branchu/tagu `archive/pre-rewrite` pod `legacy/`.

```bash
git checkout archive/pre-rewrite
# albo jedna ścieżka:
git checkout archive/pre-rewrite -- legacy/src/Services/JJDevHub.Content
```

## Sekrety

Nic wrażliwego nie należy do gita. Compose tylko podstawia zmienne.

| Plik | Rola |
|------|------|
| [`infra/docker/.env.example`](infra/docker/.env.example) | Placeholdery (w gicie) |
| `infra/docker/.env` | Lokalna kopia na laptopie (gitignored). `cp .env.example .env`, potem edycja |
| `/etc/jjdevhub/api.env` | Serwer (`root:docker`, tryb `640`). `chmod 600` odbiera odczyt kontu deploy i psuje Compose oraz cron. Przeżywa `git checkout` gałęzi `release/*` |

Klucz JWT: `openssl rand -base64 48`

## Uruchomienie (laptop)

```bash
cd infra/docker
cp .env.example .env   # raz
docker compose up --build
```

- Web: http://localhost:4200 (nginx proxuje `/api`, `/health`, `/openapi`, `/scalar` do API)
- API: http://localhost:5080
- Health: http://localhost:5080/health
- OpenAPI JSON: http://localhost:5080/openapi/v1.json (`ASPNETCORE_ENVIRONMENT=Development`)
- Scalar UI: http://localhost:5080/scalar
- Postgres: localhost:5433 (użytkownik i baza z `.env`)

API bez kontenera API (Postgres zostaje w Compose):

```bash
dotnet run --project src/JJDevHub.Api
```

Rejestracja / logowanie:

```bash
curl -s -X POST http://localhost:5080/api/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"email":"owner@test.com","password":"Password1"}'

TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"owner@test.com","password":"Password1"}' | jq -r .token)

curl -s http://localhost:5080/api/auth/me -H "Authorization: Bearer $TOKEN"
```

Web (pnpm, bez kontenera web):

```bash
cd src/Clients/web
pnpm install
pnpm start
```

http://localhost:4200 — `/api`, `/health`, `/openapi` i `/scalar` są proxowane na `:5080`. Jeśli port 4200 jest zajęty, zatrzymaj najpierw usługę `web` z Compose.

Login i register wołają `/api/auth` przez `fetch`. Strony treści nie.

Mobilka (pnpm):

```bash
cd src/Clients/mobile/JJDevHubMobile
pnpm install
pnpm start
# drugi terminal:
pnpm android
```

## CI / CD

**GitHub Actions** na PR i na `main`: [`.github/workflows/api.yml`](.github/workflows/api.yml) robi restore, build, testy i `docker build` API (Testcontainers stawia własnego Postgresa). [`.github/workflows/web.yml`](.github/workflows/web.yml) odpala `pnpm` test/build i buduje obraz Reacta. W workflow nie ma sekretów produkcyjnych.

Włącz **branch protection** na `main` (GitHub → Settings → Branches): przed merge wymagaj checka `api`.

**Wydanie self-hosted** (cron co godzinę): jeśli ruszył się `origin/main`, utwórz `release/YYYY-MM-DD.N` i `docker compose up --build` z `--env-file /etc/jjdevhub/api.env`. Patrz [`infra/ci/jjdevhub-release.cron`](infra/ci/jjdevhub-release.cron) i [`infra/ci/release-and-deploy.sh`](infra/ci/release-and-deploy.sh).

Zielony run `api` albo `web` na `main` wdraża ten commit z [`.github/workflows/deploy.yml`](.github/workflows/deploy.yml) dopiero, gdy każdy workflow wymagany przez ten sam SHA skończy się sukcesem. Filtr ścieżek nadal pomija workflow, którego commit nie dotyka. Godzinny cron zostaje zapasem, gdy runner jest offline.

Krok po kroku: [Proxmox](docs/CICD/01-proxmox.md), [Cloudflare Tunnel](docs/CICD/02-cloudflare-tunnel.md), [runner GitHub](docs/CICD/03-github.md).

Stary Jenkinsfile jest tylko na `archive/pre-rewrite`. Dalsza ścieżka CV/enterprise: Vault albo OIDC zamiast pliku env, potem Compose → k3s/k8s.

## Układ

```
src/JJDevHub.Api/                 Minimal API (.NET 11 preview), EF Core Identity, JWT
src/JJDevHub.Api/openapi/         dokument OpenAPI generowany przy buildzie Debug
src/Clients/web/                  React 19 + Vite (pnpm) + Dockerfile/nginx
src/Clients/shared/theme/         wspólne tokeny koloru i typu
src/Clients/mobile/JJDevHubMobile React Native 0.84
tests/JJDevHub.Api.Tests/         testy API (Testcontainers)
infra/docker/                     Postgres 16 + API + web
infra/ci/                         cron wydania + skrypt deployu
.github/workflows/api.yml         GitHub Actions (API)
.github/workflows/web.yml         GitHub Actions (React / pnpm)
```
