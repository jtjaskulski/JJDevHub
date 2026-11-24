# 02. Brama i `GET /versions`

Poprzedni numer to [Aspire](01-aspire.md). Następny to [Dockerfile](03-dockerfile.md).

Ten numer stawia `JJDevHub.Gateway` na YARP i endpoint `GET /versions`. Brama jest jedynym wejściem HTTP klientów. Usług do przekazania jeszcze nie ma, więc trasy w `appsettings.json` wskazują nazwy, których AppHost nie uruchamia. Działa trasa własna bramy: `/version`, `/versions` i health ze [ServiceDefaults](01-aspire.md).

AppHost woła `AddProject<Projects.JJDevHub_Gateway>("gateway")`. To jest pierwsza poprawka AppHosta od poprzedniego numeru.

## Po co

Przeglądarka, która zna pięć portów, zna pięć miejsc do podmiany i pięć certyfikatów. Jeden adres zamyka ten problem. `GET /versions` zamyka inne: na ekranie widać, który obraz odpowiada za treść. Bez tego „u mnie działa” nie ma numeru, do którego da się wrócić.

Pusty numer usługi to pusty napis, nie brak pola. Klient rysuje wiersz `version.missing` zamiast udawać, że usługi nie ma w produkcie. JSON z laptopa bez `SERVICE_VERSION` i bez sąsiadów wygląda tak:

```json
{"identity":"","news":"","learning":"","cv":"","contact":"","gateway":""}
```

Kolejność kluczy jest kolejnością parametrów rekordu `VersionReport`. Minimal API serializuje je camelCase.

## Dlaczego ta technologia

### YARP

YARP jest biblioteką odwrotnego proxy w procesie ASP.NET. Problem, który zamyka, to druga brama obok aplikacji, z własnym językiem reguł. Odpada Traefik i osobny Nginx jako brama produktu. Koszt: brama jest projektem, który trzeba wydawać tagiem `gateway-vX.Y.Z`. To jest zamierzone: brama jest komponentem, nie dodatkiem do klastra.

Na k3s Traefik i ServiceLB są wyłączone z tego samego powodu, w [numerze manifestów](../13-kubernetes/02-manifesty.md). Tu zapada decyzja, tam jej nie powtarzamy.

Pakiet to `Yarp.ReverseProxy` `2.3.0`. `Program.cs` ładuje sekcję `ReverseProxy` przez `LoadFromConfig` i dokleja `AddServiceDiscoveryDestinationResolver()`. Resolver jest w `Microsoft.Extensions.ServiceDiscovery.Yarp` `10.11.0`, tej samej linii co `Microsoft.Extensions.ServiceDiscovery` ze szkieletu. Linia `11` tego pakietu tu nie wchodzi. Wiersz jest w tabeli technologii i w `Directory.Packages.props`. Projekt bramy referencjonuje oba pakiety bez numeru wersji oraz `JJDevHub.ServiceDefaults`.

Adres klastra `http://learning` zostaje nazwą, dopóki Aspire nie poda punktu końcowego. Resolver zamienia nazwę na adres procesu, gdy zasób o tej nazwie istnieje. Dziś takich zasobów nie ma. Żądanie proxy dostaje błąd celu. Raport wersji i tak wraca.

### Konfiguracja tras w kodzie i w pliku

Trasy produktu są w `appsettings.json` bez sekretów: klaster wskazuje nazwę usługi (`http://learning`), a nie adres laptopa. Problem, który zamyka, to trasa dopisana tylko w pamięci osoby, która stawia środowisko. Odpada trasa wyklinana na maszynie. Koszt: nowa usługa to zmiana tego pliku i AppHosta w numerze, który usługę dodaje.

Każdy klaster ma jeden cel o kluczu `destination`. Ścieżka w pliku konfiguracji, z której sonda czyta adres, to `ReverseProxy:Clusters:<nazwa>:Destinations:destination:Address`. Żądanie idzie do klastra z ścieżką, z którą przyszło. `PathRemovePrefix` dla `/learning` dopisuje numer REST, nie ten.

Profil `http` w `Properties/launchSettings.json` ma `applicationUrl` `http://localhost:8080` i nie otwiera przeglądarki. Port `8080` jest nazwą bramy ze słownika. `aspire start` pokazuje ten adres na zasobie `gateway`.

### Wersja z środowiska

`SERVICE_VERSION` przychodzi z deployu i jest pełnym tagiem, na przykład `gateway-v0.1.0`. Brama woła `GET /version` sąsiadów, zbiera sześć pól i zwraca JSON. Problem, który zamyka, to wersja wpisana w kodzie, która kłamie po następnym obrazie. Odpada składanie numeru z `AssemblyVersion` bez tagu. Koszt: brak zmiennej daje pusty napis także dla samej bramy i test to widzi. Deploy ma tę zmienną ustawiać. Obraz bez niej nie jest wydaniem, co sprawdza numer deployu.

W procesie wartość jest kluczem konfiguracji `SERVICE_VERSION`. Zmienna środowiska wpada do konfiguracji ASP.NET sama. `GET /version` zwraca `Results.Text`, czyli `text/plain` i sam tag. Pusty klucz daje puste ciało i status `200`. `versions/gateway.json` zostaje `0.0.0` do pierwszego wydania. Odpowiedź HTTP nie czyta tego pliku.

Timeout sąsiada nie przewraca całego `GET /versions`. Pole sąsiada jest puste, brama zwraca `200` i swój numer, jeśli go ma.

Sonda nie korzysta ze standardowego handlera odporności ze ServiceDefaults. Ten handler ma budżet dłuższy niż dwie sekundy, a krótszy `HttpClient.Timeout` przy wciąż podpiętym handlerze nie daje hostowi wstać. Nazwany klient `versions` woła `RemoveAllResilienceHandlers()` i ustawia timeout na dwie sekundy. Odkrywanie usług z `ConfigureHttpClientDefaults` na tym kliencie zostaje, więc nazwa `http://identity` nadal może zostać rozwiązana, gdy zasób się pojawi.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| trasa | dopasowanie ścieżki do klastra w sekcji `ReverseProxy:Routes` |
| klaster | cel YARP, u nas nazwa usługi i adres `http://<nazwa>` |
| `destination` | jedyny klucz celu w każdym klastrze |
| `SERVICE_VERSION` | tag komponentu wstrzyknięty przy starcie procesu |
| `versions` | nazwa klienta HTTP, który zbiera tagi sąsiadów |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/gateway/JJDevHub.Gateway/Program.cs` | YARP, `/version`, `/versions`, ServiceDefaults |
| `src/gateway/JJDevHub.Gateway/appsettings.json` | osiem tras, pięć klastrów, bez adresów maszynowych |
| `src/gateway/JJDevHub.Gateway/Versions/VersionReport.cs` | złożenie sześciu pól |
| `src/gateway/JJDevHub.Gateway/Properties/launchSettings.json` | profil `http` na porcie `8080` |
| `src/apphost/JJDevHub.AppHost/AppHost.cs` | `AddProject` zasobu `gateway` |
| `tests/JJDevHub.Gateway.Tests/VersionsTests.cs` | fakt kształtu JSON |

`Program` jest publiczną klasą częściową w globalnej przestrzeni, żeby `WebApplicationFactory<Program>` w teście widziała punkt wejścia. Na końcu pliku nie ma drugiej bramy ani middleware autoryzacji.

## Plan

### 1. Trasy

W konfiguracji YARP jest pięć klastrów: `identity`, `news`, `learning`, `cv`, `contact`. Adres celu to odpowiednio `http://identity`, `http://news`, `http://learning`, `http://cv`, `http://contact`.

Osiem tras:

| Klucz trasy | Ścieżka | Klaster |
| --- | --- | --- |
| `identity` | `/identity/{**catch-all}` | `identity` |
| `connect` | `/connect/{**catch-all}` | `identity` |
| `well-known` | `/.well-known/{**catch-all}` | `identity` |
| `news` | `/news/{**catch-all}` | `news` |
| `learning` | `/learning/{**catch-all}` | `learning` |
| `cv` | `/cv/{**catch-all}` | `cv` |
| `contact` | `/contact/{**catch-all}` | `contact` |
| `graphql` | `/graphql/{**catch-all}` | `learning` |

`/version`, `/versions`, `/health/live` i `/health/ready` nie są w sekcji `Routes`. YARP ich nie przechwytuje. W pliku nie ma `localhost` z portem, hasła ani connection stringa. Obok tras jest tylko poziom logów i `AllowedHosts` `*`.

### 2. `GET /versions`

Rekord `VersionReport` jest zamknięty i ma dokładnie sześć napisów, w tej kolejności: `Identity`, `News`, `Learning`, `Cv`, `Contact`, `Gateway`. W JSON są to `identity`, `news`, `learning`, `cv`, `contact`, `gateway`. Puste pole to `""`.

`CollectAsync` startuje pięć odczytów naraz i czeka `Task.WhenAll`. Jeden sąsiad, który nie odpowiada, nie wydłuża pozostałych ponad własny limit. Po zebraniu pól dokleja `gateway` z `configuration["SERVICE_VERSION"]`, a gdy klucza nie ma, z `""`. Samej siebie nie woła.

`ReadAsync` czyta adres klastra. Pusty adres daje `""` bez żądania. W pozostałych przypadkach buduje `{adres}/version`, obcina końcowy ukośnik adresu i woła `GET`. Limit to `TimeSpan` dwóch sekund na własnym tokenie powiązanym z tokenem żądania. Anulowanie klienta przerywa sondę. Samo upłynięcie dwóch sekund daje pusty napis.

Pusty napis jest też wtedy, gdy poleci `HttpRequestException`, `InvalidOperationException` albo `OperationCanceledException` przy nadal żywym tokenie żądania. Status inny niż `200` daje pusty napis i nie czyta ciała. Ciało `200` wraca po `Trim`.

```csharp
app.MapGet("/version", (IConfiguration configuration) =>
{
    string version = configuration["SERVICE_VERSION"] ?? "";
    return Results.Text(version);
});

app.MapGet("/versions", async (
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    HttpClient httpClient = httpClientFactory.CreateClient(VersionReport.HttpClientName);
    VersionReport report = await VersionReport.CollectAsync(httpClient, configuration, cancellationToken);
    return Results.Ok(report);
});
```

`VersionReport.HttpClientName` to `versions`. `GET /version` na bramie zwraca tylko własny tag. Usługi dostaną taki sam endpoint, gdy powstaną. Kontrakt jest tu, żeby brama miała kogo wołać. Sonda czyta ciało jako tekst, więc tag sąsiada ma być tym samym `text/plain`, nie obiektem JSON.

`MapDefaultEndpoints()` stoi przed tymi dwoma mapowaniami, a `MapReverseProxy()` za nimi.

### 3. Autoryzacja

Na tym numerze brama nie wymaga tokenu na `/versions` i na health. Nie waliduje jeszcze JWT: nie ma klucza publicznego Identity. Walidacja dochodzi w [03-jwt](../05-tozsamosc/03-jwt-i-policies.md). W `Program.cs` nie ma `UseAuthentication` ani `RequireAuthorization`. Trasy proxy i tak nie mają celu, który odpowiada. To nie jest otwarte API produktu, tylko szkielet.

### 4. Test kształtu

`VersionsTests` podnosi `WebApplicationFactory<Program>`. Klasa wewnętrzna `GatewayFactory` dokleja słownik w pamięci na końcu konfiguracji. `SERVICE_VERSION` ma wartość `gateway-v0.1.0`. Pięć adresów klastrów wskazuje `http://127.0.0.1:1`, port zamknięty. To nadpisanie jest w teście. `appsettings.json`, który jedzie na maszynę, dalej ma nazwy usług.

Fakt czyta ciało jako `JsonDocument`, liczy właściwości i bierze każdą po nazwie.

### Co jest do czego

| Element | Kto go woła |
| --- | --- |
| YARP | każde żądanie klienta poza `/version`, `/versions` i health |
| `VersionReport.CollectAsync` | `GET /versions` |
| `GET /version` | brama, wobec każdej usługi, i człowiek przy diagnozie jednego procesu |
| klient `versions` | tylko sonda; reszta klientów hosta zostaje przy handlerze odporności |

## Testy

Nazwa: `Versions_returns_six_fields_when_neighbors_are_down`.

| Arrange | Act | Assert |
| --- | --- | --- |
| `SERVICE_VERSION=gateway-v0.1.0`, adresy klastrów `http://127.0.0.1:1` | `GET /versions` | `200`, sześć pól, `gateway` równe tagowi, reszta pusta |

```bash
dotnet test --project tests/JJDevHub.Gateway.Tests
```

Runner to Microsoft.Testing.Platform, tak jak w numerze Aspire. Ścieżka idzie przez `--project`. Projekt testowy ma `OutputType` `Exe`, `Microsoft.AspNetCore.Mvc.Testing` `10.0.14`, Shouldly i xunit.v3.

## Checkpoint

```bash
dotnet test --project tests/JJDevHub.Gateway.Tests
aspire run
```

Przy działającym `aspire run` albo `aspire start` `GET http://localhost:8080/versions` zwraca JSON z sześcioma kluczami. Wartość `gateway` zależy od tego, czy zmienna jest ustawiona. Lokalnie bez zmiennej pole jest puste i test z fabryki i tak przechodzi, bo zmienną ustawia test. `GET /version` na tym samym hoście zwraca sam tag albo puste ciało. `GET /health/live` zwraca `200`, bo brama woła `MapDefaultEndpoints`.

## Umiesz wyjaśnić

Dlaczego puste pole, a nie brak klucza?

Bo klient ma stałą kolejność wierszy. Brak klucza wygląda jak zmiana kontraktu. Pusty napis znaczy „ten komponent nie odpowiedział albo nie jest wdrożony”. Test liczy sześć właściwości i czyta je po nazwie, więc brak klucza albo `null` ten fakt oblewa.

Dlaczego timeout sąsiada nie daje `502` na `/versions`?

Bo endpoint mówi o wersjach, nie jest proxy tej usługi. Jedna leżąca usługa nie może ukryć numeru bramy i numerów pozostałych. Wyjątek sondy kończy się w `ReadAsync` i wraca jako `""`. Status całego raportu zostaje `200`.

Czemu YARP, a nie reguły w Cloudflare?

Cloudflare kończy tunel. Nie zna usług w środku. Podział tras produktu zostaje w repozytorium, obok kodu.

Co brama wolno robić z ciałem żądania?

Przekazać. Nie czyta treści notatki i nie składa strony CV. Nie ma bazy. Jedyny odczyt ciała, który brama robi na własny rachunek, to ciało `GET /version` sąsiada, i tylko gdy status to `200`.

Kiedy trasa do Learning zacznie działać?

Gdy AppHost i deploy będą uruchamiać proces o nazwie `learning`. Trasa może być wcześniej. Żądanie dostanie błąd proxy, a `/versions` dalej zwróci pusty `learning`. Adres w pliku już jest `http://learning`, więc dopisanie zasobu nie wymaga podmiany `localhost`.

Dlaczego sonda ma własny klient HTTP?

Standardowy handler odporności ze ServiceDefaults ponawia dłużej niż dwie sekundy. Sonda wersji ma zamknąć pole po dwóch sekundach i oddać raport. Klient `versions` ten handler zdejmuje. Odkrywanie nazw zostaje, bo bez niego `http://learning` nie stanie się portem procesu, gdy Learning wstanie.

Dlaczego pięć odczytów idzie naraz?

Każdy ma własny limit dwóch sekund. Czekanie po kolei dodawałoby te limity, gdy leży więcej niż jeden sąsiad. `WhenAll` trzyma cały raport przy limicie jednej sondy. Anulowanie żądania klienta wchodzi do każdego z pięciu tokenów.

Dlaczego brama nie woła `GET /version` u siebie?

Pole `gateway` ma być tagiem tego procesu nawet wtedy, gdy pętla HTTP do własnego portu nie wstanie. Źródłem jest konfiguracja, ta sama, którą `GET /version` wypisuje jako tekst. Człowiek przy jednym procesie i raport zbiorczy czytają tę samą wartość.

## Zasady

- W `appsettings.json` bramy nie ma hasła i nie ma adresu publicznego maszyny.
- `/versions` zawsze ma sześć pól, w kolejności rekordu.
- Brama nie dostaje connection stringa do żadnej z dziesięciu baz.
- Jedna brama. Drugi projekt proxy nie powstaje.
- `GET /versions` i `GET /version` są anonimowe.
- Sonda sąsiada kończy się po dwóch sekundach i nie przewraca raportu.
- Ścieżka proxy idzie do klastra bez obcinania prefiksu. Obcięcie `/learning` jest w numerze REST.

## Poza zakresem

- Obraz jest w [03-dockerfile.md](03-dockerfile.md).
- Wystawienie na hostname jest w [05-cloudflare.md](05-cloudflare.md).
- Pierwszy rollout jest w [07-deploy.md](07-deploy.md).
- JWT na bramie jest w [03-jwt.md](../05-tozsamosc/03-jwt-i-policies.md).
- Stopka klienta jest w [02-szkielet.md](../11-web/02-szkielet.md).
- `PathRemovePrefix` dla `/learning` jest w [05-rest.md](../03-learning-zapis/05-rest.md).

## Źródła

- [YARP](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/yarp-overview). Wiążące są trasy i klastry w konfiguracji. Dotyczy jednego procesu bramy. Ze źródła nie bierzemy drugiej bramy dla GraphQL albo dla telefonu.
- [RFC 9110, statusy](https://www.rfc-editor.org/rfc/rfc9110). Wiążące jest to, że `200` na `/versions` opisuje raport, nie zdrowie sąsiada. Dotyczy timeoutu pola. Ze źródła nie bierzemy `502` jako odpowiedzi całego raportu.
- [Service discovery w Aspire](https://aspire.dev/fundamentals/service-discovery/). Wiążące jest wołanie usługi po nazwie. Dotyczy adresu klastra na laptopie i `AddServiceDiscoveryDestinationResolver`. Ze źródła nie bierzemy wpisanego `localhost:port` w pliku, który jedzie na maszynę.

## Gotowe gdy

- Test sześciu pól przechodzi przy sąsiadach, którzy nie odpowiadają.
- `aspire run` uruchamia bramę i `GET /versions` zwraca JSON.
- W konfiguracji są trasy ze słownika i nie ma hasła.
- AppHost referencjonuje projekt bramy i nazywa zasób `gateway`.
- Brama woła `AddServiceDefaults` i `MapDefaultEndpoints`.
- `GET /version` zwraca tag z `SERVICE_VERSION` jako `text/plain`.
