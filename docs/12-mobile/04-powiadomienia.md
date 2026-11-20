# 04. Powiadomienia na telefonie

Poprzedni numer to [ekrany](03-ekrany.md). Następny folder zaczyna się od [kind](../13-kubernetes/01-kind.md).

Ten numer rejestruje token urządzenia w Contact, gdy zalogowany jest `owner`, i kasuje go przy wylogowaniu. Dwa kanały, studenci i rekruterzy, prowadzą do dwóch skrzynek. Student zalogowany na telefonie nie rejestruje tokenu push do skrzynek właściciela.

## Po co

API pusha bez tokenu z telefonu nie ma adresata. Rejestracja przy każdym starcie właściciela zamyka zmianę tokenu przez system. Kasowanie przy wylogowaniu zamyka okno, w którym następny człowiek na tym samym telefonie dostaje cudze powiadomienie. Dwa kanały zamykają jedną skrzynkę otwieraną na oślep.

## Dlaczego ta technologia

### `expo-notifications`

Pakiet jest na MIT i umie token FCM oraz kanały na Androidzie. Problem, który zamyka, to ręczny kod natywny. Odpada własny mostek. Koszt: konfiguracja FCM w Expo jest poza gitem w miejscu, które wskazuje dokumentacja Expo, a plik klucza nie wchodzi do repozytorium. W repozytorium jest nazwa projektu, nie sekret.

### Kiedy rejestrujemy

Funkcja `shouldRegisterPush(role)` zwraca true tylko dla `owner`. Po zalogowaniu właściciela aplikacja prosi o zgodę, bierze token i woła `PUT /devices`. Przy wylogowaniu woła `DELETE /devices` i dopiero czyści sesję. Jeśli `DELETE` padnie, i tak czyści sesję lokalnie i zostawia token do skasowania przy następnym wejściu tego samego konta, jeśli da się to zrobić. Nie blokujemy wylogowania na zawsze przez sieć. Problem, który zamyka, to token właściciela zostawiony po wylogowaniu jako jedyna ścieżka. Odpada rejestracja przy starcie aplikacji zanim wiadomo, kto jest zalogowany.

Student nie woła `PUT`. Test funkcji ról to gwarantuje. Nawet jeśli student jest właścicielem treści na innym koncie, to konto na tym telefonie jest studentem i nie dostaje skrzynek.

### Kanały

Na Androidzie dwa kanały o identyfikatorach `students` i `recruiters`, zgodnych z API. Stuknięcie niesie `kind` i otwiera `StudentInboxScreen` albo `RecruiterInboxScreen`. Nieznany `kind` otwiera `AboutScreen` albo dom, nie skrzynkę. Problem, który zamyka, to jedno powiadomienie bez celu. Odpada otwieranie skrzynki rekruterów dla każdego pusha.

### iOS

Expo mapuje kanały na kategorie, na ile API powiadomień pozwala. Jeśli kanał jest tylko androidowy, `kind` w danych powiadomienia i tak wybiera ekran. Nie wymagamy osobnego certyfikatu APNs w repozytorium.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| token urządzenia | adres FCM, nie token sesji |
| kanał | `students` albo `recruiters` |
| `shouldRegisterPush` | czysta funkcja roli |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/clients/mobile/src/push/shouldRegisterPush.ts` | rola |
| `src/clients/mobile/src/push/register.ts` | `PUT` i `DELETE` |
| `src/clients/mobile/src/push/openInbox.ts` | `kind` na ekran |
| `src/clients/mobile/src/push/channels.ts` | dwa kanały |

## Plan

### 1. Funkcja roli

`owner` true, `student` false, brak roli false.

### 2. Stuknięcie

`kind=student` daje `StudentInboxScreen`. `kind=recruiter` daje `RecruiterInboxScreen`. Inne daje `HomeScreen`.

### 3. Kolejność wylogowania

Najpierw `DELETE`, potem czyszczenie sesji, a przy błędzie sieci i tak czyszczenie sesji. Test kolejności na podstawionym kliencie: wywołanie `DELETE` jest przed czyszczeniem, gdy sieć działa.

### Co jest do czego

| Zdarzenie | Skutek |
| --- | --- |
| login `owner` | `PUT /devices` |
| login `student` | brak `PUT` |
| logout `owner` | `DELETE /devices` |
| stuknięcie | właściwa skrzynka |

## Testy

Nazwa: `student_does_not_register_push`.

| Arrange | Act | Assert |
| --- | --- | --- |
| rola `student` | `shouldRegisterPush` | `false` |

Nazwa: `recruiter_kind_opens_recruiter_inbox`.

```bash
cd src/clients/mobile && pnpm exec vitest run src/push
```

## Checkpoint

```bash
cd src/clients/mobile && pnpm exec vitest run
```

W repozytorium nie ma pliku JSON konta usługi FCM. W kodzie są dwa identyfikatory kanałów zgodne z API.

## Umiesz wyjaśnić

Dlaczego student nie dostaje pusha skrzynek?

Bo skrzynki są właściciela. Token studenta na tym telefonie zapisałby adres, na który Contact wyśle treść właściciela, jeśli API pomyli rolę. API i tak wymaga `owner` na `PUT`. Klient nie woła `PUT` dla studenta, żeby nie polegać wyłącznie na `403`.

Dlaczego kasujemy token przy wylogowaniu?

Bo telefon może dostać inne konto. Stary token nadal wskazuje urządzenie. `DELETE` odcina wysyłkę. Samo skasowanie sesji lokalnej nie odcina FCM.

Czemu wylogowanie nie czeka w nieskończoność na `DELETE`?

Bo brak sieci uwięziłby człowieka w sesji. Sesja lokalna i tak znika. Następne wejście tego konta może powtórzyć `DELETE`, jeśli token jeszcze jest w pamięci do tego czasu. Jeśli nie jest, token wygasa po stronie FCM albo zostanie nadpisany następnym `PUT` tego samego urządzenia przez nowego właściciela. API powinno wiązać token z `sub`. `PUT` nowego właściciela podmienia właściciela tokenu. To jest w kontrakcie `PUT`: token jest unikalny, ostatni `owner` wygrywa. Dopisujemy to do numeru pusha API jednym zdaniem, jeśli tam jest tylko „wiele tokenów”. Ostatni `PUT` danego tokenu zmienia `owner_sub`. Inaczej stary właściciel zostaje adresatem.

Dlaczego dwa kanały?

Bo stuknięcie ma otworzyć jedną skrzynkę. `kind` jest w danych. Kanał androida jest tym samym identyfikatorem, żeby użytkownik mógł wyciszyć rekruterów i zostawić studentów.

Gdzie jest sekret FCM?

Poza repozytorium, tak jak w numerze pusha API. Aplikacja dostaje token urządzenia od Firebase, nie klucz konta usługi. Klucz konta jest tylko na serwerze.

## Zasady

- `shouldRegisterPush` jest true tylko dla `owner`.
- `DELETE` jest wołane przy wylogowaniu właściciela.
- Porażka `DELETE` nie blokuje wylogowania.
- Nieznany `kind` nie otwiera skrzynki.
- W git nie ma klucza konta usługi.
- Identyfikatory kanałów to `students` i `recruiters`.
- Ostatni `PUT` tokenu przypina go do bieżącego `owner`.

## Poza zakresem

- Publikacja aplikacji do sklepu nie jest tym numerem.
- OpenBao dla klucza serwera jest w [05-openbao.md](../13-kubernetes/05-openbao.md).
- Edytor na telefonie nie powstaje.

## Źródła

- [Expo Notifications](https://docs.expo.dev/versions/latest/sdk/notifications/). Wiążące są token i kanały Androida. Dotyczy rejestracji. Ze źródła nie bierzemy trzymania klucza serwera w aplikacji.
- Numer pusha API. Wiążące są `PUT` i `DELETE /devices` oraz dwa kanały. Dotyczy klienta. Ze źródła nie bierzemy pusha bez roli `owner`.
- [FCM HTTP v1](https://firebase.google.com/docs/cloud-messaging/migrate-v1). Wiążące jest to, że klucz konta zostaje na serwerze. Dotyczy braku pliku JSON w `mobile`. Ze źródła nie bierzemy starszego klucza w query.

## Gotowe gdy

- Test studenta nie rejestruje pusha.
- `kind=recruiter` wybiera skrzynkę rekruterów.
- Wylogowanie woła `DELETE` przed czyszczeniem sesji, gdy sieć działa, i czyści sesję także gdy nie działa.
- W drzewie mobile nie ma JSON konta usługi.
- Kanały nazywają się `students` i `recruiters`.
