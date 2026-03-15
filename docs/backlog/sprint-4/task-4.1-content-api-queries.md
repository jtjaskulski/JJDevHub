# Task 4.1: Implementacja Content.Api - Warstwa Odczytu (Query Handlers)

| Pole | Wartosc |
|------|---------|
| Sprint | 4 - Public Face & Mobile |
| Status | IN PROGRESS |
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

**Endpoints (WorkExperienceEndpoints.cs):**
- Grupa: `/api/content/work-experiences`, tag `WorkExperiences`
- `GET /` - lista wszystkich (query param `publicOnly`)
- `GET /{id}` - po Id
- `POST /` - tworzenie (body: AddWorkExperienceCommand)
- `PUT /{id}` - aktualizacja (body: UpdateWorkExperienceRequest)
- `DELETE /{id}` - usuniecie

**Query Handlers:**
- `GetWorkExperiencesQueryHandler` - czyta z `IWorkExperienceReadStore` (MongoDB)
- `GetWorkExperienceByIdQueryHandler` - czyta z `IWorkExperienceReadStore` po Id

### Co pozostalo

- Rozszerzenie API o endpointy dla CurriculumVitae (po Task 2.1)
- Endpointy dla Blog Posts / Snippets (Content bounded context)
- API versioning (`/api/v1/content/...`)
- Response caching dla publicznych endpointow
- Rate limiting
- Swagger/OpenAPI documentation z przykladami

## Kryteria akceptacji

- [x] Minimal API endpointy dla WorkExperience CRUD
- [x] Query handlers czytajace z MongoDB
- [x] ExceptionHandlingMiddleware z proper HTTP status codes
- [x] Health checks (PostgreSQL + MongoDB)
- [x] OpenTelemetry metrics z Prometheus exporter
- [ ] Endpointy dla CurriculumVitae (CRUD + query)
- [ ] API versioning (`Asp.Versioning`)
- [ ] Response caching dla GET endpoints (publiczne)
- [ ] Rate limiting (built-in .NET 10)
- [ ] Swagger UI z przykladami request/response

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `Microsoft.AspNetCore.OpenApi` | 10.0.1 | JJDevHub.Content.Api | Juz zainstalowany - OpenAPI/Swagger |
| `Asp.Versioning.Http` | latest | JJDevHub.Content.Api | NOWY - wersjonowanie API (URL segment lub header) |
| `Microsoft.AspNetCore.RateLimiting` | built-in | JJDevHub.Content.Api | Built-in w .NET 10 - rate limiting per endpoint |

## Architektura endpointow

```
Nginx (:8081)
    └── /api/content/ → Content.Api (:8080)
            ├── /work-experiences          GET (list, publicOnly filter)
            ├── /work-experiences/{id}     GET (single)
            ├── /work-experiences          POST (create, Owner only)
            ├── /work-experiences/{id}     PUT (update, Owner only)
            ├── /work-experiences/{id}     DELETE (delete, Owner only)
            ├── /cv                        [TODO] CRUD for CurriculumVitae
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
