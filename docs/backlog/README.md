# JJDevHub - Backlog & Roadmap

> Zunifikowany backlog projektu JJDevHub. Kazdy sprint buduje na poprzednim, bez luk.

## Legenda statusow

| Status | Znaczenie |
|--------|-----------|
| `DONE` | Zadanie w pelni zaimplementowane i przetestowane |
| `IN PROGRESS` | Czesciowo zaimplementowane, wymaga dokonczenia |
| `TODO` | Nie rozpoczete |

## Roadmapa sprintow

```mermaid
gantt
    title JJDevHub Implementation Roadmap
    dateFormat YYYY-MM-DD
    axisFormat %b

    section Sprint1
    Keycloak Setup           :done, s1t1, 2026-01-01, 14d
    Vault Configuration      :done, s1t2, 2026-01-01, 14d
    Shared.Kernel            :done, s1t3, 2026-01-01, 14d
    Database Infrastructure  :done, s1t4, 2026-01-01, 14d

    section Sprint2
    Domain Aggregates        :active, s2t1, 2026-01-15, 14d
    EF Core Configuration    :done, s2t2, 2026-01-15, 14d
    Command Handlers         :done, s2t3, 2026-01-15, 14d

    section Sprint3
    Kafka Docker Setup       :done, s3t1, 2026-02-01, 14d
    Kafka Event Bus          :done, s3t2, 2026-02-01, 14d
    Consumer and MongoDB     :active, s3t3, 2026-02-01, 14d

    section Sprint4
    Content API Queries      :active, s4t1, 2026-02-15, 14d
    Nginx Reverse Proxy      :done, s4t2, 2026-02-15, 14d
    Angular Web App          :active, s4t3, 2026-02-15, 14d
    React Native Mobile      :active, s4t4, 2026-02-15, 14d

    section Sprint5
    Angular RBAC             :s5t1, 2026-03-15, 14d
    Application Tracker      :s5t2, 2026-03-15, 14d
    CV Generation Engine     :s5t3, 2026-03-15, 14d

    section Sprint6
    SonarQube Quality Gate   :active, s6t1, 2026-04-01, 14d
    Jenkinsfile Pipeline     :done, s6t2, 2026-04-01, 14d
    OpenTelemetry Setup      :active, s6t3, 2026-04-01, 14d
    Grafana Dashboards       :active, s6t4, 2026-04-01, 14d
```

## Przeglad sprintow

### Sprint 1: Identity & Foundation (Straznik i Bazy)

**Cel:** Postawienie infrastruktury oraz zablokowanie dostepu do systemu.

| Task | Nazwa | Status | Plik |
|------|-------|--------|------|
| 1.1 | [Keycloak Setup](sprint-1/task-1.1-keycloak-setup.md) | IN PROGRESS | docker-compose ma Keycloak, brak OIDC w .NET |
| 1.2 | [Vault Configuration](sprint-1/task-1.2-vault-configuration.md) | IN PROGRESS | bootstrap-vault.sh istnieje, brak VaultSharp |
| 1.3 | [Shared.Kernel](sprint-1/task-1.3-shared-kernel.md) | DONE | Pelny zestaw DDD building blocks |
| 1.4 | [Database Infrastructure](sprint-1/task-1.4-database-infrastructure.md) | DONE | PostgreSQL + MongoDB w docker-compose |

### Sprint 2: The Core & Write Side (Zapis i Reguly)

**Cel:** Obsluga logiki biznesowej i zapisu (Command).

| Task | Nazwa | Status | Plik |
|------|-------|--------|------|
| 2.1 | [Domain Aggregates](sprint-2/task-2.1-domain-aggregates.md) | IN PROGRESS | WorkExperience DONE, CurriculumVitae TODO |
| 2.2 | [EF Core Configuration](sprint-2/task-2.2-ef-core-configuration.md) | DONE | Fluent API + DateRange mapping |
| 2.3 | [Command Handlers](sprint-2/task-2.3-command-handlers.md) | DONE | MediatR + FluentValidation |

### Sprint 3: Event-Driven Sync (Rozproszenie Danych)

**Cel:** Synchronizacja zapisu z odczytem bez spowalniania API.

| Task | Nazwa | Status | Plik |
|------|-------|--------|------|
| 3.1 | [Kafka Docker Setup](sprint-3/task-3.1-kafka-docker-setup.md) | DONE | Kafka + Zookeeper w docker-compose |
| 3.2 | [Kafka Event Bus](sprint-3/task-3.2-kafka-event-bus.md) | DONE | Producer z idempotencja |
| 3.3 | [Kafka Consumer + MongoDB](sprint-3/task-3.3-kafka-consumer-mongodb.md) | IN PROGRESS | MongoDB read store gotowy, Sync worker placeholder |

### Sprint 4: Public Face & Mobile (Dla Studentow)

**Cel:** Frontend publiczny oraz prezentacja danych.

| Task | Nazwa | Status | Plik |
|------|-------|--------|------|
| 4.1 | [Content API Queries](sprint-4/task-4.1-content-api-queries.md) | IN PROGRESS | Podstawowe query handlery istnieja |
| 4.2 | [Nginx Reverse Proxy](sprint-4/task-4.2-nginx-reverse-proxy.md) | DONE | Routing do wszystkich serwisow |
| 4.3 | [Angular Web App](sprint-4/task-4.3-angular-web-app.md) | IN PROGRESS | Home, WorkExperience, About |
| 4.4 | [React Native Mobile](sprint-4/task-4.4-react-native-mobile.md) | IN PROGRESS | HomeScreen, WorkExperienceScreen |

### Sprint 5: The Secret Feature (Narzedzie dla Ciebie)

**Cel:** Ukryty modul zarzadzania kariera.

| Task | Nazwa | Status | Plik |
|------|-------|--------|------|
| 5.1 | [Angular RBAC](sprint-5/task-5.1-angular-rbac.md) | TODO | Mechanizm odkrywania ukrytego menu |
| 5.2 | [Application Tracker](sprint-5/task-5.2-application-tracker.md) | TODO | Modul sledzenia aplikacji do firm |
| 5.3 | [CV Generation Engine](sprint-5/task-5.3-cv-generation-engine.md) | TODO | Silnik generowania CV do PDF |

### Sprint 6: Observability & DevOps (Jakosc)

**Cel:** Utrzymanie i monitoring na poziomie Enterprise.

| Task | Nazwa | Status | Plik |
|------|-------|--------|------|
| 6.1 | [SonarQube Quality Gate](sprint-6/task-6.1-sonarqube-quality-gate.md) | IN PROGRESS | W docker-compose, brak Quality Gate config |
| 6.2 | [Jenkinsfile Pipeline](sprint-6/task-6.2-jenkinsfile-pipeline.md) | DONE | Pelny 9-stage pipeline |
| 6.3 | [OpenTelemetry Setup](sprint-6/task-6.3-opentelemetry-setup.md) | IN PROGRESS | Content.Api ma Prometheus metrics |
| 6.4 | [Grafana Dashboards](sprint-6/task-6.4-grafana-dashboards.md) | IN PROGRESS | content-api dashboard istnieje |
| 6.5 | [Ekstrakcja NuGet Packages](sprint-6/task-6.5-extract-nuget-packages.md) | TODO | 4 reusable pakiety do stworzenia |

## Dodatkowe dokumenty

- [NuGet Reference](nuget-reference.md) - Pelne zestawienie pakietow NuGet
- [Hosting & Cloudflare](hosting-cloudflare.md) - Architektura VPS + Cloudflare

## Podsumowanie statusow

| Status | Liczba taskow |
|--------|---------------|
| DONE | 9 |
| IN PROGRESS | 8 |
| TODO | 5 |
| **Razem** | **22** |
