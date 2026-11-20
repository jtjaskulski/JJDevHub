# 03. Strona publiczna CV

Poprzedni numer to [kopia technologii](02-kopia-technologii.md). Następny folder zaczyna się od [dwóch zapytań](../08-contact/01-dwa-zapytania.md).

Ten numer wystawia `GET /cv`. Jedno zapytanie do `cv_read` składa profil, sekcje i nazwę technologii z kopii. Brak kopii nie jest błędem `500`. Jest slugiem bez nazwy. `GET` nie woła Learning i nie czyta strumieni.

## Po co

Siedem strumieni i kopia, których nikt nie składa, nie są stroną. Czytelnik ma dostać jeden dokument. Złożenie go przy każdym żądaniu z historii stanowisk wróciłoby do problemu, który projekcja już rozwiązuje. `GET` czyta dokumenty sekcji, które projektor CV zapisuje z `cv.sections`, oraz `technology_copy`.

## Dlaczego ta technologia

### Jeden dokument odpowiedzi

`PublicCv` ma profil, listy stanowisk, projektów, edukacji, aktywności, języków i skilli. Skill ma `technologySlug`, `namePl`, `nameEn`, `catalogMissing`. `catalogMissing` jest true, gdy nie ma kopii albo `deleted` jest true. Problem, który zamyka, to `404` całej strony, gdy jedna technologia zniknęła z katalogu. Odpada wołanie Learning, żeby „dopytać”. Koszt: klient musi umieć pokazać skill bez nazwy. Pokazuje slug.

### Projektor sekcji

Z poprzedniego numeru sekcji został temat i strumień. Ten numer dopina projektor `cv-sections`, jeśli jeszcze nie zapisuje dokumentów. Dokumenty: `profile_document`, `position_document` i dalej, albo jedna tabela `section_document` z `kind`. Jedna tabela z `kind` jest prostsza i jest wyborem. Klucz `(kind, id)`.

### Dapper

Jedno zapytanie SQL z `LEFT JOIN technology_copy` po slugu skillu. Reszta sekcji osobnymi zapytaniami w jednym połączeniu, nie w pętli po HTTP. Problem, który zamyka, to N+1 do Learning. Odpada EF `Include` przez granice, których nie ma. Koszt: kilka `SELECT` w jednym handlerze odczytu. To nie jest N+1 do innej usługi.

### Widoczność

CV publiczne jest publiczne. Nie ma `private` na sekcji w tym produkcie, chyba że sekcja ma flagę `hidden`. Flaga `hidden` jest w rewizji i projektor nie oddaje jej na `GET` publiczny. Właściciel w intranecie dostanie wszystkie, w numerze frontu, przez ten sam endpoint z rolą `owner` albo przez `GET /cv/edit`. Ten numer wystawia publiczny `GET /cv` i `GET /cv/manage` dla `owner`, który zwraca także `hidden`. Dwa odczyty, jedna baza.

### Wersja

Strona nie ma jednego `ETag` na całość, bo sekcje mają własne wersje. Każda sekcja w JSON ma `version`. `PUT` sekcji używa `If-Match` tej sekcji. Nie ma `ETag` całej strony, który kłamie, że siedem wersji to jedna.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| `catalogMissing` | nie ma kopii albo technologia jest usunięta |
| `hidden` | sekcja niewidoczna publicznie, widoczna dla `owner` |
| `section_document` | bieżący stan sekcji w `cv_read` |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Cv.Application/ReadModels/PublicCv.cs` | DTO |
| `Cv.Infrastructure/Read/DapperCvQueries.cs` | SQL i join kopii |
| `Cv.Projector/SectionConsumer.cs` | dokumenty z `cv.sections` |
| `Cv.Api/Endpoints/CvEndpoints.cs` | `GET /cv` i `GET /cv/manage` |
| `tests/Cv.Api.Tests/PublicCvTests.cs` | brak kopii |

## Plan

### 1. Join

Skill bez wiersza w `technology_copy` wraca w DTO z pustą nazwą i `catalogMissing` true. Nie odrzucamy całej strony.

### 2. Hidden

`GET /cv` ma `WHERE hidden = false`. `GET /cv/manage` wymaga `owner` i nie ma tego warunku.

### 3. Zakaz

Test hosta publicznego nie rejestruje `IEventStore` ani klienta HTTP. Start przechodzi.

### Co jest do czego

| Pole DTO | Źródło |
| --- | --- |
| treść stanowiska | `section_document` |
| nazwa technologii | `technology_copy` |
| slug przy braku kopii | skill w dokumencie, flaga `catalogMissing` |

## Testy

Nazwa: `Missing_technology_copy_does_not_fail_the_page`.

| Arrange | Act | Assert |
| --- | --- | --- |
| skill `dotnet`, pusta `technology_copy` | `GET /cv` | `200`, skill z `catalogMissing` true, nazwa pusta |

```bash
dotnet test tests/Cv.Api.Tests --nologo --filter "FullyQualifiedName~Missing_technology_copy"
```

## Checkpoint

```bash
dotnet test tests/Cv.Api.Tests --nologo
aspire run
```

Przez bramę `GET /cv` zwraca profil i skill. Po usunięciu technologii w Learning i po dojściu kopii ten sam `GET` nadal jest `200`, a skill ma `catalogMissing` true. W logu tego żądania nie ma wywołania do Learning.

## Umiesz wyjaśnić

Dlaczego brak jednej technologii nie daje `404` strony?

Bo reszta CV jest prawdziwa. Zniknięcie pozycji katalogu jest stanem skillu na stronie, nie awarią składania.

Dlaczego nie ma jednego `ETag` na całe CV?

Bo siedem sekcji ma siedem wersji. Jeden nagłówek albo kłamie, albo blokuje zapis języka, gdy ktoś poprawił stanowisko. Wersja jest przy sekcji.

Dlaczego join jest dozwolony, a HTTP do Learning nie?

Bo join jest w `cv_read`, do kopii, którą CV ma u siebie. To nie jest cudza baza. HTTP do Learning przy każdym odczycie wraca do zależności, którą kopia miała usunąć.

Skąd bierze się dokument sekcji?

Z projektora, który czyta `cv.sections`. Źródłem prawdy przy edycji zostaje strumień w `cv_write`. `GET` go nie czyta.

Czemu `hidden` nie jest osobną usługą?

Bo to flaga sekcji, nie uprawnienie konta. Publiczny odczyt ją filtruje. Właściciel widzi ją w `manage`.

## Zasady

- `GET /cv` nie woła `IEventStore` i nie woła Learning.
- Brak kopii nie jest `500`.
- `GET /cv/manage` wymaga `owner`.
- Wersja jest per sekcja.
- `hidden` nie pojawia się w publicznym JSON.
- DTO nie zawiera connection stringa.

## Poza zakresem

- Wygląd strony jest w [05-tresc-publiczna.md](../11-web/05-tresc-publiczna.md).
- Edycja pól w przeglądarce jest w [06-intranet.md](../11-web/06-intranet.md).
- Cache strony jest w [06-redis.md](../13-kubernetes/06-redis.md).

## Źródła

- Numer kopii. Wiążące jest `catalogMissing` przy `deleted` i przy braku wiersza. Dotyczy DTO. Ze źródła nie bierzemy usuwania skillu automatycznie.
- Numer odczytu notatki. Wiążące jest czytanie dokumentu, nie strumienia. Dotyczy `GET /cv`. Ze źródła nie bierzemy HTML w sekcji, która jest zwykłym tekstem stanowiska. Markdown i sanitacja dotyczą tylko pól, które właściciel oznacza jako Markdown. Na tym numerze pola stanowiska są tekstem, nie HTML. Nie przepuszczamy ich przez `MarkdownHtml`, żeby nie robić z nazwy firmy dokumentu HTML. Jeśli pole jest wielowierszowe, zostaje tekstem. Klient łamie wiersze.
- [LEFT JOIN](https://www.postgresql.org/docs/current/queries-table-expressions.html). Wiążące jest zachowanie wiersza skillu bez kopii. Dotyczy SQL. Ze źródła nie bierzemy `INNER JOIN`, który wyrzuciłby skill.

## Gotowe gdy

- Test braku kopii zwraca `200` i `catalogMissing`.
- Publiczny `GET` nie zawiera sekcji `hidden`.
- `GET /cv/manage` bez tokenu zwraca `401`.
- W ścieżce `GET` nie ma `IEventStore`.
- Odpowiedź nie ma jednego `ETag` na całą stronę.
