# JJDevHub - Pełny Tutorial Architektury

> Kompletny przewodnik po architekturze DDD/CQRS, testowaniu, CI/CD, monitoringu i infrastrukturze projektu JJDevHub.

---

## Spis treści

1. [Przegląd architektury](#1-przegląd-architektury)
2. [Domain-Driven Design (DDD)](#2-domain-driven-design-ddd)
3. [CQRS - Command Query Responsibility Segregation](#3-cqrs---command-query-responsibility-segregation)
4. [Event-Driven Architecture](#4-event-driven-architecture)
5. [Clean Architecture - warstwy projektu](#5-clean-architecture---warstwy-projektu)
6. [Testowanie](#6-testowanie)
7. [CI/CD - Jenkins Pipeline](#7-cicd---jenkins-pipeline)
8. [SonarQube - jakość kodu](#8-sonarqube---jakość-kodu)
9. [Monitoring - Prometheus + Grafana](#9-monitoring---prometheus--grafana)
10. [Docker i infrastruktura](#10-docker-i-infrastruktura)
11. [Jak uruchomić projekt](#11-jak-uruchomić-projekt)

---

## 1. Przegląd architektury

### Stack technologiczny

| Warstwa | Technologia | Cel |
|---------|------------|-----|
| Backend | .NET 10, ASP.NET Core | API, logika biznesowa |
| Write DB | PostgreSQL 16 | Baza zapisu (Command side) |
| Read DB | MongoDB | Baza odczytu (Query side) |
| Message Broker | Apache Kafka | Komunikacja między serwisami |
| Frontend Web | Angular 21 + Angular Material | Klient webowy |
| Frontend Mobile | React Native + React Native Paper | Klient mobilny (iOS/Android) |
| CI/CD | Jenkins | Automatyzacja buildów |
| Code Quality | SonarQube | Analiza statyczna + pokrycie testami |
| Monitoring | Prometheus + Grafana | Metryki, dashboardy |
| Tracing | Jaeger (OpenTelemetry) | Distributed tracing (OTLP gRPC) |
| Secrets | HashiCorp Vault | Zarządzanie sekretami |
| IAM | Keycloak (OIDC) | Tożsamość, role, JWT |
| Reverse Proxy | Nginx | Routing, SSL termination |
| Konteneryzacja | Docker + Docker Compose | Orkiestracja serwisów |

### Diagram architektury

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              NGINX (reverse proxy)                          │
│                         :8081 (HTTP) / :443 (HTTPS)                         │
└──┬──────────────┬──────────────────────────────────────────────┬────────────┘
   │              │                                              │
   ▼              ▼                                              ▼
┌──────────┐  ┌──────────────────────────────────────────┐  ┌──────────┐
│ Angular  │  │           Backend Services               │  │ Jenkins  │
│ Web :80  │  │                                          │  │  :8082   │
└──────────┘  │  /api/v1/content/   → Content API :8080  │  └──────────┘
              │  /api/analytics/    → Analytics API       │
   ┌────────┐ │  /api/identity/     → Identity API       │
   │ React  │ │  /api/ai/           → AI Gateway         │
   │ Native │ │  /api/notification/ → Notification API   │
   │ Mobile │ │  /api/education/    → Education API      │
   └────────┘ │                     + Sync Worker (bg)   │
              └───────────────┬──────────────────────────┘
                              │
                ┌─────────────┼──────────────┐
                │             │              │
                ▼             ▼              ▼
         ┌───────────┐ ┌──────────┐  ┌───────────┐
         │ PostgreSQL │ │ MongoDB  │  │   Kafka   │
         │ (Write DB) │ │(Read DB) │  │  (Events) │
         │   :5433    │ │  :27018  │  │  :29092   │
         └───────────┘ └──────────┘  └───────────┘

┌──────────────┐  ┌───────────┐  ┌──────────┐  ┌───────────┐
│  SonarQube   │  │ Prometheus│  │  Grafana │  │   Vault   │
│    :9000     │  │   :9090   │  │  :3000   │  │   :8201   │
└──────────────┘  └───────────┘  └──────────┘  └───────────┘
```

### Struktura folderów

```
JJDevHub/
├── src/
│   ├── Services/
│   │   ├── JJDevHub.Content/           # Główny serwis (DDD/CQRS)
│   │   │   ├── JJDevHub.Content.Api/           # Warstwa prezentacji + Dockerfile
│   │   │   ├── JJDevHub.Content.Application/   # Warstwa aplikacji
│   │   │   ├── JJDevHub.Content.Core/          # Warstwa domenowa
│   │   │   ├── JJDevHub.Content.Infrastructure/ # Warstwa infrastruktury
│   │   │   └── JJDevHub.Content.Persistence/   # Warstwa persystencji
│   │   ├── JJDevHub.Shared.Kernel/     # Wspólne abstrakcje DDD
│   │   │   └── BuildingBlocks/
│   │   │       ├── Entity.cs                   # Bazowa klasa z Id
│   │   │       ├── AuditableEntity.cs          # Audit + soft delete
│   │   │       ├── AggregateRoot.cs            # Entity + domain events
│   │   │       ├── AuditableAggregateRoot.cs   # AuditableEntity + domain events
│   │   │       ├── IAggregateRoot.cs           # Interfejs marker
│   │   │       ├── ValueObject.cs
│   │   │       ├── IRepository.cs
│   │   │       ├── IUnitOfWork.cs
│   │   │       └── IDomainEvent.cs
│   │   ├── JJDevHub.Analytics/         # Serwis analityczny + Dockerfile
│   │   ├── JJDevHub.Identity/          # Serwis tożsamości + Dockerfile
│   │   ├── JJDevHub.AI.Gateway/        # Brama AI + Dockerfile
│   │   ├── JJDevHub.Notification/      # Serwis powiadomień + Dockerfile
│   │   ├── JJDevHub.Education/         # Serwis edukacyjny + Dockerfile
│   │   └── JJDevHub.Sync/             # Worker do synchronizacji + Dockerfile
│   └── Clients/
│       ├── web/                        # Angular 21 + Angular Material
│       │   ├── src/app/
│       │   │   ├── pages/              # Home, WorkExperience, About
│       │   │   ├── services/           # ApiService (HTTP)
│       │   │   └── models/             # TypeScript interfaces
│       │   └── Dockerfile              # Multi-stage: node → nginx
│       └── mobile/JJDevHubMobile/      # React Native + Paper
│           ├── src/
│           │   ├── screens/            # HomeScreen, WorkExperienceScreen
│           │   ├── services/           # API fetch wrapper
│           │   └── models/             # TypeScript interfaces
│           └── App.tsx                 # Navigation + Paper theme
├── tests/
│   ├── JJDevHub.Content.UnitTests/         # Testy jednostkowe
│   └── JJDevHub.Content.IntegrationTests/  # Testy integracyjne
├── infra/
│   └── docker/
│       ├── docker-compose.yml          # Orkiestracja WSZYSTKICH kontenerów
│       ├── monitoring/                 # Prometheus + Grafana config
│       ├── nginx/                      # Reverse proxy config (routing do serwisów)
│       └── bootstrap-vault.sh          # Inicjalizacja sekretów
├── .dockerignore                       # Optymalizacja Docker context
├── Jenkinsfile                         # CI/CD pipeline (9 stage'ów)
├── sonar-project.properties            # SonarQube config
└── JJDevHub.sln                        # Solution file

> **Powiązane przewodniki:** [JJDevHub — przewodnik kompleksowy (PL)](jjdevhub-przewodnik-kompleksowy.md) — mapa systemu, E2E, playbook, tutoriale technologii, FAQ.
```

---

## 2. Domain-Driven Design (DDD)

### Teoria

DDD to podejście do projektowania oprogramowania, które skupia się na **domenie biznesowej** jako centrum architektury. Kluczowe koncepty:

#### Aggregate Root

**Aggregate** to klaster obiektów domenowych traktowany jako jedna jednostka. **Aggregate Root** to główny punkt wejścia do agregatu - jedyny obiekt, przez który można modyfikować stan.

**Zasady:**
- Zewnętrzne obiekty mogą referencjonować **tylko** Aggregate Root (nigdy wewnętrzne entity)
- Zmiany wewnątrz agregatu są **atomowe** (albo wszystko się zapisze, albo nic)
- Aggregate Root odpowiada za zachowanie **spójności** danych (invariants)

W projekcie: `WorkExperience` to Aggregate Root.

#### Entity

Obiekt z **tożsamością** (identity) - dwa obiekty z tymi samymi wartościami ale różnym `Id` to różne entity.

```csharp
// Shared.Kernel/BuildingBlocks/Entity.cs
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    // Equality oparta na Id, nie na wartościach
    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (Id == Guid.Empty || other.Id == Guid.Empty) return false;
        return Id == other.Id;
    }
}
```

#### AuditableEntity (soft delete + audit fields)

Rozszerzenie `Entity` o pola audytowe i mechanizm soft delete. Encje dziedziczące po `AuditableEntity` nigdy nie są fizycznie usuwane z bazy -- zamiast tego ustawiany jest `IsActive = false`.

```csharp
// Shared.Kernel/BuildingBlocks/AuditableEntity.cs
public abstract class AuditableEntity : Entity
{
    public bool IsActive { get; protected set; } = true;
    public DateTime CreatedDate { get; protected set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; protected set; }
    public string? CreatedById { get; protected set; }
    public string? ModifiedById { get; protected set; }
    public long Version { get; protected set; }

    public void Deactivate() { IsActive = false; ModifiedDate = DateTime.UtcNow; }
    public void Activate() { IsActive = true; ModifiedDate = DateTime.UtcNow; }
    public void MarkModified() => ModifiedDate = DateTime.UtcNow;

    // Wywoływane z warstwy persystencji (internal + InternalsVisibleTo):
    internal void ApplyPersistenceOnCreate(DateTime utcNow, string? userSubject)
    {
        CreatedDate = utcNow; CreatedById = userSubject; Version = 1;
    }
    internal void ApplyPersistenceOnModify(DateTime utcNow, string? userSubject)
    {
        ModifiedDate = utcNow; ModifiedById = userSubject; Version++;
    }
}
```

**Hierarchia klas bazowych:**

```
Entity (Id)
├── AggregateRoot : Entity, IAggregateRoot (+ domain events)
├── AuditableEntity : Entity (+ IsActive, CreatedDate, ModifiedDate, CreatedById, ModifiedById, Version)
│   └── AuditableAggregateRoot : AuditableEntity, IAggregateRoot (+ domain events)
```

- **`AggregateRoot`** -- dla encji BEZ audytu/soft delete
- **`AuditableAggregateRoot`** -- dla encji Z audytem i soft delete (np. `WorkExperience`)
- **`IAggregateRoot`** -- wspólny interfejs (używany w `IRepository<T>` constraint)

`ContentDbContext` automatycznie:
1. Ustawia `CreatedDate` na `DateTime.UtcNow` przy dodawaniu nowej encji
2. Aktualizuje `ModifiedDate` przy modyfikacji
3. Stosuje **global query filter** `HasQueryFilter(e => e.IsActive)` -- soft-deleted rekordy są automatycznie pomijane w zapytaniach

#### Value Object

Obiekt **bez tożsamości** - definiowany wyłącznie przez swoje wartości. Dwa Value Objects z tymi samymi wartościami to **ten sam** obiekt. Są **immutable**.

```csharp
// Content.Core/ValueObjects/DateRange.cs
public class DateRange : ValueObject
{
    public DateTime Start { get; }
    public DateTime? End { get; }

    public DateRange(DateTime start, DateTime? end)
    {
        if (end.HasValue && end.Value < start)
            throw new ArgumentException("End date cannot be earlier than start date.");
        Start = start;
        End = end;
    }

    public bool IsCurrent => !End.HasValue;

    public int DurationInMonths
    {
        get
        {
            var endDate = End ?? DateTime.UtcNow;
            return ((endDate.Year - Start.Year) * 12) + endDate.Month - Start.Month;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End ?? DateTime.MaxValue;
    }
}
```

**Kiedy Value Object zamiast prymitywu?**
- `DateRange` zamiast dwóch osobnych `DateTime StartDate, DateTime? EndDate`
- Enkapsuluje walidację i logikę biznesową (obliczanie czasu trwania)
- Zapobiega "primitive obsession" (anti-pattern)

#### Domain Event

Zdarzenie domenowe to fakt, który **już się wydarzył** w domenie. Służy do:
1. Komunikacji między agregatami w ramach tego samego bounded context
2. Triggerowania efektów ubocznych (np. synchronizacja read modelu)
3. Zachowania SRP - encja nie musi wiedzieć o wszystkim, co się dzieje po jej zmianie

```csharp
// Bazowy interfejs - dziedziczy po INotification z MediatR
public interface IDomainEvent : INotification
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}

// Konkretne zdarzenie
public sealed record WorkExperienceCreatedDomainEvent(
    Guid WorkExperienceId,
    string CompanyName,
    string Position,
    bool IsPublic) : DomainEventBase;
```

#### Domain Exception

Wyjątki domenowe sygnalizują złamanie reguł biznesowych. Mają własną hierarchię, żeby middleware mógł je mapować na odpowiednie kody HTTP.

```csharp
// Bazowy
public abstract class DomainException : Exception { ... }

// Specyficzny dla Content
public class ContentDomainException : DomainException { ... }

// Jeszcze bardziej specyficzny
public class InvalidWorkExperienceException : ContentDomainException { ... }
```

#### Repozytorium

Interfejs repozytorium definiowany jest w **warstwie domenowej**, a implementacja w **warstwie persystencji**. Dzięki temu domena nie zależy od infrastruktury.

```csharp
// Interfejs w Core (domena)
public interface IWorkExperienceRepository : IRepository<WorkExperience>
{
    Task<WorkExperience?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<WorkExperience>> GetAllAsync(CancellationToken ct);
    Task AddAsync(WorkExperience workExperience, CancellationToken ct);
    void Update(WorkExperience workExperience);
    void Delete(WorkExperience workExperience);
}

// Implementacja w Persistence
public class WorkExperienceRepository : IWorkExperienceRepository
{
    private readonly ContentDbContext _context;
    public IUnitOfWork UnitOfWork => _context;
    // ... implementacja z EF Core
}
```

#### Unit of Work

Pattern zapewniający, że wszystkie zmiany w ramach jednej operacji biznesowej są zapisywane **atomowo**.

```csharp
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

EF Core `DbContext` naturalnie implementuje Unit of Work (śledzi zmiany i zapisuje je w jednej transakcji).

### Praktyka - encja WorkExperience

```csharp
public class WorkExperience : AuditableAggregateRoot  // Audit + soft delete
{
    public string CompanyName { get; private set; } = null!;
    public string Position { get; private set; } = null!;
    public DateRange Period { get; private set; } = null!;  // Value Object!
    public bool IsPublic { get; private set; }
    // Odziedziczone: Id, IsActive, CreatedDate, ModifiedDate

    private WorkExperience() { }  // Dla EF Core

    // Factory Method zamiast publicznego konstruktora
    public static WorkExperience Create(
        string companyName, string position,
        DateTime startDate, DateTime? endDate, bool isPublic)
    {
        ValidateCompanyName(companyName);
        ValidatePosition(position);

        var experience = new WorkExperience
        {
            CompanyName = companyName.Trim(),
            Position = position.Trim(),
            Period = new DateRange(startDate, endDate),
            IsPublic = isPublic
        };

        // Encja sama emituje domain event
        experience.AddDomainEvent(new WorkExperienceCreatedDomainEvent(
            experience.Id, experience.CompanyName,
            experience.Position, experience.IsPublic));

        return experience;
    }

    public void Update(...) { /* walidacja + AddDomainEvent */ }

    // Soft delete: Deactivate() ustawia IsActive=false, nie usuwa fizycznie z DB
    public void MarkAsDeleted()
    {
        Deactivate();  // IsActive = false, ModifiedDate = UtcNow
        AddDomainEvent(new WorkExperienceDeletedDomainEvent(Id));
    }

    public void Publish() => IsPublic = true;
    public void Hide() => IsPublic = false;

    // Walidacja w domenie, nie w kontrolerze!
    private static void ValidateCompanyName(string companyName) { ... }
}
```

**Kluczowe zasady:**
1. **Private setters** - stan zmienia się tylko przez metody biznesowe
2. **Factory method** (`Create`) zamiast publicznego konstruktora - kontrola tworzenia
3. **Walidacja w domenie** - encja chroni swoje invarianty
4. **Domain events** emitowane przez encję - nie przez handler
5. **Soft delete** - `MarkAsDeleted()` wywołuje `Deactivate()` zamiast fizycznego usunięcia
6. **Audit trail** - `CreatedDate` i `ModifiedDate` automatycznie zarządzane przez `ContentDbContext`

---

## 3. CQRS - Command Query Responsibility Segregation

### Teoria

CQRS rozdziela operacje **odczytu** (Query) od **zapisu** (Command) na osobne modele. W naszym przypadku idzie to dalej - mamy **osobne bazy danych**:

```
┌─────────────────────────────────────────────────────┐
│                    Content API                       │
│                                                     │
│  POST /work-experiences                             │
│        │                                            │
│        ▼                                            │
│  ┌──────────────┐    Domain    ┌─────────────────┐  │
│  │   Command    │───Events────▶│  Domain Event   │  │
│  │   Handler    │              │   Handlers      │  │
│  └──────┬───────┘              └───┬─────────┬───┘  │
│         │                         │         │       │
│         ▼                         ▼         ▼       │
│  ┌──────────────┐          ┌──────────┐ ┌───────┐  │
│  │  PostgreSQL  │          │ MongoDB  │ │ Kafka │  │
│  │  (Write DB)  │          │(Read DB) │ │(Async)│  │
│  └──────────────┘          └────▲─────┘ └───────┘  │
│                                 │                   │
│  GET /work-experiences          │                   │
│        │                        │                   │
│        ▼                        │                   │
│  ┌──────────────┐               │                   │
│  │    Query     │───────────────┘                   │
│  │   Handler    │  reads from MongoDB               │
│  └──────────────┘                                   │
└─────────────────────────────────────────────────────┘
```

### Dlaczego dwie bazy?

| Aspekt | PostgreSQL (Write) | MongoDB (Read) |
|--------|-------------------|----------------|
| Model | Znormalizowany, relacyjny | Zdenormalizowany, dokumentowy |
| Optymalizacja | Integralność danych, transakcje | Szybkość odczytu, elastyczne schematy |
| Skalowanie | Vertical scaling | Horizontal scaling (replica sets) |
| Użycie | Zapisy + walidacja reguł biznesowych | Prezentacja danych w UI |

### Command (zapis)

```csharp
// 1. Command - DTO z danymi do zapisu
public record AddWorkExperienceCommand(
    string CompanyName, string Position,
    DateTime StartDate, DateTime? EndDate,
    bool IsPublic) : ICommand<Guid>;

// 2. Handler - logika biznesowa
public class AddWorkExperienceCommandHandler : ICommandHandler<AddWorkExperienceCommand, Guid>
{
    private readonly IWorkExperienceRepository _repository;

    public async Task<Guid> Handle(AddWorkExperienceCommand request, CancellationToken ct)
    {
        // Tworzenie przez factory method (walidacja w domenie)
        var experience = WorkExperience.Create(
            request.CompanyName, request.Position,
            request.StartDate, request.EndDate, request.IsPublic);

        // Zapis do PostgreSQL
        await _repository.AddAsync(experience, ct);
        await _repository.UnitOfWork.SaveChangesAsync(ct);
        // ↑ SaveChanges dispatchuje domain events automatycznie!

        return experience.Id;
    }
}

// 3. Validator (FluentValidation) - walidacja na poziomie API
public class AddWorkExperienceCommandValidator : AbstractValidator<AddWorkExperienceCommand>
{
    public AddWorkExperienceCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).LessThanOrEqualTo(DateTime.UtcNow);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).When(x => x.EndDate.HasValue);
    }
}
```

**Dwa poziomy walidacji:**
1. **FluentValidation** (Application) - walidacja formatu/struktury (puste pola, długość, formaty)
2. **Domain validation** (Core) - walidacja reguł biznesowych (invarianty agregatu)

### Query (odczyt)

```csharp
// 1. Query - parametry zapytania
public record GetWorkExperiencesQuery(bool PublicOnly = false)
    : IQuery<IReadOnlyList<WorkExperienceDto>>;

// 2. Handler - czyta z MongoDB (read store)
public class GetWorkExperiencesQueryHandler
    : IQueryHandler<GetWorkExperiencesQuery, IReadOnlyList<WorkExperienceDto>>
{
    private readonly IWorkExperienceReadStore _readStore;  // MongoDB!

    public async Task<IReadOnlyList<WorkExperienceDto>> Handle(
        GetWorkExperiencesQuery request, CancellationToken ct)
    {
        return request.PublicOnly
            ? await _readStore.GetPublicAsync(ct)
            : await _readStore.GetAllAsync(ct);
    }
}
```

**Kluczowe:** Query handler **nie używa** repozytorium PostgreSQL. Czyta bezpośrednio z MongoDB, które ma zdenormalizowane dane gotowe do wyświetlenia.

### Synchronizacja Write → Read (Domain Event Handlers)

Kiedy `SaveChangesAsync` w `ContentDbContext` jest wywoływane:

1. **Collect:** Zbiera domain events z wszystkich zmienionych agregatów (przed save, bo delete detachuje entity)
2. **Begin TX:** Otwiera explicit transaction
3. **Flush aggregates:** Zapisuje agregaty do PostgreSQL (waliduje constraints, ale nie commituje)
4. **Dispatch:** Dispatchuje domain events przez MediatR — handlery upsertują MongoDB i dodają outbox entries do change trackera
5. **Flush outbox:** Zapisuje outbox entries (w tej samej transakcji)
6. **Commit:** Commituje atomowo (agregaty + outbox)

```csharp
// ContentDbContext.cs
public override async Task<int> SaveChangesAsync(CancellationToken ct)
{
    var domainEvents = CollectDomainEvents();

    await using var transaction = await Database.BeginTransactionAsync(ct);

    var result = await base.SaveChangesAsync(ct);           // flush aggregates
    await DispatchDomainEventsAsync(domainEvents, ct);      // handlers: MongoDB + outbox

    if (ChangeTracker.HasChanges())
        await base.SaveChangesAsync(ct);                    // flush outbox entries

    await transaction.CommitAsync(ct);                      // atomic commit
    return result;
}
```

Potem domain event handler synchronizuje read model i enqueue'uje integration event do outboxa:

```csharp
public class WorkExperienceCreatedDomainEventHandler
    : INotificationHandler<WorkExperienceCreatedDomainEvent>
{
    private readonly IWorkExperienceRepository _repository;  // PG (read entity)
    private readonly IWorkExperienceReadStore _readStore;     // Mongo (write read model)
    private readonly IOutboxWriter _outbox;                   // Outbox (same PG transaction)

    public async Task Handle(WorkExperienceCreatedDomainEvent notification, CancellationToken ct)
    {
        // 1. Odczytaj entity z PostgreSQL (widoczne w bieżącej transakcji)
        var entity = await _repository.GetByIdAsync(notification.WorkExperienceId, ct);

        // 2. Upsert do MongoDB (sync read model)
        var readModel = new WorkExperienceReadModel { ... };
        await _readStore.UpsertAsync(readModel, ct);

        // 3. Enqueue do outboxa (committed atomowo z agregatem)
        //    OutboxPublisherHostedService opublikuje do Kafka po commit
        _outbox.Enqueue(
            new WorkExperienceCreatedIntegrationEvent(...),
            nameof(WorkExperience),
            notification.WorkExperienceId);
    }
}
```

### MediatR Pipeline

Request flow w MediatR z naszym behavior:

```
Request (Command/Query)
    │
    ▼
┌─────────────────────────┐
│  ValidationBehavior     │  ← FluentValidation (przed handlerem!)
│  (IPipelineBehavior)    │
└────────────┬────────────┘
             │ valid? ✓
             ▼
┌─────────────────────────┐
│  Command/Query Handler  │  ← logika biznesowa
└────────────┬────────────┘
             │
             ▼
         Response
```

```csharp
// Behaviors/ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);  // → 400 Bad Request

        return await next();
    }
}
```

### Interfejsy CQRS (Shared Kernel)

```csharp
// Wszystkie oparte na MediatR
public interface ICommand<out TResponse> : IRequest<TResponse> { }
public interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse> { }

public interface IQuery<out TResponse> : IRequest<TResponse> { }
public interface IQueryHandler<in TQuery, TResponse>
    : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse> { }
```

Dzięki osobnym interfejsom mamy jasne rozróżnienie co jest Command a co Query, mimo że oba używają MediatR pod spodem.

---

## 4. Event-Driven Architecture

### Domain Events vs Integration Events

| Aspekt | Domain Event | Integration Event |
|--------|-------------|-------------------|
| Zakres | Wewnątrz bounded context | Między serwisami |
| Transport | MediatR (in-process) | Kafka (out-of-process) |
| Spójność | Synchroniczne | Eventual consistency |
| Przykład | `WorkExperienceCreatedDomainEvent` | `WorkExperienceCreatedIntegrationEvent` |

### Flow zdarzeń

```
WorkExperience.Create()
    │
    ├─ AddDomainEvent(WorkExperienceCreatedDomainEvent)
    │
    ▼
SaveChangesAsync()
    │
    ├─ 1. BEGIN TRANSACTION
    │
    ├─ 2. Flush aggregates to PostgreSQL (validates constraints) ✓
    │
    ├─ 3. Dispatch domain events via MediatR
    │      │
    │      ▼
    │   WorkExperienceCreatedDomainEventHandler
    │      │
    │      ├─ Upsert to MongoDB (sync read model)
    │      │
    │      └─ Enqueue IntegrationEvent to outbox (same EF transaction)
    │
    ├─ 4. Flush outbox entries to PostgreSQL ✓
    │
    ├─ 5. COMMIT (aggregates + outbox atomically)
    │
    │   OutboxPublisherHostedService (background)
    │      │
    │      └─ Publish from outbox to Kafka
    │              │
    │              ▼
    │         Other services (Analytics, Sync, Notification)
    │
    ▼
Return response to API
```

### Kafka - Event Bus

```csharp
// Interface w Shared.Kernel
public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent) where T : IntegrationEvent;
}

// Implementacja z Confluent.Kafka w Infrastructure
public class KafkaEventBus : IEventBus, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public async Task PublishAsync<T>(T integrationEvent) where T : IntegrationEvent
    {
        var topicName = typeof(T).Name;  // Topic = nazwa klasy eventu
        var message = new Message<string, string>
        {
            Key = integrationEvent.Id.ToString(),
            Value = JsonSerializer.Serialize(integrationEvent)
        };

        await _producer.ProduceAsync(topicName, message);
    }
}
```

**Konfiguracja producera:**
- `Acks = All` - czeka na potwierdzenie ze wszystkich replik
- `EnableIdempotence = true` - gwarantuje exactly-once delivery

---

## 5. Clean Architecture - warstwy projektu

### Dependency Rule

```
    Api  →  Application  →  Core  ←  Shared.Kernel
     ↓          ↑
     ├→  Infrastructure
     └→  Persistence
```

**Zasada:** Zależności wskazują **do wewnątrz**. Core (domena) nie zależy od niczego poza Shared.Kernel. Infrastructure/Persistence implementują interfejsy zdefiniowane w Core/Application.

### Core (warstwa domenowa)

```
JJDevHub.Content.Core/
├── Entities/
│   └── WorkExperience.cs          # Aggregate Root
├── ValueObjects/
│   └── DateRange.cs               # Value Object
├── Events/
│   ├── WorkExperienceCreatedDomainEvent.cs
│   ├── WorkExperienceUpdatedDomainEvent.cs
│   └── WorkExperienceDeletedDomainEvent.cs
├── Repositories/
│   └── IWorkExperienceRepository.cs  # Interface (nie implementacja!)
└── Exceptions/
    ├── ContentDomainException.cs
    └── InvalidWorkExperienceException.cs
```

**Zależności:** Tylko `Shared.Kernel`. Zero pakietów NuGet infrastrukturalnych.

### Application (warstwa aplikacji)

```
JJDevHub.Content.Application/
├── Commands/
│   ├── AddWorkExperience/
│   │   ├── AddWorkExperienceCommand.cs
│   │   ├── AddWorkExperienceCommandHandler.cs
│   │   └── AddWorkExperienceCommandValidator.cs
│   ├── UpdateWorkExperience/
│   │   ├── UpdateWorkExperienceCommand.cs
│   │   └── UpdateWorkExperienceCommandHandler.cs
│   └── DeleteWorkExperience/
│       ├── DeleteWorkExperienceCommand.cs
│       └── DeleteWorkExperienceCommandHandler.cs
├── Queries/
│   ├── GetWorkExperiences/
│   │   ├── GetWorkExperiencesQuery.cs
│   │   └── GetWorkExperiencesQueryHandler.cs
│   └── GetWorkExperienceById/
│       ├── GetWorkExperienceByIdQuery.cs
│       └── GetWorkExperienceByIdQueryHandler.cs
├── DTOs/
│   └── WorkExperienceDto.cs
├── ReadModels/
│   └── WorkExperienceReadModel.cs
├── Interfaces/
│   └── IWorkExperienceReadStore.cs
├── Behaviors/
│   └── ValidationBehavior.cs
├── IntegrationEvents/
│   ├── WorkExperienceCreatedIntegrationEvent.cs
│   ├── WorkExperienceCreatedDomainEventHandler.cs
│   ├── WorkExperienceUpdatedDomainEventHandler.cs
│   └── WorkExperienceDeletedDomainEventHandler.cs
└── DependencyInjection.cs
```

**Feature Folders:** Każdy command/query w osobnym folderze. Łatwo znaleźć wszystko związane z daną operacją.

**Zależności:** `Core` + `Shared.Kernel` + MediatR + FluentValidation. **Zero EF Core, zero MongoDB** - tylko interfejsy.

### Persistence (write side - PostgreSQL)

```
JJDevHub.Content.Persistence/
├── ContentDbContext.cs                # EF Core DbContext + IUnitOfWork + domain event dispatch
├── Configurations/
│   └── WorkExperienceConfiguration.cs # Fluent API mapping
├── Repositories/
│   └── WorkExperienceRepository.cs    # Implementacja IWorkExperienceRepository
└── DependencyInjection.cs
```

**Entity configuration z Owned Type i kolumnami audytowymi:**

```csharp
public class WorkExperienceConfiguration : IEntityTypeConfiguration<WorkExperience>
{
    public void Configure(EntityTypeBuilder<WorkExperience> builder)
    {
        builder.ToTable("work_experiences");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Position).HasMaxLength(200).IsRequired();

        // Kolumny audytowe (z AuditableEntity)
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(e => e.CreatedDate).HasColumnName("created_date").IsRequired();
        builder.Property(e => e.ModifiedDate).HasColumnName("modified_date");

        // Value Object jako Owned Type
        builder.OwnsOne(e => e.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("start_date").IsRequired();
            period.Property(p => p.End).HasColumnName("end_date");
        });

        builder.Ignore(e => e.DomainEvents);  // Nie mapujemy do DB
    }
}
```

### Infrastructure (Kafka + MongoDB read store)

```
JJDevHub.Content.Infrastructure/
├── Messaging/
│   └── KafkaEventBus.cs              # IEventBus → Kafka
├── ReadStore/
│   ├── MongoDbSettings.cs            # Konfiguracja MongoDB
│   ├── WorkExperienceDocument.cs     # Dokument MongoDB (internal)
│   └── MongoWorkExperienceReadStore.cs # IWorkExperienceReadStore → MongoDB
└── DependencyInjection.cs
```

**MongoDB Read Store:**

```csharp
public class MongoWorkExperienceReadStore : IWorkExperienceReadStore
{
    private readonly IMongoCollection<WorkExperienceDocument> _collection;

    public MongoWorkExperienceReadStore(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<WorkExperienceDocument>("work_experiences");
    }

    // Read operations (dla Query handlerów)
    public async Task<IReadOnlyList<WorkExperienceDto>> GetPublicAsync(CancellationToken ct)
    {
        var filter = Builders<WorkExperienceDocument>.Filter.Eq(d => d.IsPublic, true);
        var sort = Builders<WorkExperienceDocument>.Sort.Descending(d => d.StartDate);
        var documents = await _collection.Find(filter).Sort(sort).ToListAsync(ct);
        return documents.Select(MapToDto).ToList().AsReadOnly();
    }

    // Write operations (dla Domain Event handlerów - sync)
    public async Task UpsertAsync(WorkExperienceReadModel model, CancellationToken ct)
    {
        var filter = Builders<WorkExperienceDocument>.Filter.Eq(d => d.Id, model.Id);
        var document = new WorkExperienceDocument { ... };
        await _collection.ReplaceOneAsync(filter, document,
            new ReplaceOptions { IsUpsert = true }, ct);
    }
}
```

### Api (warstwa prezentacji)

```
JJDevHub.Content.Api/
├── Endpoints/
│   └── WorkExperienceEndpoints.cs     # Minimal API endpoints
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs # Global error handling
├── Program.cs                         # Composition Root + DI
├── Dockerfile                         # Multi-stage build
└── appsettings.json                   # Connection strings
```

**Minimal API z MediatR:**

```csharp
public static class WorkExperienceEndpoints
{
    public static IEndpointRouteBuilder MapWorkExperienceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/content/work-experiences")
            .WithTags("WorkExperiences");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    private static async Task<IResult> Create(
        AddWorkExperienceCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return Results.Created($"/api/v1/content/work-experiences/{id}", new { id });
    }

    // ... reszta endpointów
}
```

**Exception Handling Middleware:**

```csharp
var (statusCode, response) = exception switch
{
    ValidationException validationEx => (
        HttpStatusCode.BadRequest,                    // 400
        new ErrorResponse("Validation failed", validationEx.Errors...)),

    DomainException domainEx => (
        HttpStatusCode.UnprocessableEntity,           // 422
        new ErrorResponse(domainEx.Message)),

    KeyNotFoundException notFoundEx => (
        HttpStatusCode.NotFound,                      // 404
        new ErrorResponse(notFoundEx.Message)),

    _ => (
        HttpStatusCode.InternalServerError,           // 500
        new ErrorResponse("An unexpected error occurred."))
};
```

### Dependency Injection (Composition Root)

```csharp
// Program.cs - tu się wszystko łączy
builder.Services
    .AddApplication()                          // MediatR + FluentValidation + Behaviors
    .AddPersistence(builder.Configuration)     // EF Core + PostgreSQL + Repositories
    .AddInfrastructure(builder.Configuration); // Kafka + MongoDB Read Store

// Każda warstwa ma swój DependencyInjection.cs:
// Application:
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
services.AddValidatorsFromAssembly(assembly);
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Persistence:
services.AddDbContext<ContentDbContext>(options => options.UseNpgsql(...));
services.AddScoped<IWorkExperienceRepository, WorkExperienceRepository>();

// Infrastructure:
services.Configure<MongoDbSettings>(configuration.GetSection("MongoDb"));
services.AddSingleton<IWorkExperienceReadStore, MongoWorkExperienceReadStore>();
services.AddSingleton<IEventBus, KafkaEventBus>();
```

---

## 6. Testowanie

### Strategia testów

```
                    ┌─────────────────────┐
                    │  Integration Tests  │  ← Mało, drogie, wolne
                    │  (7 testów)         │     Testcontainers: PG + Mongo
                    │  Real HTTP + Real DB │
                    └──────────┬──────────┘
                               │
                    ┌──────────┴──────────┐
                    │    Unit Tests       │  ← Dużo, tanie, szybkie
                    │    (34 testy)       │     Mocki: NSubstitute
                    │    Domena + App     │
                    └─────────────────────┘
```

### Testy jednostkowe

**Projekt:** `tests/JJDevHub.Content.UnitTests/`

**Pakiety:**
- `xUnit` - framework testowy
- `FluentAssertions` - czytelne asercje
- `NSubstitute` - mockowanie interfejsów
- `coverlet.collector` - code coverage

#### Testowanie domeny

Domena nie wymaga mocków - testujemy czystą logikę:

```csharp
[Fact]
public void Create_WithValidData_ShouldCreateWorkExperience()
{
    var experience = WorkExperience.Create(
        "Microsoft", "Senior Developer",
        new DateTime(2023, 1, 1), null, true);

    experience.CompanyName.Should().Be("Microsoft");
    experience.Position.Should().Be("Senior Developer");
    experience.Period.IsCurrent.Should().BeTrue();
    experience.Id.Should().NotBeEmpty();
}

[Fact]
public void Create_ShouldRaiseWorkExperienceCreatedDomainEvent()
{
    var experience = WorkExperience.Create("Google", "Engineer", ...);

    experience.DomainEvents.Should().ContainSingle();
    experience.DomainEvents.First()
        .Should().BeOfType<WorkExperienceCreatedDomainEvent>();
}

[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public void Create_WithEmptyCompanyName_ShouldThrowDomainException(string? name)
{
    var act = () => WorkExperience.Create(name!, "Dev", DateTime.Now, null, true);

    act.Should().Throw<InvalidWorkExperienceException>()
        .WithMessage("*Company name*empty*");
}
```

#### Testowanie handlerów (z mockami)

```csharp
public class AddWorkExperienceCommandHandlerTests
{
    private readonly IWorkExperienceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AddWorkExperienceCommandHandler _handler;

    public AddWorkExperienceCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _repository = Substitute.For<IWorkExperienceRepository>();
        _repository.UnitOfWork.Returns(_unitOfWork);  // Mock UoW
        _handler = new AddWorkExperienceCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldAddAndSave()
    {
        var command = new AddWorkExperienceCommand("Microsoft", "Developer", ...);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(
            Arg.Is<WorkExperience>(e => e.CompanyName == "Microsoft"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

#### Testowanie walidatorów

```csharp
public class AddWorkExperienceCommandValidatorTests
{
    private readonly AddWorkExperienceCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEndDateBeforeStartDate_ShouldFail()
    {
        var command = new AddWorkExperienceCommand(
            "Microsoft", "Developer",
            new DateTime(2024, 1, 1),     // start
            new DateTime(2023, 1, 1),     // end PRZED start
            true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }
}
```

### Testy integracyjne

**Projekt:** `tests/JJDevHub.Content.IntegrationTests/`

**Pakiety:**
- `Microsoft.AspNetCore.Mvc.Testing` - `WebApplicationFactory`
- `Testcontainers.PostgreSql` - prawdziwy PostgreSQL w Docker
- `Testcontainers.MongoDb` - prawdziwy MongoDB w Docker

#### WebApplicationFactory z Testcontainers

```csharp
public class ContentApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Prawdziwe kontenery Docker!
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("jjdevhub_content_test")
        .Build();

    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Podmień PostgreSQL na Testcontainer
            services.RemoveAll<DbContextOptions<ContentDbContext>>();
            services.AddDbContext<ContentDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Podmień MongoDB na Testcontainer
            services.RemoveAll<IWorkExperienceReadStore>();
            services.Configure<MongoDbSettings>(opts =>
            {
                opts.ConnectionString = _mongo.GetConnectionString();
                opts.DatabaseName = "jjdevhub_content_test_read";
            });
            services.AddSingleton<IWorkExperienceReadStore, MongoWorkExperienceReadStore>();

            // Mock Kafka (nie chcemy wysyłać eventów w testach)
            services.RemoveAll<IEventBus>();
            services.AddSingleton(Substitute.For<IEventBus>());
        });
    }

    // Testcontainers lifecycle
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _mongo.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.StopAsync();
        await _mongo.StopAsync();
    }
}
```

#### Testy E2E przez HTTP

```csharp
public class WorkExperienceEndpointsTests : IClassFixture<ContentApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;

    public WorkExperienceEndpointsTests(ContentApiFactory factory)
    {
        _client = factory.CreateClient();  // Prawdziwy HTTP client!
    }

    [Fact]
    public async Task CreateWorkExperience_ShouldReturn201()
    {
        var command = new AddWorkExperienceCommand(
            "Integration Corp", "Test Engineer", ...);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/content/work-experiences", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetWorkExperiences_PublicOnly_ShouldFilterCorrectly()
    {
        // Create public + private
        await _client.PostAsJsonAsync("/api/v1/content/work-experiences",
            new AddWorkExperienceCommand("Public Corp", ..., isPublic: true));
        await _client.PostAsJsonAsync("/api/v1/content/work-experiences",
            new AddWorkExperienceCommand("Private Corp", ..., isPublic: false));

        // Query with filter - reads from MongoDB!
        var response = await _client.GetAsync(
            "/api/v1/content/work-experiences?publicOnly=true");
        var experiences = await response.Content
            .ReadFromJsonAsync<List<WorkExperienceDto>>();

        experiences!.Should().AllSatisfy(e => e.IsPublic.Should().BeTrue());
    }
}
```

### Uruchamianie testów

```bash
# Tylko testy jednostkowe (szybkie, bez Docker)
dotnet test tests/JJDevHub.Content.UnitTests/ --configuration Release

# Tylko testy integracyjne (wymagają Docker Desktop!)
dotnet test tests/JJDevHub.Content.IntegrationTests/ --configuration Release

# Wszystkie testy z coverage
dotnet test JJDevHub.sln --collect:"XPlat Code Coverage"
```

**Wymagania:** Docker Desktop musi być uruchomiony dla testów integracyjnych (Testcontainers automatycznie zarządza kontenerami).

---

## 7. CI/CD - Jenkins Pipeline

### Pipeline overview (9 stage'ów)

```
┌──────────┐  ┌──────────┐  ┌─────────────┐  ┌──────────────────┐  ┌──────────────┐
│ Restore  │─▶│  Build   │─▶│ Unit Tests  │─▶│Integration Tests │─▶│ Angular Build │
└──────────┘  └──────────┘  └─────────────┘  └────────┬─────────┘  └──────┬───────┘
                                                       │                   │
              ┌──────────────┐  ┌──────────────┐       │                   │
              │  Docker Build│◀─│ Quality Gate │◀──────┘───────────────────┘
              │  (8 parallel)│  └──────────────┘       ┌──────────────┐
              └──────┬───────┘                         │  SonarQube   │
                     │                                 │  Analysis    │
                     ▼                                 └──────────────┘
              ┌──────────────┐
              │   Deploy     │
              │docker-compose│
              └──────────────┘
```

### Jenkinsfile - kluczowe stage'y

```groovy
pipeline {
    agent any
    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        SONAR_HOST_URL = 'http://jjdevhub-sonarqube:9000'
        SONAR_PROJECT_KEY = 'JJDevHub'
    }

    stages {
        stage('Restore') {
            steps { sh 'dotnet restore JJDevHub.sln' }
        }

        stage('Build') {
            steps { sh 'dotnet build JJDevHub.sln --configuration Release --no-restore' }
        }

        // Testy jednostkowe - szybkie, bez infra
        stage('Unit Tests') {
            steps {
                sh '''dotnet test tests/JJDevHub.Content.UnitTests/*.csproj \
                    --configuration Release --no-build \
                    --logger "trx;LogFileName=unit-test-results.trx" \
                    --collect:"XPlat Code Coverage" \
                    --results-directory ./test-results/unit'''
            }
        }

        // Testy integracyjne - Testcontainers (Docker-in-Docker)
        stage('Integration Tests') {
            steps {
                sh '''dotnet test tests/JJDevHub.Content.IntegrationTests/*.csproj \
                    --configuration Release --no-build \
                    --logger "trx;LogFileName=integration-test-results.trx" \
                    --collect:"XPlat Code Coverage" \
                    --results-directory ./test-results/integration'''
            }
        }

        // SonarQube - analiza kodu + coverage
        stage('SonarQube Analysis') {
            steps {
                withCredentials([string(credentialsId: 'sonarqube-token', variable: 'SONAR_TOKEN')]) {
                    sh '''dotnet-sonarscanner begin \
                        /k:"${SONAR_PROJECT_KEY}" \
                        /d:sonar.host.url="${SONAR_HOST_URL}" \
                        /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml" \
                        /d:sonar.cs.vstest.reportsPaths="**/*.trx"
                    dotnet build JJDevHub.sln --configuration Release
                    dotnet-sonarscanner end'''
                }
            }
        }

        // Quality Gate - blokuje jeśli nie przeszedł
        stage('Quality Gate') {
            steps {
                script {
                    def qg = waitForQualityGate()
                    if (qg.status != 'OK') {
                        error "Quality Gate failed: ${qg.status}"
                    }
                }
            }
        }

        // Angular Build
        stage('Angular Build') {
            steps {
                dir('src/Clients/web') {
                    sh 'npm ci'
                    sh 'npx ng build --configuration production'
                }
            }
        }

        // Docker Build - 8 obrazów równolegle
        stage('Docker Build') {
            parallel {
                stage('Content API')    { steps { sh 'docker build -f src/Services/.../Dockerfile ...' } }
                stage('Analytics API')  { steps { sh 'docker build -f src/Services/.../Dockerfile ...' } }
                stage('Identity API')   { steps { sh 'docker build -f src/Services/.../Dockerfile ...' } }
                stage('AI Gateway')     { steps { sh 'docker build -f src/Services/.../Dockerfile ...' } }
                stage('Notification')   { steps { sh 'docker build -f src/Services/.../Dockerfile ...' } }
                stage('Education API')  { steps { sh 'docker build -f src/Services/.../Dockerfile ...' } }
                stage('Sync Worker')    { steps { sh 'docker build -f src/Services/.../Dockerfile ...' } }
                stage('Angular Web')    { steps { sh 'docker build -f src/Clients/web/Dockerfile ...' } }
            }
        }

        stage('Deploy') {
            steps {
                dir('infra/docker') {
                    sh 'docker-compose up -d --remove-orphans'
                }
            }
        }
    }
}
```

### Konfiguracja Jenkins

1. **Wymagane pluginy:** Pipeline, Docker Pipeline, SonarQube Scanner
2. **Credentials:** Dodaj `sonarqube-token` w Jenkins > Manage Credentials
3. **SonarQube webhook:** W SonarQube > Administration > Webhooks dodaj `http://jjdevhub-jenkins:8080/sonarqube-webhook/`
4. **Docker socket:** Jenkins musi mieć dostęp do Docker socket

---

## 8. SonarQube - jakość kodu

### Dostęp

- **URL:** http://localhost:9000
- **Login:** admin / admin (zmień po pierwszym logowaniu!)

### Co analizuje

| Metryka | Opis |
|---------|------|
| **Bugs** | Potencjalne błędy w kodzie |
| **Vulnerabilities** | Luki bezpieczeństwa |
| **Code Smells** | Problemy z maintainability |
| **Coverage** | Pokrycie testami (z coverlet) |
| **Duplications** | Duplikacja kodu |
| **Security Hotspots** | Kod wymagający review bezpieczeństwa |

### Konfiguracja (`sonar-project.properties`)

```properties
sonar.projectKey=JJDevHub
sonar.sources=src/
sonar.tests=tests/

# Co ignorować
sonar.exclusions=**/wwwroot/**,**/node_modules/**,**/bin/**,**/obj/**
sonar.coverage.exclusions=**/Migrations/**,**/Program.cs,**/DependencyInjection.cs

# Importowanie wyników testów
sonar.cs.opencover.reportsPaths=**/coverage.opencover.xml
sonar.cs.vstest.reportsPaths=**/*.trx
```

### Quality Gate

Domyślny SonarQube Quality Gate ("Sonar way"):
- Coverage nowych linii > 80%
- Duplikacja nowych linii < 3%
- Maintainability rating = A
- Reliability rating = A
- Security rating = A

Jeśli Quality Gate nie przejdzie, Jenkins pipeline się **zatrzymuje**.

### Pierwsze uruchomienie

```bash
# 1. Uruchom SonarQube
cd infra/docker
docker-compose up -d sonarqube sonarqube-db

# 2. Poczekaj ~2 min na start, potem otwórz http://localhost:9000

# 3. Wygeneruj token: My Account > Security > Generate Token

# 4. Ręczna analiza (bez Jenkins):
dotnet tool install --global dotnet-sonarscanner
dotnet-sonarscanner begin /k:"JJDevHub" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="YOUR_TOKEN"
dotnet build JJDevHub.sln
dotnet test JJDevHub.sln --collect:"XPlat Code Coverage"
dotnet-sonarscanner end /d:sonar.token="YOUR_TOKEN"
```

---

## 9. Monitoring - Prometheus + Grafana

### Prometheus

**URL:** http://localhost:9090

Prometheus **scrape'uje** metryki z aplikacji co 10 sekund:

```yaml
# monitoring/prometheus/prometheus.yml
scrape_configs:
  - job_name: 'jjdevhub-content-api'
    metrics_path: '/metrics'
    scrape_interval: 10s
    static_configs:
      - targets: ['jjdevhub-content-api:8080']
```

### Metryki z ASP.NET Core (OpenTelemetry)

Konfiguracja w `Program.cs`:

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("JJDevHub.Content.Api"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()   // HTTP request metrics
        .AddRuntimeInstrumentation()       // .NET runtime metrics (GC, threads)
        .AddPrometheusExporter());         // Expose /metrics endpoint

app.MapPrometheusScrapingEndpoint();       // GET /metrics
```

**Dostępne metryki:**

| Metryka | Opis |
|---------|------|
| `http_server_request_duration_seconds` | Histogram czasu odpowiedzi HTTP |
| `http_server_active_requests` | Aktywne requesty |
| `process_working_set_bytes` | Pamięć procesu |
| `dotnet_gc_collections_total` | Kolekcje GC per generacja |
| `dotnet_thread_pool_thread_count` | Wątki w thread pool |

### Grafana

**URL:** http://localhost:3000
**Login:** admin / admin

#### Auto-provisioning

Grafana automatycznie konfiguruje:
1. **Datasource:** Prometheus (z `provisioning/datasources/datasources.yml`)
2. **Dashboard:** Content API (z `dashboards/content-api.json`)

#### Dashboard "JJDevHub Content API"

Panele:
- **HTTP Request Rate** - req/s per endpoint
- **HTTP Request Duration (p95)** - 95. percentyl latency
- **Active HTTP Connections** - aktywne połączenia
- **Error Rate (5xx)** - rate błędów serwera
- **Process Memory** - zużycie pamięci
- **GC Collections** - kolekcje garbage collectora per generacja

#### Przykładowe zapytania PromQL

```promql
# Request rate per endpoint
rate(http_server_request_duration_seconds_count{service="content-api"}[5m])

# 95th percentile latency
histogram_quantile(0.95,
  rate(http_server_request_duration_seconds_bucket{service="content-api"}[5m]))

# Error rate (5xx)
sum(rate(http_server_request_duration_seconds_count{
  service="content-api",
  http_response_status_code=~"5.."}[5m]))

# Memory usage in MB
process_working_set_bytes{service="content-api"} / 1024 / 1024
```

### Health Checks

```
GET /health
```

Sprawdza:
- **PostgreSQL** (tag: `db`, `write`) - czy write DB odpowiada
- **MongoDB** (tag: `db`, `read`) - czy read DB odpowiada

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql", tags: ["db", "write"])
    .AddMongoDb(sp => new MongoClient(mongoUri), name: "mongodb", tags: ["db", "read"]);
```

Odpowiedź:
```json
{
  "status": "Healthy",
  "entries": {
    "postgresql": { "status": "Healthy", "duration": "00:00:00.023" },
    "mongodb": { "status": "Healthy", "duration": "00:00:00.011" }
  }
}
```

---

## 10. Docker i infrastruktura

### docker-compose.yml - pełna mapa serwisów

**Infrastruktura:**

| Serwis | Obraz | Port host | Port kontener | Opis |
|--------|-------|-----------|---------------|------|
| nginx | nginx:alpine | 8081, 444 | 80, 443 | Reverse proxy (routing do serwisów) |
| vault | hashicorp/vault | 8201 | 8200 | Secrets management |
| jenkins | jenkins/jenkins:lts-jdk17 | 8082, 50001 | 8080, 50000 | CI/CD |
| db | postgres:16-alpine | 5433 | 5432 | Write DB (PostgreSQL) |
| mongodb | mongo:latest | 27018 | 27017 | Read DB (MongoDB) |
| zookeeper | confluentinc/cp-zookeeper | - | 2181 | Kafka coordination |
| kafka | confluentinc/cp-kafka | 9092, 29092 | 9092, 29092 | Message broker (Docker: `kafka:9092`; host: `localhost:29092`) |
| sonarqube-db | postgres:16-alpine | - | 5432 | SonarQube DB |
| sonarqube | sonarqube:lts-community | 9000 | 9000 | Code quality |
| prometheus | prom/prometheus | 9090 | 9090 | Metrics scraping |
| grafana | grafana/grafana | 3000 | 3000 | Dashboards |
| jaeger | jaegertracing/all-in-one | 16686, 4317 | 16686, 4317 | Distributed tracing (OTLP gRPC) |
| keycloak | quay.io/keycloak/keycloak | 8083 | 8080 | IAM (OIDC, JWT, RBAC) |

**Serwisy aplikacyjne (budowane z Dockerfile):**

| Serwis | Dockerfile | Port | Opis |
|--------|-----------|------|------|
| angular-web | src/Clients/web/Dockerfile | 80 | Frontend Angular + Material |
| content-api | src/Services/JJDevHub.Content/.../Dockerfile | 8080 | Główny serwis CRUD (DDD/CQRS) |
| analytics-api | src/Services/JJDevHub.Analytics/.../Dockerfile | 8080 | Serwis analityczny |
| identity-api | src/Services/JJDevHub.Identity/Dockerfile | 8080 | Autentykacja i autoryzacja |
| ai-gateway | src/Services/JJDevHub.AI.Gateway/.../Dockerfile | 8080 | Brama AI |
| notification-api | src/Services/JJDevHub.Notification/Dockerfile | 8080 | Powiadomienia |
| education-api | src/Services/JJDevHub.Education/Dockerfile | 8080 | Edukacja |
| sync-worker | src/Services/JJDevHub.Sync/Dockerfile | - | Background worker (Kafka consumer) |

Każdy serwis poza Content API ma endpoint `/health` zwracający status i nazwę serwisu. Content API ma pełne health checks (PostgreSQL + MongoDB).

### Dockerfile Content API (multi-stage)

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Najpierw kopiujemy TYLKO csproj (layer caching!)
COPY ["src/Services/.../JJDevHub.Content.Api.csproj", "..."]
RUN dotnet restore
# Potem kod źródłowy
COPY src/Services/ src/Services/
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime (mniejszy obraz)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JJDevHub.Content.Api.dll"]
```

**Layer caching:** Kopiowanie `csproj` osobno od kodu powoduje, że `dotnet restore` jest cache'owany dopóki nie zmienisz zależności. Rebuild po zmianie kodu jest szybki.

### Volumes (dane trwałe)

```yaml
volumes:
  jenkins_home:        # Konfiguracja Jenkins
  postgres_data:       # Dane PostgreSQL
  mongo_data:          # Dane MongoDB
  sonarqube_db_data:   # Dane SonarQube DB
  sonarqube_data:      # Dane SonarQube
  prometheus_data:     # Dane metryk (30 dni retencji)
  grafana_data:        # Dashboardy, konfiguracja Grafana
```

### Networking

Wszystkie serwisy w jednej sieci Docker bridge `jjdevhub-net`. Komunikują się po nazwach kontenerów (DNS Docker).

### Nginx routing

Nginx routuje requesty na podstawie path prefix do odpowiedniego serwisu:

```nginx
location /api/v1/content/   { proxy_pass http://jjdevhub-content-api:8080;      }
location /api/content/      { rewrite ^/api/content/(.*)$ /api/v1/content/$1 last; } # legacy
location /api/analytics/    { proxy_pass http://jjdevhub-analytics-api:8080;    }
location /api/identity/     { proxy_pass http://jjdevhub-identity-api:8080;     }
location /api/ai/           { proxy_pass http://jjdevhub-ai-gateway:8080;       }
location /api/notification/ { proxy_pass http://jjdevhub-notification-api:8080; }
location /api/education/    { proxy_pass http://jjdevhub-education-api:8080;    }
location /auth/             { proxy_pass http://jjdevhub-keycloak:8080;         }
location /jenkins/          { proxy_pass http://jjdevhub-jenkins:8080;          }
location /                  { proxy_pass http://jjdevhub-angular:80;            } # catch-all
```

---

## 11. Jak uruchomić projekt

### Wymagania

- .NET 10 SDK
- Docker Desktop
- Node.js 20+ (dla frontendu)
- Git

### Quick start (development)

```bash
# 1. Sklonuj repo
git clone <repo-url>
cd JJDevHub

# 2. Uruchom infrastrukturę (bazy + message broker)
cd infra/docker
docker-compose up -d db mongodb kafka zookeeper

# 3. Uruchom backend (z katalogu głównego)
cd ../..
dotnet run --project src/Services/JJDevHub.Content/JJDevHub.Content.Api

# 4. Uruchom frontend Angular
cd src/Clients/web
npm install
npm start
# Otwórz http://localhost:4200

# 5. Uruchom React Native (wymaga Android Studio lub Xcode)
cd src/Clients/mobile/JJDevHubMobile
npm install
npm run android  # lub: npm run ios
```

### Pełny stack (wszystko w Docker)

```bash
cd infra/docker
docker-compose up -d --build
```

Buduje i uruchamia **wszystkie** serwisy (8 backendów + Angular + infra). Potem otwórz:

| URL | Serwis |
|-----|--------|
| http://localhost:8081 | Aplikacja (Angular przez Nginx) |
| http://localhost:8081/api/v1/content/work-experiences | Content API |
| http://localhost:9000 | SonarQube |
| http://localhost:3000 | Grafana |
| http://localhost:9090 | Prometheus |
| http://localhost:8082 | Jenkins |
| http://localhost:8201 | Vault |
| http://localhost:8083 | Keycloak (admin/admin) |
| http://localhost:16686 | Jaeger |

### Uruchamianie testów

```bash
# Testy jednostkowe (szybkie, bez Docker)
dotnet test tests/JJDevHub.Content.UnitTests/

# Testy integracyjne (wymagają Docker Desktop!)
dotnet test tests/JJDevHub.Content.IntegrationTests/

# Wszystko z coverage
dotnet test JJDevHub.sln --collect:"XPlat Code Coverage"
```

### Konfiguracja (`appsettings.json`)

Poniższe wartości dotyczą uruchamiania **z hosta** (`dotnet run`), gdzie porty są mapowane na localhost. Dla Kafka na hoście użyj `localhost:29092` (listener `PLAINTEXT_HOST`); samo `localhost:9092` zwraca w metadanych adres `kafka:9092`, niewidoczny spoza sieci Docker. Gdy API działa **wewnątrz Docker** (docker-compose), adresy to nazwy serwisów w sieci Docker (np. `jjdevhub-db:5432`, `jjdevhub-mongo:27017`, `kafka:9092`), ustawiane przez zmienne środowiskowe w `docker-compose.yml`.

```json
{
  "ConnectionStrings": {
    "ContentDb": "Host=localhost;Port=5433;Database=jjdevhub_content;Username=postgres;Password=password"
  },
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27018",
    "DatabaseName": "jjdevhub_content_read"
  },
  "Kafka": {
    "BootstrapServers": "localhost:29092"
  }
}
```

### Przydatne komendy

```bash
# Build całego solution
dotnet build JJDevHub.sln

# Restore pakietów
dotnet restore JJDevHub.sln

# Dodanie migracji EF Core
dotnet ef migrations add InitialCreate \
  --project src/Services/JJDevHub.Content/JJDevHub.Content.Persistence \
  --startup-project src/Services/JJDevHub.Content/JJDevHub.Content.Api

# Wykonanie migracji
dotnet ef database update \
  --project src/Services/JJDevHub.Content/JJDevHub.Content.Persistence \
  --startup-project src/Services/JJDevHub.Content/JJDevHub.Content.Api

# Logi kontenerów
docker-compose -f infra/docker/docker-compose.yml logs -f content-api
docker-compose -f infra/docker/docker-compose.yml logs -f analytics-api
docker-compose -f infra/docker/docker-compose.yml logs -f angular-web

# Restart konkretnego serwisu
docker-compose -f infra/docker/docker-compose.yml restart grafana

# Health check serwisów (po uruchomieniu Docker)
curl http://localhost:8081/api/v1/content/health
curl http://localhost:8081/api/analytics/health
curl http://localhost:8081/api/identity/health
```

---

## Materiały dodatkowe

### Książki
- **"Domain-Driven Design"** - Eric Evans (Blue Book)
- **"Implementing Domain-Driven Design"** - Vaughn Vernon (Red Book)
- **"Clean Architecture"** - Robert C. Martin

### Wzorce użyte w projekcie

| Wzorzec | Gdzie |
|---------|-------|
| Aggregate Root | `WorkExperience` |
| Value Object | `DateRange` |
| Repository | `IWorkExperienceRepository` / `WorkExperienceRepository` |
| Unit of Work | `ContentDbContext : IUnitOfWork` |
| CQRS | Command/Query handlers + separate databases |
| Mediator | MediatR (`IMediator.Send()`) |
| Pipeline Behavior | `ValidationBehavior` |
| Factory Method | `WorkExperience.Create()` |
| Domain Events | `IDomainEvent` + MediatR `INotification` |
| Integration Events | `IntegrationEvent` + Kafka |
| Soft Delete | `AuditableEntity.Deactivate()` + global query filter |
| Template Method | `AuditableEntity` (bazowe pola audytowe) |
| Dependency Injection | Extension methods `AddApplication()`, `AddPersistence()`, `AddInfrastructure()` |
| Strategy | `IEventBus` (Kafka impl, ale mógłby być RabbitMQ) |
| Reverse Proxy | Nginx routing do serwisów po path prefix |
