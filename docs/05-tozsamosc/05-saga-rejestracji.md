# 05. Saga rejestracji

Poprzedni numer to [audyt](04-audyt.md). Następny to [twardnienie](06-twardnienie-api.md).

Ten numer łączy zużycie zaproszenia z zapisem uczestnictwa w Learning. Identity nie zakłada konta na stałe, dopóki Learning nie potwierdzi. Timeout cofa konto. Maszyna stanów leży w `identity_write`. Nie ma rozproszonego commita między dwoma bazami.

## Po co

Rejestracja, która tworzy konto i osobno, „jak się uda”, wpis w Learning, zostawia studenta bez uczestnictwa albo uczestnictwo bez konta. Dwie bazy nie mają jednego commitu. Saga zamyka to jawnymi stanami i kompensacją. Timeout zamyka wiszenie, gdy Learning nie odpowie, bo leży projektor albo relay.

Uczestnictwo na tym numerze jest wierszem `Enrollment` w Learning: `sub` i data. Nie jest `StudentProgress`. Postęp dotyczy lekcji. Zapis na kursy jako całość jest osobnym faktem „ten `sub` jest studentem w Learning”.

## Dlaczego ta technologia

### Stany

`RegistrationSaga`: `Started`, `WaitingForLearning`, `Completed`, `Rejected`, `TimedOut`, `Compensated`. Problem, który zamyka, to bool `isDone`. Odpada czekanie w wątku HTTP przez 30 sekund na odpowiedź Learning. HTTP zwraca `202` i identyfikator sagi, gdy konto jest w `WaitingForLearning`. Klient odpytuje `GET /registrations/{id}`. Koszt: klient umie poczekać. Formularz frontu to obsłuży. Tu jest API.

### Kompensacja, nie blokada

Identity w `Started` tworzy konto z flagą `IsActive=false` i zużywa zaproszenie w tej samej transakcji, dopisuje outbox `identity.registration-requested.v1`. Po odpowiedzi `learning.enrollment-confirmed.v1` ustawia `IsActive=true` i stan `Completed`. Po `learning.enrollment-rejected.v1` albo po czasie ustawia konto jako skasowane logicznie albo je usuwa, jeśli nie ma jeszcze audytu, którego nie wolno skasować. Konto zostaje z `IsActive=false` i stanem `Compensated`, zaproszenie wraca do stanu niewydanego tylko jeśli da się to zrobić bez drugiego użycia w międzyczasie. Prościej i bezpieczniej: zaproszenie zostaje zużyte, kompensacja nie wydaje go drugi raz. Człowiek wydaje nowe. Problem, który zamyka, to automatyczne „odzużycie”, które wyściga się z drugim żądaniem. Odpada przywracanie kodu. Koszt: zużyty kod przy timeoutcie wymaga nowego zaproszenia. To jest akceptowane.

### Timeout

`RegistrationTimeout` w Identity co 15 sekund szuka sag w `WaitingForLearning` starszych niż 60 sekund i przechodzi do `TimedOut`, potem kompensacja. Zegar to `TimeProvider`. Problem, który zamyka, to saga wisząca w nieskończoność. Odpada timeout w przeglądarce jako jedyne miejsce. Koszt: 60 sekund, w których konto jest nieaktywne. Logowanie nieaktywnego konta pada kodem `identity.account.inactive`.

### Learning

Konsument w Learning, grupa `learning-enrollment`, dostaje prośbę, wstawia `Enrollment` idempotentnie po `sub` i publikuje potwierdzenie albo odmowę. Odmowa na tym numerze występuje, gdy `sub` jest pusty. Nie ma innej reguły biznesowej. Chodzi o maszynę stanów, nie o wymyślony powód odmowy. Drugi powód, prawdziwy: baza Learning nie przyjmuje zapisu. Wtedy konsument nie publikuje odmowy, tylko retry, a timeout po stronie Identity skompensuje. Odmowa jest na błąd danych. Awaria jest retry. Problem, który zamyka, to traktowanie padu bazy jako „student odrzucony”.

### Brak dwufazowego commitu

Nie ma prepare między Postgresem Identity a Postgresem Learning. Jest wiadomość i stan. Duplikat wiadomości łyka inbox, tak jak w projektorze.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| saga | maszyna stanów przez dwie usługi, bez wspólnego commita |
| kompensacja | skutek cofający, tu dezaktywacja konta |
| `202` | przyjęte, jeszcze nie `Completed` |
| timeout | przejście do `TimedOut`, gdy nie ma odpowiedzi |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Identity.Domain/RegistrationSaga.cs` | przejścia stanów |
| `Identity.Application/UseCases/RegisterStudent.cs` | start |
| `Identity.Infrastructure/RegistrationTimeout.cs` | pętla |
| `Identity.Contracts/RegistrationRequestedV1.cs` | prośba |
| `Learning.Contracts/EnrollmentConfirmedV1.cs` i `EnrollmentRejectedV1.cs` | wynik |
| `Learning.Application/UseCases/EnrollStudent.cs` | wiersz `Enrollment` |
| `Learning.Projector` albo osobny konsument w Learning | nasłuch `identity.registration` |
| `tests/Identity.Domain.Tests/RegistrationSagaTests.cs` | przejścia bez brokera |

`identity.registration` niesie tylko prośbę. Potwierdzenie i odmowa nie są faktem katalogu i nie wracają na temat Identity, bo wtedy Learning publikowałby na cudzym topicu. Osobny temat `learning.enrollment`, 3 partycje, bez kompaktowania, należy do Learning. Identity go konsumuje. Skrypt `create-topics.sh` dostaje ten temat w tym numerze. Klucz wiadomości to identyfikator sagi.

## Plan

### 1. Start

`POST /register` przyjmuje e-mail, hasło i kod zaproszenia. Nie wymaga tokenu. Waliduje zaproszenie, tworzy nieaktywne konto, sagę `WaitingForLearning`, outbox. Zwraca `202` i `registrationId`. Zły kod to `422` z `identity.invite.expired` albo pokrewnym, bez tworzenia konta.

### 2. Learning

Konsument wstawia `Enrollment` z unikalnością `sub`. Publikuje `learning.enrollment-confirmed.v1`. Przy pustym `sub` publikuje odrzucenie i nie wstawia wiersza.

### 3. Dokończenie

Konsument w Identity, grupa `identity-registration`, zamyka sagę. Aktywuje konto albo kompensuje. Inbox po `message-id`.

### 4. Odpytanie

`GET /registrations/{id}` jest anonimowy tylko z znajomością identyfikatora. Identyfikator jest `Guid` v7, nie zgadywalny w praktyce tego produktu. Odpowiedź nie zawiera hasła. Zawiera stan.

### Co jest do czego

| Stan | Konto | Zaproszenie |
| --- | --- | --- |
| `WaitingForLearning` | `IsActive=false` | zużyte |
| `Completed` | `IsActive=true` | zużyte |
| `TimedOut` i `Compensated` | `IsActive=false` | zużyte, nie wraca |
| `Rejected` | `IsActive=false` | zużyte |

## Testy

Nazwa: `Timeout_deactivates_account_when_learning_is_silent`.

| Arrange | Act | Assert |
| --- | --- | --- |
| saga `WaitingForLearning`, `FakeTimeProvider` po 61 sekundach | jeden obieg timeoutu | stan `Compensated` albo `TimedOut` potem kompensacja w tym samym obiegu, `IsActive` false |

Nazwa przejść: `Confirmed_message_activates_account`. Bez brokera, wołanie metody sagi i handlera na magazynie w pamięci.

Test z brokerem jest jeden: pełna ścieżka `Category=Integration`, Learning potwierdza, `GET` pokazuje `Completed`.

```bash
dotnet test tests/Identity.Domain.Tests --nologo --filter "FullyQualifiedName~Timeout_deactivates"
```

## Checkpoint

```bash
aspire run
```

Rejestracja z ważnym kodem kończy się `202`, a po chwili `GET` pokazuje `Completed` i logowanie tym kontem działa. Zatrzymany Learning: po 60 sekundach stan nie jest `Completed`, logowanie nie działa. Nowy kod zaproszenia jest potrzebny na kolejną próbę. Stary kod zwraca `identity.invite.consumed`.

## Umiesz wyjaśnić

Dlaczego to nie jest transakcja rozproszona?

Bo dwa Postgresy nie zatwierdzają się jednym `COMMIT`. Saga zapisuje stan lokalnie i dowozi fakt wiadomością. Drugi raz wiadomość nie zmienia wyniku dzięki inboxowi.

Dlaczego zaproszenie nie wraca po timeoutcie?

Bo powrót kodu wyściga się z opóźnioną odpowiedzią Learning, która jeszcze może przyjść. Kod zostaje zużyty. Nowy kod jest tani. Podwójne konto na ten sam kod jest drogie.

Czym odmowa różni się od awarii?

Odmowa to błąd danych, publikujemy `rejected` i kończymy. Awaria bazy Learning to retry wiadomości. Timeout Identity jest siatką bezpieczeństwa, gdy retry nie wystarczy. Nie publikujemy `rejected` tylko dlatego, że baza nie odpowiedziała.

Dlaczego `202`, a nie długi `200`?

Bo odpowiedź Learning może przyjść po czasie, w którym nie chcemy trzymać połączenia HTTP. Klient ma identyfikator i odpytuje.

Czemu Learning nie pisze do bazy Identity?

Bo to byłaby cudza baza. Pisze na swój temat. Identity sama zmienia swoje konto.

## Zasady

- Hasło nie wchodzi do wiadomości na szynie.
- Konto w `WaitingForLearning` nie przechodzi logowania.
- Timeout używa `TimeProvider`.
- Zaproszenie nie jest „odzużywane”.
- Identity nie łączy się z `learning_write`.
- Learning nie łączy się z `identity_write`.

## Poza zakresem

- Formularz rejestracji w przeglądarce jest w [04-logowanie-pkce.md](../11-web/04-logowanie-pkce.md).
- Twardnienie tego endpointu, limit prób kodu, jest w [06-twardnienie-api.md](06-twardnienie-api.md).
- Drugi pod konsumenta jest w skalowaniu. Inbox już jest.

## Źródła

- [Saga](https://microservices.io/patterns/data/saga.html). Wiążące są lokalne transakcje i kompensacja zamiast 2PC. Dotyczy rejestracji. Ze źródła nie bierzemy orkiestratora jako osobnej usługi: orkiestracja jest w Identity, bo to ona ma konto.
- [Process manager](https://www.enterpriseintegrationpatterns.com/patterns/messaging/ProcessManager.html). Wiążący jest stan między wiadomościami. Dotyczy tabeli `registration_sagas`. Ze źródła nie bierzemy trzymania stanu tylko w pamięci procesu.
- Numer outboxa i inboxu. Wiążący jest ten sam wzorzec. Dotyczy prośby i potwierdzenia. Ze źródła nie bierzemy wspólnego commita dwóch baz.

## Gotowe gdy

- Test czasu dezaktywuje konto bez brokera.
- Pełna ścieżka z Learning kończy się `Completed` i działającym logowaniem.
- Przy leżącym Learning po 60 sekundach logowanie nowym kontem nie działa.
- Wiadomość rejestracji nie zawiera hasła.
- Nie ma połączenia Identity do bazy Learning.
