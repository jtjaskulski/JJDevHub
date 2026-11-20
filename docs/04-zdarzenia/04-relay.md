# 04. Relay

Poprzedni numer to [Kafka](03-kafka.md). Następny to [projektor i inbox](05-projektor-i-inbox.md).

Ten numer stawia `Learning.Relay`: proces w tle, który czyta niewysłane wiersze outboxa i publikuje je na Kafkę. Dopiero po potwierdzeniu brokera ustawia `published_at`. Publikacja jest idempotentna po stronie producenta. Kolejność w jednym slugu wynika z klucza i z tego, że jedna partia nie wyprzedza drugiej w tym samym kluczu bez potrzeby.

## Po co

Wiersz outboxa, którego nikt nie czyta, nie jest zdarzeniem. Jest martwym wierszem. Relay zamyka drogę z tabeli na topic. Bez potwierdzenia przed `published_at` restart w środku wysyłki gubi fakt albo oznacza go jako wysłany, choć broker go nie ma. Oba końce są złe. Potwierdzenie `Acks.All` i dopiero potem update wiersza zamyka ten, który da się zamknąć bez wspólnego commita z brokerem. Drugi raz ten sam `message_id` może wyjść po restarcie. Inbox konsumenta to zniesie. Relay nie udaje, że jest dokładnie raz.

## Dlaczego ta technologia

### `FOR UPDATE SKIP LOCKED`

Partia to `SELECT ... WHERE published_at IS NULL ORDER BY id LIMIT 100 FOR UPDATE SKIP LOCKED`. Problem, który zamyka, to dwa relaye biorące ten sam wiersz. Na razie relay jest jeden. Klauzula i tak jest, bo drugi pod dojdzie później i nie chcemy wtedy przepisywać pętli. Odpada `SELECT` bez blokady. Koszt: transakcja trzyma blokadę na czas `Produce`. Partia jest mała, 100 wierszy, żeby nie trzymać jej przez minutę.

`SKIP LOCKED` wymaga transakcji. Po sukcesie produce wiersze partii dostają `published_at` i transakcja się commituje. Po błędzie produce transakcja się wycofuje, `attempts` rośnie w osobnym, krótkim update poza wycofaną transakcją albo w tej samej, jeśli błąd jest po produce i nie chcemy powtarzać w nieskończoność w tej samej sekundzie. Konkretnie: przy wyjątku produce transakcja read-lock się wycofuje, a drugi update zwiększa `attempts` i `last_error`. Wiersz zostaje niewysłany.

### Producent idempotentny

`EnableIdempotence = true`, `Acks.All`, `LingerMs = 5`, kompresja `Zstd`. Problem, który zamyka, to duplikat wewnątrz jednej sesji producenta przy retry brokera. Odpada `Acks.Leader`. Koszt: produce czeka na repliki. Przy jednym brokerze „wszystkie repliki” znaczy ten jeden broker. Nadal czekamy na potwierdzenie zapisu, nie na wrzucenie do bufora.

Klucz wiadomości to slug z kolumny `key`. Nagłówki z kolumn i ze stałych `MessageHeaders`. `producer` ma wartość `learning-relay`.

### Osobny proces

`Learning.Relay` jest workerem, nie wątkiem w API. Problem, który zamyka, to restart API przy deployu, który przerywa wysyłkę i jednocześnie obsługę HTTP. Rozdzielenie nie usuwa przestoju wysyłki, ale HTTP wraca szybciej niż dogonienie outboxa. Odpada `BackgroundService` w tym samym procesie co Kestrel, jako docelowy kształt. Na laptopie AppHost i tak uruchamia oba projekty. Koszt: drugi obraz, ten sam tag `learning-vX.Y.Z`, zgodnie ze słownikiem wersji.

Obraz buduje ten sam Dockerfile, `PROJECT=Learning.Relay`, `BASE_IMAGE` runtime, nie aspnet.

### Pętla i token

`ExecuteAsync` czeka `Relay:PollIntervalMs` (500) albo budzi się wcześniej, gdy jest praca. `CancellationToken` z hosta przerywa `Task.Delay` i produce. Nie ma `Thread.Sleep`. Nie ma `lock` w poprzek `await`: jedna pętla, jedno połączenie, brak współdzielonego stanu z drugim wątkiem. Problem, który zamyka, to deadlock z blokady trzymanej przez `await`. Odpada pętla `while` na `Task.Run` bez tokenu.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| relay | proces, który czyta outbox i woła producenta |
| `SKIP LOCKED` | pomiń wiersze zablokowane przez inną transakcję |
| `Acks.All` | produce wraca po potwierdzeniu zapisu przez broker |
| idempotentny producent | retry w sesji nie dopisuje drugi raz tego samego |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/services/Learning/Learning.Relay/Program.cs` | host workera |
| `src/services/Learning/Learning.Relay/OutboxPublisher.cs` | pętla |
| `Learning.Infrastructure/Messaging/KafkaProducerFactory.cs` | konfiguracja producenta |
| `Learning.Infrastructure/Messaging/KafkaHeaderExtensions.cs` | nagłówki |
| `tests/Learning.Relay.Tests/` | partia i `published_at`, integracja |

Paczka `Confluent.Kafka` jest w infrastrukturze. Worker referencjonuje infrastrukturę, nie odwrotnie.

## Plan

### 1. Zapytanie partii

SQL w adapterze `IOutboxReader`, nie w stringu rozrzuconym po pętli. Czyta 100 wierszy pod blokadą. Relay dla każdego woła `ProduceAsync`. Gdy wszystkie w partii przejdą, ustawia `published_at = clock` i commituje. Zegar to `TimeProvider`.

### 2. Nagłówki

`message-id` z kolumny, `message-type` z kolumny `type`, `schema-version` z kolumny, `correlation-id` z kolumny, `producer` stały. `traceparent` z kolumny, jeśli nie jest pusta. Pusta nie wysyła nagłówka, nie wysyła pustego stringa.

### 3. AppHost i compose

`AddProject<Learning_Relay>` z referencją do Postgresa zapisu i ze zmienną `Kafka:BootstrapServers`. Na maszynie ten sam tag co api, drugi obraz. `.github/deploy-components.txt` dostaje wpis, że komponent `learning` buduje dwa projekty. Jeśli Learning jeszcze nie jest w tym pliku jako api, ten numer go dopisuje razem z relayem. Deploy uczy się budować więcej niż jeden obraz na tag. Macierz w `deploy.yml` czyta listę projektów komponentu z pliku `deploy/components/learning.txt`, nie z komentarza.

### 4. Porządek

Wiersze idą `ORDER BY id`. Id jest identity, więc jest w kolejności wstawienia. Klucz slug kieruje na partycję. Relay nie przestawia wierszy w pamięci.

### Co jest do czego

| Element | Kto go woła |
| --- | --- |
| `OutboxPublisher` | host workera, nikt z HTTP |
| `KafkaProducerFactory` | relay, później każdy inny producent tej usługi |
| `published_at` | ustawiane tylko po `Produce` bez wyjątku |

## Testy

Nazwa: `Unpublished_row_is_marked_only_after_produce`.

| Arrange | Act | Assert |
| --- | --- | --- |
| wiersz outboxa, broker KRaft z topicem | jeden obieg pętli | `published_at` nie jest puste, metadata topicu widzi rekord |
| broker zatrzymany | jeden obieg | `published_at` puste, `attempts` większe o jeden |

```bash
dotnet test tests/Learning.Relay.Tests --nologo --filter "Category=Integration"
```

## Checkpoint

```bash
aspire run
```

Publikacja kursu w Development, potem w logu relayu widać wysłanie, a `SELECT published_at FROM outbox_messages` nie jest puste. Zatrzymanie kontenera Kafki, kolejna publikacja: wiersz zostaje z pustym `published_at`. Po starcie Kafki, w ciągu dwóch interwałów, wiersz dostaje datę.

## Umiesz wyjaśnić

Dlaczego to nie jest „dokładnie raz”?

Bo między potwierdzeniem brokera a commitem `published_at` proces może paść. Po restarcie wyśle ten sam `message_id` drugi raz. Konsument ma inbox. Obietnica brokera „dokładnie raz” nie zastępuje inboxu.

Po co `SKIP LOCKED`, skoro relay jest jeden?

Bo drugi pod nie może być powodem przepisywania SQL w dniu skalowania. Jeden relay i tak działa poprawnie z tą klauzulą.

Dlaczego nie `BackgroundService` w API?

Bo deploy i restart HTTP nie powinny być tym samym procesem co doganianie outboxa. Osobny obraz, ten sam numer wersji.

Co daje `Acks.All` przy jednym brokerze?

Czeka na zapis u lidera, który jest jedyną repliką. Nie wraca w momencie wrzucenia do bufora klienta. To jest różnica, która przy padzie procesu klienta ma znaczenie.

Dlaczego jedna pętla bez locka?

Bo nie ma drugiego wątku, który czyta ten sam stan. `lock` przez `await` kończy się deadlockiem albo trzymaniem blokady w czasie sieci. Token anulowania wystarcza do zatrzymania.

## Zasady

- `published_at` ustawia się po produce, w tej samej transakcji co zdjęcie blokady, nie przed produce.
- Klucz wiadomości to slug z wiersza.
- Relay nie czyta `learning_read`.
- Nie ma `Thread.Sleep` i nie ma `lock` obejmującego `await`.
- Obraz relaya ma ten sam tag wersji co api.
- Producent nie tworzy topiców.

## Poza zakresem

- Inbox i projekcja są w [05-projektor-i-inbox.md](05-projektor-i-inbox.md).
- Circuit breaker na produce jest w [09-health-i-odpornosc.md](09-health-i-odpornosc.md). Na tym numerze błąd produce tylko zwiększa `attempts`.
- Ślad w nagłówku wypełnia [08-slady-i-metryki.md](08-slady-i-metryki.md). Relay już kopiuje kolumnę, jeśli jest.

## Źródła

- [Idempotent producer](https://kafka.apache.org/documentation/#producerconfigs_enable.idempotence). Wiążące są `enable.idempotence` i `acks=all`. Dotyczy fabryki producenta. Ze źródła nie bierzemy wyłączenia idempotencji „dla szybkości” na laptopie: ta sama konfiguracja jedzie wszędzie.
- [SELECT FOR UPDATE SKIP LOCKED](https://www.postgresql.org/docs/current/sql-select.html). Wiążące jest pomijanie zablokowanych wierszy. Dotyczy partii relayu. Ze źródła nie bierzemy blokady całej tabeli.
- [BackgroundService](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.backgroundservice). Wiążące jest `ExecuteAsync` i `CancellationToken`. Dotyczy pętli. Ze źródła nie bierzemy `async void`.

## Gotowe gdy

- Wiersz dostaje `published_at` tylko wtedy, gdy broker potwierdzi.
- Przy leżącym brokerze `attempts` rośnie, a `published_at` zostaje puste.
- Po powrocie brokera ten sam wiersz schodzi.
- Relay jest osobnym projektem i osobnym obrazem o tym samym numerze wersji.
- W pętli nie ma blokady trzymanej przez `await`.
