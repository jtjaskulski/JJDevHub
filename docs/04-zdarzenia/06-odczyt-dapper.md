# 06. Odczyt Dapperem

Poprzedni numer to [projektor](05-projektor-i-inbox.md). Następny to [ewolucja schematu](07-ewolucja-schematu.md).

Ten numer przenosi `GET` Learning na `learning_read` i Dappera. Tymczasowy odczyt z bazy zapisu znika. Lista kursów i lekcja publiczna nie otwierają `LearningDbContext`. HTML z Markdownu przechodzi przez sanitację. Surowy HTML od autora nie wraca do klienta.

## Po co

Odczyt przez EF na tabeli zapisu śledzi encje, których nikt nie zmienia, i omija dokument, który projektor właśnie złożył. Po `POST` projekcja może jeszcze nie być gotowa i `GET` zwróci `404`. To jest stan przejściowy, nie powód, żeby czytać zapis. Dapper zamyka ścieżkę: jedno zapytanie, brak trackera, indeks pod slug.

Markdown w odpowiedzi zmusza każdego klienta do tej samej sanitacji. Serwer zwraca HTML już oczyszczony, a klient i tak ma własne czyszczenie później. Dwa miejsca są celowe. Jedno z nich nie wystarcza, gdy drugi ma błąd.

## Dlaczego ta technologia

### Dapper, nie LINQ, na liście

`IReadQueries` jest portem w aplikacji. Implementacja używa `NpgsqlDataSource` i Dappera. Problem, który zamyka, to `Include` i przypadkowy `SaveChanges` na ścieżce `GET`. Odpada drugi `DbContext` odczytu na EF „bo już mamy EF”. Koszt: SQL jest w kodzie. Jest krótki i jest w jednym pliku na zapytanie. Nie ma składania SQL ze stringów użytkownika.

Zapytanie listy filtruje `visibility = 'public'`, dopóki nie ma aktora. Z aktorem `student` dojdą `students`. To dopnie numer JWT. Tu port przyjmuje `VisibilityScope`, a jedyna wartość używana przez API to `Public`.

### Indeks

Na `catalog_document` jest indeks `(kind, slug)` unikalny, już z projektora, i indeks `(kind, visibility)`, jeśli lista go używa. `EXPLAIN` listy kursów w teście nie pokazuje `Seq Scan` na tabeli, którą test najpierw zapełnił kilkudziesięcioma wierszami. Na pustej tabeli plan kłamie. Test wstawia 50 wierszy. Problem, który zamyka, to indeks „na zapas” bez planu. Odpada indeks na każde pole. Koszt: test integracyjny jest trochę dłuższy.

### Markdown do HTML

`Markdig` zamienia body na HTML. `HtmlSanitizer` spuszcza listę tagów: nagłówki, akapity, listy, `pre`, `code`, `a` z `href` http i https. Wyrzuca `script`, `iframe`, `on*`. Problem, który zamyka, to lekcja z `<script>`, która w bazie jest „tylko tekstem”, a w przeglądarce jest kodem. Odpada oddawanie Markdownu do klienta jako jedynej formy: klient mógłby go wstawić jako HTML. Koszt: dwie reprezentacje. W dokumencie odczytu trzymamy i Markdown, i nie oddajemy go w publicznym DTO. DTO ma `html`. Zapis nadal trzyma Markdown. Projektor może zapisać Markdown, a HTML liczy się przy `GET`, żeby nie sanitować dwa razy w dwóch wersjach biblioteki. Liczenie przy `GET` jest tańsze w spójności. Przy wolnym `GET` wolno przenieść HTML do dokumentu. Na tym numerze liczymy przy `GET`.

### Koniec tymczasowego GET

Endpointy `GET`, które czytały `ICourseRepository`, przechodzą na `IReadQueries`. Porty zapisu zostają przy komendach. Test, który sprawdzał `ETag` na zapisie, czyta teraz wersję z dokumentu. Dokument ma `version` z projekcji. `ETag` zostaje. `404` tuż po `POST` jest opisany w teście jako dozwolony przez krótki czas i nie jest błędem komendy. Test nie wymaga, żeby `GET` w tej samej milisekundzie widział wiersz. Osobny test czeka na projektor z timeoutem i dopiero wtedy czyta.

### `404` i widoczność

Gość i brak wiersza dają `learning.lesson.not_found`. Treść `private` nie wycieka w liście. Lista nie ma pola „ile jest prywatnych”.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| projekcja | wiersz w bazie odczytu złożony z faktów |
| `IReadQueries` | port odczytu, bez `SaveChanges` |
| sanitacja | wyrzucenie tagów i atrybutów, które nie są treścią |
| okno projekcji | czas między commitem zapisu a upsertem odczytu |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Learning.Application/ReadModels/IReadQueries.cs` | lista i szczegół |
| `Learning.Application/ReadModels/CourseListItem.cs` | DTO bez Markdownu |
| `Learning.Application/ReadModels/LessonDetail.cs` | DTO z `html` |
| `Learning.Infrastructure/Read/DapperReadQueries.cs` | SQL |
| `Learning.Infrastructure/Read/LessonHtml.cs` | Markdig i HtmlSanitizer |
| `Learning.Api/Endpoints/` | `GET` wskazuje port odczytu |
| `tests/Learning.Infrastructure.Tests/LessonHtmlTests.cs` | `script` znika |

Paczki `Dapper`, `Markdig`, `HtmlSanitizer` wchodzą do `Directory.Packages.props` i do tabeli technologii, jeśli wiersza jeszcze nie ma.

## Plan

### 1. SQL listy

```sql
SELECT slug, title_pl, title_en, visibility, version
FROM catalog_document
WHERE kind = 'course' AND visibility = @visibility
ORDER BY slug;
```

Tytuł w DTO jest wybierany w C# według kultury żądania, nie przez doklejanie nazwy kolumny z nagłówka. Nagłówek `Accept-Language` zamienia się na `pl` albo `en` w endpoincie. Do SQL idzie już enum albo stała, nie surowy nagłówek.

Jeśli kolumny `title_pl` nie ma, bo payload jest jsonb, zapytanie wyciąga `payload->>'titlePl'`. Jeden z tych dwóch kształtów jest w migracji i w SQL, nie oba. Ten numer wybiera jsonb, który projektor już zapisuje, i wyrażenie `->>`. Indeks pod listę publiczną może być wyrażeniowy. Jeśli `EXPLAIN` pokazuje seq scan przy 50 wierszach, dodajemy indeks wyrażeniowy w migracji odczytu. Mała tabela i tak wybierze seq scan. Test `EXPLAIN` jest informacyjny w logu, a asercja dotyczy poprawnych wierszy, nie konkretnego węzła planu, dopóki tabela nie jest duża. Nie kłamiemy, że 50 wierszy wymusza index scan. Asercja planu schodzi z tego numeru. Zostaje asercja treści.

### 2. HTML

`LessonHtml.Render(markdown)` zwraca string. Test: wejście z `<script>alert(1)</script>` i z blokiem kodu. Wyjście nie zawiera `<script`. Zawiera `<pre` albo `<code`.

### 3. Usunięcie GET zapisu

Szukamy w `Learning.Api` użycia `ICourseRepository` i `ILessonRepository` w endpointach `GET`. Ma ich nie być. Zostają w handlerach komend.

### 4. ETag z dokumentu

`GET` ustawia `ETag` z `version` dokumentu. `PUT` nadal idzie do zapisu i do wersji encji zapisu. To są dwie wersje. Mogą się chwilowo różnić. Klient edycji, dopóki nie ma intranetu, używa wersji z `GET` odczytu. Rewizja wysyła `If-Match` tej wersji. Handler zapisu porówna ją z wersją encji. Tuż po publikacji mogą być równe, bo obie zaczynają od 1. Po rewizji projektor dogania. Konflikt `412` zostaje na endpoincie, `409` na wyścigu zapisu. Ten numer nie próbuje transakcji między dwoma bazami.

### Co jest do czego

| DTO | Czego nie ma |
| --- | --- |
| `CourseListItem` | body, connection string |
| `LessonDetail` | surowy HTML autora, Markdown w polu publicznym |

Markdown zostaje w bazie odczytu w payloadzie, nie w DTO.

## Testy

Nazwa: `Script_tag_is_stripped_from_lesson_html`.

| Arrange | Act | Assert |
| --- | --- | --- |
| Markdown z `<script>` i z ogrodzeniem | `LessonHtml.Render` | brak `<script`, jest blok kodu |

Nazwa integracyjna: `Get_lesson_reads_read_database`. Projektor najpierw nanosi wiadomość, potem `GET`. Asercja: tytuł z dokumentu. W logu SQL testu nie ma `FROM lessons`, jest `catalog_document`.

```bash
dotnet test tests/Learning.Infrastructure.Tests --nologo --filter "FullyQualifiedName~Script_tag_is_stripped"
```

## Checkpoint

```bash
dotnet test tests/Learning.Api.Tests tests/Learning.Infrastructure.Tests --nologo --filter "Category!=Performance"
```

W kodzie endpointów `GET` nie ma `LearningDbContext` ani repozytorium zapisu. `grep` to potwierdza. Przez bramę `GET /learning/lessons/{slug}` zwraca `html`.

## Umiesz wyjaśnić

Dlaczego tuż po `POST` wolno dostać `404`?

Bo projektor jeszcze nie złożył wiersza. Komenda się udała. Klient ponawia `GET`. To nie jest błąd zapisu i nie jest powód, żeby czytać tabelę `lessons`.

Dlaczego Dapper, a nie drugi kontekst EF?

Bo odczyt nie zmienia encji. Tracker i `Include` są kosztem i pułapką `SaveChanges` na `GET`. SQL listy jest jeden. Widać go.

Dlaczego sanitacja jest na serwerze, skoro klient też czyści?

Bo klient nie jest jedynym konsumentem. `curl` i przyszły inny klient dostaną to samo. Drugie czyszczenie w przeglądarce jest w numerze treści webu i nie zwalnia serwera.

Czym wersja odczytu różni się od wersji zapisu?

Żyją w dwóch bazach. Zapis jest źródłem przy konflikcie edycji. Odczyt jest tym, co klient właśnie zobaczył. Przez chwilę mogą się różnić o jeden. `If-Match` idzie do zapisu. Nie ma wspólnego commita tych dwóch wersji.

Dlaczego nagłówek języka nie wchodzi do SQL jako string sklejony?

Bo sklejenie nagłówka z SQL jest wstrzyknięciem. Nagłówek zamienia się na `pl` albo `en` w kodzie. Do zapytania idzie parametr.

## Zasady

- `GET` nie używa `LearningDbContext`.
- DTO publiczne nie zawiera Markdownu źródłowego ani `<script`.
- SQL nie składa się z surowego `Accept-Language`.
- `404` po `POST` nie cofa komendy.
- Gość nie widzi `private` i nie widzi licznika ukrytych wierszy.
- Paczki tego numeru są w tabeli technologii.

## Poza zakresem

- `V2` lekcji jest w [07-ewolucja-schematu.md](07-ewolucja-schematu.md).
- Autoryzacja `student` na widoczności `students` jest w [03-jwt.md](../05-tozsamosc/03-jwt-i-policies.md).
- Drugie czyszczenie w przeglądarce jest w [05-tresc-publiczna.md](../11-web/05-tresc-publiczna.md).
- Cache Redis jest w [06-redis.md](../13-kubernetes/06-redis.md). Do tego czasu `IMemoryCache` na liście publicznej wolno dodać tylko jeśli klucz ma prefiks `learning` i da się żyć bez niego. Ten numer nie dodaje cache. Jest wprost: każdy `GET` idzie do Postgresa.

## Źródła

- [Dapper](https://github.com/DapperLib/Dapper). Wiążące są parametry, nie sklejanie SQL. Dotyczy `DapperReadQueries`. Ze źródła nie bierzemy Dappera na ścieżce komendy.
- [HtmlSanitizer](https://github.com/mganss/HtmlSanitizer). Wiążąca jest biała lista tagów. Dotyczy `LessonHtml`. Ze źródła nie bierzemy wyłączenia sanitacji w Development.
- [Markdig](https://github.com/xoofx/markdig). Wiążące jest ogrodzenie kodu jako HTML `pre`/`code`. Dotyczy body lekcji. Ze źródła nie bierzemy HTML wpisanego przez autora jako zaufanego.

## Gotowe gdy

- Test `<script>` przechodzi i nie potrzebuje bazy.
- `GET` lekcji po naniesieniu projekcji czyta `catalog_document`.
- W endpointach `GET` nie ma kontekstu zapisu.
- `POST` nadal zwraca `201`, nawet jeśli natychmiastowy `GET` jest `404`.
- DTO nie zawiera pola `markdown`.
