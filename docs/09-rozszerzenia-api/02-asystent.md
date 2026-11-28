# 02. Asystent kursu

Poprzedni numer to [GraphQL](01-graphql.md). Następny folder zaczyna się od [testów integracyjnych](../10-testy/01-integracyjne.md).

Ten numer dodaje `POST /learning/assistant/ask`. Model odpowiada na pytanie o kurs, który pytający i tak może przeczytać. Narzędzia modelu są tylko do odczytu. Argument narzędzia jest sprawdzany, zanim poleci zapytanie. Klucz modelu nie wchodzi do przeglądarki.

## Po co

Szukanie po jednej lekcji jest `GET`. Pytanie „o czym jest ten kurs” wymaga złożenia kilku lekcji i zdania. Asystent zamyka to zdanie bez dawania modelowi dostępu do bazy i bez dawania mu zapisu. Klucz w kliencie zamknąłby się wyciekiem przy pierwszym zrzucie strony.

## Dlaczego ta technologia

### `Microsoft.Extensions.AI`

`IChatClient` jest abstrakcją. W Development wskazuje na endpoint zgodny z OpenAI, ustawiony zmienną `ASSISTANT_ENDPOINT` i `ASSISTANT_MODEL`. W testach podstawiamy klienta, który zwraca z góry ustaloną odpowiedź i woła narzędzie. Problem, który zamyka, to wklejenie SDK jednego dostawcy w endpoint. Odpada wołanie modelu z przeglądarki. Koszt: bez endpointu i bez klucza w env asystent zwraca `503` z kodem `learning.assistant.unavailable`. Nie udaje odpowiedzi.

Klucz `ASSISTANT_API_KEY` jest w env, nie w git. Na maszynie plik `600`. OpenBao przejmie go później tak jak klucz FCM.

### Narzędzia

Dwa narzędzia: `GetCourseOutline` i `GetLessonExcerpt`. Oba przyjmują slug. Oba idą przez `IReadQueries` i przez ten sam zakres widoczności co `GET`. `GetLessonExcerpt` zwraca skrót tekstu, nie cały HTML, i nie zwraca lekcji, której pytający nie może czytać. Wtedy wynik narzędzia to „not found”, a model nie dostaje treści. Problem, który zamyka, to model z connection stringiem albo z SQL. Odpada narzędzie `ExecuteSql`. Koszt: model nie odpowie na pytanie o lekcję prywatną gościa. O to chodzi.

### Walidacja argumentu

Slug z argumentu przechodzi przez `Slug` z domeny. Zły kształt nie idzie do SQL. Problem, który zamyka, to argument modelu sklejony z zapytaniem. Odpada zaufanie, że model poda poprawny identyfikator. Koszt: model dostaje błąd narzędzia i może spróbować drugi raz. Liczba wywołań narzędzi w jednym żądaniu ma limit 4. Powyżej kończymy odpowiedź bez kolejnych wywołań.

### Zapis

Nie ma narzędzia publikacji, rewizji ani ukończenia. Żądanie „opublikuj kurs” jest tekstem, na który model może odpowiedzieć słowami. Nie ma skutku w bazie. Problem, który zamyka, to model jako właściciel. Odpada „na razie tylko w Development” z narzędziem zapisu. Koszt: asystent nie wyręcza intranetu.

### Ślad

Wywołanie modelu jest spanem `learning.assistant.ask` z atrybutami: model, liczba narzędzi, czas. Nie ma treści pytania w metryce. Pytanie może być w logu na poziomie Debug w Development i nie jest na Information na maszynie. Problem, który zamyka, to treść lekcji i pytanie w Prometheusie. Odpada logowanie pełnej odpowiedzi na Information.

### Kto woła

Polityka: zalogowany `student` albo `owner`. Gość dostaje `401`. Gość i tak czyta publiczne `GET`. Asystent kosztuje wywołanie modelu, więc nie jest anonimowy. Limit na bramie: 10 żądań na minutę na `sub`, nie na IP. Osobna polityka limitera. Problem, który zamyka, to jeden token wypalający budżet. Odpada ten sam kubełek co logowanie.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| narzędzie | funkcja, którą model wolno woła, tu tylko odczyt |
| `IChatClient` | port modelu, podstawiany w teście |
| excerpt | skrót lekcji, nie cały dokument |
| budżet | limit wywołań narzędzi i limit żądań |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Learning.Api/Assistant/AssistantEndpoint.cs` | `POST /assistant/ask` |
| `Learning.Api/Assistant/CourseTools.cs` | dwa narzędzia |
| `Learning.Application/UseCases/Queries/AskCourseQuery.cs` | pytanie o kurs |
| `Learning.Application/UseCases/Handlers/AskCourseHandler.cs` | złożenie odpowiedzi |
| `tests/Learning.Api.Tests/AssistantTests.cs` | fałszywy `IChatClient` |

Paczki `Microsoft.Extensions.AI` i `Microsoft.Extensions.AI.OpenAI` w wersjach z tabeli technologii.

## Plan

### 1. Wejście

Ciało: `courseSlug`, `question`. Puste pytanie to `422` i `learning.assistant.question_blank`. Slug, którego nie ma, to `404` zanim poleci model. Nie płacimy wywołaniem za literówkę.

### 2. Narzędzia

Rejestracja na kliencie czatu. Implementacja woła odczyt z zakresem aktora. Test: gość nie wchodzi, a student pytający o kurs `private` dostaje `404` i model nie jest wołany. Student pytający o kurs `students` dostaje odpowiedź, a narzędzie lekcji `private` w tym kursie zwraca not found do modelu. Jeśli kurs `students` ma tylko publiczne lekcje, narzędzie je oddaje.

### 3. Odpowiedź

`200` i `{ "answer": "...", "toolsUsed": ["GetCourseOutline"] }`. `503`, gdy klient modelu rzuci. Bez stosu i bez klucza w ciele.

### Co jest do czego

| Narzędzie | Czego nie robi |
| --- | --- |
| `GetCourseOutline` | nie zwraca lekcji spoza zakresu |
| `GetLessonExcerpt` | nie zwraca HTML z `script`, bierze tekst po sanitacji albo Markdown obcięty do 1500 znaków |
| endpoint | nie przyjmuje connection stringa od klienta |

## Testy

Nazwa: `Private_course_does_not_call_model`.

| Arrange | Act | Assert |
| --- | --- | --- |
| kurs `private`, aktor `student`, klient modelu, który rzuca, jeśli go zawołać | `POST /assistant/ask` | `404`, wyjątek klienta nie występuje |

Nazwa: `Tool_argument_with_invalid_slug_is_rejected`.

```bash
dotnet test tests/Learning.Api.Tests --nologo --filter "FullyQualifiedName~Private_course_does_not_call|FullyQualifiedName~Tool_argument"
```

## Checkpoint

```bash
dotnet test tests/Learning.Api.Tests --nologo --filter "FullyQualifiedName~Assistant|FullyQualifiedName~Private_course_does_not_call"
```

W `appsettings.json` nie ma klucza. Jest pusta nazwa zmiennej w przykładzie env. `grep` po repozytorium na `sk-` i na `ASSISTANT_API_KEY=` z wartością nie znajduje sekretu.

## Umiesz wyjaśnić

Dlaczego model nie dostaje SQL?

Bo argument modelu jest tekstem z zewnątrz, także wtedy, gdy model „chce dobrze”. Jedyna droga do danych to narzędzie, które i tak sprawdza widoczność i slug. SQL z argumentu jest wstrzyknięciem z dodatkowym krokiem.

Dlaczego nie ma narzędzia zapisu?

Bo skutku publikacji nie chcemy opierać na zdaniu, które model uzna za polecenie. Zapis zostaje w intranecie, przy człowieku i przy `If-Match`.

Czemu gość nie pyta?

Bo wywołanie kosztuje i nie mamy limitu per człowiek bez `sub`. Publiczny odczyt lekcji zostaje. Asystent jest dla zalogowanego.

Co zostaje w metryce?

Nazwa modelu, liczba narzędzi, czas, kod wyniku. Nie pytanie i nie fragment lekcji. Fragment w etykiecie zabiłby kardynalność i wyniósł treść.

Dlaczego `503`, a nie pusta odpowiedź, gdy nie ma klucza?

Bo pusta odpowiedź wygląda jak „kurs jest o niczym”. `503` i kod `learning.assistant.unavailable` mówią, że funkcji nie ma w tej chwili. Zapis kursu działa dalej.

## Zasady

- Narzędzia są tylko do odczytu.
- Argument sluga przechodzi przez typ `Slug`.
- Klucz modelu nie jest w git ani w odpowiedzi.
- Pytanie nie jest etykietą metryki.
- Najpierw sprawdzamy, że kurs istnieje i jest widoczny, potem wołamy model.
- Limit narzędzi w jednym żądaniu to 4.
- Brak `ExecuteSql` i brak narzędzia publikacji.

## Poza zakresem

- Ekran czatu, jeśli powstanie, jest we frontcie i nie jest wymagany, żeby ten endpoint był gotowy. Wystarczy `POST`.
- Osobny model hostowany na maszynie nie jest tym numerem. Jest endpointem zgodnym z API, które opakowuje `IChatClient`.
- OpenBao dla klucza jest w [05-openbao.md](../13-kubernetes/05-openbao.md).

## Źródła

- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai). Wiążące są `IChatClient` i funkcje jako narzędzia. Dotyczy portu modelu. Ze źródła nie bierzemy klucza w aplikacji przeglądarki.
- [OWASP LLM prompt injection](https://genai.owasp.org/llmrisk/llm01-prompt-injection/). Wiążące jest to, że tekst modelu nie jest poleceniem zapisu i że narzędzie ma własną autoryzację. Dotyczy braku narzędzia publikacji i sprawdzenia widoczności w narzędziu, nie w promptcie. Ze ściągi nie bierzemy obietnicy, że prompt „nie słuchaj użytkownika” zastępuje sprawdzenie w kodzie.
- Numer widoczności Learning. Wiążący jest `404` dla treści spoza zakresu. Dotyczy asystenta tak samo jak `GET`. Ze źródła nie bierzemy wyjątku dla modelu.

## Gotowe gdy

- Test kursu prywatnego nie woła modelu.
- Zły slug w argumencie narzędzia nie idzie do SQL.
- W repozytorium nie ma wartości klucza.
- `503` przy braku konfiguracji ma kod `learning.assistant.unavailable`.
- Żadne narzędzie nie zapisuje.
