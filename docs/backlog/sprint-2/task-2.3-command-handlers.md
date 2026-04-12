# Task 2.3: Implementacja Command Handlerow (MediatR + FluentValidation)

| Pole | Wartosc |
|------|---------|
| Sprint | 2 - The Core & Write Side |
| Status | DONE |
| Priorytet | High |
| Estymacja | 8 story points |
| Powiazane pliki | `src/Services/JJDevHub.Content/JJDevHub.Content.Application/Commands/`, `src/Services/JJDevHub.Content/JJDevHub.Content.Application/Behaviors/`, `src/Services/JJDevHub.Content/JJDevHub.Content.Application/IntegrationEvents/` |

## Opis

Warstwa Application implementuje CQRS pattern uzywajac MediatR jako mediatora. Komendy (Commands) modyfikuja stan przez agregaty domenowe i repozytorium PostgreSQL. Zapytania (Queries) czytaja dane z MongoDB read store. FluentValidation waliduje komendy w pipeline MediatR przed dotarciem do handlera. Po udanym zapisie, Domain Event Handlers publikuja Integration Events na Kafka.

### Co jest zaimplementowane

**Commands (zapis):**
- `AddWorkExperienceCommand` + `Handler` - tworzy agregat przez Factory Method, zapisuje, zwraca Guid
- `UpdateWorkExperienceCommand` + `Handler` - laduje agregat po Id, wywoluje `Update()`, zapisuje
- `DeleteWorkExperienceCommand` + `Handler` - laduje agregat, `MarkAsDeleted()` + `Delete()`, zapisuje

**Validators (FluentValidation):**
- `AddWorkExperienceCommandValidator`:
  - CompanyName: required, max 200
  - Position: required, max 200
  - StartDate: required, not future
  - EndDate: > StartDate (jesli podane)

**Queries (odczyt z MongoDB):**
- `GetWorkExperiencesQuery` + `Handler` - czyta z `IWorkExperienceReadStore`, filtr `publicOnly`
- `GetWorkExperienceByIdQuery` + `Handler` - czyta z `IWorkExperienceReadStore` po Id

**Domain Event Handlers (sync write -> read):**
- `WorkExperienceCreatedDomainEventHandler` - laduje encje, buduje read model, upsert do MongoDB, publikuje Integration Event na Kafka
- `WorkExperienceUpdatedDomainEventHandler` - analogicznie
- `WorkExperienceDeletedDomainEventHandler` - usuwa z read store, publikuje Integration Event

**Pipeline Behaviors:**
- `ValidationBehavior<TRequest, TResponse>` - uruchamia wszystkie validatory FluentValidation przed handlerem, rzuca `ValidationException` jesli bledy

**DTOs i Read Models:**
- `WorkExperienceDto` - record DTO dla API
- `WorkExperienceReadModel` - model do MongoDB upsert
- `IWorkExperienceReadStore` - interfejs read store (GetById, GetAll, GetPublic, Upsert, Delete)

**Integration Events:**
- `WorkExperienceCreatedIntegrationEvent` - ExperienceId, CompanyName, Position
- `WorkExperienceUpdatedIntegrationEvent` - WorkExperienceId
- `WorkExperienceDeletedIntegrationEvent` - WorkExperienceId

## Kryteria akceptacji

- [x] Command handlers dla Add, Update, Delete WorkExperience
- [x] FluentValidation z AddWorkExperienceCommandValidator
- [x] ValidationBehavior w MediatR pipeline
- [x] Query handlers czytajace z MongoDB (IWorkExperienceReadStore)
- [x] Domain Event Handlers synchronizujace write store z read store
- [x] Integration Events publikowane na Kafka po pomyslnym zapisie
- [x] DependencyInjection rejestrujaca MediatR, validatory, behaviors

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `MediatR` | 14.0.0 | JJDevHub.Content.Application | Mediator pattern - dispatching commands, queries, domain events |
| `FluentValidation` | 12.1.1 | JJDevHub.Content.Application | Fluent walidacja komend w pipeline |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.1 | JJDevHub.Content.Application | Automatyczna rejestracja walidatorow z assembly |

## Przeplywy danych

### Command Flow (zapis)
```
[API Endpoint] → POST /api/v1/content/work-experiences
    → MediatR.Send(AddWorkExperienceCommand)
        → ValidationBehavior → FluentValidation
        → AddWorkExperienceCommandHandler
            → WorkExperience.Create(...) [Factory Method + Domain Event]
            → IWorkExperienceRepository.AddAsync()
            → IUnitOfWork.SaveChangesAsync()
                → ContentDbContext dispatches Domain Events
                    → WorkExperienceCreatedDomainEventHandler
                        → IWorkExperienceReadStore.UpsertAsync() [MongoDB]
                        → IEventBus.PublishAsync() [Kafka]
    ← return Guid (new Id)
```

### Query Flow (odczyt)
```
[API Endpoint] → GET /api/v1/content/work-experiences?publicOnly=true
    → MediatR.Send(GetWorkExperiencesQuery)
        → GetWorkExperiencesQueryHandler
            → IWorkExperienceReadStore.GetPublicAsync() [MongoDB]
    ← return IReadOnlyList<WorkExperienceDto>
```

## Zaleznosci

- **Wymaga:** Task 1.3 (CQRS interfaces), Task 2.1 (agregaty), Task 2.2 (repozytorium)
- **Blokuje:** Task 3.2 (KafkaEventBus implementuje IEventBus), Task 4.1 (Content API endpointy)

## Notatki techniczne

- Domain Event Handlers sa wywolywane wewnatrz `SaveChangesAsync` - synchronicznie w tej samej transakcji. Jesli upsert do MongoDB lub publish na Kafka sie nie powiedzie, cala transakcja jest rollbackowana.
- Na przyszlosc mozna rozwazyc Outbox Pattern (Transactional Outbox) aby oddzielić zapis do PostgreSQL od publikacji na Kafka. Obecnie sa polaczone w jednej transakcji co moze powodowac problemy przy niedostepnosci Kafka.
- FluentValidation validatory sa skanowane automatycznie z assembly (`AddValidatorsFromAssembly`).
- `ValidationBehavior` rzuca `ValidationException` ktory jest przechwytywany przez `ExceptionHandlingMiddleware` w API i zwracany jako HTTP 400.
