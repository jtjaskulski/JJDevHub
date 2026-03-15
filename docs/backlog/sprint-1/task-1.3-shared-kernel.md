# Task 1.3: Setup Shared.Kernel - Bazowe Klasy DDD

| Pole | Wartosc |
|------|---------|
| Sprint | 1 - Identity & Foundation |
| Status | DONE |
| Priorytet | High |
| Estymacja | 5 story points |
| Powiazane pliki | `src/Services/JJDevHub.Shared.Kernel/` |

## Opis

Shared.Kernel to wspolna biblioteka zawierajaca bazowe abstrakcje DDD (Domain-Driven Design), interfejsy CQRS oraz kontrakty messagingu. Wszystkie bounded contexty (Content, Analytics, etc.) odwoluja sie do tego projektu, co zapewnia spojnosc wzorcow architektonicznych w calym systemie.

### Co jest zaimplementowane

Pelny zestaw building blocks DDD:

**BuildingBlocks:**
- `Entity` - bazowa klasa z `Guid Id`, equality po Id i typie
- `AuditableEntity` - rozszerza Entity o `IsActive`, `CreatedDate`, `ModifiedDate`, metody `Deactivate()`, `Activate()`, `MarkModified()`
- `AggregateRoot` - rozszerza Entity o kolekcje `DomainEvents`, metody `AddDomainEvent()`, `ClearDomainEvents()`
- `AuditableAggregateRoot` - polaczenie AuditableEntity + IAggregateRoot
- `IAggregateRoot` - interfejs marker z `DomainEvents` i `ClearDomainEvents()`
- `ValueObject` - bazowa klasa z equality przez `GetEqualityComponents()`
- `IRepository<T>` - generyczny interfejs repozytorium z `UnitOfWork`
- `IUnitOfWork` - interfejs z `SaveChangesAsync(CancellationToken)`
- `IDomainEvent` - interfejs z `Id`, `OccurredOn`, rozszerza MediatR `INotification`
- `DomainEventBase` - bazowy record implementujacy IDomainEvent

**CQRS:**
- `ICommand<TResponse>` - rozszerza `IRequest<TResponse>`
- `ICommandHandler<TCommand, TResponse>` - handler komend
- `IQuery<TResponse>` - rozszerza `IRequest<TResponse>`
- `IQueryHandler<TQuery, TResponse>` - handler zapytan

**Messaging:**
- `IEventBus` - interfejs z `PublishAsync<T>(T integrationEvent)`
- `IntegrationEvent` - bazowy record z `Id`, `OccurredOn`

**Exceptions:**
- `DomainException` - bazowy wyjatek domenowy

## Kryteria akceptacji

- [x] Entity z Guid Id i equality
- [x] AggregateRoot z kolekcja domain events
- [x] AuditableEntity z soft delete i audit fields
- [x] ValueObject z structural equality
- [x] IRepository i IUnitOfWork
- [x] IDomainEvent rozszerzajacy MediatR INotification
- [x] CQRS interfaces (ICommand, IQuery, handlery)
- [x] IEventBus i IntegrationEvent dla messagingu
- [x] DomainException jako bazowy wyjatek

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `MediatR` | 14.0.0 | JJDevHub.Shared.Kernel | IDomainEvent rozszerza INotification, CQRS interfejsy rozszerzaja IRequest |

## Architektura klas

```
Entity (Guid Id)
├── AggregateRoot (+ DomainEvents)
└── AuditableEntity (+ IsActive, CreatedDate, ModifiedDate)
    └── AuditableAggregateRoot (+ DomainEvents)

ValueObject (structural equality)

IDomainEvent : INotification
└── DomainEventBase (record)

IntegrationEvent (record)

ICommand<T> : IRequest<T>
IQuery<T> : IRequest<T>
```

## Zaleznosci

- **Wymaga:** Nic (to jest fundament)
- **Blokuje:** Task 2.1 (agregaty domenowe), Task 2.3 (command handlers), Task 3.2 (event bus)

## Notatki techniczne

- Projekt uzywa `record` dla eventow (DomainEventBase, IntegrationEvent) co daje immutability i value-based equality.
- `IAggregateRoot` jako interfejs marker pozwala na ograniczenie generycznego `IRepository<T>` tylko do agregatow.
- Separacja `Entity` i `AuditableEntity` pozwala na tworzenie encji bez audit fields (np. lookup tables).
- MediatR jest jedynym pakietem NuGet - minimalna lista zaleznosci dla kernel.
