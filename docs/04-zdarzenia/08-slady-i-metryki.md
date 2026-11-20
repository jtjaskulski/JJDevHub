# 08. Ślady i metryki przez Kafkę

Poprzedni numer to [ewolucja](07-ewolucja-schematu.md). Następny to [health](09-health-i-odpornosc.md).

Ten numer łączy ślad `POST` z śladem projektora. `traceparent` jedzie w nagłówku wiadomości. Metryki liczą przyjęte lekcje, wysłany outbox i opóźnienie między `occurred_at` a zapisem odczytu. Log używa `LoggerMessage`, nie interpolacji na gorącej ścieżce.

## Po co

Bez propagacji widać ślad HTTP i osobno ślad konsumenta. Nie widać, że to ta sama publikacja. Nagłówek `traceparent` zamyka łańcuch. Metryka opóźnienia zamyka pytanie „czy projektor nadąża”, bez czytania logu po kolei.

Korelacja `X-Correlation-Id` zostaje. To nie jest ślad. Ślad ma identyfikator z W3C. Mieszanie ich w jedno pole psuje oba narzędzia.

## Dlaczego ta technologia

### `Activity` w procesie, SDK w hoście

Relay i projektor wołają `ActivitySource` o nazwie `JJDevHub.Learning`. Źródło jest w infrastrukturze albo w aplikacji jako typ bez paczki OpenTelemetry: `Activity` jest w BCL. SDK, które wysyła OTLP, jest w ServiceDefaults, już podpięte do workera tak samo jak do API. Problem, który zamyka, to `Console.WriteLine` jako jedyny sposób zobaczenia pętli. Odpada własny protokół. Koszt: nazwy spanów trzeba trzymać stabilnie, bo tablica Grafany ich używa.

Span producenta: `learning.outbox.publish`, kind `Producer`. Span konsumenta: `learning.catalog.consume`, kind `Consumer`, parent z nagłówka.

### Propagacja

Przy zapisie outboxa `EfOutbox` wpisuje `Activity.Current?.Id` do `trace_parent`, jeśli bieżący ślad istnieje. Relay kopiuje kolumnę do nagłówka. Projektor wyciąga kontekst przez `Propagators.DefaultTextMapPropagator` i startuje span jako dziecko. Problem, który zamyka, to nowy trace na każdą wiadomość, niepowiązany z `POST`. Odpada przekazywanie correlation id jako trace id. Koszt: pusty nagłówek, gdy `POST` nie miał aktywności, startuje nowy ślad. To jest poprawne, nie błąd.

### Metryki

`Meter` o nazwie `JJDevHub.Learning`. Liczniki: `learning.lessons.published`, `learning.outbox.published`. Histogram: `learning.projection.lag.seconds`, różnica między `occurred_at` a chwilą upsertu. Problem, który zamyka, to brak liczby w Grafanie poza ogólnym HTTP RED bramy. Odpada licznik na każdy slug. Etykieta wolno zawiera `kind` i wynik `stored` albo `duplicate`. Nie wolno zawierać slugu. Koszt: nie zobaczymy jednej lekcji na wykresie. Zobaczymy ją w śladzie.

### `LoggerMessage`

Log pętli jest źródłem generowanym, z placeholderami. Problem, który zamyka, to interpolacja `$"published {slug}"`, która alokuje string nawet, gdy poziom logu jest wyłączony, i która wciąga slug do szablonu. Slug może być w polu strukturalnym. Nie jest w szablonie wiadomości jako zmieniająca się część template. Szablon jest stały: `Published lesson {Slug}`. To jest dozwolone jako pole. Zabronione jest budowanie szablonu ze sluga.

### Tablica

Do `deploy/grafana/` dochodzi `learning-projection.json`: lag i liczba publikacji. Import tak samo jak tablica bramy, na localhost. Bez nowego hostname.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| `traceparent` | nagłówek W3C, rodzic spanu konsumenta |
| span kind | `Producer` albo `Consumer` |
| lag | sekundy od faktu do projekcji |
| `LoggerMessage` | źródło generowane, stały szablon |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Learning.Infrastructure/Telemetry/LearningTelemetry.cs` | `ActivitySource`, `Meter`, instrumenty |
| `Learning.Infrastructure/Adapters/EfOutbox.cs` | zrzut `Activity.Current.Id` |
| `Learning.Infrastructure/Messaging/KafkaTracePropagation.cs` | wstawienie i wyciągnięcie kontekstu |
| `Learning.Relay/OutboxPublisher.cs` | span producenta |
| `Learning.Projector/CatalogConsumer.cs` | span konsumenta i histogram |
| `deploy/grafana/learning-projection.json` | tablica |

Workery wołają `AddServiceDefaults`, więc eksporter już jest. Ten numer nie dodaje drugiego SDK.

## Plan

### 1. Zapis nagłówka

Gdy jest `Activity.Current`, jego `Id` ląduje w kolumnie. Relay, jeśli kolumna niepusta, ustawia nagłówek `traceparent`. Nie parsujemy go ręcznie przecinkami. Używamy propagatora.

### 2. Konsument

Przed handlerem wiadomości `StartActivity` z parentem z propagatora. Po upsertie `Record` na histogramie. Przy duplikacie licznik `duplicate` rośnie, histogram też wolno zapisać: lag duplikatu jest mniej ciekawy, więc go nie zapisujemy. Zapisujemy lag tylko przy pierwszym zastosowaniu.

### 3. Atrybuty

Na spanie są `messaging.system=kafka`, `messaging.destination` równe nazwie topicu, `messaging.message.id`. Nie ma treści lekcji. Nie ma connection stringa.

### 4. Test

Test jednostkowy propagatora: wstawienie do słownika nagłówków i wyciągnięcie daje ten sam trace id. Nie potrzebuje brokera. Test integracyjny z brokerem jest dodatkowy, jeśli jednostkowy nie rusza `Activity`. Jednostkowy wystarcza, bo propagacja jest czystą funkcją na nagłówkach.

### Co jest do czego

| Instrument | Kto inkrementuje |
| --- | --- |
| `learning.lessons.published` | handler albo projektor przy pierwszym zapisie dokumentu lekcji; jedno miejsce, projektor, bo on wie, że dokument powstał |
| `learning.outbox.published` | relay po produce |
| `learning.projection.lag.seconds` | projektor przy pierwszym zastosowaniu |

Licznik publikacji w projektorze, nie w handlerze, żeby nie liczyć odmowy, która nie weszła na szynę, i nie liczyć dwa razy po stronie HTTP i konsumenta.

## Testy

Nazwa: `Traceparent_roundtrip_keeps_trace_id`.

| Arrange | Act | Assert |
| --- | --- | --- |
| `Activity` z znanym id | `Inject` do nagłówków i `Extract` | trace id dziecka równy rodzicowi |

```bash
dotnet test tests/Learning.Infrastructure.Tests --nologo --filter "FullyQualifiedName~Traceparent_roundtrip"
```

## Checkpoint

```bash
aspire run
```

`POST` lekcji w Development. W Grafanie na maszynie albo w dashboardzie Aspire na laptopie widać jeden ślad z spanem HTTP i spanem `learning.catalog.consume`. Histogram ma punkt. W logu projektora szablon nie zawiera wklejonego sluga jako części stałej: wiadomość ma placeholder.

Zatrzymanie `otel-lgtm` nie psuje `POST`. Ślad przepada, lekcja zostaje.

## Umiesz wyjaśnić

Czym `traceparent` różni się od `X-Correlation-Id`?

`traceparent` wiąże spany w drzewo i ma format W3C. Correlation id jest dla człowieka i dla odpowiedzi HTTP. Oba mogą iść obok siebie. Jeden nie zastępuje drugiego.

Dlaczego span konsumenta jest dzieckiem, a nie nowym korzeniem?

Bo inaczej nie da się z wykresu przejść od wolnego `POST` do wolnego projektora. Nowy korzeń wygląda jak inne żądanie.

Dlaczego slug nie jest etykietą metryki?

Bo każdy slug to nowa seria. Prometheus tego nie uniesie, gdy katalog dorośnie. Slug jest atrybutem spanu albo polem logu, jeśli poziom logu na to pozwala. Nie jest etykietą.

Co daje `LoggerMessage`?

Stały szablon i brak alokacji, gdy poziom jest wyłączony. Interpolacja buduje string zawsze. Na pętli wiadomości to widać.

Dlaczego licznik lekcji jest w projektorze?

Bo fakt „dokument jest w odczycie” zna projektor. Handler zna zapis. Dwa liczniki na te dwa miejsca wolno mieć, ale ten numer ma jeden, żeby tablica nie pokazywała dwóch prawd. Wysyłka outboxa ma osobny licznik, bo to inne pytanie: czy relay nadąża.

## Zasady

- Treść lekcji nie wchodzi do atrybutu spanu ani do metryki.
- Etykieta metryki nie zawiera sluga ani `message-id`.
- Brak nagłówka trace nie jest błędem przetwarzania.
- Pad eksportera nie wycofuje transakcji outboxa ani dokumentu.
- Nie ma drugiego SDK obok ServiceDefaults.
- Correlation id nie jest wpisywany w pole trace id.

## Poza zakresem

- Health i circuit breaker są w [09-health-i-odpornosc.md](09-health-i-odpornosc.md).
- Alerty burn-rate nie wchodzą. Jest tablica. Próg stronniczy na lag może dojść później i nie jest warunkiem tego numeru.
- Audyt nie jedzie tym śladem jako zamiennik. Audyt ma własny numer.

## Źródła

- [W3C trace context](https://www.w3.org/TR/trace-context/). Wiążący jest nagłówek `traceparent`. Dotyczy propagacji przez Kafkę. Ze źródła nie bierzemy własnego formatu „prawie W3C”.
- [OpenTelemetry messaging semantic conventions](https://opentelemetry.io/docs/specs/semconv/messaging/). Wiążące są kind `Producer` i `Consumer` oraz atrybut destination. Dotyczy nazw spanów. Ze źródła nie bierzemy treści wiadomości w atrybucie.
- [LoggerMessage source generator](https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator). Wiążący jest stały szablon. Dotyczy pętli relayu i projektora. Ze źródła nie bierzemy interpolacji stringa w tej pętli.
- [Metrics cardinality](https://prometheus.io/docs/practices/naming/). Wiążący jest zakaz etykiet o wysokiej kardynalności. Dotyczy braku sluga. Ze źródła nie bierzemy licznika per użytkownik.

## Gotowe gdy

- Test rundy `traceparent` przechodzi bez brokera.
- Po `POST` widać span HTTP i span konsumenta w jednym śladzie na laptopie albo na maszynie.
- Histogram lagu ma serię bez etykiety sluga.
- Zatrzymany collector nie cofa publikacji.
- Log pętli używa `LoggerMessage`.
