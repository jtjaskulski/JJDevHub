# Aspire

.NET Aspire składa system w jednym miejscu. `JJDevHub.AppHost` opisuje zasoby: serwer Postgresa z bazami usług, broker Kafka z Kafka UI i projekty usług. `JJDevHub.ServiceDefaults` daje każdej usłudze jednym wywołaniem telemetrię, health checki, service discovery i odporność klienta HTTP. Ten numer podpina istniejące API z [src/api](../../src/api) i zasoby dla News. Kolejne numery dopisują swoje projekty do AppHost: Identity w [15-identity.md](15-identity.md), News i gateway w [16-usluga-news.md](16-usluga-news.md), Users i Notifications w [17-uzytkownicy-i-powiadomienia.md](17-uzytkownicy-i-powiadomienia.md).

## Po co

Do tej pory każda biblioteka miała swój kontener w teście, a [infra/docker/docker-compose.yml](../../infra/docker/docker-compose.yml) znał jedno API. Przy pięciu procesach, sześciu bazach i brokerze ręczne Compose i connection stringi w `appsettings.json` przestają się skalować. eShop ma `eShop.AppHost` (zasoby i zależności w C#) i `eShop.ServiceDefaults` (wspólna konfiguracja usług). `aspire run` stawia całość lokalnie z dashboardem logów, śladów i metryk. Ten sam opis zasobów generuje Compose na serwer.

Aspire to też kierunek, w którym idzie .NET. Nowe szablony i integracje (`Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`, `Aspire.Confluent.Kafka`) dostają health checki i telemetrię bez ręcznej konfiguracji.

## Klasy i ścieżki

| Plik | Odpowiedzialność |
| --- | --- |
| `src/aspire/JJDevHub.AppHost/JJDevHub.AppHost.csproj` | SDK `Aspire.AppHost.Sdk`, `Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.Kafka`, `Aspire.Hosting.Docker` |
| `src/aspire/JJDevHub.AppHost/AppHost.cs` | zasoby i projekty |
| `src/aspire/JJDevHub.AppHost/ResourceNames.cs` | stałe nazw zasobów (`news-write`, `kafka`…) |
| `src/aspire/JJDevHub.ServiceDefaults/JJDevHub.ServiceDefaults.csproj` | `IsAspireSharedProject`, OpenTelemetry, service discovery, resilience |
| `src/aspire/JJDevHub.ServiceDefaults/ServiceDefaultsExtensions.cs` | `AddServiceDefaults`, `MapDefaultEndpoints` |
| `src/aspire/JJDevHub.ServiceDefaults/TelemetryExtensions.cs` | OTel: zasoby, źródła, eksportery |
| `src/aspire/JJDevHub.ServiceDefaults/HealthCheckExtensions.cs` | `self`, tagi `live` |
| `src/api/Program.cs` | `AddServiceDefaults()` zamiast ręcznego OTel, `MapDefaultEndpoints()` |
| `src/building-blocks/JJDevHub.Cqrs`, `JJDevHub.Projections`, `JJDevHub.IntegrationEventLog`, `JJDevHub.EventBus.Kafka` | `ActivitySource` o nazwie projektu |
| `tests/JJDevHub.AppHost.Tests/` | `Aspire.Hosting.Testing` |

## Tutorial

### 1. Projekty

Szablony z pakietu `Aspire.ProjectTemplates` (`dotnet new install Aspire.ProjectTemplates`):

```bash
dotnet new aspire-apphost -n JJDevHub.AppHost -o src/aspire/JJDevHub.AppHost
dotnet new aspire-servicedefaults -n JJDevHub.ServiceDefaults -o src/aspire/JJDevHub.ServiceDefaults
dotnet new aspire-xunit -n JJDevHub.AppHost.Tests -o tests/JJDevHub.AppHost.Tests
dotnet sln add src/aspire/JJDevHub.AppHost src/aspire/JJDevHub.ServiceDefaults tests/JJDevHub.AppHost.Tests
dotnet add src/aspire/JJDevHub.AppHost reference src/api
dotnet add src/api reference src/aspire/JJDevHub.ServiceDefaults
dotnet add tests/JJDevHub.AppHost.Tests reference src/aspire/JJDevHub.AppHost
```

Wersje `Aspire.*` idą do [Directory.Packages.props](../../Directory.Packages.props). Wersja SDK AppHost jest w `JJDevHub.AppHost.csproj` (`<Project Sdk="Aspire.AppHost.Sdk/…">`), a [global.json](../../global.json) może ją przypiąć w `msbuild-sdks`. Szablon generuje kod ServiceDefaults. Zostaje on rozbity na trzy pliki według reguły jednego typu na plik.

### 2. Zasoby

`ResourceNames` trzyma stałe: `postgres`, `news-write`, `news-read`, `identity`, `users`, `notifications`, `kafka`, `api`. Te same nazwy czytają usługi jako `ConnectionStrings:<nazwa>`.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres(ResourceNames.Postgres)
    .WithImageTag("16-alpine")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var newsWrite = postgres.AddDatabase(ResourceNames.NewsWrite, "news_write");
var newsRead = postgres.AddDatabase(ResourceNames.NewsRead, "news_read");
var jjdevhub = postgres.AddDatabase("DefaultConnection", "jjdevhub");

var kafka = builder.AddKafka(ResourceNames.Kafka)
    .WithKafkaUI()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.JJDevHub_Api>(ResourceNames.Api)
    .WithReference(jjdevhub)
    .WaitFor(jjdevhub);

builder.Build().Run();
```

- Postgres jest jeden, bazy są osobne. Obraz `16-alpine`, ten sam co w testach.
- `Persistent` zostawia kontenery między uruchomieniami `aspire run`, więc dane dev nie znikają.
- Baza obecnego API dostaje nazwę zasobu `DefaultConnection`. Istniejący `GetConnectionString("DefaultConnection")` działa bez zmian do [15-identity.md](15-identity.md).
- `newsWrite`, `newsRead` i `kafka` nie mają jeszcze odbiorcy. Projekt `News.API` podepnie je w `16`. Zmienne zostają, żeby `16` dopisał tylko jeden blok.
- Hasło Postgresa i klucz JWT to parametry Aspire (`builder.AddParameter("jwt-key", secret: true)`). W dev biorą się z user secrets AppHost, nie z repozytorium.

### 3. ServiceDefaults

`AddServiceDefaults(this IHostApplicationBuilder builder)`:

- `ConfigureOpenTelemetry`:
  - logi przez OTel z `IncludeFormattedMessage` i `IncludeScopes`,
  - metryki: ASP.NET Core, `HttpClient`, runtime, eksporter Prometheus (`/metrics` zostaje, jak dziś w [src/api/Program.cs](../../src/api/Program.cs)),
  - ślady: ASP.NET Core, `HttpClient`, `Npgsql`, źródła `JJDevHub.*` z bibliotek,
  - OTLP tylko wtedy, gdy ustawiona jest zmienna `OTEL_EXPORTER_OTLP_ENDPOINT`. W dev ustawia ją Aspire (dashboard), a na VM [infra/docker/.env.example](../../infra/docker/.env.example) (Jaeger, tylko ślady),
- `AddDefaultHealthChecks`: check `self` z tagiem `live`,
- `AddServiceDiscovery`,
- `ConfigureHttpClientDefaults`: `AddStandardResilienceHandler` i `AddServiceDiscovery`.

`MapDefaultEndpoints(this WebApplication app)`:

- `/health` — wszystkie checki: baza, Kafka, `self`,
- `/alive` — tylko tag `live`,
- `/metrics` — Prometheus.

Obecne ręczne `MapGet("/health")` i blok `AddOpenTelemetry` w [src/api/Program.cs](../../src/api/Program.cs) znikają na rzecz tych dwóch wywołań. Test `AuthAndHealthTests` sprawdza dalej `/health` z kodem 200.

### 4. Źródła śladów w bibliotekach

Każda biblioteka z pracą w tle albo na granicy ma statyczne `ActivitySource` o nazwie projektu:

- `JJDevHub.Cqrs` — span `Send {RequestName}` w `LoggingBehavior`,
- `JJDevHub.Projections` — span na przejście runnera,
- `JJDevHub.IntegrationEventLog` — span na wysyłkę i odbiór,
- `JJDevHub.EventBus.Kafka` — span `publish {topic}` i `process {topic}`, z `traceparent` z [13-kafka.md](13-kafka.md).

`System.Diagnostics.ActivitySource` jest w platformie. Biblioteki nie dostają pakietu OpenTelemetry.

### 5. Klienci Aspire w usługach

Od `16` usługi biorą połączenia przez integracje klienckie zamiast własnych `UseNpgsql`:

```csharp
builder.AddNpgsqlDbContext<NewsWriteContext>(ResourceNames.NewsWrite);
builder.AddKafkaProducer<string, string>(ResourceNames.Kafka);
```

Integracja dodaje health check, ślady i metryki. `AddNewsWrite` z [09-komenda.md](09-komenda.md) i `AddNewsRead` z [10-projekcja.md](10-projekcja.md) dostają przeciążenia bez connection stringa, które pomijają `AddDbContext`. `AddKafkaEventBus` z [13-kafka.md](13-kafka.md) bierze `IProducer<string, string>` z DI, gdy już jest zarejestrowany.

### 6. Publikacja na VM

AppHost dostaje środowisko Compose:

```csharp
builder.AddDockerComposeEnvironment("jjdevhub");
```

`aspire publish -o artifacts/compose` generuje `docker-compose.yaml` i `.env` z parametrami. Katalog `artifacts/` jest w `.gitignore`. Generuje go CI w [18-ci-uslug.md](18-ci-uslug.md). Prometheus, Jaeger i Grafana zostają w [infra/docker/docker-compose.yml](../../infra/docker/docker-compose.yml) i łączą się z usługami przez wspólną zewnętrzną sieć Dockera `jjdevhub`. Sieć dopina `PublishAsDockerComposeService` na każdym projekcie. Nazwy metod publikacji zmieniały się między wersjami Aspire, więc przed generowaniem kodu trzeba je sprawdzić w dokumentacji wersji z `Directory.Packages.props`.

### Co jest do czego

- `JJDevHub.AppHost` — jedno źródło prawdy o tym, co się uruchamia i od czego zależy. `aspire run` w dev, `aspire publish` na serwer.
- `ResourceNames` — nazwy zasobów współdzielone z usługami. Literówka jest błędem kompilacji, a nie pustym connection stringiem.
- `AddPostgres` + `AddDatabase` — jeden serwer, baza na usługę, zgodnie z [architektura.md](architektura.md).
- `AddKafka().WithKafkaUI()` — broker w trybie KRaft i podgląd tematów, w tym DLQ.
- `WaitFor` — usługa startuje, gdy baza odpowiada. Bez wyścigu przy migracji.
- `JJDevHub.ServiceDefaults` — każda usługa ma te same endpointy zdrowia, tę samą telemetrię i odporny `HttpClient`.
- `ActivitySource` w bibliotekach — ślad przechodzi od `POST` przez outbox i Kafkę do konsumenta.
- `AddDockerComposeEnvironment` — most do obecnego deployu na VM przez Compose.

## Testy

Projekt `tests/JJDevHub.AppHost.Tests`, klasa `AppHostTests`. `DistributedApplicationTestingBuilder.CreateAsync<Projects.JJDevHub_AppHost>()`, start, `ResourceNotifications.WaitForResourceHealthyAsync`. Wymaga Dockera na runnerze. Timeout testu: 3 minuty.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Api_becomes_healthy` | aplikacja uruchomiona | `GET /health` na `api` | 200 |
| `Api_exposes_alive` | aplikacja uruchomiona | `GET /alive` na `api` | 200 |
| `Postgres_has_service_databases` | aplikacja uruchomiona | `SELECT datname FROM pg_database` przez connection string `postgres` | są `news_write`, `news_read`, `jjdevhub` |
| `Kafka_becomes_healthy` | aplikacja uruchomiona | `WaitForResourceHealthyAsync("kafka")` | bez timeoutu |

Istniejące `tests/JJDevHub.Api.Tests` przechodzą bez zmian poza endpointem `/health`, który daje teraz ServiceDefaults.

`dotnet test tests/JJDevHub.AppHost.Tests` i `dotnet test tests/JJDevHub.Api.Tests`.

## Zasady

- Nazwy zasobów są stałymi w `ResourceNames` i są nazwami connection stringów w usługach.
- Usługa woła `AddServiceDefaults()` i `MapDefaultEndpoints()` jako pierwsze i ostatnie wywołanie konfiguracji.
- Sekrety to parametry Aspire. W dev są w user secrets, na VM w `/etc/jjdevhub/<usługa>.env`. W repozytorium ich nie ma.
- Biblioteki budulcowe nie referencjonują Aspire ani OpenTelemetry. Mają tylko `ActivitySource`.
- Jedna baza na usługę, nawet gdy serwer jest wspólny.

## Poza zakresem

- Kubernetes, Helm i `aspire publish` do Azure.
- Zastąpienie Prometheusa, Jaegera i Grafany dashboardem Aspire na serwerze. Dashboard jest dla dev.
- Identity, News.API, Users, Notifications w AppHost — numery `15`–`17`.
- Pipeline publikacji — [18-ci-uslug.md](18-ci-uslug.md).

## Źródła

- [.NET Aspire overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) — AppHost, zasoby, integracje.
- [Service defaults](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults) — `AddServiceDefaults`, health checks, OTel, resilience.
- [Aspire PostgreSQL integration](https://learn.microsoft.com/en-us/dotnet/aspire/database/postgresql-entity-framework-integration) — `AddPostgres`, `AddDatabase`, `AddNpgsqlDbContext`.
- [Aspire Apache Kafka integration](https://learn.microsoft.com/en-us/dotnet/aspire/messaging/kafka-integration) — `AddKafka`, `WithKafkaUI`, `AddKafkaProducer`.
- [Aspire testing](https://learn.microsoft.com/en-us/dotnet/aspire/testing/write-your-first-test) — `DistributedApplicationTestingBuilder`.
- [Aspire deployment with Docker Compose](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/overview) — `aspire publish` i środowisko Compose.
- [dotnet/eShop — eShop.AppHost](https://github.com/dotnet/eShop/tree/main/src/eShop.AppHost) i [eShop.ServiceDefaults](https://github.com/dotnet/eShop/tree/main/src/eShop.ServiceDefaults) — wzorzec składu.
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/) — źródła, eksportery OTLP i Prometheus.

## Gotowe gdy

- `aspire run` stawia Postgresa z bazami `jjdevhub`, `news_write`, `news_read`, Kafkę z Kafka UI i istniejące API.
- API ma `/health`, `/alive` i `/metrics` z `MapDefaultEndpoints`, a ręczny blok OTel zniknął z `Program.cs`.
- `aspire publish -o artifacts/compose` generuje plik Compose bez sekretów w treści.
- `dotnet test tests/JJDevHub.AppHost.Tests` i `dotnet test tests/JJDevHub.Api.Tests` kończą się kodem 0.
