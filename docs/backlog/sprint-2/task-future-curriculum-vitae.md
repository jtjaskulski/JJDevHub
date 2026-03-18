# Future task: Curriculum Vitae aggregate (EF + commands)

| Pole | Wartość |
|------|---------|
| Status | Backlog |
| Priorytet | Medium (gdy scope produktowy CV jest zamrożony) |
| Zależy od | Task 2.2 wzorce, Task 2.3 CQRS, obecny slice `WorkExperience` |

## Cel

Rozszerzenie write modelu o agregat **CurriculumVitae** (lub root CV z kolekcjami) zgodnie z notatką z [task-2.2](task-2.2-ef-core-configuration.md): Fluent API z **nested owned types** lub osobne encje dla kolekcji (Skills, Education, Projects) — decyzja DDD: jeden agregat vs wiele.

## Zakres techniczny (propozycja)

### Persistence (EF Core)

- `CurriculumVitaeConfiguration`: tabela główna + mapowanie:
  - Owned types dla value objects (np. okresy, poziomy umiejętności), **lub**
  - `OwnsMany` / child tables jeśli kolekcje mają własne identity.
- Global filter / audyt jak `WorkExperience` jeśli encje dziedziczą `AuditableEntity`.
- Repository: `ICurriculumVitaeRepository`, rejestracja w DI.

### Application

- Commands: `CreateCvCommand`, `UpdateCvSectionCommand`, … (doprecyzować po UX).
- FluentValidation dla komend publicznych.
- Domain events + handlery synchronizujące read model (Mongo) i integracje (Kafka / outbox zgodnie z [transactional-outbox-kafka.md](../transactional-outbox-kafka.md)).

### API

- Endpointy minimal API lub kontrolery pod `/api/content/cv` (lub wersjonowane).

## Kryteria akceptacji (szkic)

- [ ] Konfiguracja EF bez atrybutów w warstwie Core.
- [ ] Migracja PostgreSQL stosowalna lokalnie i w CI.
- [ ] Co najmniej jeden happy-path command + test integracyjny (opcjonalnie).
- [ ] Read model w Mongo spójny z zapisem (lub outbox + projekcja).

## Notatki

- Rozważyć granicę transakcji: duży CV = duży lock — ewentualnie pod-agregaty lub ograniczenie rozmiaru zapisu na żądanie.
- Spójność z istniejącym `WorkExperience`: jako sekcja CV vs osobny bounded context — do decyzji architektonicznej.
