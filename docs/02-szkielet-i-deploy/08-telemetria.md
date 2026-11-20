# 08. Telemetria na maszynie

Poprzedni numer to [deploy](07-deploy.md). Następny folder zaczyna się od [jądra](../03-learning-zapis/01-kernel-i-domena.md).

Ten numer stawia na maszynie kontener `grafana/otel-lgtm` i podpina bramę do OTLP. Dashboard Aspire zostaje na laptopie. Na maszynie ślady są w Tempo, metryki w Prometheusie, logi w Loki, a ogląda się je w Grafanie tego kontenera. Panel nie dostaje nazwy publicznej.

## Po co

`GET /versions` mówi, co działa. Nie mówi, dlaczego żądanie trwało trzy sekundy. Bez śladu na maszynie zostaje `docker logs` i zgadywanie. Jeden kontener zamyka skład obserwowalności na czas, w którym produktem jest jedna brama i później kilka usług. Osobny Jaeger, osobny Prometheus i osobna Loki to trzy razy więcej ruchu w sieci i trzy wersje do podbicia, zanim jest co oglądać.

Awaria collectora gubi sygnał z okna awarii. Nie gubi żądania HTTP. Brama ma dalej zwracać `/versions`, gdy Grafana leży.

## Dlaczego ta technologia

### `grafana/otel-lgtm`

Obraz niesie collector, Tempo, Prometheus, Loki i Grafanę. Problem, który zamyka, to cztery manifesty na jedną bramę. Odpada Jaeger all-in-one jako osobny magazyn obok Tempo: dwa magazyny śladów rozjadą się w identyfikatorach, których człowiek szuka. Koszt: licencja Grafany w obrazie jest AGPL i obraz zostaje osobnym procesem. Nie linkujemy jej do bramy. Obraz jest przypięty tagiem w AppHost, nie `:latest`. Tag z dnia numeru wpisuje się w AppHost i w przykład env jako komentarz z numerem wersji obrazu, bez hasła.

### OTLP do collectora, nie `/metrics` na bramie

Brama eksportuje OTLP na adres collectora w sieci compose. Nie wystawia publicznie `/metrics`. Problem, który zamyka, to Prometheus skrobiący bramę przez tunel. Odpada scrape podów przez YARP. Koszt: bez collectora nie ma metryk. To jest akceptowane. Żądanie nie zależy od scrape.

Na laptopie `aspire run` nadal podstawia endpoint dashboardu. AppHost w trybie publish podstawia endpoint `http://otel-lgtm:4317`. Dwa tryby, jedna rejestracja w ServiceDefaults: eksporter czyta zmienną, nie `if (Production)`.

### Kardynalność

Etykieta metryki nie dostaje identyfikatora żądania, sluga treści ani adresu e-mail. Problem, który zamyka, to Prometheus, który umiera od serii. Odpada „na wszelki wypadek pełny URL”. Koszt: nie zobaczymy jednego sluga na wykresie. Zobaczymy trasę. Slug, gdy będzie potrzebny, zostaje w logu albo w atrybucie śladu, nie w etykiecie metryki.

### Log na stdout

Proces pisze log strukturalny na stdout. Kontener LGTM zbiera go przez sterownik albo przez OTLP logi, zależnie od tego, co obraz w przypiętej wersji przyjmuje bez dodatkowej konfiguracji. Nie ma pliku logu na dysku bramy. Problem, który zamyka, to log, który rotuje się inaczej w każdej usłudze. Odpada `File` sink jako domyślny.

Pole logu nie zawiera ciała żądania, nagłówka `Authorization` ani ciasteczka.

### Panel bez nazwy

Grafana słucha na `127.0.0.1:3000` maszyny. Nie ma wpisu w tunelu. Wejście jest z sesji na maszynie. Problem, który zamyka, to panel na haśle `admin` wystawiony obok CV. Odpada publikacja „na chwilę”. Koszt: wykres ogląda się przez SSH. To wystarcza, dopóki panel nie ma Cloudflare Access. Access jest dozwolony później i nie idzie przez YARP.

Hasło Grafany, jeśli obraz wymaga zmiany, jest w `/etc/jjdevhub/compose.env`. Nie jest `admin` zapisane w git.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| span | kawałek śladu jednego żądania |
| RED | rate, errors, duration na trasie |
| collector | proces, który przyjmuje OTLP i rozdziela sygnały |
| kardynalność | liczba serii metryki; identyfikator w etykiecie ją wysadza |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/apphost/JJDevHub.AppHost/AppHost.cs` | kontener `otel-lgtm` w publish i w run, z innym endpointem niż dashboard, gdy publish |
| `src/service-defaults/JJDevHub.ServiceDefaults/Extensions.cs` | bez zmiany kontraktu; już czyta `OTEL_EXPORTER_OTLP_ENDPOINT` |
| `deploy/grafana/gateway-red.json` | tablica RED bramy, commitowana |

Tablica jest JSON Grafany i da się ją zaimportować. Nie jest klikana od zera na maszynie i zapominana.

## Plan

### 1. Kontener w AppHost

Zasób kontenera `grafana/otel-lgtm` z przypiętym tagiem. Porty `4317` i `4318` są w sieci compose, nie na `0.0.0.0` maszyny. Port `3000` jest na `127.0.0.1`. Brama dostaje `OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-lgtm:4317` tylko w środowisku publish. W `aspire run` Aspire sam kieruje na dashboard i tego nie nadpisujemy drugim adresem.

### 2. Zasób bramy

`service.name` to `gateway`. `service.version` to wartość `SERVICE_VERSION`. Oba ustawia ServiceDefaults ze zmiennych, które już istnieją: `OTEL_SERVICE_NAME` i atrybut wersji z `SERVICE_VERSION`. Nie wpisujemy nazwy w kodzie bramy osobno od nazwy komponentu.

### 3. Tablica

`deploy/grafana/gateway-red.json` ma trzy panele: liczba żądań, odsetek błędów, czas trwania. Zapytanie PromQL grupuje po trasie, nie po pełnym URL. Import na maszynie jest poleceniem z checkpointu albo provisioningiem w kontenerze, jeśli obraz w przypiętej wersji czyta katalog provisioning bez dodatkowej budowy. Jeśli nie czyta, checkpoint importuje tablicę raz i README mówi, że plik w git jest źródłem, a kliknięcie na maszynie się nie liczy.

### 4. Awaria

Zatrzymanie kontenera `otel-lgtm` i `GET /health/live` bramy nadal zwraca `200`. To jest test ręczny checkpointu. Nie ma testu, który wymaga żywego collectora, żeby brama wstała.

### Co jest do czego

| Sygnał | Gdzie ląduje na maszynie |
| --- | --- |
| ślad | Tempo |
| metryka | Prometheus w tym samym obrazie |
| log | Loki |
| podgląd | Grafana na localhost |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| brama i otel-lgtm działają | jedno `GET /versions` | w Grafanie da się znaleźć ślad o `service.name=gateway` |
| otel-lgtm zatrzymany | `GET /health/live` | `200` |

## Checkpoint

```bash
curl -fsS http://127.0.0.1:8080/versions
curl -fsS -o /dev/null http://127.0.0.1:3000/login
docker stop otel-lgtm
curl -fsS http://127.0.0.1:8080/health/live
docker start otel-lgtm
```

Nazwa kontenera w poleceniu ma zgadzać się z nazwą z wygenerowanego compose. Jeśli generator nazwie ją inaczej, checkpoint używa tej nazwy, a numer nie wymyśla drugiej.

Z laptopa spoza maszyny port `3000` nie odpowiada. W `deploy/cloudflare/config.yml` nie ma `3000`.

## Umiesz wyjaśnić

Dlaczego nie Jaeger?

Bo LGTM już ma Tempo. Drugi magazyn śladów to drugi adres w głowie i drugi dysk. Jaeger nie wnosi tu sygnału, którego Tempo nie przyjmie.

Dlaczego brama nie wystawia `/metrics` na tunel?

Bo tunel jest dla produktu. Metryki idą do collectora w sieci lokalnej. Scrape przez publiczną nazwę miesza monitoring z API.

Co wolno dać w etykietę metryki?

Nazwę usługi, wersję, trasę, kod statusu. Nie wolno dać sluga, identyfikatora użytkownika ani correlation id. To ostatnie jest w logu i w śladzie.

Dlaczego panel nie ma hostname?

Bo hasło panelu i publiczna nazwa to kwestia czasu, nie planu. Wejście jest z maszyny. Access można dodać później, obok bramy, nie przez nią.

Co zostaje na laptopie?

Dashboard Aspire przy `aspire run`. Laptop nie wysyła śladów na maszynę. Inaczej lokalny ruch zaśmieca wykres wydania.

## Zasady

- Obraz LGTM ma przypięty tag.
- Porty OTLP nie są na `0.0.0.0`.
- Grafana nie jest w tunelu.
- Etykieta metryki nie zawiera identyfikatora treści ani osoby.
- Pad collectora nie psuje `/health/live`.
- Log nie zawiera `Authorization`.

## Poza zakresem

- Propagacja `traceparent` przez Kafkę dojdzie w [08-slady.md](../04-zdarzenia/08-slady-i-metryki.md), razem z pierwszą wiadomością.
- HPA czytające metryki jest w [07-skalowanie.md](../13-kubernetes/07-skalowanie.md). Do tego czasu wystarczy metrics-server klastra, a tu klastra jeszcze nie ma.
- Redis i jego metryki nie istnieją.

## Źródła

- [OpenTelemetry](https://opentelemetry.io/docs/concepts/). Wiążące są trzy sygnały i `service.name`. Dotyczy eksportu OTLP z ServiceDefaults. Ze źródła nie bierzemy drugiego SDK w `Program.cs` bramy.
- [grafana/otel-lgtm](https://github.com/grafana/docker-otel-lgtm). Wiążące jest to, że jeden obraz niesie collector, Tempo, Prometheus, Loki i Grafanę. Dotyczy wyboru zamiast Jaegera. Ze źródła nie bierzemy `:latest`.
- [Prometheus cardinality](https://prometheus.io/docs/practices/naming/). Wiążący jest zakaz etykiet o wysokiej kardynalności. Dotyczy braku sluga w metryce. Ze źródła nie bierzemy scrape przez publiczny hostname.

## Gotowe gdy

- Po `GET /versions` ślad bramy jest w Grafanie na maszynie.
- Zatrzymany collector nie psuje `GET /health/live`.
- Port `3000` nie jest w konfiguracji tunelu i nie słucha na adresie innym niż localhost.
- Tablica RED jest plikiem w git.
- Laptopowy `aspire run` nadal pokazuje dashboard Aspire, a nie wysyła ruchu na maszynę.
