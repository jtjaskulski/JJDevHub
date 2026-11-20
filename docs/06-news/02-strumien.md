# 02. Strumień w Postgresie

Poprzedni numer to [agregat](01-agregat-i-zdarzenie.md). Następny to [zapis](03-zapis.md).

Ten numer zapisuje strumień `Article` w `news_write`. Jest `IEventStore`: dopisanie oczekujące wersji i odczyt zdarzeń. Implementacja najpierw przechodzi test w pamięci, potem ten sam test na Postgresie. Wersja strumienia i pozycja globalna to dwa liczniki.

## Po co

Agregat w pamięci znika z procesem. Strumień w tabeli zostawia brzmienia. Optymistyczna współbieżność na wersji strumienia zamyka dwa zapisy tego samego artykułu. Osobna, globalna pozycja zamyka pytanie „co się stało po kolei w tej usłudze”, którego sama wersja jednego strumienia nie odpowiada. Projekcja może użyć pozycji jako kursora. Na tym numerze kursor jeszcze nie jedzie do odczytu. Kolumna już jest, żeby nie migrować jej w pośpiechu później.

## Dlaczego ta technologia

### Tabela zdarzeń, nie jedna kolumna JSON z całym artykułem

`stream_events`: `stream_id`, `version`, `position` bigint, `type`, `payload` jsonb, `occurred_at`. Klucz `(stream_id, version)`. `position` unikalny, z sekwencji. Problem, który zamyka, to nadpisanie jednego wiersza artykułu, które kasuje historię. Odpada trzymanie listy zdarzeń w jednej komórce, bo równoległy zapis nie umie jej bezpiecznie rozszerzyć bez czytania całości. Koszt: odczyt strumienia to kilka wierszy. Artykuł nie ma ich tysięcy.

`stream_id` to `article/{guid}`, nie sam slug. Slug może być w payloadzie. Identyfikator strumienia nie zmienia się, gdyby kiedykolwiek zmieniała się reguła sluga. Slug i tak jest zamrożony. Identyfikator i tak jest `Guid`.

### Oczekiwana wersja

`Append` przyjmuje `expectedVersion`. Pierwszy zapis oczekuje `0`. Konflikt zwraca `Result` z `stream.version.conflict` i rodzajem `Conflict`. Nie rzuca, poza awarią połączenia. Problem, który zamyka, to dwa `Revise` na wersji 3, które oba dopisują 4. Unikalność `(stream_id, version)` jest siatką pod spodem. Port tłumaczy naruszenie na `Result`. Odpada `SELECT` wersji i `INSERT` bez unikalności.

### Ten sam port, dwie implementacje

`IEventStore` leży w `src/shared/JJDevHub.EventStore`. Implementacja pamięci jest w tym projekcie i służy testom domenowym News oraz później Cv. Implementacja Postgresa jest w `JJDevHub.EventStore.Postgres`. Problem, który zamyka, to interfejs w infrastrukturze News, którego Cv nie weźmie bez referencji do News. Wspólna biblioteka powstaje teraz, przy pierwszym strumieniu, i od razu jest pisana bez słowa `Article`. Koszt: drugi projekt. Payload jest JSON-em i nazwą typu z atrybutu. Deserializacja do typu CLR jest po stronie usługi, która zna swoje zdarzenia. Port trzyma `StoredEvent` z nazwą i bajtami, nie z `ArticleCreated`.

To jest wyjątek od „biblioteka dopiero przy drugiej usłudze” tylko w połowie: Cv jeszcze nie ma, ale interfejs bez typu News jest warunkiem, żeby Cv w ogóle mogło dojść bez przepisywania. Pamięć i Postgres są w bibliotece. Mapowanie zdarzeń News zostaje w News.

### Pozycja globalna

Sekwencja `stream_position` w `news_write`. Każde dopisanie bierze następną wartość, także gdy w jednym `Append` jest kilka zdarzeń. Problem, który zamyka, to używanie wersji strumienia jako kursora projekcji: wersja 4 artykułu A i wersja 4 artykułu B nie mówią, co było później. Odpada jeden licznik na oba znaczenia. Koszt: dziury w pozycji przy wycofanej transakcji, jeśli sekwencja nie cofa się. Dziury są dozwolone. Kursor idzie do przodu.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| strumień | uporządkowane zdarzenia jednego agregatu |
| oczekiwana wersja | warunek dopisania, `0` dla nowego |
| pozycja globalna | licznik przez wszystkie strumienie usługi |
| `StoredEvent` | koperta: nazwa, wersja, pozycja, JSON |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/shared/JJDevHub.EventStore/IEventStore.cs` | `Append`, `Read` |
| `src/shared/JJDevHub.EventStore/InMemoryEventStore.cs` | testy |
| `src/shared/JJDevHub.EventStore.Postgres/` | SQL i transakcja |
| `tests/JJDevHub.EventStore.Tests/` | konflikt wersji, bez kontenera i z kontenerem |

## Plan

### 1. Dopisanie

`Append(streamId, expectedVersion, events, ct)` w transakcji wstawia wiersze. Wersja w wierszu to `expectedVersion + indeks`. Gdy `expectedVersion` nie równa się aktualnej wersji strumienia, `Result` z konfliktem i brak nowych wierszy. Pusta lista zdarzeń to `stream.events.empty`. Pusty `streamId` to `stream.key.blank`.

### 2. Odczyt

`Read` zwraca zdarzenia po `version` rosnąco. Nie zwraca pozycji do agregatu. Agregat nie zna pozycji. Pozycję zwraca osobna metoda `ReadAfter(position)`, której użyje projektor w następnym numerze, jeśli zechce czytać strumień zamiast Kafki. Droga produktu do odczytu i tak jest przez outbox i Kafkę, jak w Learning. `ReadAfter` zostaje, bo strumień jest źródłem, a nie tylko kolejka. Projekcja z Kafki jest dla klienta. Odtworzenie agregatu przy zapisie czyta `Read`, nie Kafkę.

### 3. Test na obu implementacjach

Ten sam zestaw faktów idzie na pamięć i na Postgresa. Konflikt: dwa dopisania z `expectedVersion` 0, drugie pada, w strumieniu jest jedna seria.

### Co jest do czego

| Metoda | Kto ją woła |
| --- | --- |
| `Append` | handler zapisu News |
| `Read` | handler, zanim wywoła metodę agregatu |
| `ReadAfter` | na razie nikt w HTTP; zostaje przy magazynie |

## Testy

Nazwa: `Second_append_with_same_expected_version_conflicts`.

| Arrange | Act | Assert |
| --- | --- | --- |
| pierwsze `Append` z wersją oczekiwaną `0` | drugie też z `0` | kod `stream.version.conflict`, liczba zdarzeń w strumieniu równa pierwszej partii |

Test pamięci nie ma cechy. Test Postgresa ma `Category=Integration` i atrybut teorii, jeśli da się podać dwie implementacje. Jeśli nie, dwa fakty o tej samej nazwie w dwóch klasach.

```bash
dotnet test tests/JJDevHub.EventStore.Tests --nologo --filter "Category!=Integration"
```

## Checkpoint

```bash
dotnet test tests/JJDevHub.EventStore.Tests --nologo
```

Integracyjny fakt przechodzi na `postgres:18` i bazie `news_write`. W tabeli widać `position` rosnące. Domena News nadal nie referencjonuje projektu Postgresa.

## Umiesz wyjaśnić

Dlaczego wersja i pozycja to nie to samo?

Wersja mówi, który to fakt w tym artykule, i pilnuje zapisu. Pozycja mówi, który to fakt w całej usłudze, i nadaje się na kursor. Jedna liczba na oba znaczenia myli odtworzenie artykułu z „co było później w innym artykule”.

Dlaczego port nie zwraca `ArticleCreated`?

Bo biblioteka strumienia nie zna News. Zna nazwę i JSON. News deserializuje po atrybucie. Inaczej Cv nie użyje tego portu bez referencji do artykułu.

Co daje unikalność `(stream_id, version)` ponad sprawdzenie w kodzie?

Dwa połączenia mogą jednocześnie przeczytać wersję 3. Sprawdzenie w kodzie obu puści. Unikalność puści jedno. Drugie dostaje konflikt, nie dwa zdarzenia z wersją 4.

Dlaczego nie jedna komórka JSON?

Bo dopisanie wymaga czytania i pisania całości, a dwa takie zapisy gubią się nawzajem bez wersji na każdym fakcie. Wiersze dopisuje się na końcu.

Po co implementacja w pamięci, skoro i tak jest Postgres?

Żeby test agregatu i test konfliktu chodziły bez Dockera. Ta sama umowa. Postgres jest w teście integracyjnym, nie zamiast umowy.

## Zasady

- Konflikt wersji to `Result`, nie `500`.
- Pusty strumień i pusta lista nie dopisują wiersza.
- `IEventStore` nie ma typu z News ani z Cv.
- Odtworzenie agregatu czyta strumień, nie bazę odczytu.
- Sekwencja pozycji może mieć dziury.
- Domena nie referencjonuje magazynu.

## Poza zakresem

- Handler, który woła `Append` i outbox, jest w [03-zapis.md](03-zapis.md).
- Odczyt klienta jest w [04-odczyt-markdown.md](04-odczyt-markdown.md).
- Cv użyje tego samego portu w [01-sekcje.md](../07-cv/01-sekcje.md).

## Źródła

- [Optimistic concurrency](https://learn.microsoft.com/ef/core/saving/concurrency). Wiążąca jest oczekiwana wersja, nawet jeśli tu zapis jest SQL-em, nie EF. Dotyczy `expectedVersion`. Ze źródła nie bierzemy ostatni zapis wygrywa.
- [PostgreSQL sequences](https://www.postgresql.org/docs/current/sql-createsequence.html). Wiążące jest to, że wycofana transakcja nie cofa sekwencji. Dotyczy dziur w pozycji. Ze źródła nie bierzemy pozycji jako wersji strumienia.
- Numer agregatu. Wiążące jest `Replay` z faktów po kolei. Dotyczy `Read` sortowanego po `version`. Ze źródła nie bierzemy czytania strumienia na ścieżce `GET` strony publicznej.

## Gotowe gdy

- Test konfliktu przechodzi w pamięci i na Postgresie.
- Wiersz ma `type` z atrybutu, nie z nazwy CLR w kolumnie, jeśli serializacja już wkłada atrybut; jeśli JSON jest robiony w następnym numerze, kolumna `type` i tak jest atrybutem już tu, a payload składa test z gotowego JSON.
- Biblioteka strumienia buduje się bez referencji do News.
- Dwa liczniki są w tabeli i w teście, który czyta oba.
- Pusta lista zwraca `stream.events.empty`.
