# 02. Skrzynki i SSE

Poprzedni numer to [dwa zapytania](01-dwa-zapytania.md). Następny to [push](03-push.md).

Ten numer daje właścicielowi dwie listy i strumień `text/event-stream`. Student nie czyta skrzynki rekruterów. Rekruter nie czyta żadnej. Zdarzenie na żywo idzie z Kafki do procesu API, nie przez bazę odczytu w pętli co sekundę.

## Po co

Samo `POST` zostawia wiadomość w tabeli, której właściciel nie widzi. Dwie listy zamykają podgląd. SSE zamyka odświeżanie ręczne: nowa wiadomość dochodzi do otwartej skrzynki. Jedna lista na oba rodzaje zapytań zmuszałaby klienta do filtra, który da się pominąć. Dlatego są dwa endpointy.

## Dlaczego ta technologia

### Dwie projekcje

Projektor `contact-projector` pisze `student_inquiry_document` i `recruiter_inquiry_document` w `contact_read`. `GET /inquiries/students` i `GET /inquiries/recruiters` czytają Dapperem. Polityka `owner`. Problem, który zamyka, to `GET` po tabeli zapisu. Odpada jedna lista z kolumną `kind` jako jedyne API. Kolumna może być w bazie, w API są dwa adresy. Koszt: dwa zapytania, które wolno trzymać w jednym pliku.

Student, który wywoła `GET` skrzynki, dostaje `403`. Nie `404`. Tu nie ukrywamy istnienia funkcji przed zalogowanym studentem. Ukrywamy przed nim treść, i `403` jest wprost. Gość dostaje `401`.

### SSE

`GET /inquiries/stream` wymaga `owner` i zwraca `text/event-stream`. Proces API ma konsumenta grupy `contact-sse`. Wiadomość z `contact.integration` idzie na kanał w pamięci i do otwartych połączeń. Inbox w tym konsumencie zapisuje `message-id`, żeby po restarcie nie wylać całej retencji na ekran, jeśli offset i tak jest w grupie. Offset grupy wystarcza. Inbox zostaje dla spójności z resztą konsumentów. Problem, który zamyka, to odpytywanie `GET` co sekundę. Odpada WebSocket jako drugi protokół na to samo. Koszt: połączenie jest długie. Proxy bramy nie buforuje tej odpowiedzi. YARP ma być skonfigurowany tak, żeby nie składał całego streamu w pamięci. Sprawdzenie jest w teście: pierwszy event wychodzi zanim strumień się skończy. W teście hosta wystarczy, że nagłówek `Content-Type` jest `text/event-stream`.

Event ma pole `kind` równe `student` albo `recruiter` i identyfikator. Nie ma treści wiadomości w SSE, jeśli lista i tak ją dociągnie. Wysyłamy identyfikator i rodzaj. Klient woła `GET` jednej skrzynki albo wstawia wiersz, jeśli payload jest mały. Ten numer wkłada do eventa identyfikator, rodzaj i skrót nadawcy, nie całą treść. Treść zostaje w `GET`. Mniejszy wyciek w logu proxy.

### Autoryzacja strumienia

Przeglądarka `EventSource` nie ustawia nagłówka `Authorization` w prostym API. Dlatego dopuszczamy `access_token` w query wyłącznie na tym endpoincie, tak jak robi się to dla SSE, i tylko gdy nagłówka nie ma. Token w query trafia do logu, jeśli logujemy URL. Log dostępu tej ścieżki nie zawiera query. Problem, który zamyka, to SSE bez możliwości zalogowania. Odpada osobny cookie tylko dla tego. Koszt: token w query jest widoczny w historii przeglądarki. Czas życia access tokenu jest 15 minut. To jest akceptowane i opisane. Inne endpointy nie przyjmują tokenu z query. Test: `GET /inquiries/students?access_token=` bez nagłówka dostaje `401`.

### Dwa połączenia

Właściciel otwiera jeden stream i dostaje oba rodzaje. Nie ma dwóch URL-i SSE. Rozróżnia `kind`. Problem, który zamyka, to dwa połączenia do utrzymania w przeglądarce. Odpada jeden stream bez `kind`, który nie mówi, którą skrzynkę odświeżyć.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| skrzynka | lista jednego rodzaju zapytań |
| SSE | odpowiedź, która zostaje otwarta i dostaje kolejne eventy |
| `kind` | `student` albo `recruiter` w evencie |
| `access_token` w query | tylko na strumieniu, nigdy na `POST` |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Contact.Api/Endpoints/InboxEndpoints.cs` | dwa `GET` i stream |
| `Contact.Api/Stream/InquiryStream.cs` | kanał i `text/event-stream` |
| `Contact.Api/Stream/SseConsumer.cs` | grupa `contact-sse` |
| `Contact.Projector/` | dokumenty odczytu |
| `tests/Contact.Api.Tests/InboxAuthTests.cs` | student dostaje `403` |

## Plan

### 1. Listy

Paginacja `take`, domyślnie 50, maksimum 100. Sortowanie od najnowszych. Puste skrzynki to `200` i pusta lista, nie `404`.

### 2. Stream

Połączenie bez tokenu to `401`. Event w formacie SSE: `event: inquiry`, `data:` JSON. Keepalive co 15 sekund jako komentarz SSE, żeby proxy nie ucięło ciszy.

### 3. Brama

Trasa `/contact` już jest. Ten numer sprawdza, że YARP nie włącza buforowania odpowiedzi dla `text/event-stream`. Jeśli wersja YARP buforuje domyślnie, wyłączamy to na tej trasie.

### Co jest do czego

| Endpoint | Kto |
| --- | --- |
| `GET /inquiries/students` | `owner` |
| `GET /inquiries/recruiters` | `owner` |
| `GET /inquiries/stream` | `owner` |
| `POST` z poprzedniego numeru | nadawca, nie właściciel |

## Testy

Nazwa: `Student_cannot_read_recruiter_inbox`.

| Arrange | Act | Assert |
| --- | --- | --- |
| token `student` | `GET /inquiries/recruiters` | `403` |

Nazwa: `Stream_content_type_is_event_stream`.

```bash
dotnet test tests/Contact.Api.Tests --nologo --filter "FullyQualifiedName~Student_cannot_read|FullyQualifiedName~Stream_content_type"
```

## Checkpoint

```bash
dotnet test tests/Contact.Api.Tests --nologo
aspire run
```

Jako właściciel, otwarte stream i `POST` rekrutera z drugiego klienta daje event z `kind` równym `recruiter`. Student na liście dostaje `403`. W logu dostępu ścieżki stream nie ma wartości `access_token`.

## Umiesz wyjaśnić

Dlaczego dwa `GET`, a nie jeden z filtrem?

Bo filtr w query łatwo pominąć i pokazać obie skrzynki jako jedną. Dwa adresy są w routingu i w polityce. Klient i tak może zawołać oba. Nie może dostać obu z adresu, który miał być jedną skrzynką, przez zapomnienie parametru.

Dlaczego treść nie idzie w SSE?

Bo stream jest długi, proxy i logi go dotykają, a lista i tak jest źródłem do rysowania. Identyfikator wystarcza, żeby wiedzieć, którą skrzynkę odświeżyć. Mniej treści w miejscu, które trudniej filtrować.

Czemu token w query tylko tu?

Bo `EventSource` w przeglądarce nie doda nagłówka. Na zwykłym `GET` nagłówek jest i query z tokenem byłoby drugim, gorszym wejściem. Dlatego tam jest `401`.

Dlaczego student dostaje `403`, a nie `404`?

Bo jest zalogowany i endpoint nie jest tajemnicą istnienia wiersza. Tajemnicą jest cudza wiadomość. `403` mówi „nie ta rola”. `404` zostawiamy dla treści ukrytej przed gościem, jak lekcja.

Co trzyma połączenie przy braku wiadomości?

Komentarz keepalive co 15 sekund. Bez tego tunel albo proxy uzna połączenie za martwe.

## Zasady

- Dwa `GET` skrzynek, oba `owner`.
- SSE nie zawiera pełnej treści wiadomości.
- `access_token` w query działa tylko na stream.
- Log tej ścieżki nie zawiera query.
- Pusta lista to `200`.
- Student nie czyta żadnej skrzynki.
- Projektor nie czyta `contact_write`.

## Poza zakresem

- Powiadomienie na telefon jest w [03-push.md](03-push.md).
- Ekrany skrzynek są w [06-intranet.md](../11-web/06-intranet.md) i w [04-powiadomienia.md](../12-mobile/04-powiadomienia.md).

## Źródła

- [Server-sent events](https://html.spec.whatwg.org/multipage/server-sent-events.html). Wiążące są `text/event-stream`, `event` i `data`. Dotyczy strumienia skrzynek. Ze źródła nie bierzemy WebSocket, dopóki klient nie potrzebuje wysyłać po tym samym połączeniu.
- [YARP transforms](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/http-transforms). Wiążące jest niebuforowanie długiej odpowiedzi. Dotyczy trasy Contact. Ze źródła nie bierzemy osobnej bramy dla SSE.
- Numer JWT. Wiążąca jest polityka `owner`. Dotyczy list. Ze źródła nie bierzemy tokenu z query na `POST`.

## Gotowe gdy

- Student dostaje `403` na skrzynce rekruterów.
- Stream ma `Content-Type` `text/event-stream`.
- Nowy `POST` rekrutera widać jako event `kind=recruiter` u właściciela.
- `GET` listy z `access_token` w query bez nagłówka daje `401`.
- Pusta skrzynka daje `200` i pustą listę.
