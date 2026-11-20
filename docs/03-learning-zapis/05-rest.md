# 05. REST zapisu

Poprzedni numer to [EF Core](04-ef-core.md). Następny to [kurs i lekcja](06-kurs-i-lekcja.md).

Ten numer wystawia zapis Learning jako Minimal API. Dokłada `ProblemDetails`, `ETag`, `Idempotency-Key` i prefiks ścieżki, który brama ścina. Jeszcze nie ma listy publicznej Dapperem. Jest `GET` po slugu czytany z bazy zapisu tylko na czas tego numeru, oznaczony w kodzie jako tymczasowy, i usunięty w numerze odczytu. Bez tego `GET` nie da się pokazać `ETag`. Numer odczytu przenosi `GET` na `learning_read` i ten tymczasowy odczyt znika.

## Po co

Handler bez HTTP nie jest produktem. Klient potrzebuje statusu, nie stosu wyjątku. `ProblemDetails` zamyka kształt błędu. `ETag` zamyka dwa okna edycji, które numer EF już umie wykryć, ale jeszcze nie umie nazwać nagłówkiem. `Idempotency-Key` zamyka podwójne kliknięcie publikacji: drugi raz wraca ta sama odpowiedź, nie drugi kurs.

## Dlaczego ta technologia

### Minimal API

Endpoint jest funkcją w klasie `CourseEndpoints`, wołaną z `MapCourseEndpoints`. Problem, który zamyka, to kontroler, który wciąga regułę do akcji. Odpada MVC jako drugi styl obok. Koszt: filtry pisze się jako `IEndpointFilter`. Filtr idempotencji jest jeden, na grupie zapisów.

### `ProblemDetails` i kody

Wyjątek nie wychodzi do klienta. Middleware zamienia nieobsłużony wyjątek na `500` z kodem `learning.unhandled` i bez stosu. `Result` z rodzajem `Invalid` daje `422`, `NotFound` daje `404`, `Conflict` daje `409`, `Forbidden` daje `403`. Walidacja kształtu JSON, zanim powstanie komenda, daje `400`. Problem, który zamyka, to jeden status na wszystko. Odpada zwracanie `200` z polem `error`. Koszt: klient rozgałęzia po `type`, nie po zdaniu.

Pole `type` jest kodem `learning.title.blank`. Pole `title` jest zdaniem z zasobu `pl` albo `en` według `Accept-Language`. Zasoby leżą przy hoście. Jądro nie ma zdań.

### `ETag` i `If-Match`

`GET` tymczasowy zwraca `ETag: "v{version}"`. `PUT` bez `If-Match` daje `428`. `If-Match` niezgodny z wersją daje `412`, a gdy wersja zjadła zapis między odczytem a `WHERE`, handler i tak zwraca `409` z `learning.version.conflict`. `428` i `412` są odmową brzegu, zanim handler zapisze. `409` jest odmową wyścigu. Problem, który zamyka, to edycja bez numeru wersji. Odpada `PUT`, który nadpisuje zawsze. Koszt: klient trzyma `ETag`.

### Idempotencja

Filtr czyta `Idempotency-Key`. Klucz z odpowiedzią leży w tabeli `idempotency_keys` w `learning_write`, z zakresem `publish-course`. Drugie żądanie z tym samym kluczem i tym samym ciałem zwraca zapisaną odpowiedź i nagłówek `Idempotent-Replayed: true`. Ten sam klucz i inne ciało daje `409` z kodem `learning.idempotency.mismatch`. Problem, który zamyka, to dwa kursy po podwójnym kliknięciu. Odpada polegance na samym unikalnym slugu: slug łapie duplikat, ale nie oddaje pierwszej odpowiedzi bajt w bajt. Koszt: tabela i przeterminowanie. Wiersze starsze niż 24 godziny usuwa ten sam filtr przy zapisie, nie osobny system.

### Wersja ścieżki

Ścieżki usługi to `/technologies`, `/courses`, `/lessons`. Brama ma prefiks `/learning` i transformację `PathRemovePrefix`. Wersja łamiąca API, gdy przyjdzie, dostanie `/learning/v2` jako drugą trasę, nie podmianę tej. Na tym numerze jest tylko jeden kształt. Problem, który zamyka, to wersja w nagłówku, której nie widać w logu tunelu. Odpada brak wersji w ogóle: prefiks usługi jest wersją „pierwszą”, a `v2` dojdzie, gdy kontrakt się złamie. Nie dodajemy `/v1` na zapas obok braku `/v1`.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| `ProblemDetails` | ciało błędu z `type`, `title`, `status` |
| `ETag` | cytat wersji wiersza |
| `If-Match` | warunek „zapiszę tylko, jeśli nadal widzę tę wersję” |
| `Idempotency-Key` | klucz, pod którym leży pierwsza odpowiedź |
| `428` | brak warunku wstępnego |
| `412` | warunek nie jest już prawdziwy |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Learning.Api/Program.cs` | skład, OpenAPI, Scalar w Development |
| `Learning.Api/Endpoints/TechnologyEndpoints.cs` | publikacja technologii |
| `Learning.Api/Endpoints/CourseEndpoints.cs` | publikacja i rewizja |
| `Learning.Api/Endpoints/LessonEndpoints.cs` | publikacja; treść lekcji rozszerzy następny numer |
| `Learning.Api/Endpoints/IdempotencyFilter.cs` | klucz i powtórka |
| `Learning.Api/Endpoints/ProblemDetailsMapper.cs` | `Result` na status |
| `Learning.Api/Resources/Errors.pl.resx` i `Errors.en.resx` | zdania `title` |
| `Learning.Infrastructure` | tabela `idempotency_keys`, migracja |
| `src/gateway` | `PathRemovePrefix` dla `/learning` |

OpenAPI jest w Development pod `/openapi/v1.json`. Scalar jest pod `/scalar`. Brama nie publikuje Scalar na hostname. W produkcji host ich nie mapuje.

## Plan

### 1. Publikacja kursu

`POST /courses` wymaga `Idempotency-Key`. Ciało ma slug, slug technologii, tytuły, widoczność. Sukces to `201` i `Location: /courses/{slug}`. Odmowa domeny to `422` z `application/problem+json`.

### 2. Odczyt tymczasowy i ETag

`GET /courses/{slug}` czyta bazę zapisu przez port `ICourseRepository`, nie przez Dappera. W komentarzu przy endpoincie jest jedno zdanie: do usunięcia w numerze odczytu. Odpowiedź ma `ETag`. `PUT /courses/{slug}` wymaga `If-Match`. Komenda rewizji dostaje wersję wyciętą z nagłówka. Brak nagłówka nie woła handlera.

To jest jedyny `GET` zapisu. Lista kursów nie powstaje, żeby nie utrwalać odczytu z tabeli zapisu.

### 3. Język

`Accept-Language: en` daje angielskie `title` przy tym samym `type`. Brak nagłówka daje `pl`. Nieznany język daje `pl`.

### 4. Korelacja

Odpowiedź niesie `X-Correlation-Id` z ServiceDefaults. `ProblemDetails` dostaje rozszerzenie `correlationId`. Nie dostaje connection stringa.

### Co jest do czego

| Element | Kto go woła |
| --- | --- |
| `ProblemDetailsMapper` | endpoint po `Result` i middleware po wyjątku |
| `IdempotencyFilter` | grupa `POST` |
| `ETag` | klient edycji, później intranet |
| Scalar | człowiek na laptopie, tylko Development |

## Testy

Nazwa: `Put_without_if_match_returns_428`.

| Arrange | Act | Assert |
| --- | --- | --- |
| istniejący kurs | `PUT` bez `If-Match` | status `428`, w bazie ta sama wersja |

Drugi fakt, integracyjny: `Post_same_idempotency_key_replays_response` oczekuje `201` dwa razy z tym samym `Location` i drugim nagłówkiem `Idempotent-Replayed`.

Test `422` na pusty tytuł zostaje jednostkowy na mapperze albo integracyjny na hoście. Wystarczy jeden: host z fabryką i magazynem w pamięci, bez Postgresa, dla `428`. Idempotencja jest integracyjna, bo dotyczy tabeli.

```bash
dotnet test tests/Learning.Api.Tests --nologo --filter "FullyQualifiedName~Put_without_if_match"
```

## Checkpoint

```bash
dotnet test tests/Learning.Api.Tests --nologo
aspire run
```

`POST /learning/courses` przez bramę, z kluczem, tworzy kurs. `GET` zwraca `ETag`. Drugi `POST` z tym samym kluczem nie tworzy drugiego wiersza. Scalar w Development otwiera się na porcie usługi, nie przez tunel.

## Umiesz wyjaśnić

Dlaczego `422`, a nie `400`, dla pustego tytułu?

`400` znaczy, że kształt żądania jest zły: nie ma pola, zły JSON. `422` znaczy, że kształt jest dobry, a reguła domeny odmawia. Klient inaczej poprawia formularz, inaczej poprawia serializację.

Dlaczego `type` nie jest zdaniem?

Bo zdanie zmienia się z językiem. Klient, który porówna polski komunikat, zepsuje się po tłumaczeniu. Kod zostaje.

Czym `428` różni się od `412`?

`428` to „nie powiedziałeś, którą wersję widziałeś”. `412` to „powiedziałeś, ale to już nie jest ta wersja”. `409` to wyścig, który zdarzył się mimo zgodnego nagłówka, gdy ktoś zdążył między sprawdzeniem a zapisem. Wszystkie trzy zostają.

Dlaczego idempotencja nie zastępuje unikalnego sluga?

Slug pilnuje adresu. Klucz idempotencji pilnuje powtórki tego samego żądania, także gdy drugi raz ma inny slug przez błąd klienta: wtedy jest `409` niedopasowania, a nie drugi kurs.

Dlaczego tymczasowy `GET` z zapisu ma zniknąć?

Bo odczyt listy i strony ma iść do `learning_read`. Zostawienie `GET` na zapisie omija projekcję i czyta model, którego klient nie powinien znać. Zostaje tylko na czas braku projekcji.

## Zasady

- Stos wyjątku nie wchodzi do odpowiedzi.
- `PUT` i `PATCH` wersjonowane bez `If-Match` nie wołają handlera.
- `POST` tworzący wymaga `Idempotency-Key`.
- Scalar i OpenAPI nie są mapowane poza Development.
- Zdania są w resx, nie w jądrze.
- Tymczasowy `GET` zapisu jest oznaczony i ma numer, który go usuwa.

## Poza zakresem

- Pełna lekcja, widoczność i postęp są w [06-kurs-i-lekcja.md](06-kurs-i-lekcja.md).
- Odczyt docelowy jest w [06-odczyt-dapper.md](../04-zdarzenia/06-odczyt-dapper.md).
- JWT `owner` jest w [03-jwt.md](../05-tozsamosc/03-jwt-i-policies.md). Do tego czasu endpointy zapisu na maszynie nie są wystawiane publicznie jako edycja: tunel prowadzi do bramy, a brama do Learning. Ten numer nie dodaje roli. Jeśli tag Learning wjeżdża na maszynę przed Identity, zapis jest wyłączony konfiguracją `Learning:WritesEnabled=false` poza Development. Flaga jest w tym numerze. Domyślnie na maszynie zapis jest wyłączony, dopóki nie ma JWT. Odczyt tymczasowy może zostać. To zamyka dziurę „każdy, kto zna hostname, publikuje kurs”.

## Źródła

- [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457). Wiążące są `type`, `title` i `status`. Dotyczy ciała błędu. Ze źródła nie bierzemy dowolnego URI w `type`. U nas `type` jest kodem.
- [RFC 9110, If-Match](https://www.rfc-editor.org/rfc/rfc9110#name-if-match). Wiążące są `412` i warunek wstępny. Dotyczy `ETag`. Ze źródła nie bierzemy słabego `ETag` na wersję wiersza: wersja jest dokładna, walidator jest silny.
- [Idempotency-Key](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/). Wiążące jest trzymanie pierwszej odpowiedzi i odmowa przy innym ciele. Dotyczy filtra. Ze źródła nie bierzemy klucza bez zakresu: zakres jest nazwą operacji, żeby klucz z publikacji kursu nie zderzył się z kluczem z publikacji lekcji.

## Gotowe gdy

- `PUT` bez `If-Match` zwraca `428` i nie zmienia wersji.
- Drugi `POST` z tym samym kluczem nie tworzy drugiego kursu.
- Pusty tytuł daje `422`, `type` równe `learning.title.blank`, a `title` zmienia się z `Accept-Language`.
- Poza Development Scalar nie jest zmapowany.
- Poza Development przy `Learning:WritesEnabled=false` `POST` zwraca `404`, nie tworzy wiersza. `404`, nie `403`, żeby nie zdradzać wyłącznika jako osobnego stanu dla klienta publicznego. W Development flaga jest włączona.
