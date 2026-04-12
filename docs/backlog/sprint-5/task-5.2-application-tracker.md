# Task 5.2: Serwis ApplicationTracker - Modul Sledzenia Aplikacji

| Pole | Wartosc |
|------|---------|
| Sprint | 5 - The Secret Feature |
| Status | DONE |
| Priorytet | Medium |
| Estymacja | 13 story points |
| Powiazane pliki | `JJDevHub.Content.Core` (JobApplication i encje), `JJDevHub.Content.Persistence`, `JJDevHub.Content.Application`, `JobApplicationEndpoints.cs`, `JobApplicationReadStore`, `admin-tracker.page.*`, `KafkaConsumerService` (topiki aplikacji) |

## Opis

ApplicationTracker to modul dostepny dla roli `Owner`. Pozwala na sledzenie aplikacji o prace (firma, stan, wymagania, notatki, etapy rozmow). Zapis po stronie write (PostgreSQL + EF), read model w MongoDB (Kafka), API wersjonowane pod `/api/v1/content/applications`.

## Kryteria akceptacji

- [x] Agregat `JobApplication` z walidacja domenowa
- [x] Encje: wymagania, notatki, etapy rozmow (zgodnie z modelem w kodzie)
- [x] Value Objects / enumy: `CompanyInfo`, `ApplicationStatus`, kategorie wymagan itd.
- [x] Command/Query Handlers z MediatR
- [x] API: `/api/v1/content/applications` (OwnerOnly), podsciezki requirements / notes / interview-stages, dashboard `/dashboard`
- [x] Persystencja PostgreSQL (EF Core, migracja `AddJobApplications`)
- [x] Read model MongoDB + synchronizacja Kafka
- [x] Angular: `admin/tracker` — lista, formularz, szczegoly, filtry, dashboard statystyk

## Kolejne iteracje (opcjonalnie)

- Scislejsza maszyna stanow dla `ApplicationStatus` (przejscia miedzy stanami)
- Paginacja serwerowa przy duzej liczbie rekordow
- Testy integracyjne `JobApplicationEndpointsTests` (w repo; uruchomienie zalezy od srodowiska)

## Model domenowy

```
JobApplication (AuditableAggregateRoot)
├── CompanyInfo (ValueObject)
│   ├── CompanyName: string (required, max 200)
│   ├── Location: string (optional, e.g. "Gdansk")
│   ├── WebsiteUrl: string (optional)
│   └── Industry: string (optional)
├── Position: string (required, max 200)
├── Status: ApplicationStatus (enum)
│   └── Draft | Applied | PhoneScreen | TechnicalInterview
│       | OnSite | Offer | Accepted | Rejected | Withdrawn
├── AppliedDate: DateOnly
├── Requirements: List<CompanyRequirement> (Entity)
├── Notes: List<ApplicationNote> (Entity)
└── InterviewStages: List<InterviewStage> (Entity)
```

## Zaleznosci

- **Wymaga:** Task 1.1 (Keycloak - rola Owner), Task 2.1 (DDD patterns), Task 5.1 (Angular RBAC)
- **Powiazane:** Task 5.3 (generowanie CV moze korzystac z `LinkedCurriculumVitaeId` i danych aplikacji)

## Notatki techniczne

- Endpointy wymagaja polityki `OwnerOnly` — w devie testowy pipeline moze ja przepuszczac (patrz `ContentApiFactory`).
- Filtrowanie listy: query `status`, `companyContains`.
