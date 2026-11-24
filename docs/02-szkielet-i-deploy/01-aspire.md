# 01. Aspire i ServiceDefaults

Poprzedni numer to [wersje](../01-bramki/09-wersje.md). Następny to [brama](02-gateway.md).

Ten numer stawia `JJDevHub.AppHost` i `JJDevHub.ServiceDefaults`. `aspire run` podnosi dashboard. Bazy i brokera nadal nie ma. Wspólny skład telemetrii, health, odkrywania usług i handlera odporności jest w jednym projekcie. Brama i każda późniejsza usługa HTTP wołają go z `Program.cs` zamiast kopiować rejestrację.

Zasób `gateway` w AppHoście dopisuje [numer bramy](02-gateway.md). Ten numer zostawia skład bez Postgresa i bez Kafki.

## Po co

Pięć usług z pięcioma kopiami konfiguracji OpenTelemetry rozjedzie się w tydzień: jedna eksportuje ślady, druga nie, trzecia loguje token. Jedno miejsce, `ServiceDefaults`, zamyka ten rozjazd zanim powstanie druga usługa.

Bez AppHosta lokalny stos jest listą poleceń w czyjejś głowie. Dashboard zbiera logi i ślady na laptopie. Nie jedzie na maszynę. Na maszynie telemetrię zbiera [osobny numer](08-telemetria.md).

Pierwszy host, który z tego korzysta, to `JJDevHub.Gateway`. Jego `Program.cs` zaczyna się od `AddServiceDefaults()` i po `Build()` woła `MapDefaultEndpoints()`. Health bramy, nagłówek `X-Correlation-Id` i eksport OTLP na dashboard biorą się z tej pary, nie z drugiej kopii w projekcie bramy.

## Dlaczego ta technologia

### Aspire 13

AppHost to program, który opisuje zasoby i projekty. Problem, który zamyka, to ręczny compose jako jedyny opis lokalnego środowiska, pisany obok kodu i od razu nieaktualny. Odpada start od ręcznego `compose.yaml` jako źródła prawdy. Koszt: trzeba trzymać linię pakietów `13.6.5` i nie mieszać jej z hostingiem Kafki Aspire, którego obraz jest poza licencją. Broker dojdzie jako zwykły kontener w [numerze Kafki](../04-zdarzenia/03-kafka.md).

Projekt `src/apphost/JJDevHub.AppHost/JJDevHub.AppHost.csproj` ma SDK `Aspire.AppHost.Sdk/13.6.5`, `OutputType` `Exe` i `AspireUseCliBundle`. `aspire.config.json` w korzeniu wskazuje ten plik, więc `aspire run` i `aspire start` z katalogu repozytorium biorą ten skład i nie pytają o drugi AppHost.

Profil `http` w `Properties/launchSettings.json` słucha na `http://localhost:15000`. Zmienna `ASPIRE_ALLOW_UNSECURED_TRANSPORT` jest włączona, bo zasoby tego etapu idą po HTTP. Endpoint OTLP dashboardu w profilu to `http://localhost:19000`, a usługa zasobów `http://localhost:20000`. To porty laptopa. W `appsettings.json` AppHosta są tylko poziomy logów: `Default` na `Information`, `Microsoft.AspNetCore` i `Aspire.Hosting.Dcp` na `Warning`. Adresu OTLP tam nie ma.

### ServiceDefaults

Biblioteka wołana z `Program.cs` każdej usługi HTTP. Problem, który zamyka, to kopiowanie rejestracji telemetrii. Odpada wspólna klasa bazowa `Program`. Koszt: każdy nowy host ma jedną linijkę `AddServiceDefaults()` i jedną `MapDefaultEndpoints()`. Brak tej linijki jest błędem testu architektury, gdy test architektury dojdzie. Do tego czasu jest zasadą tego numeru.

Projekt `src/service-defaults/JJDevHub.ServiceDefaults` jest biblioteką z `IsAspireSharedProject` i `FrameworkReference` na `Microsoft.AspNetCore.App`. Pakiety, bez wersji w `.csproj`, to `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, trzy instrumentacje (`AspNetCore`, `Http`, `Runtime`), `Microsoft.Extensions.Http.Resilience` i `Microsoft.Extensions.ServiceDiscovery`. Wersje są w `Directory.Packages.props` i w tabeli [technologie.md](../00-fundamenty/technologie.md).

Klasa `Extensions` leży w namespace `Microsoft.Extensions.Hosting`. Host web ma ten namespace w usings SDK, więc `Program.cs` bramy woła metody bez dodatkowego `using`.

### OpenTelemetry w SDK, nie w domenie

API śladu (`Activity`) jest w BCL. SDK i eksporter są w ServiceDefaults, czyli w hoście. Domena ich nie referencjonuje. Problem, który zamyka, to `using OpenTelemetry` w regule kursu. Odpada ręczne `Console.WriteLine` jako telemetria. Koszt: bez endpointu OTLP eksport jest wyłączony, a nie wskazuje na zmyślony adres. `aspire run` podstawia endpoint dashboardu sam, przez zmienną `OTEL_EXPORTER_OTLP_ENDPOINT` w procesie potomnym.

`ConfigureOpenTelemetry` czyta `builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]`. Pusty napis i brak klucza zostawiają eksporter niezarejestrowany. Niepusty klucz woła `UseOtlpExporter()` bez adresu wpisanego w kodzie. Eksporter bierze adres ze zmiennej, którą wstawił Aspire albo deploy.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| AppHost | projekt składu, nie proces produkcyjny |
| `ServiceDefaults` | wspólna rejestracja hosta HTTP |
| OTLP | protokół, którym proces wysyła ślady, metryki i logi |
| dashboard | UI Aspire na laptopie |
| `live` | tag checku, który proces odpowiada |
| `X-Correlation-Id` | nagłówek dla człowieka czytającego log |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/apphost/JJDevHub.AppHost/AppHost.cs` | `DistributedApplication`; zasób `gateway` dopisuje numer bramy |
| `src/apphost/JJDevHub.AppHost/appsettings.json` | poziomy logów, bez adresu OTLP i bez hasła |
| `src/apphost/JJDevHub.AppHost/Properties/launchSettings.json` | profil `http` dashboardu |
| `src/service-defaults/JJDevHub.ServiceDefaults/Extensions.cs` | `AddServiceDefaults`, `MapDefaultEndpoints` |
| `aspire.config.json` | ścieżka do AppHosta |
| `tests/JJDevHub.ServiceDefaults.Tests/ServiceDefaultsHost.cs` | host testowy w `Production`, port `0` |
| `tests/JJDevHub.ServiceDefaults.Tests/HealthLiveTests.cs` | `GET /health/live` poza Development |
| `tests/JJDevHub.ServiceDefaults.Tests/CorrelationIdTests.cs` | echo i generowanie nagłówka |

AppHost referencjonuje `Aspire.AppHost.Sdk` i projekt bramy. ServiceDefaults referencjonuje pakiety OpenTelemetry, health i `Microsoft.Extensions.Http.Resilience` oraz `Microsoft.Extensions.ServiceDiscovery` w wersjach z [technologie.md](../00-fundamenty/technologie.md). Wersje wchodzą do `Directory.Packages.props` w tym samym scaleniu, z wierszem w tabeli, jeśli jeszcze go nie ma.

## Plan

### 1. AppHost

`AppHost.cs` tworzy builder i woła `Build().Run()`. Postgresa i Kafki nie dodaje. Jedyny zasób projektu to brama, dopisana w następnym numerze:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.JJDevHub_Gateway>("gateway");

builder.Build().Run();
```

Nazwa zasobu `gateway` jest nazwą, pod którą później widać proces na dashboardzie. Klasa `Projects.JJDevHub_Gateway` powstaje z referencji projektu w `.csproj`. `IsAspireHost` zostaje zgodnie z szablonem SDK. Projekt nie jest publikowany jako usługa produktu.

### 2. ServiceDefaults

`AddServiceDefaults` robi cztery rzeczy, w tej kolejności: telemetria, check `self`, `AddServiceDiscovery`, domyślny pipeline `HttpClient`.

```csharp
public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
    where TBuilder : IHostApplicationBuilder
{
    builder.ConfigureOpenTelemetry();
    builder.AddDefaultHealthChecks();
    builder.Services.AddServiceDiscovery();
    builder.Services.ConfigureHttpClientDefaults(http =>
    {
        http.AddStandardResilienceHandler();
        http.AddServiceDiscovery();
    });

    return builder;
}
```

Telemetria:

- Logi idą przez `AddOpenTelemetry` z `IncludeFormattedMessage` i `IncludeScopes`. Zakres z correlation id wchodzi w log, bo scope jest włączony.
- Metryki: ASP.NET Core, `HttpClient`, runtime.
- Ślady: źródło o nazwie aplikacji, ASP.NET Core, `HttpClient`. Filtr ASP.NET Core pomija ścieżki, które `StartsWithSegments` dopasuje do `/health/live` albo `/health/ready`. Ślad health nie zalewa dashboardu przy każdym probe.

Check `self` zwraca `HealthCheckResult.Healthy()` i ma tag `live`. Innych checków ten numer nie dodaje.

`ConfigureHttpClientDefaults` dokleja standardowy handler odporności, a potem odkrywanie usług, do każdego klienta zarejestrowanego w hoście. Brama zdejmuje handler odporności z jednego nazwanego klienta, sondy wersji. Reszta klientów zostaje przy budżecie odporności.

`MapDefaultEndpoints` najpierw wstawia middleware korelacji, potem mapuje oba health. Kolejność jest taka, że health też dostaje nagłówek korelacji. Oba endpointy są mapowane także poza środowiskiem Development. Odpowiedź nie zawiera ciała wyjątku ani connection stringa. Domyślny writer health pisze krótki stan tekstowy. Test sprawdza, że w ciele nie ma słowa `exception` ani fragmentu `Host=`.

`/health/ready` bierze wszystkie checki. `/health/live` bierze te, których tagi zawierają `live`. Dziś oba widzą tylko `self`. Gdy dojdzie check bazy, wejdzie do ready i zostanie poza live.

### 3. Korelacja

Middleware czyta `X-Correlation-Id`. Stała nazywa się `Extensions.CorrelationHeader`. Pusty nagłówek i sam biały znak dają `Guid` w formacie `N`, trzydzieści dwa znaki szesnastkowe bez myślników. Niepusty nagłówek wraca bez zmiany, także gdy nie jest guidem. Test wysyła `abc123` i dostaje `abc123`.

Wartość ląduje w nagłówku odpowiedzi i w zakresie logu pod kluczem `CorrelationId`. Logger ma kategorię `JJDevHub.Correlation`. To nie jest drugi trace id. Ślad ma własny `traceparent`. Korelacja jest dla człowieka czytającego log, także gdy eksporter OTLP nie wstał.

### 4. Test

Projekt testowy referencjonuje ServiceDefaults. Nie jest wyjątkiem od reguły „projekt produkcyjny ma test”; AppHost jest wyjątkiem, ServiceDefaults nie jest.

`ServiceDefaultsHost.StartAsync` buduje aplikację z `EnvironmentName` `Production`, więc fakt health nie jedzie w Development. Do konfiguracji wkłada pusty `OTEL_EXPORTER_OTLP_ENDPOINT`, żeby test nie eksportował na zmyślony adres. `UseUrls("http://127.0.0.1:0")` bierze wolny port. Potem `AddServiceDefaults`, `MapDefaultEndpoints` i `StartAsync`. Test czyta jedyny adres z `app.Urls`.

Dwa fakty: `Health_live_returns_ok_outside_development` i `Correlation_id_is_echoed_or_generated`. Drugi fakt ma jeden skutek: nagłówek jest albo guidem `N`, albo echem wartości, którą klient przysłał.

### Co jest do czego

| Typ | Kto woła |
| --- | --- |
| `AddServiceDefaults` | `Program.cs` bramy i każdej usługi |
| `MapDefaultEndpoints` | ten sam `Program.cs`, po zbudowaniu aplikacji |
| `Extensions.CorrelationHeader` | middleware i test korelacji |
| AppHost | człowiek na laptopie, `aspire run`; CI deployu woła `aspire publish`, nie `aspire run` |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| host z `ServiceDefaultsHost`, środowisko `Production`, pusty endpoint OTLP | `GET /health/live` | status `200` |
| to samo | ciało odpowiedzi | nie zawiera słowa `exception` ani `Host=` |
| ten sam host, bez nagłówka | `GET /health/live` | jeden `X-Correlation-Id` w formacie `N` |
| ten sam host, nagłówek `abc123` | `GET /health/live` | odpowiedź ma `abc123` |

Nazwa faktu health: `Health_live_returns_ok_outside_development`. Nazwa faktu korelacji: `Correlation_id_is_echoed_or_generated`.

`dotnet test` na SDK 10 z xunit.v3 idzie przez Microsoft.Testing.Platform. Przełącznik `test.runner` jest w `global.json`. Ścieżka projektu idzie przez `--project`.

```bash
dotnet test --project tests/JJDevHub.ServiceDefaults.Tests
```

## Checkpoint

```bash
dotnet build JJDevHub.slnx -c Release --nologo
dotnet test --project tests/JJDevHub.ServiceDefaults.Tests --filter "Category!=Integration&Category!=Performance"
aspire run --help
test -f aspire.config.json
```

Build i test kończą się kodem zero. Dashboard da się otworzyć z `aspire run` albo `aspire start` i zatrzymać przez `aspire stop`. Skład nie wymaga bazy. Port produktu `8080` należy do bramy z następnego numeru. Ten numer odpowiada za to, że proces bramy ma `/health/live`, `/health/ready` i korelację bez własnej kopii tej rejestracji.

## Umiesz wyjaśnić

Dlaczego AppHost nie jedzie na produkcję?

Bo opisuje skład deweloperski i publikację. Na maszynie działają obrazy usług, nie proces, który kompiluje projekty. Publikacja produkuje compose. Sam AppHost nie jest kontenerem bramy.

Co się dzieje, gdy nie ma endpointu OTLP?

Eksporter się nie rejestruje. Proces wstaje i obsługuje HTTP. Brak dashboardu nie jest awarią produktu. Test hosta wkłada pusty klucz właśnie po to, żeby ta ścieżka była tą, którą sprawdza fakt.

Czym `/health/live` różni się od `/health/ready`?

Live mówi, że proces odpowiada. Ready wykonuje wszystkie checki, także te, które dojdą z bazą i brokerem. Na tym numerze ready i tak sprawdza tylko `self`, bo innych checków nie ma. Rozdzielenie ścieżek jest po to, żeby późniejszy check bazy nie wyłączał procesu z kubeleta na stałe: live zostaje zielony, ready czerwony. W kodzie różnicę robi predykat `check.Tags.Contains("live")` na `/health/live`. Ready mapuje `MapHealthChecks` bez predykatu.

Dlaczego odporność jest w domyślnym handlerze HTTP, a nie w domenie?

Ponawianie jest sprawą adaptera. Reguła kursu nie wie, czy woła HTTP. Handler z ServiceDefaults obejmuje klienty zarejestrowane w hoście. Klient, którego budżet ma być krótszy niż standardowy, zdejmuje ten handler u siebie. Tak robi nazwany klient `versions` w bramie.

Po co correlation id obok trace id?

Trace id łączy spany. Correlation id jest w logu i w odpowiedzi, także gdy ślad nie wyszedł z procesu, bo nie było eksportera. Format `N` jest dla wartości, którą proces sam wygenerował. Wartość od klienta wraca taka, jaka przyszła.

Dlaczego health nie wchodzi w ślad?

Bo probe kubeleta i test wołają tę ścieżkę często. Filtr `StartsWithSegments` na `/health/live` i `/health/ready` zostawia ślad dla `/versions` i dla tras proxy. Check nadal jest w metrykach ASP.NET Core. W śladzie go nie ma.

## Zasady

- Domena nie referencjonuje ServiceDefaults ani OpenTelemetry.
- AppHost nie zawiera connection stringa z hasłem.
- `/health` nie zwraca szczegółów wyjątku.
- Pakiety tego numeru są w `Directory.Packages.props` bez wersji w `.csproj`.
- `Aspire.Hosting.Kafka` nie jest dodawany.
- Adres OTLP stoi w zmiennej procesu, nie w `appsettings.json`.

## Poza zakresem

- Brama i `GET /versions` są w [02-gateway.md](02-gateway.md).
- Postgres jest w [03-postgres.md](../03-learning-zapis/03-postgres.md).
- Kafka jest w [03-kafka.md](../04-zdarzenia/03-kafka.md).
- `otel-lgtm` na maszynie jest w [08-telemetria.md](08-telemetria.md).
- Propagacja śladu przez Kafkę jest w [08-slady.md](../04-zdarzenia/08-slady-i-metryki.md).

## Źródła

- [.NET Aspire](https://aspire.dev/get-started/aspire-overview/). Wiążące jest rozdzielenie AppHosta i projektów usług. Dotyczy tego, że dashboard zostaje na laptopie. Ze źródła nie bierzemy AppHosta jako procesu na maszynie.
- [Service defaults](https://aspire.dev/fundamentals/service-defaults/). Wiążące jest jedno miejsce telemetrii, health i odkrywania. Dotyczy `AddServiceDefaults`. Ze źródła nie bierzemy drugiej kopii tej konfiguracji w `Program.cs` usługi.
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/). Wiążące jest włączanie eksportera konfiguracją, nie adresem wpisanym w kod. Dotyczy `OTEL_EXPORTER_OTLP_ENDPOINT`. Ze źródła nie bierzemy SDK w projekcie domeny.

## Gotowe gdy

- `dotnet test --project tests/JJDevHub.ServiceDefaults.Tests` przechodzi, w tym fakt health poza Development i fakt korelacji.
- `aspire run` otwiera dashboard i nie wymaga bazy.
- W AppHost nie ma zasobu Postgres ani Kafka.
- Eksporter OTLP nie ma adresu na sztywno w `appsettings.json`.
- Wiersze pakietów tego numeru są w `Directory.Packages.props` i w tabeli technologii.
