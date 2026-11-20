# 06. Intranet

Poprzedni numer to [treść publiczna](05-tresc-publiczna.md). Następny to [Playwright](07-playwright.md).

Ten numer dodaje trasy `/intranet` dla roli `owner`. Są edytory CV, notatek i kursów oraz dwie skrzynki. Zapis wysyła `If-Match`. Konflikt `412` pokazuje klucz `editor.conflict` i przycisk przeładowania. Stream skrzynek odświeża listę.

## Po co

Edycja na stronie publicznej miesza czytelnika z właścicielem. Osobny prefiks i strażnik zamykają wejście. `If-Match` zamyka dwa okna z tą samą notatką: drugie dostaje konflikt zamiast cichego nadpisania. Dwie skrzynki zamykają jedną listę „wiadomości”.

## Dlaczego ta technologia

### `OwnerRoute`

Brak tokenu prowadzi na `/login` z `next=/intranet`. Token studenta prowadzi na `/pl`. Token `owner` wpuszcza. Problem, który zamyka, to ukryty link jako jedyne zabezpieczenie. Odpada sprawdzanie roli tylko w belce. Koszt: każdy ekran intranetu jest pod tym strażnikiem. Test montuje jeden ekran poza nim i oczekuje odrzucenia, jeśli ktoś doda trasę obok.

### Edytor

Pole tekstowe, przycisk `editor.save`. Wersja z `ETag` ostatniego `GET` idzie w `If-Match`. `412` nie czyści pola. Pokazuje konflikt i `editor.reload`, który ponownie bierze `GET` i zastępuje treść. Problem, który zamyka, to zapis, który w ciszy wygrywa z drugim oknem. Odpada `PUT` bez wersji. Koszt: właściciel musi przeładować. Tak ma być.

### Skrzynki

Dwa adresy ze słownika. Każdy `GET` swojej listy. `EventSource` na `/contact/inquiries/stream` z tokenem w query, bo tak pozwala API, tylko na tym URL. Po evencie Query unieważnia listę tego `kind`. Problem, który zamyka, to odświeżanie ręczne. Odpada jeden widok na obie skrzynki. Koszt: otwarte połączenie, gdy intranet jest otwarty. Przy wyjściu z tras połączenie się zamyka.

### CV

Edytor sekcji zapisuje jedną sekcję, nie cały dokument naraz. Wersja jest tej sekcji. Problem, który zamyka, to jeden przycisk „zapisz wszystko”, który wysyła niezmienione sekcje i podbija im wersje. Odpada zapis całej strony jednym `PUT`. Koszt: więcej przycisków. Są przy sekcjach.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| `OwnerRoute` | wpuszcza tylko `owner` |
| konflikt | `412`, treść w polu zostaje |
| skrzynka | jedna lista, jeden adres |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/clients/web/src/OwnerRoute.tsx` | strażnik |
| `src/clients/web/src/intranet/` | ekrany edycji i skrzynek |
| `src/clients/web/src/intranet/useSaveWithMatch.ts` | `If-Match` i konflikt |
| `src/clients/web/src/intranet/inboxStream.ts` | `EventSource` |

## Plan

### 1. Strażnik

Test trzech aktorów: gość, student, właściciel. Oczekiwane przejścia jak wyżej.

### 2. Zapis

Podstawiony klient: `PUT` zawiera `If-Match` równy `ETag` z `GET`. Odpowiedź `412` ustawia stan konfliktu i nie wywołuje drugiego `PUT`.

### 3. Stream

Po evencie `kind=student` unieważniona jest lista studentów, nie lista rekruterów.

### Co jest do czego

| Ekran | Zapis |
| --- | --- |
| notatka | `PUT` z `If-Match` |
| kurs i lekcja | to samo |
| sekcja CV | `PUT` tej sekcji |
| skrzynki | brak `POST`, tylko odczyt i stream |

Intranet nie wysyła zapytania rekrutera ani studenckiego.

## Testy

Nazwa: `student_is_sent_home_from_intranet`.

| Arrange | Act | Assert |
| --- | --- | --- |
| rola `student` | wejście na `/intranet` | przekierowanie na `/pl` |

Nazwa: `save_sends_if_match`.

```bash
cd src/clients/web && pnpm exec vitest run src/intranet src/OwnerRoute.test.tsx
```

## Checkpoint

```bash
cd src/clients/web && pnpm exec vitest run
```

Jako właściciel da się otworzyć obie skrzynki i zapisać notatkę. Drugie okno ze starym `ETag` pokazuje `editor.conflict`. Jako student `/intranet` wraca na `/pl`.

## Umiesz wyjaśnić

Dlaczego student nie wchodzi do intranetu?

Bo intranet jest edycją treści właściciela i skrzynkami właściciela. Student ma formularz i ukończenie lekcji. Mieszanie ról na jednym adresie kończy się przyciskiem, który API i tak odrzuci, po tym jak pokaże cudzą listę w szkicu.

Dlaczego konflikt nie nadpisuje pola?

Bo w polu jest praca człowieka z tego okna. Automatyczne wczytanie wersji z serwera skasowałoby ją. Przycisk przeładowania jest jawny.

Czemu CV nie zapisuje się w całości?

Bo sekcje mają osobne wersje. Jeden zapis całej strony albo wysyła niezmienione sekcje, albo gubi konflikt na jednej z nich. Przycisk przy sekcji jest uczciwszy.

Dlaczego stream używa query, a lista nie?

Bo tak jest ograniczone w API: tylko SSE nie umie nagłówka w `EventSource`. Lista używa nagłówka. Query na liście i tak dostałoby `401`.

Co się dzieje po wyjściu z intranetu?

Połączenie streamu jest zamykane. Nie zostaje w tle na stronie publicznej.

## Zasady

- Każda trasa pod `/intranet` jest w `OwnerRoute`.
- Zapis edytora wysyła `If-Match`.
- `412` nie czyści pola samo.
- Dwie skrzynki, dwa adresy.
- Stream zamyka się przy opuszczeniu tras.
- Intranet nie tworzy zapytań.

## Poza zakresem

- Playwright przejdzie jedną z tych ścieżek w [07-playwright.md](07-playwright.md).
- Telefon nie ma edytora. Ma skrzynki w [03-ekrany.md](../12-mobile/03-ekrany.md).
- Ruch przycisków nie jest wymagany.

## Źródła

- Numer REST Learning i numer zapisu News. Wiążące są `If-Match`, `412` i `428`. Dotyczy `useSaveWithMatch`. Ze źródła nie bierzemy zapisu bez wersji w intranecie.
- Numer skrzynek. Wiążące są dwa `GET` i SSE z `kind`. Dotyczy ekranów. Ze źródła nie bierzemy jednej listy.
- [EventSource](https://developer.mozilla.org/en-US/docs/Web/API/EventSource). Wiążące jest to, że nagłówek autoryzacji nie jest w prostym API. Dotyczy tokenu w query tylko na stream. Ze źródła nie bierzemy zostawiania połączenia po wyjściu z ekranu.

## Gotowe gdy

- Student nie zostaje na `/intranet`.
- Test zapisu widzi `If-Match`.
- `412` pokazuje konflikt bez czyszczenia pola.
- Event studenta nie odświeża listy rekruterów.
- Obie skrzynki mają osobne adresy.
