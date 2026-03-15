# Task 1.4: Uruchomienie Infrastruktury Bazodanowej

| Pole | Wartosc |
|------|---------|
| Sprint | 1 - Identity & Foundation |
| Status | DONE |
| Priorytet | High |
| Estymacja | 3 story points |
| Powiazane pliki | `infra/docker/docker-compose.yml` |

## Opis

Uruchomienie PostgreSQL (write store, CQRS command side) i MongoDB (read store, CQRS query side) przez Docker Compose. Obie bazy stanowia fundament architektury CQRS - PostgreSQL przechowuje znormalizowane dane domenowe, a MongoDB sluzy jako zdenormalizowany read store zoptymalizowany pod zapytania frontendowe.

### Co jest zaimplementowane

Oba kontenery sa w pelni skonfigurowane w `docker-compose.yml`:

**PostgreSQL 16:**
- Obraz: `postgres:16-alpine`
- Port: `5433:5432` (niestandardowy port zewnetrzny aby uniknac konfliktu z lokalna instancja)
- Baza: `jjdevhub_content`
- Uzytkownik/haslo: `jjdevhub` / `jjdevhub_secret`
- Wolumen: `postgres_data` (persystencja danych)
- Health check: `pg_isready`

**MongoDB:**
- Obraz: `mongo:latest`
- Port: `27018:27017` (niestandardowy port zewnetrzny)
- Baza: `jjdevhub_content_read`
- Wolumen: `mongo_data` (persystencja danych)

## Kryteria akceptacji

- [x] PostgreSQL 16 uruchomiony w Docker z persystentnym wolumenem
- [x] MongoDB uruchomiona w Docker z persystentnym wolumenem
- [x] Porty dostepne z hosta (5433, 27018) dla rozwoju lokalnego
- [x] Health check PostgreSQL skonfigurowany
- [x] Obie bazy w sieci `jjdevhub-net`
- [x] Content.Api ma health checks do obu baz (`/health` endpoint)

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.0 | JJDevHub.Content.Persistence | EF Core provider dla PostgreSQL |
| `MongoDB.Driver` | 3.6.0 | JJDevHub.Content.Infrastructure | Oficjalny driver MongoDB dla .NET |
| `AspNetCore.HealthChecks.NpgSql` | 9.0.0 | JJDevHub.Content.Api | Health check PostgreSQL |
| `AspNetCore.HealthChecks.MongoDb` | 9.0.0 | JJDevHub.Content.Api | Health check MongoDB |

## Konfiguracja docker-compose

```yaml
db:
  image: postgres:16-alpine
  container_name: jjdevhub-db
  ports:
    - "5433:5432"
  environment:
    POSTGRES_DB: jjdevhub_content
    POSTGRES_USER: jjdevhub
    POSTGRES_PASSWORD: jjdevhub_secret
  volumes:
    - postgres_data:/var/lib/postgresql/data
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U jjdevhub"]
  networks:
    - jjdevhub-net

mongodb:
  image: mongo:latest
  container_name: jjdevhub-mongo
  ports:
    - "27018:27017"
  volumes:
    - mongo_data:/data/db
  networks:
    - jjdevhub-net
```

## Zaleznosci

- **Wymaga:** Docker i Docker Compose zainstalowane
- **Blokuje:** Task 1.2 (Vault potrzebuje connection stringow), Task 2.2 (EF Core), Task 3.3 (MongoDB read store)

## Notatki techniczne

- Porty 5433 i 27018 zamiast domyslnych 5432/27017 zapobiegaja konfliktom z lokalnymi instancjami baz.
- Na produkcji nalezy dodac autentykacje MongoDB (obecnie brak MONGO_INITDB_ROOT_USERNAME/PASSWORD).
- Wolumeny `postgres_data` i `mongo_data` zachowuja dane miedzy restartami kontenerow.
- PostgreSQL health check (`pg_isready`) jest uzywany przez Docker do okreslenia gotowosci kontenera - inne serwisy z `depends_on: condition: service_healthy` czekaja na gotowa baze.
