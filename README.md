# JJDevHub

Minimal stack: one .NET API (Identity + JWT + PostgreSQL), empty Angular client, React Native scaffold.

Kafka, Keycloak, CQRS, Vault, Jenkins and the old microservices are **not** in this tree.

## Restore the previous codebase

The pre-rewrite tree lives on branch/tag `archive/pre-rewrite` under `legacy/`.

```bash
git checkout archive/pre-rewrite
# or a single path:
git checkout archive/pre-rewrite -- legacy/src/Services/JJDevHub.Content
```

## Run

Postgres + API:

```bash
cd infra/docker
docker compose up --build
```

- API: http://localhost:5080
- Health: http://localhost:5080/health
- OpenAPI: http://localhost:5080/openapi/v1.json
- Postgres: localhost:5433 (`jjdevhub` / `postgres` / `password`)

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

Web (pnpm):

```bash
cd src/Clients/web
pnpm install
pnpm start
```

http://localhost:4200 — `/api` is proxied to `:5080`.

Mobile (pnpm):

```bash
cd src/Clients/mobile/JJDevHubMobile
pnpm install
pnpm start
# other terminal:
pnpm android
```

## Layout

```
src/JJDevHub.Api/                 Minimal API, EF Core Identity, JWT
src/Clients/web/                  Angular 21
src/Clients/mobile/JJDevHubMobile React Native 0.84
infra/docker/                     Postgres 16 + API
```
