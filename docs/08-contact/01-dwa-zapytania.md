# 01. Dwa zapytania

Poprzedni numer to [strona CV](../07-cv/03-strona-publiczna.md). Następny to [skrzynki](02-skrzynki-sse.md).

Ten numer przyjmuje dwa rodzaje wiadomości: `StudentInquiry` od zalogowanego studenta i `RecruiterInquiry` z formularza bez konta. Nie ma klasy `Contact`. Zapis jest wierszem EF w `contact_write`, nie strumieniem. Jeszcze nie ma pusha i nie ma strumienia na żywo.

## Po co

Jeden formularz „kontakt” miesza studenta, który już ma konto, z rekruterem, który konta nie ma. Dwa agregaty mają inne uprawnienie i inny los na telefonie właściciela. Bez tego podziału push musiałby zgadywać, którą skrzynkę otworzyć.

Historia brzmienia wiadomości nie jest produktem. Wiadomość raz przyjęta zostaje. Dlatego nie ma strumienia. Jest wiersz i fakt na szynę, gdy dojdzie numer pusha. Ten numer zapisuje wiersz i outbox, żeby tamten nie dokładał modelu.

## Dlaczego ta technologia

### Dwa typy

`StudentInquiry` wymaga `ICurrentActor` ze rolą `student`. `sub` jest w wierszu. Ciało nie niesie identyfikatora studenta. `RecruiterInquiry` nie ma `sub`. Ma imię, e-mail i treść. Problem, który zamyka, to pole `role` w formularzu publicznym, które każdy ustawi na `student`. Odpada jeden endpoint z przełącznikiem. Koszt: dwa endpointy.

### Walidacja

Pusty tekst to `contact.message.blank`. E-mail rekrutera jest wymagany i sprawdzany jako kształt, nie jako istnienie skrzynki. Zły kształt to `422` i `contact.email.invalid`. Student nie podaje e-maila w formularzu: e-mail jest na koncie, a do wiersza kopiujemy go w chwili zapisu, żeby właściciel widział adres nawet, gdy student go później zmieni. To jest kopia, nie FK do Identity. Problem, który zamyka, to złączenie do `identity_write`. Odpada wołanie Identity po HTTP przy każdym `GET` skrzynki.

### Outbox od razu

W tej samej transakcji co wiersz idzie `contact.student-inquiry-received.v1` albo `contact.recruiter-inquiry-received.v1` na `contact.integration`. Klucz to identyfikator zapytania. Problem, który zamyka, to dopisywanie szyny później, gdy wiersze już są i nie mają faktu. Odpada wysyłka pusha w endpoincie. Push dojdzie w osobnym numerze i przeczyta ten fakt. Na tym numerze relay Contact publikuje, a konsument może jeszcze nie wysyłać pusha. Jeśli nikt nie konsumuje, wiadomości leżą na topicu. To jest w porządku. Relay ma być, żeby wiersz nie został z `published_at` pustym w nieskończoność bez procesu. Relay jest w tym numerze. Konsument pusha nie jest.

### Idempotencja

`POST` rekrutera wymaga `Idempotency-Key`, bo nie ma tokenu, który ogranicza powtórkę do osoby, a podwójne kliknięcie i tak jest. `POST` studenta też wymaga klucza. Drugi raz zwraca tę samą odpowiedź.

### `404` i istnienie

Endpoint studenta bez roli `student` to `403` z `inquiry.student.forbidden`, bo użytkownik jest zalogowany i ma wiedzieć, że to nie jego ścieżka. To nie jest ukryta treść. To zły formularz. Rekruter nie ma tokenu. Gość na endpoincie studenta dostaje `401`.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| `StudentInquiry` | wiadomość od `sub` roli `student` |
| `RecruiterInquiry` | wiadomość bez konta |
| kopia e-maila | adres w chwili zapisu, nie login do Identity |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Contact.Domain/StudentInquiry.cs` | reguła roli i treści |
| `Contact.Domain/RecruiterInquiry.cs` | imię, e-mail, treść |
| `Contact.Application/UseCases/Commands/ReceiveStudentInquiryCommand.cs` | wejście studenta |
| `Contact.Application/UseCases/Handlers/ReceiveStudentInquiryHandler.cs` | handler studenta |
| `Contact.Application/UseCases/Commands/ReceiveRecruiterInquiryCommand.cs` | wejście rekrutera |
| `Contact.Application/UseCases/Handlers/ReceiveRecruiterInquiryHandler.cs` | handler rekrutera |
| `Contact.Contracts/` | dwa rekordy faktów |
| `Contact.Api/Endpoints/InquiryEndpoints.cs` | dwa `POST` |
| `Contact.Relay/` | publikacja outboxa |
| `tests/Contact.Domain.Tests/` | pusta treść i rola |

Nie ma pliku `Contact.cs`.

## Plan

### 1. Student

`POST /inquiries/student` z polityką `student`. Treść i opcjonalny temat. Handler kopiuje e-mail z claimu `email`, nie z ciała. Brak claimu e-mail to `contact.email.missing` i `422`, nie zgadywanie.

### 2. Rekruter

`POST /inquiries/recruiter` anonimowy, z limitem bramy. Ten numer dopisuje ścieżkę do polityki limitu z numeru twardnienia, obok rejestracji. Inaczej formularz publiczny jest bez hamulca. To jest dopisek w konfiguracji bramy, w tym samym scaleniu.

### 3. Zapis

EF, tabele `student_inquiries` i `recruiter_inquiries`. Nie jedna tabela z dyskryminatorem. Dwa agregaty, dwie tabele. TPH odpada z tego samego powodu co przy kursie i lekcji: inne pola, inne uprawnienie, inna tabela jest czytelniejsza niż dyskryminator. Próba TPH była w Learning. Tu jej nie powtarzamy.

### Co jest do czego

| Endpoint | Token | Fakt |
| --- | --- | --- |
| `POST /inquiries/student` | `student` | `contact.student-inquiry-received.v1` |
| `POST /inquiries/recruiter` | brak | `contact.recruiter-inquiry-received.v1` |

## Testy

Nazwa: `Student_handler_rejects_non_student_actor`.

| Arrange | Act | Assert |
| --- | --- | --- |
| aktor `owner` | `ReceiveStudentInquiry` | `inquiry.student.forbidden`, brak wiersza |

Nazwa: `Blank_message_returns_invalid`.

```bash
dotnet test tests/Contact.Domain.Tests tests/Contact.Application.Tests --nologo
```

## Checkpoint

```bash
dotnet test tests/Contact.Domain.Tests --nologo
test ! -f src/services/Contact/Contact.Domain/Contact.cs
aspire run
```

Jako student, `POST` przez bramę zwraca `201`. Jako właściciel, ten sam URL zwraca `403`. Formularz rekrutera bez tokenu zwraca `201`. W outboxie są dwa różne `type`. W bazie Identity nie przybyło połączenia z procesu Contact. Connection string Contact wskazuje tylko `contact_write` i `contact_read`.

## Umiesz wyjaśnić

Dlaczego to nie jest strumień?

Bo nie pokazujemy historii edycji wiadomości. Wiadomość jest przyjęta albo nie. Strumień został przy notatce i przy sekcjach CV, gdzie brzmienie się zmienia i jest treścią.

Dlaczego dwie tabele?

Bo to dwa agregaty z różnymi polami. Jedna tabela z dyskryminatorem zostawia puste kolumny i ułatwia zapytanie, które miesza skrzynki. Skrzynki mają być osobne.

Dlaczego e-mail studenta jest kopiowany?

Bo właściciel czyta skrzynkę później, a konto studenta może mieć inny adres. Kopia jest stanem wiadomości. Nie jest powodem, żeby Contact logował się do Identity.

Dlaczego rekruter nie może oznaczyć się jako student?

Bo nie ma tokenu `student`. Samo pole w JSON nie nadaje roli. Rola jest w tokenie, który wystawia Identity.

Po co outbox, skoro push jest później?

Bo fakt ma powstać w tej samej transakcji co wiersz. Dopisanie go później wymaga odgadywania, które wiersze już zostały wysłane. Taniej zrobić to od pierwszej wiadomości.

## Zasady

- Brak klasy `Contact`.
- `StudentInquiry` nie bierze `sub` z ciała.
- Rekruter nie przechodzi przez endpoint studenta.
- Brak FK i braku connection stringa do Identity.
- Dwa fakty, dwa `type`.
- Limit na `POST` rekrutera jest włączony na bramie.
- Hasło i token nie wchodzą do wiersza ani do outboxa.

## Poza zakresem

- Lista dla właściciela i SSE są w [02-skrzynki-sse.md](02-skrzynki-sse.md).
- Push jest w [03-push.md](03-push.md).
- Formularze na stronie są w [05-tresc-publiczna.md](../11-web/05-tresc-publiczna.md).

## Źródła

- Numer JWT. Wiążąca jest rola z tokenu, nie z ciała. Dotyczy studenta. Ze źródła nie bierzemy `owner` jako nadawcy zapytania studenckiego.
- Numer outboxa. Wiążący jest jeden commit z wierszem. Dotyczy faktu kontaktu. Ze źródła nie bierzemy wysyłki pusha w handlerze HTTP.
- Numer twardnienia. Wiążący jest limit na anonimowym `POST`. Dotyczy rekrutera. Ze źródła nie bierzemy wyłączenia limitu dla tej ścieżki.

## Gotowe gdy

- Właściciel dostaje `403` na ścieżce studenta, student dostaje `201`.
- Pusta treść zwraca `contact.message.blank`.
- W domenie nie ma klasy `Contact`.
- Outbox ma właściwy `type` dla każdego z dwóch `POST`.
- Contact nie ma connection stringa Identity.
