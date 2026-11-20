# 01. GraphQL katalogu

Poprzedni numer to [push](../08-contact/03-push.md). Następny to [asystent](02-asystent.md).

Ten numer wystawia `POST /graphql` na Learning. Zapytanie katalogu bierze technologię i jej kursy jednym żądaniem, bez N+1. Mutacja, jeśli jest, idzie przez te same handlery co REST i wymaga `owner`. REST zostaje. GraphQL nie jest drugą drogą zapisu z innymi regułami.

## Po co

Ekran technologii woła listę kursów osobnym `GET`. To jest poprawne i zostaje. GraphQL zamyka przypadek, w którym klient chce drzewo „technologia, kursy, liczba lekcji” i nie chcemy dawać mu pięciu round-tripów ani specjalnego DTO pod jeden ekran. DataLoader zamyka N+1: resolver kursu nie robi zapytania na technologię w pętli.

## Dlaczego ta technologia

### Hot Chocolate

`HotChocolate.AspNetCore` i `HotChocolate.Data` są na MIT, w wersjach z tabeli technologii. Problem, który zamyka, to własny parser zapytań. Odpada doklejanie pól do REST „bo klientowi zabrakło”. Koszt: drugi kontrakt obok OpenAPI. OpenAPI zostaje źródłem klienta webu. GraphQL jest dla zapytań katalogu, nie dla całego produktu. News, CV i Contact nie dostają GraphQL w tym numerze.

### DataLoader

Loader kursów po slugu technologii zbiera klucze z jednego żądania i robi jedno SQL. Problem, który zamyka, to resolver, który w pętli woła Dappera. Odpada `Include` wszystkiego na zapas. Koszt: loader jest scoped na żądanie. Nie jest singletonem, bo niósłby klucze między użytkownikami.

### Błędy

Błąd pola nie zwraca `200` z pustą stroną bez informacji. Hot Chocolate oddaje błąd w `errors` i dane, które są. Tam, gdzie REST dałby `404` dla ukrytej lekcji, resolver zwraca `null` i błąd z kodem `learning.lesson.not_found`, tym samym co REST. Nie ma osobnego kodu „graphql”. Problem, który zamyka, to dwa języki błędów. Odpada wyjątek domeny jako `500`.

### Autoryzacja

Zapytanie odczytu publicznego jest anonimowe i stosuje ten sam zakres widoczności co `GET`. Mutacja `publishCourse` wymaga `owner` i woła `PublishCourseHandler`. Nie otwiera `DbContext` w resolverze mutacji. Problem, który zamyka, to zapis obok handlera, bez idempotencji i bez audytu. Odpada mutacja, która wstawia SQL. Koszt: mutacja i tak jest gorsza w idempotencji niż `POST` z kluczem. Dlatego mutacja publikacji przyjmuje `idempotencyKey` jako argument. Brak klucza to błąd walidacji.

### Limit

Głębokość zapytania i liczba węzłów mają limit w konfiguracji. Problem, który zamyka, to zapyta nie zagnieżdżone w kosztowne drzewo. Odpada brak limitu. Koszt: uczciwe duże zapytanie ekranu musi się mieścić. Próg jest w teście: zapytanie głębsze niż limit pada.

### Brama

Trasa `/graphql` już jest w słowniku i prowadzi do Learning. Ten numer ją realizuje. Nie ma drugiego GraphQL na bramie.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| resolver | funkcja pola |
| DataLoader | zbicie wielu kluczy w jedno zapytanie na żądanie |
| mutacja | zapis przez istniejący handler |
| `errors` | lista błędów obok `data` |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Learning.Api/GraphQL/Query.cs` | katalog |
| `Learning.Api/GraphQL/Mutation.cs` | woła handlery |
| `Learning.Api/GraphQL/CourseByTechnologyLoader.cs` | DataLoader |
| `tests/Learning.Api.Tests/GraphQlTests.cs` | jeden SQL na kursy, przez licznik zapytań albo log |

## Plan

### 1. Zapytanie

Pole `technology(slug)` zwraca technologię i kursy. Loader bierze slug. Test z dwiema technologiami w jednym zapytaniu oczekuje, że zapytań o kursy jest jedno, nie dwa. Licznik jest w dekoratorze `IReadQueries` w teście.

### 2. Widoczność

Gość nie dostaje kursu `private`. Wynik jest `null` na tym węźle albo kurs znika z listy. Lista pomija ukryte. Pojedyncze pole lekcji ukrytej zwraca błąd z kodem REST. Nie zwraca tytułu ukrytej lekcji.

### 3. Mutacja

`publishCourse` wywołuje handler. Resolver nie zawiera reguły pustego tytułu. Test pustego tytułu i tak dostaje `learning.title.blank`.

### Co jest do czego

| Pole | Skąd dane |
| --- | --- |
| technologia, kursy | `IReadQueries` i loader |
| mutacja | handler aplikacji |
| błąd | ten sam kod co `ProblemDetails.type` |

## Testy

Nazwa: `Two_technologies_load_courses_in_one_query`.

| Arrange | Act | Assert |
| --- | --- | --- |
| dwie technologie z kursami | jedno zapytanie GraphQL | licznik odczytu kursów równy 1 |

```bash
dotnet test tests/Learning.Api.Tests --nologo --filter "FullyQualifiedName~Two_technologies_load_courses"
```

## Checkpoint

```bash
dotnet test tests/Learning.Api.Tests --nologo --filter "FullyQualifiedName~GraphQl|FullyQualifiedName~Two_technologies"
aspire run
```

`POST /graphql` z zapytaniem technologii zwraca kursy. Zapytanie ponad limit głębokości zwraca błąd bez wykonywania resolverów liści. W Development da się otworzyć narzędzie do zapytań tylko lokalnie, nie przez tunel. Jeśli Hot Chocolate wystawia UI, jest wyłączone poza Development, tak jak Scalar.

## Umiesz wyjaśnić

Dlaczego REST zostaje?

Bo klient OpenAPI, idempotencja i `ETag` są już na REST. GraphQL nie zastępuje `PUT` z `If-Match`. Jest drzewem odczytu i cienką mutacją, która i tak woła ten sam handler.

Czym DataLoader różni się od `Include`?

`Include` ładuje graf, który serwer uzna za potrzebny. Loader ładuje dokładnie klucze, o które resolvery tego jednego żądania spytały, i robi to raz. Nie ciągnie EF na odczyt, którego nie chcemy.

Dlaczego błąd ma ten sam kod co REST?

Bo klient nie powinien mieć dwóch słowników. `learning.title.blank` zostaje `learning.title.blank`.

Co się stanie z zagnieżdżonym zapytaniem bez limitu?

Jedno żądanie potrafi wygenerować pracę większą niż sto `GET`. Limit głębokości i złożoności to odpowiednik limitu ciała.

Dlaczego mutacja nie pisze SQL?

Bo ominęłaby audyt, wersję i outbox. Handler już to robi. Resolver jest adapterem HTTP, tak jak endpoint REST.

## Zasady

- Resolver odczytu nie woła `IEventStore` ani `SaveChanges`.
- Mutacja woła handler, nie `DbContext`.
- Ukryta lekcja nie zwraca tytułu.
- UI GraphQL nie jest na tunelu.
- Jeden `/graphql`, na Learning, przez bramę.
- Limit głębokości jest włączony.

## Poza zakresem

- Klient webu nadal używa OpenAPI. Nie generujemy klienta GraphQL w tym numerze.
- Asystent jest w [02-asystent.md](02-asystent.md) i nie woła GraphQL, tylko port odczytu.
- Subskrypcja GraphQL nie zastępuje SSE skrzynek.

## Źródła

- [Hot Chocolate](https://chillicream.com/docs/hotchocolate/v13). Wiążące są resolver i DataLoader na żądanie. Dotyczy katalogu. Ze źródła nie bierzemy subskrypcji jako drugiego kanału push.
- [GraphQL over HTTP](https://graphql.github.io/graphql-over-http/). Wiążące jest `POST` z zapytaniem. Dotyczy ścieżki `/graphql`. Ze źródła nie bierzemy `GET` z zapytaniem w query stringu, które ląduje w logu dostępu.
- Numer odczytu Learning. Wiążąca jest widoczność. Dotyczy resolvera. Ze źródła nie bierzemy osobnego kodu błędu tylko dla GraphQL.

## Gotowe gdy

- Test dwóch technologii robi jedno pobranie kursów.
- Pusty tytuł w mutacji zwraca `learning.title.blank`.
- Zapytanie ponad limit nie wykonuje liści.
- Poza Development nie ma UI GraphQL.
- REST `GET /courses` nadal działa.
