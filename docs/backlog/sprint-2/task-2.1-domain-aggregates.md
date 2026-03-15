# Task 2.1: Implementacja Agregatow Domenowych

| Pole | Wartosc |
|------|---------|
| Sprint | 2 - The Core & Write Side |
| Status | IN PROGRESS |
| Priorytet | High |
| Estymacja | 13 story points |
| Powiazane pliki | `src/Services/JJDevHub.Content/JJDevHub.Content.Core/Entities/`, `src/Services/JJDevHub.Content/JJDevHub.Content.Core/ValueObjects/`, `src/Services/JJDevHub.Content/JJDevHub.Content.Core/Events/` |

## Opis

Implementacja agregatow domenowych stanowi serce logiki biznesowej. Kazdy agregat enkapsuluje reguly biznesowe, walidacje i emituje domain events przy zmianach stanu. Zadanie obejmuje istniejacy agregat `WorkExperience` (DONE) oraz nowy agregat `CurriculumVitae` (TODO) - centralny element ukrytego modulu CV Manager.

### Co juz jest zrobione (WorkExperience)

**Agregat `WorkExperience`** (dziedziczy `AuditableAggregateRoot`):
- Wlasciwosci: `CompanyName`, `Position`, `Period` (DateRange), `IsPublic`
- Factory Method: `Create(companyName, position, startDate, endDate, isPublic)` z walidacja
- Metody: `Update(...)`, `MarkAsDeleted()`, `Publish()`, `Hide()`
- Walidacja: CompanyName i Position nie puste, max 200 znakow
- Domain Events: `WorkExperienceCreatedDomainEvent`, `WorkExperienceUpdatedDomainEvent`, `WorkExperienceDeletedDomainEvent`

**Value Object `DateRange`:**
- Wlasciwosci: `Start`, `End` (nullable = "do teraz")
- Computed: `IsCurrent` (gdy End is null), `DurationInMonths`
- Walidacja: End >= Start
- Equality przez Start/End

**Repozytorium `IWorkExperienceRepository`:**
- Rozszerza `IRepository<WorkExperience>`
- Metody: `GetByIdAsync`, `GetAllAsync`, `GetPublicAsync`, `AddAsync`, `Update`, `Delete`

### Co pozostalo (CurriculumVitae)

Nowy agregat `CurriculumVitae` obslugujacy kompletne CV:

- **Skills** (lista umiejetnosci z kategoriami)
- **Education** (wyksztalcenie)
- **Projects** (projekty portfolio)
- **PersonalInfo** (dane kontaktowe, bio)
- **Powiazanie z WorkExperience** przez referencje Id

## Kryteria akceptacji

- [x] Agregat `WorkExperience` z Factory Method, walidacja, domain events
- [x] Value Object `DateRange` z IsCurrent i DurationInMonths
- [x] `IWorkExperienceRepository` z pelnym CRUD
- [ ] Agregat `CurriculumVitae` z encjami potomnymi (Skill, Education, Project)
- [ ] Value Objects: `PersonalInfo`, `SkillLevel`, `EducationDegree`
- [ ] Domain Events dla CurriculumVitae: Created, Updated, SkillAdded, EducationAdded
- [ ] `ICurriculumVitaeRepository` z metodami CRUD
- [ ] Testy jednostkowe dla nowego agregatu (analogiczne do WorkExperience tests)

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| - | - | JJDevHub.Content.Core | Core nie ma zaleznosci NuGet (tylko ProjectReference do Shared.Kernel) |

## Kroki implementacji

1. **Zdefiniuj Value Objects dla CurriculumVitae:**
   - `PersonalInfo` - imie, nazwisko, email, telefon, lokalizacja, bio
   - `SkillLevel` - enum: Beginner, Intermediate, Advanced, Expert
   - `EducationDegree` - enum: BSc, MSc, PhD, Certificate

2. **Zdefiniuj encje potomne:**
   - `Skill` (Entity) - nazwa, kategoria (Backend, Frontend, DevOps, etc.), poziom (SkillLevel)
   - `Education` (Entity) - uczelnia, kierunek, stopien (EducationDegree), DateRange
   - `Project` (Entity) - nazwa, opis, URL, lista technologii, DateRange

3. **Zaimplementuj agregat `CurriculumVitae`:**
   - Dziedziczy `AuditableAggregateRoot`
   - Kolekcje: `Skills`, `Educations`, `Projects`
   - Referencja: `List<Guid> WorkExperienceIds`
   - Value Object: `PersonalInfo`
   - Factory Method: `Create(personalInfo)`
   - Metody: `AddSkill(...)`, `RemoveSkill(...)`, `AddEducation(...)`, `AddProject(...)`, `LinkWorkExperience(Guid)`
   - Domain Events per zmiana stanu

4. **Stworz repozytorium:**
   - `ICurriculumVitaeRepository` w Core
   - Implementacja w Persistence (Task 2.2 rozszerzenie)

5. **Napisz testy jednostkowe:**
   - Testy Factory Method z poprawnymi/niepoprawnymi danymi
   - Testy metod domenowych (AddSkill, RemoveSkill, etc.)
   - Testy walidacji Value Objects
   - Testy emitowania domain events

## Zaleznosci

- **Wymaga:** Task 1.3 (Shared.Kernel - bazowe klasy DDD)
- **Blokuje:** Task 5.2 (Application Tracker), Task 5.3 (CV Generation Engine)

## Notatki techniczne

- `CurriculumVitae` jest agregatem z encjami potomnymi (Skill, Education, Project). Encje potomne nie maja wlasnych repozytoriow - sa ladowane i zapisywane przez agregat root.
- Referencja do `WorkExperience` jest przez Id (nie przez nawigacyjna wlasciwosc) - to sa oddzielne agregaty. Laczenie danych odbywa sie na poziomie read model w MongoDB.
- Factory Method `Create()` zapewnia, ze agregat nigdy nie powstaje w niepoprawnym stanie (Invariant Protection Pattern).
- Kazda zmiana stanu agregatu emituje domain event, ktory jest potem przetwarzany przez MediatR handlers (Task 2.3).
