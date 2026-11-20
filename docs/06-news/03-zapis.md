# 03. Zapis notatki

Poprzedni numer to [strumień](02-strumien.md). Następny to [odczyt](04-odczyt-markdown.md).

Ten numer spina `Article` ze strumieniem, outboxem i HTTP. Zapis ładuje strumień, robi `Replay`, woła metodę, dopisuje zdarzenia i wiersz outboxa w jednej transakcji. `POST` wymaga `owner`. Kształt na szynie jest rekordem `News.Contracts`, nie zdarzeniem domenowym.

## Po co

Agregat i magazyn osobno nie tworzą notatki przez bramę. Handler zamyka kolejność: najpierw historia, potem decyzja, potem jeden commit historii i faktu na szynę. Bez outboxa w tej samej transakcji projektor zobaczy artykuł, którego strumień nie ma, albo odwrotnie. Ta sama reguła co przy kursie, inny magazyn stanu.

## Dlaczego ta technologia

### Handler jak w Learning, inny port

`ReviseArticleHandler` bierze `IEventStore`, nie repozytorium EF encji. Problem, który zamyka, to zapis artykułu jako `UPDATE` wiersza obok strumienia, który potem kłamie. Odpada drugi model bieżącego stanu w zapisie. Koszt: każde polecenie czyta cały strumień artykułu. Artykuł jest krótki. Nie optymalizujemy migawki.

### Rekord na szynę

`ArticleRevisedV1` w `News.Contracts` ma slug, tytuły, widoczność i `occurredAt`. Nie ma listy wszystkich zdarzeń od początku świata. Problem, który zamyka, to konsument, który musiałby znać `Apply`. Odpada publikacja `ArticleRevised` z domeny. Koszt: handler mapuje ręcznie. Jedno miejsce.

Topic `news.articles`, klucz slug, zgodnie ze słownikiem. Relay i projektor News są kopią układu Learning: osobne projekty, ten sam tag `news-vX.Y.Z`. Nie współdzielą procesu z Learning.

### Idempotencja i `ETag`

`POST /articles` wymaga `Idempotency-Key`. `PUT` wymaga `If-Match` z wersją strumienia. Brak nagłówka to `428`. Zła wersja to `409` i `stream.version.conflict`. Problem, który zamyka, to edycja notatki bez wersji, którą klient zobaczył. Odpada `PUT` bez warunku. Wersja w `ETag` na tym numerze, zanim jest odczyt projekcji, bierze się z odpowiedzi `PUT` i `POST`. `GET` dojdzie w następnym numerze. Do tego czasu `GET` nie czyta strumienia.

### Autoryzacja

Polityka `owner` na zapisie. Aktor w audycie, tak jak przy lekcji. Utworzenie i rewizja dopisują wpis audytu w tej samej transakcji. `after` zawiera tytuł i widoczność, nie token.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| załadowanie | `Read` i `Replay` przed decyzją |
| niezatwierdzone zdarzenia | lista z agregatu, którą `Append` zapisuje |
| rekord integracyjny | kształt na `news.articles` |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `News.Application/UseCases/CreateArticle.cs` | utworzenie |
| `News.Application/UseCases/ReviseArticle.cs` | rewizja |
| `News.Application/UseCases/PublishArticle.cs` | publikacja |
| `News.Contracts/ArticleCreatedV1.cs` i sąsiedzi | szyna; nazwy zgodne z faktami, osobne typy |
| `News.Api/Endpoints/ArticleEndpoints.cs` | HTTP |
| `News.Relay`, `News.Projector` | ten sam wzorzec co Learning, topic `news.articles` |
| `tests/News.Application.Tests/` | konflikt bez kontenera, magazyn w pamięci |

Nazwy rekordów na szynie: `news.article-created.v1` i dalej, atrybut na rekordzie kontraktu. Zdarzenie domenowe ma ten sam łańcuch. Dwa typy, jedna nazwa, żeby nie mieć dwóch identyfikatorów tego samego faktu. Projektor rozróżnia je po tym, że na szynie leży JSON kontraktu. Do strumienia idzie JSON zdarzenia domenowego o tym samym kształcie pól, które kontrakt kopiuje. Żeby nie utrzymywać dwóch JSON-ów, handler serializuje kontrakt i do strumienia, a domena zostaje przy typie CLR w procesie. Kolumna `type` i tak jest łańcuchem. Payload w strumieniu jest payloadem kontraktu. Zdarzenie domenowe nie jest serializowane osobno. `Apply` dostaje typ CLR złożony z tego JSON przy `Replay`. Jedna mapa.

To upraszcza: strumień i szyna niosą ten sam JSON. Różnica zostaje w tym, że na szynę idzie tylko fakt, który handler świadomie kopiuje do outboxa. Przy rewizji i utworzeniu i publikacji kopiujemy wszystkie. Nie ma zdarzenia domenowego, które zostaje wyłącznie w strumieniu, na tym numerze.

## Plan

### 1. Kolejność w handlerze

Odczyt strumienia, deserializacja do zdarzeń CLR, `Replay`, metoda, `Append` oczekiwanej wersji, `IOutbox.Enqueue`, audyt, `Commit` jednej transakcji. `IUnitOfWork` obejmuje połączenie strumienia i outbox. Oba są w `news_write`, więc jedna transakcja Postgresa jest możliwa. Implementacja nie otwiera dwóch połączeń.

### 2. HTTP

`POST /articles` zwraca `201` i `ETag` wersji. `PUT /articles/{slug}` wymaga `If-Match`. Polityka `owner`. Idempotencja jak w Learning, tabela w `news_write`.

### 3. Relay

Kopiuje układ Learning. Inny topic, inna grupa docelowa `news-projector`. Projektor na tym numerze może tylko sprawdzać inbox i nie budować jeszcze HTML. Zapis dokumentu odczytu jest w następnym numerze. Żeby relay miał sens, projektor już konsumuje i odkłada w inboxie, a dokument dopina następny numer. Pusta konsumpcja bez dokumentu gubi skutek, jeśli offset zejdzie. Dlatego ten numer albo nie wdraża projektora, albo projektor zapisuje surowy JSON do `news_read.article_document`. Następny numer dodaje HTML przy `GET`, niekoniecznie nową tabelę. Zapisujemy surowy dokument już tu. HTML przy odczycie, jak lekcja.

### Co jest do czego

| Krok | Baza |
| --- | --- |
| strumień i outbox i audyt | `news_write`, jeden commit |
| dokument | `news_read`, po Kafce, z inboxem |

## Testy

Nazwa: `Revise_with_stale_version_returns_conflict`.

| Arrange | Act | Assert |
| --- | --- | --- |
| artykuł w wersji 1 w magazynie pamięci | `Revise` z oczekiwaną wersją 0 | `stream.version.conflict`, brak nowego zdarzenia |

```bash
dotnet test tests/News.Application.Tests --nologo
```

## Checkpoint

```bash
dotnet test tests/News.Application.Tests tests/News.Domain.Tests --nologo
aspire run
```

Jako właściciel, `POST` notatki i `PUT` z `If-Match` przechodzą. Drugi `PUT` ze starym `ETag` dostaje `409`. W `outbox_messages` jest wiersz z `news.article-created.v1`. Bez tokenu `POST` dostaje `401`.

## Umiesz wyjaśnić

Dlaczego odczyt strumienia jest w komendzie, a nie w `GET`?

Bo decyzja potrzebuje stanu złożonego z historii. Strona publiczna potrzebuje jednego brzmienia pod slug. To drugie jest projekcją. Składanie historii na każde `GET` listy jest wolne i omija indeks.

Dlaczego ten sam JSON w strumieniu i na szynie?

Bo dwa JSON-e tego samego faktu rozjadą się przy pierwszym polu. Typ CLR zostaje w procesie do `Apply`. Na zewnątrz i w tabeli jest kontrakt.

Co daje jedna transakcja strumienia i outboxa?

Nie ma zdarzenia na szynie bez zdarzenia w historii i odwrotnie, w granicach tej bazy. Kafka i tak dostanie fakt później. To jest ten sam outbox co w Learning.

Dlaczego `GET` jeszcze nie składa artykułu?

Bo następny numer ma jedno miejsce HTML i widoczności. Tymczasowy `GET` ze strumienia zostałby na zawsze, tak jak omal został w Learning.

Kto commituje wersję?

`Append` zapisuje wersję, której oczekiwał handler po `Replay`. `ETag` jest tą wersją. Nie ma osobnego `version` na boku strumienia.

## Zasady

- Handler zapisu nie otwiera `news_read`.
- Na szynę nie wychodzi typ z `News.Domain`.
- `PUT` bez `If-Match` nie woła handlera.
- Zapis wymaga `owner`.
- Jedno połączenie na strumień, outbox i audyt.
- Projektor News nie łączy się z `news_write`.

## Poza zakresem

- HTML i `GET` są w [04-odczyt-markdown.md](04-odczyt-markdown.md).
- Wskazanie lekcji slugiem z notatki, jeśli dojdzie, jest polem Markdown albo osobnym polem sluga, nie kluczem do `learning_write`. Nie wchodzi, dopóki ekran tego nie wymaga. Na tym numerze nie ma pola `lessonSlug`.

## Źródła

- Numer outboxa Learning. Wiążący jest jeden commit. Dotyczy strumienia i outboxa. Ze źródła nie bierzemy publikacji z handlera prosto na Kafkę.
- Numer agregatu. Wiążące jest `Replay` przed metodą. Dotyczy handlera. Ze źródła nie bierzemy zapisu pól agregatu poza `Append`.
- [RFC 9110, If-Match](https://www.rfc-editor.org/rfc/rfc9110#name-if-match). Wiążące są `428` i `412`. Dotyczy `PUT` notatki. Ze źródła nie bierzemy zapisu bez wersji dla właściciela.

## Gotowe gdy

- Test starej wersji przechodzi na magazynie w pamięci.
- `POST` bez tokenu daje `401`, z właścicielem `201` i `ETag`.
- W outboxie jest nazwa `news.article-created.v1`.
- W payloadzie na szynie nie ma nazwy klasy `ArticleCreated`.
- `GET` listy notatek jeszcze nie czyta strumienia.
