# 05. Projektor i inbox

Poprzedni numer to [relay](04-relay.md). Następny to [odczyt Dapperem](06-odczyt-dapper.md).

Ten numer stawia `Learning.Projector`: konsumenta `learning.catalog`, tabelę inbox i zapis do `learning_read`. Drugie dostarczenie tego samego `message-id` nie zmienia odczytu drugi raz. Piąta porażka tej samej wiadomości ląduje na `learning.catalog.dlq`. Offset commituje się po obsłużeniu, po duplikacie albo po świadomym pominięciu, nie przed.

## Po co

Relay bez konsumenta zostawia topic pełen faktów i pustą bazę odczytu. Klient nadal czytałby zapis. Projektor zamyka drogę do `learning_read`. Inbox zamyka powtórkę: relay może wysłać drugi raz, a drugi pod projektora, gdy dojdzie, może dostać tę samą partię po rebalansie. Bez inboxu ukończenie albo wiersz katalogu dostałby podwójną treść. Tu jeszcze nie ma podwójnego efektu ubocznego poza zapisem wiersza, ale reguła jest ta sama, której Contact użyje przy pushu.

## Dlaczego ta technologia

### Konsument z ręcznym commitem

`EnableAutoCommit = false`, `EnableAutoOffsetStore = false`, `AutoOffsetReset.Earliest`, `PartitionAssignmentStrategy.CooperativeSticky`. Grupa `learning-projector`. Problem, który zamyka, to commit offsetu w momencie pobrania, zanim baza odczytu przyjmie wiersz. Odpada auto-commit. Koszt: po padzie procesora ta sama wiadomość wróci. Inbox robi z tego brak skutku.

`StoreOffset` woła się po sukcesie transakcji odczytu albo po rozpoznaniu duplikatu. Potem commit offsetu zgodnie z API klienta: najpierw store, potem commit w interwale albo jawnie po partii. W tym numerze commit jest jawny po każdej wiadomości. Wolniej i czytelniej. Nie optymalizujemy partii, dopóki jedna wiadomość nie jest poprawna.

### Inbox

Tabela `inbox_messages` w `learning_read` z kluczem `(consumer, message_id)`. `TryRegister` wstawia wiersz. Naruszenie unikalności znaczy „już było” i zwraca `false`. Problem, który zamyka, to sprawdzenie `SELECT` i potem `INSERT` bez unikalności: dwa procesy oba widzą brak. Odpada sam `SELECT`. Koszt: tabela inbox rośnie. Nie jest kompaktowana. Usuwanie starych wpisów nie wchodzi w tym numerze, bo usunięcie wpisu przywraca skutek przy ponownej dostawie. Zostają.

Inbox jest w bazie odczytu, w tej samej transakcji co zmiana wiersza odczytu. Duplikat nie otwiera osobnej transakcji zapisu: `INSERT` inboxu i `INSERT` dokumentu albo się udają razem, albo wcale. Przy duplikacie transakcja się wycofuje albo w ogóle nie zaczyna aktualizacji dokumentu, a offset i tak schodzi, bo skutek już był.

### DLQ po pięciu próbach

Licznik prób trzyma projektor w pamięci na czas życia wiadomości i w logu. Trwalszy licznik nie jest potrzebny, jeśli przy restarcie liczymy od zera i ta sama zła wiadomość znowu pięć razy wpadnie na DLQ. To jest akceptowane. Problem, który zamyka, to pętla na zatrutej wiadomości, która blokuje partycję. Odpada nieskończony retry w miejscu. Koszt: zła wiadomość wymaga człowieka i Kafka UI albo `kafka-console-consumer` na DLQ. Nie ma automatycznego powrotu na temat główny. Powrót, gdyby był, użyłby tego samego `message-id`, żeby inbox nadal widział duplikat.

Pięć to stała `Projector:MaxAttempts`.

### Deserializacja po wersji

Konsument czyta `message-type` i `schema-version`. Znany typ `V1` idzie w `ContractJson`. Nieznany typ ląduje na DLQ od razu, bez pięciu prób: powtórka nie nauczy starego procesu nowego typu. Problem, który zamyka, to wyjątek deserializacji w kółko. Odpada rzucanie w pętli jako jedyna obsługa.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| consumer group | `learning-projector`; offset jest wspólny dla grupy |
| inbox | unikalność `(consumer, message_id)` |
| offset | pozycja w partycji; commit po skutku |
| DLQ | temat na wiadomość, której nie stosujemy |
| CooperativeSticky | strategia przypisania partycji, która nie odbiera wszystkich naraz |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/services/Learning/Learning.Projector/CatalogConsumer.cs` | pętla |
| `Learning.Infrastructure/Messaging/KafkaConsumerFactory.cs` | konfiguracja konsumenta |
| `Learning.Infrastructure/Read/Inbox.cs` | `TryRegister` |
| `Learning.Infrastructure/Read/ReadDocumentStore.cs` | zapis wiersza odczytu, na razie surowy JSON |
| `tests/Learning.Projector.Tests/` | duplikat i DLQ |

Kształt tabeli odczytu dopina następny numer. Tu wystarczy tabela `read_document (slug, kind, payload jsonb, version)` albo wiersz, który następny numer rozszerzy migracją. Żeby nie malować dwa razy, ten numer tworzy `inbox_messages` i `catalog_document` z kolumnami, których Dapper użyje: `slug`, `kind`, `language` nie jest osobno, payload ma obie kultury, `visibility`, `updated_at`. Następny numer dopisuje zapytania, nie wymyśla drugiej tabeli.

## Plan

### 1. Pętla

`Consume` w `while (!token.IsCancellationRequested)`. Deserializacja. `TryRegister` razem z upsertem dokumentu. `StoreOffset` i `Commit`. Przy `false` z inboxu sam offset, bez upsertu.

### 2. Upsert

Klucz dokumentu to `(kind, slug)`. `LessonRevisedV1` nadpisuje payload lekcji. Starsza wiadomość, która przyjdzie później w tej samej partycji, nie powinna się zdarzyć przy jednym producencie i kluczu slug. Jeśli się zdarzy, wygrywa większy `updated_at` z nagłówka albo z pola, nie ślepy upsert. Wiadomości `V1` niosą `occurredAt` dodane do rekordu, jeśli go jeszcze nie ma. Jeśli kontrakt z numeru kontraktów nie ma `occurredAt`, ten numer dodaje pole na końcu rekordu. To jest kompatybilne wstecz dla JSON, gdy konsument ignoruje nieznane pole, a stary producent go nie wysyła: wtedy `occurredAt` przy deserializacji jest domyślne i upsert i tak nadpisuje. Lepiej dodać pole teraz, zanim poleci pierwszy tag, niż robić `V2` dzień później. Kontrakt z poprzedniego numeru jest poprawiany w tym scaleniu tylko jeśli tag jeszcze nie wyszedł. Jeśli wyszedł, pole nie wchodzi do `V1`, tylko czeka na numer ewolucji. Ten numer zakłada, że tag Learning ze zdarzeniami jeszcze nie wyszedł, bo relay dopiero powstał. Pole `OccurredAt` dopinamy do rekordów `V1`.

To jest jedyna świadoma zmiana kontraktu `V1` zanim wyjdzie na maszynę. Po pierwszym tagu z tymi rekordami pola się nie zmienia. Zasada „nie edytuj V1” obowiązuje od pierwszego tagu, nie od pierwszego pliku.

### 3. DLQ

Po pięciu wyjątkach zapisu projektor publikuje oryginalne bajty na `learning.catalog.dlq` z nagłówkiem `error` i commituje offset źródłowy. Nieznany `message-type` idzie na DLQ za pierwszym razem.

### Co jest do czego

| Element | Skutek przy duplikacie |
| --- | --- |
| inbox | `false`, bez drugiego upsertu |
| offset | schodzi także przy duplikacie |
| DLQ | nie dostaje duplikatu, który inbox rozpoznał |

## Testy

Nazwa: `Same_message_id_does_not_apply_twice`.

| Arrange | Act | Assert |
| --- | --- | --- |
| jedna wiadomość `LessonPublishedV1` | drugi raz te same bajty i ten sam `message-id` | jeden wiersz w `catalog_document`, dwa podejścia, drugie `TryRegister` zwraca `false` |

Nazwa: `Unknown_type_goes_to_dlq_once`.

```bash
dotnet test tests/Learning.Projector.Tests --nologo --filter "Category=Integration"
```

## Checkpoint

```bash
aspire run
```

Publikacja lekcji, po chwili `SELECT` w `learning_read` zwraca slug. Ponowne wysłanie tego samego `message-id` narzędziem testowym nie dodaje drugiego wiersza. Zatruta wiadomość po pięciu próbach jest na DLQ, a partycja główna nie stoi: kolejna dobra wiadomość schodzi.

## Umiesz wyjaśnić

Dlaczego offset nie schodzi przed zapisem?

Bo wtedy pad między commitem offsetu a commitem bazy gubi fakt na zawsze. W drugą stronę, pad po bazie a przed offsetem, daje powtórkę. Powtórkę inbox umie. Gubienia nie umie cofnąć.

Dlaczego inbox jest w tej samej transakcji co dokument?

Bo sam inbox bez dokumentu oznacza „już zrobione” i nigdy nie zrobi. Sam dokument bez inboxu zrobi się drugi raz. Jedna transakcja albo żaden z tych dwóch wierszy.

Czemu nieznany typ nie ma pięciu prób?

Bo za piątym razem nadal jest nieznany. To nie jest awaria sieci. To jest wersja, której ten proces nie ma. DLQ i człowiek, albo nowy proces, który typ zna.

Co z kolejnością dwóch slugów?

Nie ma jej między partycjami. Jest w jednym slugu, bo klucz partycji to slug. Projektor nie sortuje globalnie.

Dlaczego DLQ nie wraca sam na temat?

Bo wróciłby w pętli. Powrót jest ręczny i z tym samym `message-id`, żeby inbox nie zastosował skutku drugi raz, jeśli część skutku jednak była.

## Zasady

- Auto-commit wyłączony.
- Inbox i dokument w jednej transakcji na `learning_read`.
- Projektor nie otwiera `learning_write`.
- Nieznany typ idzie na DLQ bez pięciu prób.
- Piąty błąd zapisu idzie na DLQ, offset źródłowy schodzi.
- Grupa nazywa się `learning-projector`.

## Poza zakresem

- Zapytania HTTP do tego dokumentu są w [06-odczyt-dapper.md](06-odczyt-dapper.md).
- `V2` i upcaster są w [07-ewolucja-schematu.md](07-ewolucja-schematu.md).
- Drugi pod i rebalans są w [07-skalowanie.md](../13-kubernetes/07-skalowanie.md). Inbox już jest na to gotowy.
- Ślad między producerem a konsumentem jest w [08-slady-i-metryki.md](08-slady-i-metryki.md).

## Źródła

- [Kafka consumer](https://kafka.apache.org/documentation/#consumerconfigs). Wiążące są `enable.auto.commit=false` i ręczny offset. Dotyczy pętli. Ze źródła nie bierzemy commita na początku pętli.
- [Idempotent Receiver](https://www.enterpriseintegrationpatterns.com/patterns/messaging/IdempotentReceiver.html). Wiążące jest to, że druga dostawa nie ma drugiego skutku. Dotyczy inboxu. Ze źródła nie bierzemy „dokładnie raz” jako obietnicy Kafki.
- [Dead letter](https://kafka.apache.org/documentation/#connect_errorreporting). Wiążące jest odsunięcie zatrutej wiadomości, żeby partycja ruszyła. Dotyczy DLQ. Ze źródła nie bierzemy automatycznego powrotu bez tego samego `message-id`.

## Gotowe gdy

- Ten sam `message-id` zostawia jeden dokument.
- Offset nie schodzi, gdy transakcja odczytu się wycofa.
- Nieznany typ jest na DLQ po jednej próbie.
- Projektor nie ma connection stringa do `learning_write`.
- Tabela inboxu ma klucz `(consumer, message_id)`.
