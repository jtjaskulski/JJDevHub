# 03. Push

Poprzedni numer to [skrzynki](02-skrzynki-sse.md). Następny folder zaczyna się od [GraphQL](../09-rozszerzenia-api/01-graphql.md).

Ten numer wysyła powiadomienie na telefon właściciela. Dwa kanały: studenci i rekruterzy. Token urządzenia rejestruje się przy zalogowaniu `owner` i kasuje przy wylogowaniu. Drugie dostarczenie tego samego `message-id` nie wysyła drugiego pusha. Nie ma osobnej usługi powiadomień.

## Po co

SSE działa, gdy skrzynka jest otwarta. Telefon w kieszeni jej nie trzyma. Push zamyka tę lukę. Dwa kanały zamykają pytanie, którą skrzynkę otworzyć po stuknięciu. Osobna usługa „notification” miałaby trzecią bazę i ten sam fakt. Adapter w Contact wystarcza.

## Dlaczego ta technologia

### FCM HTTP v1

Wysyłka jest żądaniem HTTP do FCM z tokenem urządzenia. Klucz konta usługi leży w env maszyny, nie w git, do czasu OpenBao. W Development bez klucza adapter loguje „push skipped” i nie rzuca, jeśli konfiguracja mówi `Push:Enabled=false`. Na maszynie brak klucza przy `Enabled=true` jest błędem startu albo błędem wysyłki, która idzie w retry. Nie wysyłamy „na pusto” udając sukces. Problem, który zamyka, to sekret w `appsettings.json`. Odpada biblioteka, która wymaga klucza w pliku projektu. Koszt: trzeba znać HTTP v1, nie starsze API z kluczem serwera w query.

APNs nie ma osobnej ścieżki w tym numerze. FCM przyjmuje rejestrację także dla iOS przez swoją warstwę, jeśli telefon tak zarejestruje token. Osobny klucz APNs nie wchodzi, dopóki klient Expo nie pokaże, że FCM nie wystarcza. Numer telefonu używa Expo i FCM.

### Dwa kanały

Żądanie do FCM niesie `channel_id` albo odpowiednik w danych: `students` albo `recruiters`. Aplikacja na telefonie mapuje to na kanał powiadomień. Problem, który zamyka, to jedno powiadomienie „masz wiadomość” bez skrzynki. Odpada jeden kanał. Koszt: klient musi założyć dwa kanały. Założy je w numerze telefonu.

### Inbox

Konsument `contact-push` zapisuje inbox przed wysyłką albo w tej samej transakcji co zamiar wysyłki. Jeśli wyślemy przed inboxem, pad między FCM a zapisem wyśle drugi raz. Jeśli zapiszemy inbox przed FCM i FCM padnie, nie wyślemy wcale. Lepszy układ: wiersz `push_outbox` w `contact_write` w tej samej transakcji co inbox „zamierzam”, a wysyłka ustawia `sent_at`. To jest drugi outbox. Mamy już outbox na Kafkę. Push jest skutkiem konsumpcji, nie drugim faktem. Prosty układ: konsumujemy, w transakcji `contact_read` albo w tabeli w `contact_write` robimy `TryRegister` inboxu. Jeśli `false`, kończymy. Jeśli `true`, wołamy FCM. Gdy FCM padnie, kompensujemy usunięcie inboxu albo, lepiej, nie commitujemy inboxu przed sukcesem i trzymamy blokadę krótko. FCM bywa wolne. Dlatego: tabela `push_delivery (message_id primary key, sent_at null)`. Wstawienie wiersza z pustym `sent_at` jest „wzięte”. Wyścig dwóch podów wstawia jeden. Zwycięzca wysyła i ustawia `sent_at`. Porażka zostawia `sent_at` puste i pętla ponawia. Drugi pod widzi wiersz i nie wysyła, dopóki `sent_at` jest puste i `attempts` rośnie u tego, który wygrał `UPDATE ... WHERE sent_at IS NULL`. Dość. Uproszczenie w specyfikacji: jeden konsument w grupie na tym etapie, inbox po sukcesie FCM, a przy wyjątku offset nie schodzi, więc będzie retry. Drugi raz po sukcesie i padzie przed offsetem: FCM może dostać drugi push. Żeby to zamknąć, `message_id` jest kluczem idempotencji także po stronie danych FCM, jeśli API to ma, albo po naszej tabeli `push_delivery` wstawionej przed wywołaniem z unikalnością `message_id`. Wstawienie przed wywołaniem: jeśli proces padnie po insert i przed FCM, retry widzi wiersz i wysyła, jeśli `sent_at` puste. Jeśli padnie po FCM i przed `sent_at`, retry wyśle drugi raz. Okno zostaje wąskie. Zapisujemy je w zasadach jako akceptowane tylko gdy `sent_at` jest w tej samej świadomości: po odpowiedzi 200 z FCM ustawiamy `sent_at` natychmiast. Drugi raz jest możliwy wyłącznie w tym oknie. Inbox Kafki na offset nie wystarcza. Tabela `push_delivery` jest obowiązkowa.

### Rejestracja tokenu

`PUT /devices` z polityką `owner` zapisuje token. `DELETE /devices` kasuje token tego urządzenia. Wylogowanie klienta woła `DELETE`. Bez tokenu konsument zapisuje dostawę jako `skipped` i nie woła FCM. Nie ponawia w nieskończoność braku tokenu. `sent_at` zostaje puste, a `skipped_at` ustawione, żeby retry nie wysyłał wstecz, gdy właściciel dopiero później włączy powiadomienia. Nowa wiadomość, po rejestracji, już pójdzie.

### Treść pusha

Tytuł kanału i pierwsza linia, bez pełnej treści wiadomości rekrutera, jeśli jest długa. Skrót do 80 znaków. Problem, który zamyka, to pełna treść na ekranie blokady. Odpada pusta treść, z której nie wiadomo, o co chodzi. Koszt: skrót i tak jest treścią. Właściciel tego chce. Nie wkładamy e-maila rekrutera w systemowy podgląd, jeśli da się ograniczyć do imienia i skrótu.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| token urządzenia | adres pusha, nie hasło konta |
| kanał | `students` albo `recruiters` |
| `push_delivery` | wiersz idempotencji wysyłki |
| FCM HTTP v1 | aktualne API wysyłki, nie starszy klucz serwera |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Contact.Api/Endpoints/DeviceEndpoints.cs` | `PUT` i `DELETE` |
| `Contact.Infrastructure/Push/FcmSender.cs` | HTTP v1 |
| `Contact.Projector/PushConsumer.cs` albo `Contact.Push/Program.cs` | osobny proces `contact-push`, żeby SSE i push nie były jednym wątkiem |
| `tests/Contact.Push.Tests/DuplicateTests.cs` | drugi raz nie woła FCM |

Osobny projekt `Contact.Push` jest czytelniejszy niż trzecia pętla w API. Obraz jest w tym samym tagu `contact-vX.Y.Z`.

## Plan

### 1. Urządzenie

Tabela `device_tokens`: `owner_sub`, `token`, `platform`. Jeden właściciel, wiele tokenów, jeśli ma dwa telefony. Wysyłka idzie na wszystkie aktywne. `DELETE` kasuje jeden token.

### 2. Konsument

Czyta `contact.integration`. Rozróżnia `type`. Wstawia `push_delivery`. Woła FCM z kanałem. Ustawia `sent_at`. Przy braku tokenu ustawia `skipped_at`.

### 3. Sekret

`FCM_SERVICE_ACCOUNT_JSON` jest ścieżką do pliku poza gitem albo treścią w env maszyny. W repozytorium jest pusta zmienna w przykładzie. Log nie drukuje JSON konta usługi.

### Co jest do czego

| Fakt | Kanał |
| --- | --- |
| `contact.student-inquiry-received.v1` | `students` |
| `contact.recruiter-inquiry-received.v1` | `recruiters` |

## Testy

Nazwa: `Second_delivery_does_not_call_fcm`.

| Arrange | Act | Assert |
| --- | --- | --- |
| `push_delivery` z `sent_at` ustawionym | drugi obieg tego `message-id` | licznik klienta FCM bez zmian |

Nazwa: `Missing_token_sets_skipped_and_does_not_throw`.

```bash
dotnet test tests/Contact.Push.Tests --nologo
```

## Checkpoint

```bash
dotnet test tests/Contact.Push.Tests --nologo
```

Z `Push:Enabled=false` w Development `POST` rekrutera nie wywala API, a w logu jest `push skipped`. Na maszynie, gdy klucz jest, test ręczny na telefonie jest w numerze klienta, nie tu. Tu wystarcza, że klient HTTP FCM jest podstawiony w teście i dostaje kanał `recruiters`.

## Umiesz wyjaśnić

Dlaczego nie ma usługi Notification?

Bo jedynym nadawcą jest Contact i jedynym odbiorcą jest telefon właściciela. Osobna usługa dodałaby bazę i nie usunęłaby żadnej zależności. Adapter w procesie Contact wystarcza.

Dlaczego dwa kanały?

Bo stuknięcie ma otworzyć właściwą skrzynkę. Jeden kanał zmusza aplikację do zgadywania albo do trzeciego ekranu „wszystko”.

Czemu okno podwójnego pusha jeszcze jest?

Bo FCM i baza nie mają wspólnego commita. Tabela `push_delivery` zamyka powtórkę po zapisanym `sent_at`. Zostaje krótki czas między odpowiedzią FCM a `sent_at`. To jest ten sam rodzaj okna co przy outboxie, tylko w drugą stronę. Nie obiecujemy dokładnie raz. Obiecujemy, że retry po zapisanym sukcesie nie wyśle drugi raz.

Dlaczego brak tokenu nie jest retry w nieskończoność?

Bo bez urządzenia nie ma adresata. Wiadomość zostaje w skrzynce. Push nie jest jedyną drogą. Gdy telefon się zarejestruje, dostanie kolejne wiadomości, nie zaległą burzę.

Gdzie jest klucz FCM?

Poza gitem, w env maszyny. W logu go nie ma. OpenBao przejmie go w fazie klastra. Do tego czasu plik o uprawnieniu `600`.

## Zasady

- Dwa kanały, dwa `type` z poprzedniego numeru.
- `push_delivery` ma unikalny `message-id`.
- Po `sent_at` nie wołamy FCM drugi raz.
- Brak tokenu ustawia `skipped_at`, nie pętlę.
- Sekret FCM nie wchodzi do git.
- `DELETE /devices` przy wylogowaniu jest częścią kontraktu, klient go zawoła.
- Treść pusha jest skrótem, bez hasła i bez tokenu sesji.

## Poza zakresem

- Ekran telefonu i rejestracja w Expo są w [04-powiadomienia.md](../12-mobile/04-powiadomienia.md).
- OpenBao dla klucza FCM jest w [05-openbao.md](../13-kubernetes/05-openbao.md).
- Drugi pod konsumenta jest w [07-skalowanie.md](../13-kubernetes/07-skalowanie.md). Tabela dostawy jest już na to gotowa.

## Źródła

- [FCM HTTP v1](https://firebase.google.com/docs/cloud-messaging/migrate-v1). Wiążące jest konto usługi i brak starego klucza serwera w query. Dotyczy `FcmSender`. Ze źródła nie bierzemy SDK, które wymaga pliku w repozytorium.
- Numer inboxu. Wiążące jest to, że druga dostawa nie ma drugiego skutku. Dotyczy `push_delivery`. Ze źródła nie bierzemy „dokładnie raz” jako obietnicy.
- Numer skrzynek. Wiążące są dwa rodzaje zapytań. Dotyczy kanałów. Ze źródła nie bierzemy pełnej treści w evencie pusha, jeśli skrót wystarcza do otwarcia skrzynki.

## Gotowe gdy

- Test drugiego obiegu nie woła FCM.
- Brak tokenu nie rzuca i ustawia `skipped_at`.
- Wiadomość studenta i rekrutera dostają różne kanały w teście klienta podstawionego.
- W git nie ma JSON konta usługi.
- `PUT` i `DELETE /devices` wymagają `owner`.
