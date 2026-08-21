# Task 4.1: Implementacja Content.Api - Warstwa Odczytu (Query Handlers)

| Pole | Wartosc |
|------|---------|
| Sprint | 4 - Public Face & Mobile |
| Status | DONE |
| Priorytet | High |
| Estymacja | 5 story points |
| Powiazane pliki | `src/Services/JJDevHub.Content/JJDevHub.Content.Api/`, `src/Services/JJDevHub.Content/JJDevHub.Content.Application/Queries/` |

## Opis

Content.Api to glowny serwis backendowy obslugujacy zapytania publiczne (blog, portfolio) i operacje zapisu (CV management). Warstwa odczytu czyta bezposrednio z MongoDB (read store) co zapewnia niska latencje. API uzywa ASP.NET Core Minimal APIs z MediatR do dispatching zapytan.

### Co juz jest zrobione

**API Layer:**
- `Program.cs` z pelna konfiguracja serwisow (Application, Persistence, Infrastructure)
- Health checks: PostgreSQL (`ContentDb`) + MongoDB
- OpenTelemetry metrics: AspNetCore, Runtime instrumentation, Prometheus exporter
- `ExceptionHandlingMiddleware`: ValidationException -> 400, DomainException -> 422, KeyNotFoundException -> 404

**Endpoints (wersjonowane pod `/api/v{apiVersion}/content`):**
- `WorkExperienceEndpoints`: `/work-experiences` — CRUD + query `publicOnly` na liscie
- `CurriculumVitaeEndpoints`: `/cv` — GET lista/po id, POST create, PUT personal info, DELETE, POST skills/educations/projects/work-experiences link

**Query Handlers:**
- `GetWorkExperiencesQueryHandler`, `GetWorkExperienceByIdQueryHandler` — MongoDB
- `GetCurriculumVitaesQueryHandler`, `GetCurriculumVitaeByIdQueryHandler` — MongoDB

**Infra API:** `Asp.Versioning.Http`, `OutputCache` (lista work-experiences), `RateLimiter` (global + polityka `writes`), Swagger UI (dev) + `WithDescription` z przykladami JSON na wybranych operacjach

### Co pozostalo

- Endpointy dla Blog Posts / Snippets (Content bounded context)

## Kryteria akceptacji

- [x] Minimal API endpointy dla WorkExperience CRUD
- [x] Query handlers czytajace z MongoDB
- [x] ExceptionHandlingMiddleware z proper HTTP status codes
- [x] Health checks (PostgreSQL + MongoDB)
- [x] OpenTelemetry metrics z Prometheus exporter
- [x] Endpointy dla CurriculumVitae (CRUD + query)
- [x] API versioning (`Asp.Versioning`)
- [x] Response caching dla GET endpoints (publiczne)
- [x] Rate limiting (built-in .NET 10)
- [x] Swagger UI z przykladami request/response

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `Microsoft.AspNetCore.OpenApi` | 10.0.1 | JJDevHub.Content.Api | Juz zainstalowany - OpenAPI/Swagger |
| `Asp.Versioning.Http` | 8.1.1 | JJDevHub.Content.Api | Wersjonowanie API (URL segment) |
| `Swashbuckle.AspNetCore` | 10.x | JJDevHub.Content.Api | Swagger UI (Development) |
| `Microsoft.AspNetCore.RateLimiting` | built-in | JJDevHub.Content.Api | Built-in w .NET 10 - rate limiting per endpoint |

## Architektura endpointow

```
Nginx (:8081)
    ├── /api/v1/content/ → Content.Api (:8080)
    └── /api/content/    → rewrite → /api/v1/content/ (legacy)
Content.Api
            ├── /work-experiences          GET (list, publicOnly), GET {id}, POST, PUT, DELETE
            ├── /cv                        GET /, GET /{id}, POST, PUT /{id}, DELETE /{id}, subpaths skills/educations/projects/work-experiences; POST {id}/pdf, GET pdf-download/{fileId} (Owner)
            ├── /applications              GET /, GET /dashboard, GET /{id}, CRUD + requirements/notes/interview-stages (Owner) — Task 5.2
            ├── /health                    GET (health checks)
            └── /metrics                   GET (Prometheus)
```

## Zaleznosci

- **Wymaga:** Task 2.3 (Command/Query Handlers), Task 3.2 (KafkaEventBus w DI)
- **Blokuje:** Task 4.3 (Angular konsumuje API), Task 4.4 (React Native konsumuje API)

## Notatki techniczne

- Minimal APIs zamiast Controllers - mniej boilerplate, lepsze performance, natywne wsparcie w .NET 10.
- Query handlers czytaja z MongoDB (nie PostgreSQL) - to jest kluczowa roznica CQRS. Write side uzywa EF Core + PostgreSQL, read side uzywa MongoDB driver bezposrednio.
- `ExceptionHandlingMiddleware` mapuje wyjatki na HTTP status codes: `ValidationException` -> 400 z lista bledow, `DomainException` -> 422, `KeyNotFoundException` -> 404.
- OpenTelemetry metrics sa eksportowane na `/metrics` w formacie Prometheus. Grafana dashboard (`content-api.json`) wizualizuje te metryki.
- Na przyszlosc: Response Caching z `OutputCache` (built-in .NET 10) dla publicznych GET endpointow. Invalidacja cache po kazdym zapisie.
