# 01. Jądro i domena Learning

Poprzedni numer to [telemetria](../02-szkielet-i-deploy/08-telemetria.md). Następny to [porty](02-use-case-i-porty.md).

Ten numer stawia `JJDevHub.Shared.Kernel` i `Learning.Domain`. Jądro ma `Result`, `Entity` i `ValueObject`. Domena ma `Category`, `Technology`, `Course`, `Lesson` i `StudentProgress` jako zwykły stan. Nie ma EF, nie ma Kafki, nie ma endpointu. Reguła, która nie daje się sprawdzić testem jednostkowym bez kontenera, jest w złym projekcie.

## Po co

Gdy reguła tytułu mieszka w endpoincie, drugi endpoint jej nie powtórzy albo powtórzy ją inaczej. Jądro zamyka język błędu: każda usługa zwraca `Result`, a nie wyjątek „bo tak wyszło”. Domena Learning zamyka nazwy, zanim pojawi się tabela. Późniejsza tabela ma nazywać się jak ta klasa, a nie odwrotnie.

`StudentProgress` nie jest strumieniem. Postęp studenta nie jest historią, którą strona publiczna odtwarza. Jest wierszem „ta lekcja została ukończona”.

## Dlaczego ta technologia

### `Result` zamiast wyjątku dla błędu oczekiwanego

`Result` i `Result<T>` niosą sukces albo `ResultError` z rodzajem i kodem. Problem, który zamyka, to `try/catch` na pusty tytuł, który wygląda jak awaria procesu. Odpada wyjątek domenowy jako sterowanie przepływem. Koszt: każda metoda, która może odmówić, zwraca `Result`. Wyjątek zostaje dla IO i dla błędu składu, których domena i tak nie łapie, bo nie woła IO.

Rodzaje: `None`, `Invalid`, `NotFound`, `Conflict`, `Forbidden`. Kod ma gramatykę `segment.element.powod`. Na tym numerze powstają `learning.title.blank`, `learning.slug.invalid`, `learning.visibility.unknown`.

### `readonly record struct` dla `Slug`

`Slug` jest wartością. Problem, który zamyka, to `string` wpuszczany wszędzie, także z wielką literą i ze spacją. Odpada goły `string` w encji. Koszt: konwersja na brzegu HTTP jest jawna. W środku domeny nie ma już „prawie sluga”.

`ref struct` nie wchodzi. `Slug` żyje w encji na stercie, a `ref struct` nie może być polem klasy. To jest ta decyzja, nie brak wiedzy o `ref`.

### Encje bez frameworka

Projekt `Learning.Domain` nie ma `PackageReference`. Problem, który zamyka, to reguła, która nie kompiluje się bez EF. Odpada bazowa klasa z `DbContext`. Koszt: encja nie wie, jak się zapisze. O tym wiedzą porty w następnym numerze.

Identyfikator to `Guid` z `Guid.CreateVersion7()` w fabryce. Nie ma slugu złożonego z nazwy przy każdym zapisie. Slug przychodzi w komendzie i po publikacji metoda domeny odmawia jego zmiany kodem `learning.slug.frozen`.

### `LocalizedText`

Mapa kultury na tekst, na start `pl` i `en`. Nowa kultura nie dokłada kolumny. Problem, który zamyka, to `title_pl` i `title_en` jako dwa pola, które trzeci język pomija. Odpada kolumna na język. Koszt: zapytanie „po slugu i języku” filtruje w odczycie, nie w kształcie tabeli zapisu. Zapis trzyma mapę. Odczyt, gdy dojdzie, wystawia jeden język na wiersz albo wyciąga klucz z JSON.

Pusta mapa i język z pustym tekstem są `learning.title.blank`.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| encja | obiekt z tożsamością `Guid` |
| value object | równość po składowych, bez własnego życia |
| `Result` | oczekiwany sukces albo oczekiwana odmowa |
| slug | identyfikator w adresie, niezmienny po publikacji |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/shared/JJDevHub.Shared.Kernel/Result.cs` | `Result`, `Result<T>`, `ResultError`, `ResultErrorKind` |
| `src/shared/JJDevHub.Shared.Kernel/Entity.cs` | `Guid Id` |
| `src/shared/JJDevHub.Shared.Kernel/ValueObject.cs` | równość po składowych |
| `src/services/Learning/Learning.Domain/Slug.cs` | walidacja i zamrożenie |
| `src/services/Learning/Learning.Domain/LocalizedText.cs` | mapa `pl`/`en` |
| `src/services/Learning/Learning.Domain/Visibility.cs` | `public`, `students`, `private` |
| `src/services/Learning/Learning.Domain/Category.cs` | półka katalogu |
| `src/services/Learning/Learning.Domain/Technology.cs` | technologia, slug niezmienny |
| `src/services/Learning/Learning.Domain/Course.cs` | kurs wskazujący slug technologii |
| `src/services/Learning/Learning.Domain/Lesson.cs` | lekcja, Markdown w `LocalizedText` |
| `src/services/Learning/Learning.Domain/StudentProgress.cs` | ukończenie lekcji, bez historii brzmień |
| `tests/JJDevHub.Shared.Kernel.Tests/` | fakty `Result` |
| `tests/Learning.Domain.Tests/` | fakty reguł |

`Category` nie ma historii brzmień. Zmiana nazwy wyświetlanej nie zmienia sluga. To samo dotyczy technologii, kursu i lekcji.

## Plan

### 1. Jądro

`Result.Ok()` i `Result.Fail(ResultErrorKind, string code)`. `Result<T>.Ok(value)`. Brak wyjątku w tych metodach. `Entity` ma chroniony konstruktor i `Guid Id`. `ValueObject` porównuje składowe.

Jądro nie ma typu `Lesson` ani `Article`. Test architektury z późniejszej fazy to sprawdzi. Na tym numerze wystarczy brak referencji: projekt jądra nie referencjonuje Learning.

### 2. Fabryki

`Technology.Publish(Slug slug, LocalizedText name, Slug categorySlug, TimeProvider clock)` zwraca `Result<Technology>`. `Lesson` trzyma `body` jako `LocalizedText` z Markdownem, nie z HTML. HTML powstanie przy odczycie, w numerze notatki dla News i w tym samym helperze dla lekcji, gdy dojdzie odczyt. Na tym numerze body jest tekstem. Pustość jest odmową.

`StudentProgress.Complete(string studentId, Slug lessonSlug, DateTimeOffset at)` zwraca rekord. Powtórne ukończenie jest sukcesem z tą samą chwilą, nie drugim wierszem. Reguła „drugi raz nie zmienia `CompletedAt`” jest w typie.

### 3. Zegar

Metody, które potrzebują czasu, dostają `TimeProvider` albo gotowy `DateTimeOffset` z zewnątrz. Domena nie woła `DateTimeOffset.UtcNow` w środku reguły. Test podstawia `FakeTimeProvider`.

### Co jest do czego

| Typ | Kto go woła |
| --- | --- |
| `Result` | każda fabryka, która może odmówić |
| `Slug` | encje i później adres HTTP |
| `StudentProgress` | komenda ukończenia, nie projekcja publiczna |
| jądro | Learning i każda następna usługa |

## Testy

Nazwa: `Publish_blank_title_returns_invalid`.

| Arrange | Act | Assert |
| --- | --- | --- |
| `LocalizedText` bez `pl` i bez `en` | `Technology.Publish` | `Result` nie jest sukcesem, kod `learning.title.blank`, rodzaj `Invalid` |

Drugi fakt: `ChangeSlug_after_publish_returns_frozen` z kodem `learning.slug.frozen`.

```bash
dotnet test tests/Learning.Domain.Tests tests/JJDevHub.Shared.Kernel.Tests --nologo
```

## Checkpoint

```bash
dotnet test tests/Learning.Domain.Tests --nologo --filter "Category!=Integration&Category!=Performance"
dotnet list src/services/Learning/Learning.Domain/Learning.Domain.csproj package
```

Lista paczek projektu domeny jest pusta. Testy przechodzą bez Dockera.

## Umiesz wyjaśnić

Dlaczego pusty tytuł nie rzuca wyjątku?

Bo to nie jest awaria procesu. To odmowa, którą brzeg HTTP zamieni na `422`. Wyjątek zostałby złapany za szeroko i wróciłby jako `500`.

Dlaczego `Slug` nie jest `ref struct`?

Bo musi być polem encji. `ref struct` nie żyje na stercie w obiekcie. Używamy `readonly record struct`, który jest wartością i może być polem.

Dlaczego postęp nie jest strumieniem zdarzeń?

Bo strona publiczna go nie odtwarza, a student nie ogląda historii swoich kliknięć jako produktu. Jeden wiersz „ukończone o tej godzinie” wystarcza. Strumień mają News i Cv, gdzie historia brzmienia jest treścią.

Co się psuje, gdy slug się zmienia po publikacji?

Adres kursu i wskaźnik w CV wskazują stary slug. Zamrożenie zostawia adres. Zmiana nazwy wyświetlanej idzie w `LocalizedText`, nie w slugu.

Dlaczego jądro nie zna `Lesson`?

Bo wtedy test pustego tytułu ciągnąłby Learning, a zmiana reguły lekcji zmieniałaby kontrakt jądra używany przez News.

## Zasady

- `Learning.Domain` nie referencjonuje EF, ASP.NET, Kafki ani OpenTelemetry.
- Jądro nie referencjonuje usług.
- Brak `DateTimeOffset.UtcNow` w domenie.
- Slug po `Publish` nie ma metody, która go podmienia w sukcesie.
- Markdown lekcji nie jest HTML-em.

## Poza zakresem

- Porty i handlery są w [02-use-case-i-porty.md](02-use-case-i-porty.md).
- Tabele są w [04-ef-core.md](04-ef-core.md).
- Strumień zdarzeń jest w [01-agregat.md](../06-news/01-agregat-i-zdarzenie.md) i nie dotyczy tych encji.
- Sanityzacja HTML jest przy odczycie, w [06-odczyt-dapper.md](../04-zdarzenia/06-odczyt-dapper.md) i [04-odczyt-markdown.md](../06-news/04-odczyt-markdown.md).

## Źródła

- Eric Evans, *Domain-Driven Design*. Wiążące jest to, że agregat jest granicą spójności, a język w kodzie jest językiem produktu. Dotyczy nazw `Lesson` i `Technology`, nie nazw tabel. Ze źródła nie bierzemy jednego modelu na pięć usług.
- [Result pattern](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects). Wiążące jest trzymanie wartości i odmowy poza wyjątkiem. Dotyczy `Result` i `Slug`. Ze strony o value objectach nie bierzemy biblioteki, która generuje je z atrybutów.
- [Choosing between struct and class](https://learn.microsoft.com/dotnet/standard/design-guidelines/choosing-between-class-and-struct). Wiążące jest to, że mała wartość bez tożsamości może być struct. Dotyczy `Slug`. Ze źródła nie bierzemy `ref struct` jako pola encji.

## Gotowe gdy

- Oba projekty testowe przechodzą bez kontenera.
- `Learning.Domain.csproj` nie ma `PackageReference`.
- `Publish` z pustym tytułem zwraca `learning.title.blank`.
- Zmiana sluga po publikacji zwraca `learning.slug.frozen`.
- W jądrze nie ma nazwy `Lesson`, `Article`, `Account` ani `Skill`.
