# 01. Aspire i ServiceDefaults

Poprzedni numer to [wersje](../01-bramki/09-wersje.md). Następny to [brama](02-gateway.md).

Ten numer stawia `JJDevHub.AppHost` i `JJDevHub.ServiceDefaults`. `aspire run` podnosi dashboard. Jeszcze nie ma bazy, brokera ani bramy. Jest wspólny skład telemetrii, health, odkrywania usług i handlera odporności, z którego następne projekty korzystają zamiast kopiować `Program.cs`.

## Po co

Pięć usług z pięcioma kopiami konfiguracji OpenTelemetry rozjedzie się w tydzień: jedna eksportuje ślady, druga nie, trzecia loguje token. Jedno miejsce, `ServiceDefaults`, zamyka ten rozjazd zanim powstanie druga usługa.

Bez AppHosta lokalny stos jest listą poleceń w czyjejś głowie. Dashboard zbiera logi i ślady na laptopie. Nie jedzie na maszynę. Na maszynie telemetrię zbiera [osobny numer](08-telemetria.md).

## Dlaczego ta technologia

### Aspire 13

AppHost to program, który opisuje zasoby i projekty. Problem, który zamyka, to ręczny compose jako jedyny opis lokalnego środowiska, pisany obok kodu i od razu nieaktualny. Odpada start od ręcznego `compose.yaml` jako źródła prawdy. Koszt: trzeba trzymać linię pakietów `13.6.5` i nie mieszać jej z hostingiem Kafki Aspire, którego obraz jest poza licencją. Broker dojdzie jako zwykły kontener w [numerze Kafki](../04-zdarzenia/03-kafka.md).

`aspire.config.json` wskazuje projekt AppHosta, żeby polecenie `aspire run` z korzenia nie pytało, który projekt jest składem.

### ServiceDefaults

Biblioteka wołana z `Program.cs` każdej usługi HTTP. Problem, który zamyka, to kopiowanie rejestracji telemetrii. Odpada wspólna klasa bazowa `Program`. Koszt: każdy nowy host ma jedną linijkę `AddServiceDefaults()` i jedną `MapDefaultEndpoints()`. Brak tej linijki jest błędem testu architektury, gdy test architektury dojdzie. Do tego czasu jest zasadą tego numeru.

### OpenTelemetry w SDK, nie w domenie

API śladu (`Activity`) jest w BCL. SDK i eksporter są w ServiceDefaults, czyli w hoście. Domena ich nie referencjonuje. Problem, który zamyka, to `using OpenTelemetry` w regule kursu. Odpada ręczne `Console.WriteLine` jako telemetria. Koszt: bez endpointu OTLP eksport jest wyłączony, a nie wskazuje na zmyślony adres. `aspire run` podstawia endpoint dashboardu sam.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| AppHost | projekt składu, nie proces produkcyjny |
| `ServiceDefaults` | wspólna rejestracja hosta HTTP |
| OTLP | protokół, którym proces wysyła ślady, metryki i logi |
| dashboard | UI Aspire na laptopie |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/apphost/JJDevHub.AppHost/AppHost.cs` | `DistributedApplication`, na tym numerze bez zasobów poza środowiskiem compose z numeru compose |
| `src/service-defaults/JJDevHub.ServiceDefaults/Extensions.cs` | `AddServiceDefaults`, `MapDefaultEndpoints` |
| `aspire.config.json` | ścieżka do AppHosta |
| `tests/JJDevHub.ServiceDefaults.Tests/` | test endpointu health |

AppHost referencjonuje `Aspire.AppHost.Sdk` i na tym numerze nic więcej z tabeli pakietów poza tym, czego wymaga sam host. ServiceDefaults referencjonuje pakiety OpenTelemetry, health i `Microsoft.Extensions.Http.Resilience` oraz `Microsoft.Extensions.ServiceDiscovery` w wersjach z [technologie.md](../00-fundamenty/technologie.md). Wersje wchodzą do `Directory.Packages.props` w tym samym scaleniu, z wierszem w tabeli, jeśli jeszcze go nie ma.

## Plan

### 1. AppHost

Projekt SDK AppHost, `TargetFramework` `net10.0`. `AppHost.cs` tworzy builder i woła `Build().Run()`. Nie dodaje Postgresa ani Kafki. Nie dodaje projektu bramy: bramy jeszcze nie ma.

`IsAspireHost` zostaje zgodnie z szablonem SDK. Projekt nie jest publikowany jako usługa produktu.

### 2. ServiceDefaults

`AddServiceDefaults`:

- OpenTelemetry: tracing, metrics, logi; instrumentacja ASP.NET Core, `HttpClient` i runtime.
- Eksporter OTLP tylko wtedy, gdy `OTEL_EXPORTER_OTLP_ENDPOINT` nie jest pusty.
- `AddServiceDiscovery`.
- `AddStandardResilienceHandler` jako domyślny handler `HttpClient`.
- Health check o nazwie `self` z tagiem `live`.

`MapDefaultEndpoints` mapuje `/health/live` na checki z tagiem `live` i `/health/ready` na wszystkie checki. Oba są mapowane także poza środowiskiem Development. Odpowiedź nie zawiera ciała wyjątku ani connection stringa.

### 3. Korelacja

Middleware w ServiceDefaults czyta `X-Correlation-Id`. Gdy nagłówka nie ma, generuje `Guid` w formacie `N`. Wartość wraca w odpowiedzi i wchodzi w zakres logu. Nie jest drugim trace id. Ślad ma własny `traceparent`. Korelacja jest dla człowieka czytającego log.

### 4. Test

Projekt testowy referencjonuje ServiceDefaults. Nie jest wyjątkiem od reguły „projekt produkcyjny ma test”; AppHost jest wyjątkiem, ServiceDefaults nie jest.

### Co jest do czego

| Typ | Kto woła |
| --- | --- |
| `AddServiceDefaults` | `Program.cs` bramy i każdej usługi |
| `MapDefaultEndpoints` | ten sam `Program.cs`, po zbudowaniu aplikacji |
| AppHost | człowiek na laptopie, `aspire run`; CI deployu woła `aspire publish`, nie `aspire run` |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| host zbudowany z `AddServiceDefaults` i `MapDefaultEndpoints`, bez środowiska Development | `GET /health/live` | status `200` |
| to samo | ciało odpowiedzi | nie zawiera słowa `exception` ani `Host=` |

Nazwa faktu: `Health_live_returns_ok_outside_development`.

```bash
dotnet test tests/JJDevHub.ServiceDefaults.Tests --nologo
```

## Checkpoint

```bash
dotnet build JJDevHub.slnx -c Release --nologo
dotnet test tests/JJDevHub.ServiceDefaults.Tests --nologo --filter "Category!=Integration&Category!=Performance"
aspire run --help
test -f aspire.config.json
```

Build i test kończą się kodem zero. Dashboard da się otworzyć z `aspire run` i zatrzymać. Na tym numerze nie ma jeszcze procesu, który odpowiada na porcie produktu.

## Umiesz wyjaśnić

Dlaczego AppHost nie jedzie na produkcję?

Bo opisuje skład deweloperski i publikację. Na maszynie działają obrazy usług, nie proces, który kompiluje projekty. Publikacja produkuje compose. Sam AppHost nie jest kontenerem bramy.

Co się dzieje, gdy nie ma endpointu OTLP?

Eksporter się nie rejestruje. Proces wstaje i obsługuje HTTP. Brak dashboardu nie jest awarią produktu.

Czym `/health/live` różni się od `/health/ready`?

Live mówi, że proces odpowiada. Ready wykonuje wszystkie checki, także te, które dojdą z bazą i brokerem. Na tym numerze ready i tak sprawdza tylko `self`, bo innych checków nie ma. Rozdzielenie ścieżek jest po to, żeby późniejszy check bazy nie wyłączał procesu z kubeleta na stałe: live zostaje zielony, ready czerwony.

Dlaczego odporność jest w domyślnym handlerze HTTP, a nie w domenie?

Ponawianie jest sprawą adaptera. Reguła kursu nie wie, czy woła HTTP. Handler z ServiceDefaults obejmuje klienty zarejestrowane w hoście.

Po co correlation id obok trace id?

Trace id łączy spany. Correlation id jest w logu i w odpowiedzi, także gdy ślad nie wyszedł z procesu, bo nie było eksportera.

## Zasady

- Domena nie referencjonuje ServiceDefaults ani OpenTelemetry.
- AppHost nie zawiera connection stringa z hasłem.
- `/health` nie zwraca szczegółów wyjątku.
- Pakiety tego numeru są w `Directory.Packages.props` bez wersji w `.csproj`.
- `Aspire.Hosting.Kafka` nie jest dodawany.

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

- `dotnet test tests/JJDevHub.ServiceDefaults.Tests` przechodzi, w tym fakt health poza Development.
- `aspire run` otwiera dashboard i nie wymaga bazy.
- W AppHost nie ma zasobu Postgres ani Kafka.
- Eksporter OTLP nie ma adresu na sztywno w `appsettings.json`.
- Wiersze pakietów tego numeru są w `Directory.Packages.props` i w tabeli technologii.
