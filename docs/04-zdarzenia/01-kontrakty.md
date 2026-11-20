# 01. Kontrakty zdarzeń

Poprzedni numer to [EF zaawansowany](../03-learning-zapis/07-ef-zaawansowany.md). Następny to [outbox](02-outbox.md).

Ten numer stawia `Learning.Contracts`: rekordy faktów, nazwy topiców i nagłówków, serializację przez source generator. Zdarzenie domenowe nie istnieje jako osobny typ na szynie. Na szynę wychodzi rekord z tego projektu. Learning jeszcze nic nie publikuje. Jest kształt, który outbox za chwilę zapisze.

## Po co

Jeśli na szynę wyjdzie encja `Lesson`, konsument Cv będzie zależał od prywatnych pól Learning i zepsuje się przy zmianie `Version`. Rekord kontraktu zamyka granicę: na zewnątrz widać slug, język i widoczność, nie tracker EF.

Nazwa klasy nie może być kluczem w bazie. Za rok klasa nazywa się inaczej, a wiersze zostają. Kluczem jest stały łańcuch `learning.lesson-published.v1`.

## Dlaczego ta technologia

### Rekord i source generator

`LessonPublishedV1` jest `sealed record`. `ContractJsonContext` dziedziczy z `JsonSerializerContext` i ma `[JsonSerializable]` dla każdego rekordu. Problem, który zamyka, to refleksja przy każdej wiadomości i nazwa typu CLR w payloadzie. Odpada `JsonSerializer.Serialize<object>` bez kontekstu. Koszt: nowy rekord to jedna linia w kontekście. Zapomnienie tej linii wywala kompilację, nie produkcję, jeśli test serializacji obejmuje wszystkie typy z kontekstu.

`System.Text.Json` z `JsonSerializerDefaults.Web` daje camelCase. Pole `slug`, nie `Slug`, w bajtach na szynie.

### Nazwa z atrybutu, nie z `typeof`

`EventNameAttribute` ląduje w jądrze w tym numerze, obok `Result`. Nosi łańcuch `segment.fakt.vN`. Metoda `EventName.For<T>()` czyta atrybut. Brak atrybutu jest wyjątkiem przy starcie testu, nie cichym użyciem nazwy klasy. Problem, który zamyka, to `message-type` równe `Learning.Contracts.LessonPublishedV1`. Odpada nazwa klasy jako kontrakt. Koszt: atrybut i rekord muszą być zgodne. Test to sprawdza.

Zdarzenie domenowe, którego ten produkt w Learning nie ma, bo Learning nie jest event sourcingiem, nie dostaje tego atrybutu. Atrybut jest na rekordzie integracyjnym. News użyje tego samego atrybutu na zdarzeniu domenowym i na rekordzie. Jądro nie zna ani jednego, ani drugiego.

### Nagłówki

Stałe w `MessageHeaders`: `message-id`, `message-type`, `schema-version`, `correlation-id`, `traceparent`, `producer`. Problem, który zamyka, to trzy nazwy na ten sam nagłówek w relayu i w projektorze. Odpada literał w każdym pliku. Koszt: zmiana nagłówka jest zmianą łamiącą i dostaje nową nazwę, nie cichą podmianę.

`message-id` jest `Guid`. To jest klucz inboxu. Nie jest kluczem partycji. Kluczem partycji jest slug.

### Wersja schematu w nagłówku

`schema-version` to liczba. Rekord `V1` ma `1`. Przyszły `V2` ma `2` i własny typ. Oba mogą leżeć na tym samym topicu. Problem, który zamyka, to pole „opcjonalne”, które starzy konsumenci biorą za obowiązkowe, bo ktoś zmienił znaczenie. Odpada edycja `V1` w miejscu. Koszt: upcaster w numerze ewolucji. Tu zostawiamy tylko `V1` i test, że liczba w nagłówku jest `1`.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| kontrakt | rekord, który wolno znać innej usłudze |
| `message-type` | łańcuch z atrybutu, nie nazwa CLR |
| `schema-version` | liczba przy rekordzie `V1`, `V2` |
| source generator | `JsonSerializerContext` generowany przy kompilacji |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/shared/JJDevHub.Shared.Kernel/EventNameAttribute.cs` | `segment.fakt.vN` |
| `Learning.Contracts/KafkaTopics.cs` | `learning.catalog`, `learning.integration` |
| `Learning.Contracts/MessageHeaders.cs` | nazwy nagłówków |
| `Learning.Contracts/Catalog/TechnologyPublishedV1.cs` | slug, nazwy, slug kategorii |
| `Learning.Contracts/Catalog/CoursePublishedV1.cs` | slug, slug technologii, tytuły, widoczność |
| `Learning.Contracts/Catalog/LessonPublishedV1.cs` | slug, slug kursu, tytuły, widoczność, `order` |
| `Learning.Contracts/Catalog/LessonRevisedV1.cs` | slug i pola, które wolno zmienić |
| `Learning.Contracts/Integration/IntegrationStateV1.cs` | ostatni stan technologii albo kursu na temat kompaktowany |
| `Learning.Contracts/ContractJson.cs` | opcje i kontekst |
| `tests/Learning.Contracts.Tests/` | atrybut i runda serializacji |

`IntegrationStateV1` niesie `kind` o wartości `technology` albo `course`, slug, nazwy i `deleted`. Cv będzie to czytać. Learning publikuje. Treść lekcji nie wchodzi do stanu kompaktowanego: lekcja nie jest kopią w CV.

## Plan

### 1. Atrybut

```csharp
[EventName("learning.lesson-published.v1")]
public sealed record LessonPublishedV1(
    string Slug,
    string CourseSlug,
    string TitlePl,
    string TitleEn,
    string Visibility,
    int Order);
```

`EventName.For<LessonPublishedV1>()` zwraca ten łańcuch. Wzorzec atrybutu jest sprawdzany wyrażeniem regularnym w teście jądra: małe litery, segmenty, `v` i liczba.

### 2. Serializacja

`ContractJson.Serialize(record)` używa kontekstu. Wynik zawiera `"slug"` i nie zawiera `AssemblyQualifiedName`. Test deserializuje z powrotem i porównuje pola.

### 3. Topic

`KafkaTopics.Catalog = "learning.catalog"`. `KafkaTopics.Integration = "learning.integration"`. Żadnych innych nazw na ten sam fakt. Projektor katalogu czyta `learning.catalog`. Cv, gdy powstanie, czyta `learning.integration`.

### Co jest do czego

| Rekord | Topic | Kto go później zapisze |
| --- | --- | --- |
| `TechnologyPublishedV1`, `CoursePublishedV1`, `LessonPublishedV1`, `LessonRevisedV1` | `learning.catalog` | handler przez outbox |
| `IntegrationStateV1` | `learning.integration` | ten sam handler, drugi wiersz outboxa |

Dwa wiersze outboxa na jedną publikację technologii są decyzją następnego numeru. Ten numer tylko daje dwa rekordy, żeby tamten nie wymyślał kształtu.

## Testy

Nazwa: `Lesson_published_event_name_is_stable_and_json_has_no_clr_name`.

| Arrange | Act | Assert |
| --- | --- | --- |
| `LessonPublishedV1` z slugiem `linq` | `EventName.For` i `ContractJson.Serialize` | nazwa równa `learning.lesson-published.v1`, JSON zawiera `"slug":"linq"`, nie zawiera `Learning.Contracts` |

```bash
dotnet test tests/Learning.Contracts.Tests tests/JJDevHub.Shared.Kernel.Tests --nologo
```

## Checkpoint

```bash
dotnet test tests/Learning.Contracts.Tests --nologo
grep -R "JsonSerializer.Serialize" src/services/Learning/Learning.Contracts -g '*.cs' && exit 1 || true
```

W projekcie kontraktów nie ma wołania serializacji bez kontekstu. `grep` nic nie znajduje albo znajduje tylko metodę w `ContractJson`, która używa kontekstu. Drugi `grep` na `AssemblyQualifiedName` w tym projekcie jest pusty.

## Umiesz wyjaśnić

Dlaczego nazwa klasy nie jest kluczem?

Bo zmiana namespace albo nazwy pliku nie może zmienić wierszy, które już leżą w outboxie i na topicu. Łańcuch z atrybutu zostaje, klasę wolno przenieść.

Po co source generator, skoro zwykły `Serialize` działa?

Zwykłe wywołanie na `object` używa refleksji i dopisuje informacje o typie, których konsument spoza .NET nie chce. Generator zna kształt przy kompilacji i nie ciągnie refleksji na gorącej ścieżce relayu.

Czym `learning.catalog` różni się od `learning.integration`?

Katalog niesie fakt „opublikowano” i „zrewidowano” dla projektora Learning. Integracja niesie ostatni stan sluga dla Cv i jest kompaktowana. Lekcja nie jedzie integracją, bo CV nie składa strony z treści lekcji.

Dlaczego atrybut jest w jądrze, a rekord w Contracts?

Atrybut jest mechanizmem nazwy. Rekord jest kształtem jednej usługi. Jądro z rekordem `LessonPublishedV1` znałoby Learning. Tego nie chcemy. News dostanie własny projekt Contracts i ten sam atrybut.

Co wolno innej usłudze referencjonować?

Projekt `Learning.Contracts`. Nie wolno referencjonować `Learning.Domain` ani `Learning.Infrastructure`.

## Zasady

- Każdy rekord na szynie ma `[EventName]`.
- Serializacja idzie przez `ContractJsonContext`.
- Payload nie zawiera nazwy CLR ani connection stringa.
- Nowa wersja znaczenia to nowy rekord `V2`, nie edycja `V1`.
- `message-id` nie jest kluczem partycji.

## Poza zakresem

- Zapis wiersza outboxa jest w [02-outbox.md](02-outbox.md).
- Broker i utworzenie topicu są w [03-kafka.md](03-kafka.md).
- Upcaster `V2` jest w [07-ewolucja-schematu.md](07-ewolucja-schematu.md).
- `traceparent` wypełni [08-slady-i-metryki.md](08-slady-i-metryki.md). Tu jest tylko nazwa nagłówka.

## Źródła

- [System.Text.Json source generation](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation). Wiążące jest `JsonSerializerContext` i brak refleksji na typie z atrybutem. Dotyczy `ContractJson`. Ze źródła nie bierzemy `TypeNameHandling`.
- [Schema evolution](https://docs.confluent.io/platform/current/schema-registry/fundamentals/schema-evolution.html). Wiążące jest dodawanie pola opcjonalnego i nietykanie znaczenia starego pola. Dotyczy decyzji, że `V1` zostaje. Ze źródła nie bierzemy Confluent Schema Registry jako serwera: rejestr u nas jest atrybutem i testem, nie osobnym klastrem. Obraz Confluent i tak jest poza licencją.
- Martin Fowler, *Event Notification*. Wiążące jest to, że fakt na zewnątrz ma jawną nazwę i nie jest modelem wewnętrznym. Dotyczy rekordu zamiast encji. Ze artykułu nie bierzemy wołania zwrotnego HTTP do Learning przy każdym zdarzeniu.

## Gotowe gdy

- Test nazwy i JSON przechodzi bez kontenera.
- W jądrze jest atrybut, nie ma rekordu Learning.
- `KafkaTopics` ma dwie nazwy ze słownika i żadnej trzeciej na ten sam byt.
- Projekt `Learning.Contracts` nie referencjonuje infrastruktury.
- Inna usługa, której jeszcze nie ma, ma w tym numerze zapisane, że wolno jej wziąć tylko ten projekt.
