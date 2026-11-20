# 01. Agregat notatki

Poprzedni numer to [twardnienie](../05-tozsamosc/06-twardnienie-api.md). Następny to [strumień](02-strumien.md).

Ten numer stawia `Article` jako agregat ze zdarzeniami. Stan powstaje z `Apply`. Metoda publiczna nie zapisuje pól z boku. Nie ma jeszcze tabeli strumienia. Jest agregat w pamięci i test, który odtwarza go z listy zdarzeń.

## Po co

Notatka ma kolejne brzmienia. Bieżący wiersz, jak w Learning, gubi poprzedni tytuł. Strumień zdarzeń zostawia brzmienia, a bieżący stan i tak da się złożyć. To jest ten kawałek produktu, w którym historia jest treścią, nie kosztem.

Jeśli `Publish` ustawi pole `IsPublished` obok `Apply`, odtworzenie z zdarzeń pominie to pole albo ustawi je drugi raz. Jedna droga, `Apply`, zamyka rozjazd.

## Dlaczego ta technologia

### Zdarzenie domenowe

`ArticleCreated`, `ArticleRevised`, `ArticlePublished` są faktami w procesie. Mają `[EventName]`: `news.article-created.v1`, `news.article-revised.v1`, `news.article-published.v1`. Problem, który zamyka, to mutacja właściwości bez śladu. Odpada setter publiczny. Koszt: każda zmiana to nowy typ zdarzenia albo świadome pole w `ArticleRevised`. Na tym numerze rewizja niesie tytuł i Markdown obu kultur oraz widoczność. Nie niesie sluga. Slug jest w zdarzeniu utworzenia i potem jest zamrożony.

Te zdarzenia nie wychodzą jeszcze na szynę. Nazwa jest od razu stabilna, bo ten sam łańcuch trafi do tabeli strumienia. Rekord integracyjny na szynę będzie osobny, w `News.Contracts`, w numerze zapisu. Nie wysyłamy typu domenowego.

### `Apply` i `Replay`

`AggregateRoot` w jądrze dostaje listę niezatwierdzonych zdarzeń, `Version` i metody `Apply`, `Replay`. `Article.Revise` sprawdza regułę, buduje zdarzenie i woła `Apply`. `Replay` woła tylko `Apply`. Problem, który zamyka, to dwa miejsca zmiany stanu. Odpada logika w `Apply`, która odrzuca: `Apply` nie zwraca `Result`. Odmowa jest przed zbudowaniem zdarzenia. Koszt: łatwo pomylić i włożyć `if` do `Apply`. Test odtworzenia to łapie, bo stan po `Replay` ma być równy stanowi po metodach.

### Pusty tytuł

`Article.Create` z pustym tytułem zwraca `news.title.blank`. Nie powstaje zdarzenie. Widoczność jest tą samą trójką co w Learning.

### Brak `StudentProgress`

Tu nie ma „zwykłego stanu obok”. Cały artykuł jest strumieniem. Osobna tabela bieżącego stanu, jeśli powstanie, będzie projekcją, nie źródłem.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| agregat | granica spójności, tu jeden artykuł |
| zdarzenie domenowe | fakt w procesie i w strumieniu, nie na szynie |
| `Apply` | jedyne miejsce, które zmienia pola |
| `Replay` | złożenie stanu z historii, bez nowych faktów |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/shared/JJDevHub.Shared.Kernel/AggregateRoot.cs` | wersja, lista zdarzeń, `Replay` |
| `src/services/News/News.Domain/Article.cs` | reguły i `Apply` |
| `src/services/News/News.Domain/ArticleCreated.cs` i sąsiedzi | fakta |
| `tests/News.Domain.Tests/ArticleTests.cs` | odtworzenie |

Jądro nadal nie zna słowa `Article`. Zdarzenia leżą w News.

## Plan

### 1. Jądro

`AggregateRoot` trzyma `Version` od zera. `Raise(event)` woła `Apply` i dodaje zdarzenie do listy niezatwierdzonych oraz podbija wersję. `Replay(events)` czyści listę niezatwierdzonych, woła `Apply` po kolei i ustawia wersję na liczbę zdarzeń. `Replay` nie dopisuje nowych.

### 2. Artykuł

`Create` zwraca `Result<Article>`. `Revise` i `Publish` są metodami instancji i też zwracają `Result`. `Publish` na już opublikowanym jest sukcesem bez drugiego zdarzenia, jak ukończenie lekcji: drugie kliknięcie nie zmienia historii. To jest decyzja. Alternatywa, drugie `ArticlePublished`, zaśmieca strumień. Bierzemy brak drugiego zdarzenia.

### 3. Równość po replay

Test buduje artykuł metodami, bierze zdarzenia, tworzy pusty artykuł i robi `Replay`. Tytuł, widoczność i wersja są równe. Potem zmienia tytuł na kopii z metod i sprawdza, że pierwsza instancja się nie zmieniła: zdarzenia są wartościami.

### Co jest do czego

| Metoda | Zdarzenie |
| --- | --- |
| `Create` | `news.article-created.v1` |
| `Revise` | `news.article-revised.v1` |
| `Publish` pierwszy raz | `news.article-published.v1` |
| `Publish` drugi raz | brak nowego zdarzenia |

## Testy

Nazwa: `Replay_matches_state_built_by_methods`.

| Arrange | Act | Assert |
| --- | --- | --- |
| artykuł utworzony, zrewidowany i opublikowany | `Replay` na nowej instancji | ten sam tytuł `pl`, ta sama widoczność, ta sama wersja |

Nazwa: `Create_blank_title_returns_invalid`.

```bash
dotnet test tests/News.Domain.Tests --nologo
```

## Checkpoint

```bash
dotnet test tests/News.Domain.Tests --nologo --filter "Category!=Integration"
dotnet list src/services/News/News.Domain/News.Domain.csproj package
```

Lista paczek jest pusta. Testy nie podnoszą kontenera.

## Umiesz wyjaśnić

Dlaczego `Apply` nie zwraca błędu?

Bo odtworzenie nie może odmówić zdarzeniu, które już zaszło. Odmowa jest w metodzie, zanim zdarzenie powstanie. `Apply` tylko ustawia pola.

Co się psuje, gdy metoda ustawi pole z boku?

`Replay` złoży inny stan niż ten, który metoda właśnie pokazała użytkownikowi. Strumień przestaje być źródłem. Test równości to łapie.

Dlaczego drugie `Publish` nie dopisuje zdarzenia?

Bo stan się nie zmienia. Drugi fakt „opublikowano” nie niesie nowej informacji, a podbija wersję i psuje `ETag` bez powodu.

Czym to różni się od `Lesson`?

Lekcja jest wierszem. Poprzednie brzmienie ginie. Artykuł trzyma brzmienia, bo notatka jest materiałem, którego kolejne wersje chcemy móc złożyć. Koszt to odtworzenie przy zapisie, nie przy `GET`.

Dlaczego zdarzenie domenowe nie idzie na Kafkę?

Bo konsument musiałby znać typ z `News.Domain`. Na szynę wyjdzie rekord z `News.Contracts`, złożony w handlerze. Ten numer jeszcze go nie wysyła.

## Zasady

- Pole agregatu zmienia się tylko w `Apply`.
- `Replay` nie dopisuje zdarzeń.
- Pusty tytuł nie tworzy instancji w stanie sukcesu.
- Slug nie zmienia się po `Create`.
- Domena News nie referencjonuje EF ani Kafki.
- Nazwy zdarzeń są w atrybucie.

## Poza zakresem

- Tabela strumienia jest w [02-strumien.md](02-strumien.md).
- Handler HTTP i outbox są w [03-zapis.md](03-zapis.md).
- HTML jest w [04-odczyt-markdown.md](04-odczyt-markdown.md).

## Źródła

- Eric Evans, *Domain-Driven Design*. Wiążący jest agregat jako granica spójności. Dotyczy jednego `Article` na strumień. Ze źródła nie bierzemy wspólnego agregatu „treść” z lekcją.
- [Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html). Wiążące jest złożenie stanu z faktów i brak nowej decyzji przy odtworzeniu. Dotyczy `Apply` i `Replay`. Ze źródła nie bierzemy osobnego produktu magazynu zdarzeń poza Postgresem tej usługi.
- Numer jądra. Wiążące jest `Result` dla odmowy. Dotyczy pustego tytułu. Ze źródła nie bierzemy wyjątku jako sterowania.

## Gotowe gdy

- Test odtworzenia przechodzi i wersja się zgadza.
- Pusty tytuł zwraca `news.title.blank` i zero zdarzeń.
- Drugie `Publish` nie zwiększa liczby zdarzeń.
- `News.Domain` nie ma paczek.
- W jądrze nie ma typu `Article`.
