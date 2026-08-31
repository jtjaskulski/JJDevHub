# Web

Angular 21 client. Use **pnpm**. Talks to the API through `/api` (dev proxy or nginx in Docker).

## Local (API on :5080)

```bash
pnpm install
pnpm start
```

http://localhost:4200 — `/api`, `/health`, `/openapi` and `/scalar` are proxied to http://localhost:5080.

## Docker

From the repo root:

```bash
cd infra/docker
cp .env.example .env   # once
docker compose up --build
```

Web: http://localhost:4200 (nginx → `api:8080`).
