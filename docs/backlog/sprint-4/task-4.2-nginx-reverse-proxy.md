# Task 4.2: Setup Nginx jako Reverse Proxy

| Pole | Wartosc |
|------|---------|
| Sprint | 4 - Public Face & Mobile |
| Status | DONE |
| Priorytet | High |
| Estymacja | 3 story points |
| Powiazane pliki | `infra/docker/nginx/nginx.conf`, `infra/docker/docker-compose.yml` |

## Opis

Nginx sluzy jako centralny punkt wejscia (reverse proxy) dla calego systemu. Kieruje ruch HTTP na odpowiednie mikroserwisy na podstawie sciezki URL. W produkcji bedzie terminowal SSL (z certyfikatem Cloudflare Origin CA) i stosowal dodatkowe naglowki bezpieczenstwa.

### Co jest zaimplementowane

**Nginx w docker-compose:**
- Obraz: `nginx:alpine`
- Porty: `8081:80` (HTTP), `444:443` (HTTPS)
- Wolumen: `./nginx/nginx.conf:/etc/nginx/nginx.conf:ro`
- Container: `jjdevhub-nginx`

**Routing w nginx.conf:**

| Location | Upstream | Serwis |
|----------|----------|--------|
| `/api/v1/content/` | `jjdevhub-content-api:8080` | Content API (kanoniczna sciezka, zgodna z wersjonowaniem API) |
| `/api/content/` | `jjdevhub-content-api:8080` | Content API (legacy; `rewrite` do `/api/v1/content/...` przed proxy) |
| `/api/analytics/` | `jjdevhub-analytics-api:8080` | Analytics API |
| `/api/identity/` | `jjdevhub-identity-api:8080` | Identity API |
| `/api/ai/` | `jjdevhub-ai-gateway:8080` | AI Gateway |
| `/api/notification/` | `jjdevhub-notification-api:8080` | Notification API |
| `/api/education/` | `jjdevhub-education-api:8080` | Education API |
| `/jenkins/` | `jjdevhub-jenkins:8080` | Jenkins CI |
| `/` | `jjdevhub-angular:80` | Angular SPA (default) |

**Proxy headers:**
- `Host`, `X-Real-IP`, `X-Forwarded-For`, `X-Forwarded-Proto`

**Worker config:**
- `worker_connections 1024`

## Kryteria akceptacji

- [x] Nginx uruchomiony w Docker z custom nginx.conf
- [x] Routing do wszystkich mikroserwisow przez sciezki URL
- [x] Proxy headers przekazywane do upstreamow
- [x] Angular SPA jako domyslna lokalizacja (`/`)
- [x] Porty HTTP (8081) i HTTPS (444) dostepne

## Wymagane pakiety NuGet

Brak - to jest zadanie infrastrukturalne (Nginx + Docker).

## Architektura sieci

```
[Klient / Przegladarka]
        |
        v
[Nginx :8081/:443]
    ├── /                         → Angular SPA (:80)
    ├── /api/v1/content/          → Content API (:8080)
    ├── /api/content/             → Content API (:8080), rewrite → /api/v1/content/
    ├── /api/analytics/           → Analytics API (:8080)
    ├── /api/identity/            → Identity API (:8080)
    ├── /api/ai/                  → AI Gateway (:8080)
    ├── /api/notification/        → Notification API (:8080)
    ├── /api/education/           → Education API (:8080)
    └── /jenkins/                 → Jenkins (:8080)
```

## Zaleznosci

- **Wymaga:** Wszystkie mikroserwisy uruchomione w Docker (chociaz Nginx startuje niezaleznie)
- **Powiazane:** Task 4.1 ([task-4.1-content-api-queries.md](task-4.1-content-api-queries.md)) — wersjonowanie Content API (`/api/v{version}/content/`) i opis endpointow
- **Blokuje:** Task 4.3 (Angular laczy sie przez Nginx), hosting-cloudflare (Nginx za Cloudflare)

## Notatki techniczne

- Nginx uzywa wewnetrznych nazw kontenerow Docker jako upstreamy (np. `jjdevhub-content-api`). Dziala to dzieki wspolnej sieci `jjdevhub-net`.
- W produkcji nalezy dodac:
  - SSL termination z Cloudflare Origin CA (`ssl_certificate`, `ssl_certificate_key`)
  - Security headers: `X-Content-Type-Options`, `X-Frame-Options`, `Strict-Transport-Security`
  - Gzip compression
  - Rate limiting per location (`limit_req_zone`)
  - Access logging w formacie JSON (dla analizy w Grafana)
- Port 8081 zamiast 80 i 444 zamiast 443 zapobiegaja konfliktom z lokalnymi serwisami.
- Dokumentacja: https://nginx.org/en/docs/http/ngx_http_proxy_module.html
