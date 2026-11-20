# 06. Kurs, lekcja i postęp

Poprzedni numer to [REST](05-rest.md). Następny to [EF zaawansowany](07-ef-zaawansowany.md).

Ten numer domyka zapis Learning: kategoria, technologia, kurs, lekcja i ukończenie lekcji. Widoczność jest regułą odczytu, ale już przy zapisie ma dozwolone wartości. Markdown lekcji zostaje w bazie. Blok kodu jest częścią tego Markdownu, nie osobną tabelą.

## Po co

Sam kurs bez lekcji nie jest kompendium. Lekcja bez widoczności albo jest publiczna zawsze, albo wymaga osobnego systemu uprawnień na każde zdanie. Trzy wartości, `public`, `students`, `private`, zamykają produkt: gość, student, właściciel. Postęp bez endpointu zostaje kolumną, której nikt nie ustawia.

Kategoria jest półką. Nie ma historii brzmień i nie jedzie na szynę jako strumień. Ma slug, bo adres technologii i tak grupuje się po kategorii.

## Dlaczego ta technologia

### Trzy stany widoczności

Enum `Visibility` jest już w domenie. Ten numer pilnuje go na brzegu i w teście lekcji. Problem, który zamyka, to flaga `isPublic`, obok której i tak powstaje druga flaga „tylko studenci”. Odpada zestaw booli. Koszt: zmiana widoczności jest rewizją z `If-Match`, nie osobnym zasobem.

Treść `private` i treść `students` dla gościa będzie `404` na odczycie. Tego `GET` jeszcze nie ma w docelowej bazie odczytu. W tym numerze tymczasowy `GET` lekcji z zapisu stosuje tę samą regułę, żeby nie utrwalić dziury. Gość nie jest jeszcze rozpoznany, bo nie ma JWT. Do czasu JWT tymczasowy `GET` zwraca tylko `public`. `students` i `private` dają `404`. Właściciel zobaczy je po numerze JWT, który doda aktora do tego samego endpointu. Nie robimy tu obejścia „w Development widać wszystko” na hoście wystawionym tunelem. W Development `aspire run` może mieć flagę `Learning:ExposeAll=true`. Na maszynie flaga nie istnieje.

### Markdown w kolumnie, nie w repozytorium

Body lekcji jest `LocalizedText` z Markdownem. Problem, który zamyka, to lekcja jako plik w git, który wymaga deployu przy poprawce przecinka. Odpada CMS obok produktu. Koszt: body bywa duże. Kolumna `jsonb` to uniesie. Limit długości to 200_000 znaków na język. Powyżej `learning.lesson.too_long` i `422`.

Blok kodu jest ogrodzony w Markdownie. Nie ma tabeli `code_examples`. Renderer dojdzie przy odczycie i zdecyduje, jak rysować ogrodzenie. Zapis nie renderuje HTML.

### Kolejność lekcji

`Lesson.Order` jest `int`. Dwie lekcje kursu mogą dostać ten sam numer tylko jeśli test tego zabrania: unikalność `(course_slug, "order")` w bazie. Problem, który zamyka, to lista, która skacze przy każdym odczycie. Odpada kolejność „po dacie utworzenia” jako jedyna. Koszt: wstawienie lekcji między dwie istniejące wymaga zmiany `order` sąsiadów. Na tym numerze klient przysyła `order`. Przestawianie hurtowe jest jednym `PUT` na lekcję, nie sagą.

### Ukończenie

`POST /lessons/{slug}/completion` woła `CompleteLessonHandler`. Bez identyfikatora studenta zwraca `learning.student.missing` i `422`, albo `401`, gdy numer JWT już każe wymagać tokenu. Na tym numerze, przed JWT, endpoint istnieje i przy `WritesEnabled=false` zwraca `404` tak jak inne zapisy. W Development przyjmuje `StudentId` w ciele. To ciało zniknie w numerze JWT: identyfikator przyjdzie z `sub`. Test tego numeru sprawdza idempotencję ukończenia: drugi raz nie zmienia `CompletedAt`.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| widoczność | `public`, `students`, `private` |
| półka | `Category`, porządek katalogu, nie materiał historyczny |
| `order` | kolejność lekcji w kursie |
| ukończenie | jeden wiersz postępu, nie zdarzenie publiczne |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Learning.Api/Endpoints/CategoryEndpoints.cs` | `POST /categories` |
| `Learning.Api/Endpoints/LessonEndpoints.cs` | publikacja, rewizja, ukończenie |
| `Learning.Domain/Lesson.cs` | limit długości i widoczność |
| `Learning.Infrastructure/Persistence/Configurations/LessonConfiguration.cs` | unikalność kolejności |
| `tests/Learning.Domain.Tests/LessonTests.cs` | długość i widoczność |
| `tests/Learning.Api.Tests/LessonVisibilityTests.cs` | `404` dla niepublicznej |

## Plan

### 1. Kategoria i technologia

`POST /categories` i `POST /technologies` używają tego samego filtra idempotencji i tej samej flagi zapisu. Technologia bez istniejącej kategorii dostaje `learning.category.missing` i `422`. Slug kategorii w technologii jest zamrożony razem ze slugiem technologii: przeniesienie do innej kategorii jest rewizją pola `categorySlug`, dozwoloną, bo nie zmienia adresu technologii. Adres technologii to jej slug, nie ścieżka kategorii.

### 2. Lekcja

`POST /courses/{slug}/lessons` tworzy lekcję. Body jest Markdownem w obu kulturach; jedna kultura może być pusta tylko jeśli druga nie jest, zgodnie z `LocalizedText`. Obie puste to `learning.title.blank` albo `learning.lesson.body_blank`, zależnie od pola. Osobny kod na body: `learning.lesson.body_blank`.

`PUT /lessons/{slug}` zmienia tytuł, body, widoczność i `order`. Nie zmienia sluga. Wymaga `If-Match`.

### 3. Widoczność na tymczasowym GET

`GET /lessons/{slug}` zwraca `200` tylko dla `public`, dopóki nie ma aktora. Inna widoczność to `404` i kod `learning.lesson.not_found`. Ten sam kod, co przy braku wiersza. Test ma oba arrange: brak wiersza i wiersz `private`. Oba kończą się `404` z tym samym `type`.

### 4. Postęp

`POST /lessons/{slug}/completion` w Development. Drugie wywołanie zwraca `200` i ten sam `CompletedAt`. Nie ma `201` za drugim razem. Tabela ma unikalność pary student–lekcja, więc wyścig dwóch pierwszych zapisów kończy się jednym wierszem: handler łapie naruszenie unikalności i czyta istniejący wiersz.

### Co jest do czego

| Endpoint | Handler | Uwaga |
| --- | --- | --- |
| `POST /categories` | `PublishCategoryHandler` | dopisać handler, jeśli poprzedni numer miał tylko technologię, kurs i lekcję |
| `POST /technologies` | `PublishTechnologyHandler` | wymaga kategorii |
| `POST /courses` | `PublishCourseHandler` | wymaga technologii |
| `POST /courses/{slug}/lessons` | `PublishLessonHandler` | wymaga kursu |
| `PUT /lessons/{slug}` | `ReviseLessonHandler` | `If-Match` |
| `POST /lessons/{slug}/completion` | `CompleteLessonHandler` | idempotentny w domenie |

`PublishCategoryHandler` jest w tym numerze, jeśli nie powstał przy portach. Porty miały `ICategoryRepository`. Handler dopisujemy tu, jednym przypadkiem użycia, nie drugim stylem.

## Testy

Nazwa: `Get_private_lesson_returns_not_found`.

| Arrange | Act | Assert |
| --- | --- | --- |
| lekcja `private` w magazynie | `GET /lessons/{slug}` bez aktora | `404`, `type` równe `learning.lesson.not_found` |

Nazwa drugiego faktu, domena: `Complete_twice_keeps_the_first_timestamp`.

```bash
dotnet test tests/Learning.Domain.Tests tests/Learning.Api.Tests --nologo --filter "Category!=Integration"
```

Test unikalności `order` jest integracyjny: dwie lekcje z tym samym `order` w jednym kursie, druga dostaje konflikt z bazy zamieniony na `learning.lesson.order_taken` i `409`.

## Checkpoint

```bash
dotnet test tests/Learning.Api.Tests --nologo --filter "FullyQualifiedName~Get_private_lesson"
aspire run
```

Przez bramę da się opublikować kategorię, technologię, kurs i lekcję publiczną, a `GET` tej lekcji zwraca Markdown, nie HTML. `GET` lekcji `private` zwraca `404`. W odpowiedzi publicznej nie ma fragmentu body lekcji prywatnej.

## Umiesz wyjaśnić

Dlaczego prywatna lekcja nie zwraca `403`?

Bo `403` mówi, że wiersz istnieje i nie wolno go czytać. Gość nie ma się tego dowiedzieć. `404` jest tą samą odpowiedzią co przy literówce w slugu.

Dlaczego blok kodu nie ma tabeli?

Bo jest fragmentem treści lekcji. Osobna tabela rozjeżdża się z Markdownem przy pierwszej edycji: tekst mówi co innego niż wiersz. Renderer czyta ogrodzenie.

Dlaczego ukończenie drugi raz jest `200`, nie błędem?

Bo przycisk „ukończ” wciśnięty dwa razy nie jest pomyłką użytkownika, tylko retry. Zmiana czasu na drugą chwilę kłamałaby, kiedy naprawdę skończył.

Czy zmiana kategorii technologii zmienia adres?

Nie. Adresem jest slug technologii. `categorySlug` jest polem. CV i tak wskaże slug technologii, nie kategorię.

Dlaczego zapis na maszynie nadal może być wyłączony?

Bo nie ma jeszcze JWT. Flaga z numeru REST zostaje. Ten numer nie włącza zapisu na tunelu. Włączy go numer polityk, gdy `owner` będzie sprawdzany.

## Zasady

- Trzy widoczności, żadnej czwartej flagi.
- Brak tabeli na blok kodu.
- `GET` bez aktora nie zwraca body lekcji innej niż `public`.
- Drugie ukończenie nie przesuwa `CompletedAt`.
- Slug lekcji nie zmienia się w `PUT`.
- Zapis na maszynie zostaje wyłączony do czasu JWT.

## Poza zakresem

- HTML i sanitacja są w [06-odczyt-dapper.md](../04-zdarzenia/06-odczyt-dapper.md).
- Szyna z faktami katalogu jest w [01-kontrakty.md](../04-zdarzenia/01-kontrakty.md).
- Rola `student` przy ukończeniu jest w [03-jwt.md](../05-tozsamosc/03-jwt-i-policies.md).
- Ekran lekcji jest w [05-tresc-publiczna.md](../11-web/05-tresc-publiczna.md).

## Źródła

- [RFC 9110, 404 i 403](https://www.rfc-editor.org/rfc/rfc9110). Wiążące jest to, że `403` potwierdza istnienie zasobu. Dotyczy lekcji niewidocznej. Ze źródła nie bierzemy `401` dla gościa na treści prywatnej: gość dostaje `404`, nie prośbę o logowanie, która też zdradza wiersz. Prośba o logowanie jest na endpoincie ukończenia, gdy ten wymaga tokenu.
- [CommonMark](https://spec.commonmark.org/). Wiążące jest ogrodzenie kodu jako część dokumentu. Dotyczy braku osobnej tabeli. Ze źródła nie bierzemy HTML w bazie.
- [PostgreSQL unique constraint](https://www.postgresql.org/docs/current/ddl-constraints.html). Wiążąca jest unikalność `(course_slug, order)`. Dotyczy kolejności lekcji. Ze źródła nie bierzemy unikalności samej kolumny `order` w całej tabeli: dwa kursy mogą mieć lekcję o `order` równym zero.

## Gotowe gdy

- Da się przejść ścieżkę kategoria → technologia → kurs → lekcja publiczna w Development.
- `GET` lekcji `private` i `GET` nieznanego sluga dają ten sam `type`.
- Dwie lekcje z tym samym `order` w jednym kursie dają `learning.lesson.order_taken`.
- Drugie ukończenie nie zmienia chwili.
- Body w odpowiedzi tymczasowej jest Markdownem.
- Na maszynie `WritesEnabled` pozostaje `false`.
