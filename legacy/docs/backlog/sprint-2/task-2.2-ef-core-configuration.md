# Task 2.2: Konfiguracja EF Core z PostgreSQL (Fluent API)

| Pole | Wartosc |
|------|---------|
| Sprint | 2 - The Core & Write Side |
| Status | DONE |
| Priorytet | High |
| Estymacja | 5 story points |
| Powiazane pliki | `src/Services/JJDevHub.Content/JJDevHub.Content.Persistence/` |

## Opis

Entity Framework Core z providerem Npgsql sluzy jako ORM dla write store (PostgreSQL). Konfiguracja uzywa Fluent API (nie Data Annotations) co pozwala na pelna separacje domeny od infrastruktury - klasy domenowe nie maja atrybutow EF Core. Obejmuje rowniez mapowanie Value Objects (np. `DateRange` jako Owned Type) i automatyczna dyspozycje domain events po `SaveChangesAsync`.

### Co jest zaimplementowane

**ContentDbContext:**
- `DbSet<WorkExperience>` z pelna konfiguracja Fluent API
- Nadpisany `SaveChangesAsync` z automatyczna dyspozycja domain events przez MediatR
- Audit fields (CreatedDate, ModifiedDate) ustawiane automatycznie

**WorkExperience Configuration (Fluent API):**
- Tabela: `work_experiences`
- CompanyName: `VARCHAR(200)`, required
- Position: `VARCHAR(200)`, required
- Period (DateRange): Owned Type z mapowaniem `Start` i `End`
- IsPublic, IsActive: boolean columns
- Global Query Filter: `IsActive == true` (soft delete)

**WorkExperienceRepository:**
- Implementuje `IWorkExperienceRepository`
- CRUD nad `ContentDbContext`
- `Delete` uzywa soft delete przez `MarkAsDeleted()`

**DependencyInjection:**
- Rejestracja `ContentDbContext` z Npgsql provider
- Rejestracja `IWorkExperienceRepository` jako Scoped
- Rejestracja `IUnitOfWork` -> `ContentDbContext`

## Kryteria akceptacji

- [x] ContentDbContext z SaveChangesAsync dispatching domain events
- [x] Fluent API configuration dla WorkExperience (brak Data Annotations w domenie)
- [x] DateRange jako Owned Type (Value Object mapping)
- [x] Global Query Filter dla soft delete
- [x] Audit fields automatycznie ustawiane
- [x] WorkExperienceRepository implementujacy IWorkExperienceRepository
- [x] DependencyInjection z Npgsql provider

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `Microsoft.EntityFrameworkCore` | 10.0.3 | JJDevHub.Content.Persistence | Bazowy ORM framework |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.0 | JJDevHub.Content.Persistence | Provider EF Core dla PostgreSQL |

## Architektura persystencji

```
Content.Api (Program.cs)
    │
    ├── AddPersistence(configuration)
    │       │
    │       ├── ContentDbContext (EF Core)
    │       │       ├── DbSet<WorkExperience>
    │       │       ├── SaveChangesAsync → dispatch domain events
    │       │       └── OnModelCreating → Fluent API configs
    │       │
    │       ├── IWorkExperienceRepository → WorkExperienceRepository
    │       └── IUnitOfWork → ContentDbContext
    │
    └── Connection String: "Host=jjdevhub-db;Port=5432;Database=jjdevhub_content;..."
```

## Zaleznosci

- **Wymaga:** Task 1.3 (Shared.Kernel - IRepository, IUnitOfWork), Task 1.4 (PostgreSQL w docker-compose)
- **Blokuje:** Task 2.3 (Command Handlers uzywaja repozytorium)

## Notatki techniczne

- Fluent API zamiast Data Annotations = domena nie zna EF Core. Klasy w `Core` nie maja `[Table]`, `[Column]`, `[Required]` etc.
- Owned Types mapuja Value Objects (DateRange) na kolumny w tej samej tabeli - nie tworza oddzielnej tabeli.
- Global Query Filter `e.IsActive` automatycznie filtruje soft-deleted rekordy we wszystkich zapytaniach. Aby odczytac usuniety rekord, trzeba uzyc `IgnoreQueryFilters()`.
- Domain events sa dispatchowane w `SaveChangesAsync` PRZED commitem transakcji. Jesli handler rzuci wyjatek, cala operacja jest rollbackowana.
- Na przyszlosc: rozszerzenie o `CurriculumVitaeConfiguration` z nested Owned Types dla kolekcji (Skill, Education, Project).
