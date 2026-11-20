# 02. Outbox

Poprzedni numer to [kontrakty](01-kontrakty.md). Następny to [Kafka](03-kafka.md).

Ten numer zapisuje wiersz outboxa w tej samej transakcji co zmianę kursu albo lekcji. Nikt jeszcze nie publikuje na broker. Jest wiersz z `published_at` pustym. Pad procesu po commicie nie gubi faktu. Pad przed commitem nie zostawia faktu bez zapisu.

## Po co

Publikacja na Kafkę w handlerze, obok `SaveChanges`, ma dwa złe zakończenia. Commit bazy i błąd brokera: kurs jest, zdarzenia nie ma, Cv nigdy się nie dowie. Wysłanie na broker i wyjątek bazy: zdarzenie jest, kursu nie ma, projektor zbuduje widok z niczego. Jedna transakcja z wierszem outboxa zamyka oba. Osobny proces wyśle wiersz później. Ten numer jest tylko o wierszu.

## Dlaczego ta technologia

### Tabela w bazie zapisu

`outbox_messages` leży w `learning_write`. Kolumny: `id` bigint identity, `message_id` uuid unikalny, `topic`, `key`, `type`, `schema_version`, `payload` jsonb, `correlation_id`, `trace_parent` nullable, `occurred_at`, `published_at` nullable, `attempts`, `last_error`. Problem, który zamyka, to outbox w Redisie albo w drugiej bazie, której nie obejmuje commit kursu. Odpada osobna baza „na zdarzenia”. Koszt: tabela rośnie. Wiersze z `published_at` starsze niż siedem dni usuwa relay, gdy powstanie. Do tego czasu test nie generuje milionów wierszy.

### Port `IOutbox` w aplikacji

`EnqueueAsync(OutboxMessage)` dopisuje do kontekstu, nie commituje sam. Commit zostaje w `IUnitOfWork`. Problem, który zamyka, to handler wołający `DbContext` albo Kafka producenta. Odpada publikacja w endpoincie. Koszt: handler publikacji ma dwie zależności więcej, outbox i zegar, i musi je wołać. Test pamięta, czy wołał.

`OutboxMessage` jest rekordem w aplikacji. Nie jest encją domeny. Domena kursu nie wie, że istnieje szyna.

### Dwa wiersze przy publikacji technologii

Handler po udanej fabryce dopisuje `TechnologyPublishedV1` na `learning.catalog` i `IntegrationStateV1` na `learning.integration`. Ten sam `SaveChanges`. Problem, który zamyka, to projektor katalogu i Cv czytające jeden temat i mieszające fakty jednorazowe z ostatnim stanem. Odpada jeden temat na wszystko. Koszt: dwa wiersze albo żaden, nigdy jeden z dwóch, bo to jedna transakcja.

Klucz wiadomości (`key`) to slug. `message_id` to nowy `Guid`. `type` to wynik `EventName.For`.

### Brak publikacji przy odmowie

Gdy fabryka zwraca błąd, handler nie woła outboxa i nie woła `SaveChanges`. Test to już sprawdzał dla braku technologii. Ten numer dopisuje asercję: lista outboxa jest pusta.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| outbox | tabela wiadomości do wysłania, w tej samej bazie co stan |
| `published_at` | puste, dopóki relay nie dostanie potwierdzenia brokera |
| `message_id` | identyfikator dostawy, klucz przyszłego inboxu |
| key | klucz partycji, u nas slug |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Learning.Application/Ports/IOutbox.cs` | `EnqueueAsync` |
| `Learning.Application/Ports/OutboxMessage.cs` | rekord wiadomości |
| `Learning.Infrastructure/Persistence/Rows/OutboxRow.cs` | wiersz EF |
| `Learning.Infrastructure/Adapters/EfOutbox.cs` | implementacja portu |
| `Learning.Infrastructure/Persistence/Migrations/` | migracja tabeli |
| handlery publikacji i rewizji | dopisują wiadomość przed `SaveChanges` |

`EfOutbox` nie woła Kafki. Referencja do `Confluent.Kafka` w tym projekcie jeszcze nie jest wymagana. Jeśli infrastruktura już ją ma z innego powodu, adapter outboxa i tak jej nie używa.

## Plan

### 1. Migracja

`AddOutbox`. Indeks częściowy na wiersze z `published_at IS NULL`, żeby relay nie skanował wysłanych. Unikalność `message_id`.

### 2. Handler

`PublishCourseHandler` po `AddAsync` kursu woła `IOutbox.EnqueueAsync` dwa razy: katalog i stan integracji. Potem jeden `SaveChangesAsync`. `ReviseLessonHandler` woła outbox z `LessonRevisedV1`. Nie woła stanu integracji, jeśli lekcja nie wchodzi do `learning.integration`. Nie wchodzi.

Korelacja bierze się z portu `ICorrelationId`, ustawionego przez middleware na scoped. W teście bez HTTP port zwraca stały identyfikator. Nie czytamy `HttpContext` w handlerze.

### 3. Payload

`EfOutbox` serializuje rekord przez `ContractJson`. Do kolumny idzie string JSON. Nie idzie graf encji.

### Co jest do czego

| Element | Kto go woła |
| --- | --- |
| `IOutbox` | handler, w tej samej jednostce pracy co repozytorium |
| `OutboxRow` | tylko adapter |
| indeks `published_at IS NULL` | przyszły relay |

## Testy

Nazwa: `Publish_course_writes_course_and_outbox_or_neither`.

| Arrange | Act | Assert |
| --- | --- | --- |
| baza po migracji, technologia już jest | `PublishCourseHandler` | w jednej transakcji jest kurs i dwa wiersze outboxa; `published_at` jest puste |
| fabryka odmawia, na przykład zły slug | ten sam handler | brak nowego kursu i brak nowego wiersza outboxa |

Drugi przypadek da się zrobić transakcją, która rzuca w teście po `Enqueue`, jeśli podstawimy `IUnitOfWork` rzucający przed commitem. Oczekiwanie: po wyjątku nowy `DbContext` nie widzi wiersza. To wymaga prawdziwej transakcji, więc test jest `Category=Integration`.

```bash
dotnet test tests/Learning.Infrastructure.Tests --nologo --filter "FullyQualifiedName~Publish_course_writes_course_and_outbox"
```

## Checkpoint

```bash
dotnet test tests/Learning.Infrastructure.Tests --nologo --filter "FullyQualifiedName~outbox"
```

W bazie testowej `SELECT count(*) FROM outbox_messages WHERE published_at IS NULL` po teście publikacji jest większe od zera. Po teście odmowy dla tej samej metody nie ma wiersza z kluczem, którego slug był niepoprawny.

## Umiesz wyjaśnić

Dlaczego nie wysyłamy w handlerze?

Bo broker i baza nie mają wspólnego commita. Outbox zamienia dwa zasoby na jeden commit i późniejszą wysyłkę. Wysyłka może się powtórzyć. Zapis kursu nie może zostać bez wiersza ani z wierszem bez kursu.

Czemu dwa wiersze, a nie jeden?

Bo katalog i stan dla Cv mają inny czas życia. Katalog trzyma fakt. Temat kompaktowany trzyma ostatni stan sluga i gubi pośrednie nazwy. Jeden payload nie obsłuży obu bez warunku w każdym konsumentie.

Co jest kluczem partycji, a co kluczem inboxu?

Slug jest kluczem partycji, żeby kolejne fakty tej samej technologii poszły na tę samą partycję i zachowały kolejność. `message_id` jest identyfikatorem dostawy. Inbox rozpozna powtórkę. Slug by jej nie rozpoznał, bo slug wraca przy każdej rewizji legalnie.

Dlaczego domena nie buduje rekordu kontraktu?

Bo rekord jest kształtem integracji. Domena zostałaby zależna od JSON i od nazw topiców. Handler jest miejscem mapowania. Mapowanie jest ręczne.

Co z `trace_parent` na tym numerze?

Kolumna jest pusta. Numer śladów ją wypełni z `Activity.Current`. Pusta kolumna nie blokuje wysyłki.

## Zasady

- Outbox jest w `learning_write`, nie w `learning_read`.
- `Enqueue` nie commituje sam.
- Odmowa fabryki nie zostawia wiersza.
- Payload powstaje z `ContractJson`, nie z encji.
- Adapter outboxa nie otwiera połączenia do Kafki.
- Lekcja nie tworzy wiersza na `learning.integration`.

## Poza zakresem

- Broker jest w [03-kafka.md](03-kafka.md).
- Pętla wysyłki jest w [04-relay.md](04-relay.md).
- Inbox konsumenta jest w [05-projektor-i-inbox.md](05-projektor-i-inbox.md).
- Ślad w nagłówku jest w [08-slady-i-metryki.md](08-slady-i-metryki.md).

## Źródła

- [Transactional outbox](https://microservices.io/patterns/data/transactional-outbox.html). Wiążący jest jeden commit stanu i wiadomości oraz osobna wysyłka. Dotyczy tabeli w `learning_write`. Ze źródła nie bierzemy pollingu bazy przez inną usługę. Wysyła proces tej samej usługi.
- [PostgreSQL partial indexes](https://www.postgresql.org/docs/current/indexes-partial.html). Wiążący jest indeks `WHERE published_at IS NULL`. Dotyczy przyszłego `SELECT` relayu. Ze źródła nie bierzemy pełnego skanu tabeli jako „na razie wystarczy” w kodzie relayu, gdy relay powstanie.
- Poprzedni numer kontraktów. Wiążąca jest nazwa z atrybutu w kolumnie `type`. Dotyczy `EfOutbox`. Ze źródła nie bierzemy nazwy klasy w tej kolumnie.

## Gotowe gdy

- Test integracyjny pokazuje kurs i dwa wiersze albo nic.
- `published_at` po handlerze jest puste.
- W projekcie outboxa nie ma `Produce`.
- `LessonRevisedV1` nie trafia na temat integracji.
- Migracja `AddOutbox` jest w git.
