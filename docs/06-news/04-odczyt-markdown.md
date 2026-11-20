# 04. Odczyt notatki

Poprzedni numer to [zapis](03-zapis.md). Następny folder zaczyna się od [sekcji CV](../07-cv/01-sekcje.md).

Ten numer wystawia `GET` notatki z `news_read`. Markdown zamienia się na HTML tą samą sanitacją co lekcja. Lista pokazuje tylko to, co projektor zapisał i co wolno roli. `GET` nie ładuje strumienia.

## Po co

Zapis notatki bez strony zostawia właściciela z `ETag` i bez podglądu. Czytelnik ma dostać HTML, nie zdarzenia. Okno po `POST`, w którym `GET` jest `404`, zostaje i jest opisane, nie zatkane czytaniem strumienia.

## Dlaczego ta technologia

### Ten sam `LessonHtml` w współdzielonym miejscu

Sanitacja była w Learning. News potrzebuje jej drugi raz. Kod jedzie do `src/shared/JJDevHub.Content/MarkdownHtml.cs`. Learning i News wołają to samo. Problem, który zamyka, to dwie białe listy tagów, z których jedna przepuści `script`. Odpada kopiowanie klasy. Koszt: wspólny projekt. Powstaje teraz, przy drugim konsumencie, zgodnie z zasadą bibliotek.

### Dapper

`IReadQueries` w News, SQL po `article_document`. Filtr widoczności jak w Learning: gość tylko `public`, student dodatkowo `students`, właściciel wszystko, ukryte jako `404` z `news.article.not_found`. Problem, który zamyka, to `GET` składający artykuł z `stream_events`. Odpada EF na odczycie. Koszt: SQL w repozytorium.

### Lista

`GET /articles` zwraca slug, tytuł w języku żądania i widoczność. Nie zwraca HTML listy całych notatek. Szczegół `GET /articles/{slug}` zwraca `html` i `ETag` z wersji dokumentu. Wersja dokumentu jest wersją strumienia, którą projektor zapisał z wiadomości. `If-Match` przy `PUT` nadal idzie do strumienia. Jak przy lekcji, chwilowy rozjazd o jeden jest możliwy i nie scala się transakcją przez dwie bazy.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| dokument notatki | wiersz w `news_read`, nie strumień |
| `html` | wynik sanitacji, jedyne pole treści w DTO |
| okno | czas, w którym zapis już jest, a dokument jeszcze nie |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/shared/JJDevHub.Content/MarkdownHtml.cs` | Markdig i biała lista |
| `News.Infrastructure/Read/DapperArticleQueries.cs` | lista i szczegół |
| `News.Api/Endpoints/ArticleEndpoints.cs` | `GET` |
| `Learning.Infrastructure/Read/LessonHtml.cs` | staje się cienkim wywołaniem wspólnego kodu albo znika |
| `tests/JJDevHub.Content.Tests/MarkdownHtmlTests.cs` | `script` |

Projektor News, jeśli w poprzednim numerze zapisał surowy JSON, tu tylko upewniamy się, że `GET` czyta ten dokument. Nie dokładamy drugiej tabeli.

## Plan

### 1. Przeniesienie sanitacji

Test `<script>` z Learning przechodzi na projekcie `JJDevHub.Content`. Learning zostaje zielony. News używa tego samego wywołania.

### 2. SQL

Parametr widoczności, nie string sklejony z nagłówka. Język `pl` albo `en` wybierany w C#.

### 3. Zakaz strumienia na GET

Test architektury albo `grep` w teście: projekt `News.Api` przy endpointach `GET` nie referencjonuje wywołania `IEventStore.Read`. Handler `PUT` wolno mu użyć przez aplikację, nie w funkcji `GET`.

### Co jest do czego

| Endpoint | Źródło |
| --- | --- |
| `GET /articles` | `article_document` |
| `GET /articles/{slug}` | `article_document` i `MarkdownHtml` |
| `PUT /articles/{slug}` | strumień, bez zmian wobec poprzedniego numeru |

## Testy

Nazwa: `Get_article_does_not_load_stream`.

| Arrange | Act | Assert |
| --- | --- | --- |
| dokument w `news_read`, strumień, który przy odczycie rzuciłby wyjątek, gdyby ktoś go zawołał | `GET` | `200` i HTML, wyjątek strumienia nie występuje |

Najprostsza asercja: fałszywy `IEventStore` w hoście `GET`, który rzuca. Jeśli kompozycja `GET` w ogóle go nie ma w zasięgu, test hosta bez rejestracji magazynu przechodzi. To jest lepsze. Host `GET` w teście nie rejestruje `IEventStore`. Jeśli start rzuci, ktoś wciągnął strumień do odczytu.

Nazwa: `Private_article_is_not_found_for_guest`.

```bash
dotnet test tests/News.Api.Tests tests/JJDevHub.Content.Tests --nologo
```

## Checkpoint

```bash
dotnet test tests/News.Api.Tests tests/JJDevHub.Content.Tests --nologo --filter "Category!=Performance"
```

Przez bramę właściciel tworzy notatkę publiczną, po chwili `GET` zwraca HTML bez `<script`, nawet jeśli Markdown je zawierał. Gość dostaje `404` na notatkę `private`. Natychmiastowy `GET` tuż po `POST` wolno, że jest `404`. Test nie wymaga inaczej. Ręcznie widać to w pierwszym ułamku sekundy albo wcale, zależnie od relayu. Specyfikacja nie każe tego okna usuwać.

## Umiesz wyjaśnić

Dlaczego `GET` nie składa historii?

Bo lista i strona są zapytaniem pod slug. Historia jest do decyzji przy zapisie. Składanie jej na odczycie omija dokument i robi z `GET` drugą komendę.

Dlaczego sanitacja jest wspólna?

Bo druga kopia białej listy rozjedzie się i jedna treść będzie bezpieczna, a druga nie. Lekcja i notatka są tym samym rodzajem HTML.

Czemu okno `404` zostaje?

Bo projekcja jest asynchroniczna. Zatkanie okna odczytem zapisu związałoby stronę publiczną z magazynem strumienia. Klient ponawia.

Skąd `ETag` na `GET`, skoro wersja żyje w strumieniu?

Projektor zapisuje wersję z wiadomości. To jest wersja, którą klient zobaczył. `PUT` porównuje ją ze strumieniem. Chwilowa różnica daje `409` albo `412` i klient odświeża. Nie ma wspólnego commita.

Dlaczego gość nie dostaje innego kodu dla notatki prywatnej?

Z tego samego powodu co przy lekcji. `404` nie mówi, że wiersz jest.

## Zasady

- `GET` nie woła `IEventStore`.
- DTO nie zawiera Markdownu źródłowego.
- `<script` nie przechodzi testu HTML.
- Widoczność ukryta i brak wiersza mają ten sam `type` dla gościa.
- News nie czyta `learning_read` ani `news_write` na `GET`.
- Wspólna sanitacja jest w `JJDevHub.Content`.

## Poza zakresem

- Ekran notatki jest w [05-tresc-publiczna.md](../11-web/05-tresc-publiczna.md).
- Edytor jest w [06-intranet.md](../11-web/06-intranet.md).
- Cache jest w [06-redis.md](../13-kubernetes/06-redis.md).

## Źródła

- Numer odczytu Learning. Wiążące są Dapper, sanitacja i `404` po `POST`. Dotyczy notatki. Ze źródła nie bierzemy tymczasowego `GET` ze strumienia.
- [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457). Wiążący jest stabilny `type`. Dotyczy `news.article.not_found`. Ze źródła nie bierzemy osobnego statusu „ukryte”.
- Numer agregatu. Wiążące jest to, że `Replay` nie jest ścieżką strony. Dotyczy zakazu `IEventStore` na `GET`. Ze źródła nie bierzemy wyjątku dla właściciela: właściciel też czyta dokument, tylko bez filtra widoczności.

## Gotowe gdy

- Test gościa na notatce prywatnej zwraca `news.article.not_found`.
- Host `GET` wstaje bez rejestracji `IEventStore`.
- Test `script` jest w projekcie wspólnym i Learning go używa.
- Lista nie zwraca pełnego HTML wszystkich notatek.
- `PUT` nadal wymaga `If-Match` i `owner`.
