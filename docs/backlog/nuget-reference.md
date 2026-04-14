# JJDevHub - NuGet Package Reference

> Pelne zestawienie wszystkich pakietow NuGet uzywanych w projekcie oraz rekomendacje nowych pakietow do dodania.
>
> Szukasz informacji o ekstrakcji wlasnych pakietow NuGet z tego projektu? Zobacz [Task 6.5 - Ekstrakcja NuGet Packages](sprint-6/task-6.5-extract-nuget-packages.md).

## Legenda

| Symbol | Znaczenie |
|--------|-----------|
| Zainstalowany | Pakiet juz jest w `.csproj` |
| Do dodania | Pakiet rekomendowany, jeszcze nie zainstalowany |

---

## 1. JJDevHub.Shared.Kernel

Wspolna biblioteka DDD building blocks. Minimalna lista zaleznosci.

| Pakiet | Wersja | Status | Uzasadnienie |
|--------|--------|--------|--------------|
| `MediatR` | 14.0.0 | Zainstalowany | IDomainEvent : INotification, CQRS interfaces (ICommand, IQuery) : IRequest |

---

## 2. JJDevHub.Content.Core

Warstwa domenowa. Brak pakietow NuGet - tylko ProjectReference do Shared.Kernel.

| Pakiet | Wersja | Status | Uzasadnienie |
|--------|--------|--------|--------------|
| - | - | - | Zero dependencies - czysta domena |

---

## 3. JJDevHub.Content.Application

Warstwa aplikacji z CQRS handlers i walidacja.

| Pakiet | Wersja | Status | Uzasadnienie |
|--------|--------|--------|--------------|
| `MediatR` | 14.0.0 | Zainstalowany | Mediator pattern - dispatching commands, queries, domain events |
| `FluentValidation` | 12.1.1 | Zainstalowany | Fluent walidacja komend (np. CompanyName required, max 200) |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.1 | Zainstalowany | Automatyczna rejestracja walidatorow z assembly |

---

## 4. JJDevHub.Content.Persistence

Warstwa persystencji (EF Core + PostgreSQL).

| Pakiet | Wersja | Status | Uzasadnienie |
|--------|--------|--------|--------------|
| `Microsoft.EntityFrameworkCore` | 10.0.3 | Zainstalowany | ORM framework - DbContext, migrations, Fluent API |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.0 | Zainstalowany | EF Core provider dla PostgreSQL |

---

## 5. JJDevHub.Content.Infrastructure

Warstwa infrastruktury (Kafka, MongoDB).

| Pakiet | Wersja | Status | Uzasadnienie |
|--------|--------|--------|--------------|
| `Confluent.Kafka` | 2.13.0 | Zainstalowany | Kafka producer (KafkaEventBus) - publikacja Integration Events |
| `MongoDB.Driver` | 3.6.0 | Zainstalowany | MongoDB read store - zdenormalizowane widoki CQRS |
| `Microsoft.Extensions.Configuration.Abstractions` | 10.0.0 | Zainstalowany | IConfiguration do odczytu Kafka/MongoDB settings |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.0 | Zainstalowany | DI registration |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.0 | Zainstalowany | ILogger |
| `Microsoft.Extensions.Options` | 10.0.0 | Zainstalowany | Options pattern (MongoDbSettings) |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | 10.0.0 | Zainstalowany | Bind configuration sections to options |
| `QuestPDF` | 2024.12.3 | Zainstalowany | Generowanie CV do PDF - fluent API, Community license |

---

## 6. JJDevHub.Content.Api

Warstwa prezentacji (Minimal API + observability).

| Pakiet | Wersja | Status | Uzasadnienie |
|--------|--------|--------|--------------|
| `Microsoft.AspNetCore.OpenApi` | 10.0.1 | Zainstalowany | OpenAPI/Swagger documentation |
| `AspNetCore.HealthChecks.NpgSql` | 9.0.0 | Zainstalowany | Health check PostgreSQL |
| `AspNetCore.HealthChecks.MongoDb` | 9.0.0 | Zainstalowany | Health check MongoDB |
| `OpenTelemetry.Exporter.Prometheus.AspNetCore` | 1.15.0-beta.1 | Zainstalowany | Prometheus metrics exporter na /metrics |
| `OpenTelemetry.Extensions.Hosting` | 1.15.0 | Zainstalowany | OpenTelemetry hosting integration |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.12.0 | Zainstalowany | HTTP server metrics (request rate, latency) |
| `OpenTelemetry.Instrumentation.Runtime` | 1.12.0 | Zainstalowany | .NET runtime metrics (GC, memory, threads) |
| `Keycloak.AuthServices.Authentication` | 2.8.0 | Zainstalowany | Integracja OIDC z Keycloak |
| `Keycloak.AuthServices.Authorization` | 2.8.0 | Zainstalowany | Polityki autoryzacji na bazie Keycloak roles |
| `VaultSharp` | 1.17.5.1 | Zainstalowany | Klient HashiCorp Vault |
| `VaultSharp.Extensions.Configuration` | 1.1.4 | Zainstalowany | Vault jako .NET Configuration Provider |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.12.0 | Zainstalowany | OTLP exporter do Jaeger (gRPC) |
| `Asp.Versioning.Http` | 8.1.1 | Zainstalowany | API versioning - URL segment `/api/v{version}/...` |
| `Swashbuckle.AspNetCore` | 10.1.5 | Zainstalowany | Swagger UI / OpenAPI generation |
| `OpenTelemetry.Instrumentation.Http` | latest | Do dodania | Instrumentacja HttpClient outgoing calls |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | latest | Do dodania | Instrumentacja EF Core queries |
| `Serilog.AspNetCore` | latest | Do dodania | Strukturalne logowanie |
| `Serilog.Sinks.Console` | latest | Do dodania | Log output do konsoli/Docker logs |
| `Serilog.Enrichers.OpenTelemetry` | latest | Do dodania | Trace ID w logach Serilog |

---

## 7. JJDevHub.Sync

Background worker - Kafka consumer.

| Pakiet | Wersja | Status | Uzasadnienie |
|--------|--------|--------|--------------|
| `Microsoft.Extensions.Hosting` | 10.0.1 | Zainstalowany | BackgroundService hosting |
| `Confluent.Kafka` | 2.13.0 | Zainstalowany | Kafka consumer do odbioru Integration Events |
| `MongoDB.Driver` | 3.6.0 | Zainstalowany | MongoDB upsert read models |

---

## 8. Pozostale serwisy (stubs)

Kazdy stub serwis ma tylko jeden pakiet:

| Serwis | Pakiet | Wersja | Status |
|--------|--------|--------|--------|
| JJDevHub.Identity | `Microsoft.AspNetCore.OpenApi` | 10.0.1 | Zainstalowany |
| JJDevHub.Education | `Microsoft.AspNetCore.OpenApi` | 10.0.1 | Zainstalowany |
| JJDevHub.Notification | `Microsoft.AspNetCore.OpenApi` | 10.0.1 | Zainstalowany |
| JJDevHub.AI.Gateway | `Microsoft.AspNetCore.OpenApi` | 10.0.1 | Zainstalowany |
| JJDevHub.Analytics.Api | `Microsoft.AspNetCore.OpenApi` | 10.0.1 | Zainstalowany |

---

## 9. Projekty testowe

### JJDevHub.Content.UnitTests

| Pakiet | Wersja | Status | Uzasadnienie |
|--------|--------|--------|--------------|
| `xunit` | 2.9.3 | Zainstalowany | Test framework |
| `xunit.runner.visualstudio` | 3.1.0 | Zainstalowany | Test runner adapter (VS, CLI) |
| `Microsoft.NET.Test.Sdk` | 18.0.1 | Zainstalowany | Test SDK |
| `NSubstitute` | 5.3.0 | Zainstalowany | Mocking framework (interfejsy, virtual methods) |
| `FluentAssertions` | 8.3.0 | Zainstalowany | Czytelne asercje (Should().Be(), Should().Contain()) |
| `coverlet.collector` | 8.0.0 | Zainstalowany | Zbieranie pokrycia kodu (OpenCover format) |

### JJDevHub.Content.IntegrationTests

| Pakiet | Wersja | Status | Uzasadnienie |
|--------|--------|--------|--------------|
| `xunit` | 2.9.3 | Zainstalowany | Test framework |
| `xunit.runner.visualstudio` | 3.1.0 | Zainstalowany | Test runner adapter |
| `Microsoft.NET.Test.Sdk` | 18.0.1 | Zainstalowany | Test SDK |
| `NSubstitute` | 5.3.0 | Zainstalowany | Mocking |
| `FluentAssertions` | 8.3.0 | Zainstalowany | Asercje |
| `coverlet.collector` | 8.0.0 | Zainstalowany | Pokrycie kodu |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.3 | Zainstalowany | WebApplicationFactory - in-memory API testing |
| `Testcontainers.PostgreSql` | 4.10.0 | Zainstalowany | PostgreSQL w Docker kontenerze testowym |
| `Testcontainers.MongoDb` | 4.10.0 | Zainstalowany | MongoDB w Docker kontenerze testowym |
| `Testcontainers.Kafka` | latest | Do dodania | Kafka w Docker kontenerze testowym (cross-cutting) |

---

## 10. Cross-cutting - Pakiety rekomendowane globalnie

Pakiety ktore moga byc dodane do wielu serwisow:

| Pakiet | Wersja | Uzasadnienie | Sprint |
|--------|--------|--------------|--------|
| `Microsoft.Extensions.Http.Resilience` | latest | Polly v8 - retry, circuit breaker, timeout dla HttpClient | Cross-cutting |
| `Asp.Versioning.Http` | latest | Wersjonowanie API (URL segment: `/api/v1/...` lub header) | Cross-cutting |
| `Serilog.AspNetCore` | latest | Strukturalne logowanie - wspolne dla wszystkich serwisow | Sprint 6.3 |
| `Serilog.Sinks.Console` | latest | Log output do konsoli (formatowanie JSON lub text) | Sprint 6.3 |
| `Serilog.Sinks.Seq` | latest | Opcjonalnie - centralny serwer logow Seq | Sprint 6.3 |

---

## Podsumowanie

| Kategoria | Zainstalowane | Do dodania |
|-----------|---------------|------------|
| Shared.Kernel | 1 | 0 |
| Content (Core + Application + Persistence + Infrastructure + Api) | 25 | 5 |
| Sync Worker | 3 | 0 |
| Stub Services (5x) | 5 | 0 |
| Unit Tests | 6 | 0 |
| Integration Tests | 9 | 1 |
| Cross-cutting | 0 | 5 |
| **Razem** | **49** | **11** |
