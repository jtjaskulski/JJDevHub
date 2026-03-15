# Task 5.2: Serwis ApplicationTracker - Modul Sledzenia Aplikacji

| Pole | Wartosc |
|------|---------|
| Sprint | 5 - The Secret Feature |
| Status | TODO |
| Priorytet | Medium |
| Estymacja | 13 story points |
| Powiazane pliki | Nowe pliki do stworzenia |

## Opis

ApplicationTracker to ukryty modul dostepny tylko dla roli `Owner`. Pozwala na dodawanie wymagan konkretnych firm (np. "Intel - Gdansk", "Google - Warszawa"), sledzenie statusu aplikacji (Applied, Interview, Offer, Rejected) oraz laczenie wymagan firmy z posiadanymi umiejetnosciami i doswiadczeniem. Dane te sa potem uzywane przez CV Generation Engine (Task 5.3) do generowania spersonalizowanych CV.

### Co trzeba zrobic

1. Nowy bounded context `ApplicationTracker` lub rozszerzenie `Content` context
2. Agregat `JobApplication` z encjami potomnymi
3. CRUD endpointy API (chronione rola Owner)
4. Angular strona administracyjna

## Kryteria akceptacji

- [ ] Agregat `JobApplication` z walidacja domenowa
- [ ] Encje: `CompanyRequirement`, `ApplicationNote`, `InterviewStage`
- [ ] Value Objects: `CompanyInfo` (nazwa, lokalizacja, URL), `ApplicationStatus` (enum)
- [ ] CRUD Command/Query Handlers z MediatR
- [ ] API endpointy: `/api/content/applications` (chronione Owner)
- [ ] Persystencja w PostgreSQL (EF Core, nowe tabele)
- [ ] Read model w MongoDB (synchronizacja przez Kafka)
- [ ] Angular strona: lista aplikacji, formularz dodawania, widok szczegolowy
- [ ] Filtrowanie po statusie, firmie, dacie
- [ ] Dashboard ze statystykami: ile Applied, Interview, Offer, Rejected

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| - | - | - | Wykorzystuje istniejace pakiety z Content bounded context (MediatR, EF Core, FluentValidation, Kafka, MongoDB) |

## Model domenowy

```
JobApplication (AuditableAggregateRoot)
├── CompanyInfo (ValueObject)
│   ├── CompanyName: string (required, max 200)
│   ├── Location: string (optional, e.g. "Gdansk")
│   ├── WebsiteUrl: string (optional)
│   └── Industry: string (optional)
├── Position: string (required, max 200)
├── Status: ApplicationStatus (ValueObject/Enum)
│   └── Draft | Applied | PhoneScreen | TechnicalInterview
│       | OnSite | Offer | Accepted | Rejected | Withdrawn
├── AppliedDate: DateOnly
├── Requirements: List<CompanyRequirement> (Entity)
│   ├── Description: string
│   ├── Category: RequirementCategory (Backend, Frontend, DevOps, Soft)
│   ├── Priority: RequirementPriority (MustHave, NiceToHave)
│   └── IsMet: bool (czy spelniasz to wymaganie)
├── Notes: List<ApplicationNote> (Entity)
│   ├── Content: string
│   ├── CreatedAt: DateTime
│   └── NoteType: NoteType (General, Interview, Feedback)
└── InterviewStages: List<InterviewStage> (Entity)
    ├── StageName: string (e.g. "Phone Screen", "System Design")
    ├── ScheduledAt: DateTime
    ├── Status: StageStatus (Scheduled, Completed, Cancelled)
    └── Feedback: string (optional)
```

## Kroki implementacji

1. **Zdecyduj o umiejscowieniu w architekturze:**
   - Opcja A: Rozszerzenie `Content` bounded context (prostsze, wspoldzielony DbContext)
   - Opcja B: Nowy bounded context `JJDevHub.ApplicationTracker` (czystsza separacja, wiecej kodu)
   - Rekomendacja: Opcja A (wspolny Content context) bo ApplicationTracker bezposrednio odwoluje sie do WorkExperience i Skills

2. **Zdefiniuj model domenowy w Content.Core:**
   - `Entities/JobApplication.cs` (AuditableAggregateRoot)
   - `Entities/CompanyRequirement.cs` (Entity)
   - `Entities/ApplicationNote.cs` (Entity)
   - `Entities/InterviewStage.cs` (Entity)
   - `ValueObjects/CompanyInfo.cs`
   - `ValueObjects/ApplicationStatus.cs`
   - `Repositories/IJobApplicationRepository.cs`

3. **Dodaj EF Core konfiguracje w Persistence:**
   - `Configurations/JobApplicationConfiguration.cs` (Fluent API)
   - Rozszerz `ContentDbContext` o `DbSet<JobApplication>`
   - Migracja: `dotnet ef migrations add AddJobApplications`

4. **Zaimplementuj Application layer:**
   - Commands: `CreateJobApplication`, `UpdateJobApplication`, `AddRequirement`, `AddNote`, `UpdateStatus`, `AddInterviewStage`
   - Queries: `GetAllApplications`, `GetApplicationById`, `GetApplicationsByStatus`
   - Validators per command
   - Domain Event Handlers -> Integration Events -> Kafka -> MongoDB

5. **Dodaj API endpointy:**
   - `JobApplicationEndpoints.cs` - grupa `/api/content/applications`
   - Wszystkie endpointy: `.RequireAuthorization("OwnerOnly")`

6. **Zbuduj Angular UI:**
   - `AdminModule/ApplicationTracker/` z komponentami:
     - `ApplicationListComponent` - tabela z sortowaniem i filtrowaniem
     - `ApplicationFormComponent` - formularz dodawania/edycji
     - `ApplicationDetailComponent` - widok szczegolowy z timeline
     - `ApplicationDashboardComponent` - statystyki (pie chart statusow)

## Zaleznosci

- **Wymaga:** Task 1.1 (Keycloak - rola Owner), Task 2.1 (DDD patterns), Task 5.1 (Angular RBAC)
- **Blokuje:** Task 5.3 (CV Generation korzysta z wymagan firmy)

## Notatki techniczne

- `JobApplication` jest oddzielnym agregatem od `WorkExperience` i `CurriculumVitae`. Laczenie danych (ktore doswiadczenia pasuja do wymagan firmy) odbywa sie na poziomie Application layer lub read model w MongoDB.
- `ApplicationStatus` jako enum z przejsciami (state machine pattern) - nie kazde przejscie jest dozwolone (np. nie mozna przejsc z `Rejected` do `Applied`).
- Wymagania firmy (`CompanyRequirement`) z flaga `IsMet` pozwalaja na automatyczne obliczanie "match score" - procent wymagan spelnionych.
- Wszystkie endpointy wymagaja roli `Owner` - to jest calkowicie prywatny modul.
- Angular Material Table z `MatSort` i `MatPaginator` zapewnia sortowanie i paginacje po stronie klienta.
