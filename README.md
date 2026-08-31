# JJDevHub

Minimal stack: one .NET 11 (Preview 7) API (Identity + JWT + PostgreSQL), Angular 21 web client, React Native scaffold.

Kafka, Keycloak, CQRS, Vault, Jenkins and the old microservices are **not** in this tree.

## Restore the previous codebase

The pre-rewrite tree lives on branch/tag `archive/pre-rewrite` under `legacy/`.

```bash
git checkout archive/pre-rewrite
# or a single path:
git checkout archive/pre-rewrite -- legacy/src/Services/JJDevHub.Content
```

## Secrets

Nothing sensitive belongs in git. Compose only interpolates variables.

| File | Purpose |
|------|---------|
| [`infra/docker/.env.example`](infra/docker/.env.example) | Placeholders (committed) |
| `infra/docker/.env` | Local laptop copy (gitignored). `cp .env.example .env` then edit |
| `/etc/jjdevhub/api.env` | Server (`chmod 600`). Survives `git checkout` of `release/*` |

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
dotnet run --project src/JJDevHub.Api
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

Regenerate the Angular API client after endpoint changes:

```bash
dotnet tool restore
dotnet build src/JJDevHub.Api          # writes src/JJDevHub.Api/openapi/JJDevHub.Api.json
DOTNET_ROLL_FORWARD=Major dotnet nswag run src/JJDevHub.Api/nswag.json
```

(`DOTNET_ROLL_FORWARD=Major` is needed when the SDK on PATH is 11 and NSwag still targets .NET 10.)

The client is `src/Clients/web/src/app/api/jjdevhub-api.client.ts` (`JjdevhubApiClient`).

Mobile (pnpm):

```bash
cd src/Clients/mobile/JJDevHubMobile
pnpm install
pnpm start
# other terminal:
pnpm android
```

## CI / CD

**GitHub Actions** on PR and `main`: [`.github/workflows/api.yml`](.github/workflows/api.yml) restores, builds, tests, and `docker build`s the API (Testcontainers starts its own Postgres). [`.github/workflows/web.yml`](.github/workflows/web.yml) runs `pnpm` test/build and builds the Angular image. No production secrets in the workflows.

Enable **branch protection** on `main` (GitHub → Settings → Branches): require the `api` check before merge.

**Self-hosted release** (hourly cron): if `origin/main` moved, create `release/YYYY-MM-DD.N` and `docker compose up --build` with `--env-file /etc/jjdevhub/api.env`. See [`infra/ci/jjdevhub-release.cron`](infra/ci/jjdevhub-release.cron) and [`infra/ci/release-and-deploy.sh`](infra/ci/release-and-deploy.sh).

Old Jenkinsfile: only on `archive/pre-rewrite`. Next steps for a CV/enterprise path: Vault or OIDC instead of the env file, then Compose → k3s/k8s.

## Layout

```
src/JJDevHub.Api/                 Minimal API (.NET 11 preview), EF Core Identity, JWT
src/JJDevHub.Api/openapi/         OpenAPI document generated at Debug build
src/Clients/web/                  Angular 21 (pnpm) + Dockerfile/nginx
src/Clients/web/src/app/api/      NSwag TypeScript client
src/Clients/mobile/JJDevHubMobile React Native 0.84
tests/JJDevHub.Api.Tests/         API tests (Testcontainers)
infra/docker/                     Postgres 16 + API + web
infra/ci/                         release cron + deploy script
.github/workflows/api.yml         GitHub Actions (API)
.github/workflows/web.yml         GitHub Actions (Angular / pnpm)
```
