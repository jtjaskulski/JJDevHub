# 05. Treść publiczna

Poprzedni numer to [logowanie](04-logowanie-pkce.md). Następny to [intranet](06-intranet.md).

Ten numer wypełnia ekrany publiczne: strona główna, CV, technologia, kursy, lekcja, notatki i formularz rekrutera. HTML z API wchodzi do DOM tylko w `TrustedHtml`. Drugi raz czyści go DOMPurify. Formularz studenta jest pod `/:lang/student/contact` i wymaga roli `student`.

## Po co

Szkielet bez treści nie jest stroną. Osobne wstawienie HTML w każdym ekranie kończy się `dangerouslySetInnerHTML` w pięciu plikach, z czego jeden zapomni oczyścić. Jeden komponent zamyka drogę HTML. Formularz publiczny, który umie wysłać zapytanie studenta, obchodzi token. Dlatego publiczny formularz jest tylko rekrutera.

## Dlaczego ta technologia

### `TrustedHtml`

Jedyny plik z `dangerouslySetInnerHTML`. Przed wstawieniem woła DOMPurify w wydaniu Apache-2.0. Problem, który zamyka, to zaufanie, że serwer zawsze oczyścił. Serwer czyści. Klient czyści drugi raz. Odpada Markdown renderowany w przeglądarce z surowego pola, którego DTO i tak nie ma. Koszt: dwa razy ta sama biała lista. Rozjazd list jest lepszy niż jeden błąd po stronie klienta bez siatki.

### Ekrany

Dane z Query i klienta OpenAPI. Pusta lista pokazuje klucz `empty.list`. Błąd odczytu pokazuje `error.load`. Nieznany slug przy dobrym języku zostaje w tym języku i pokazuje `error.notFound`. Zły język w ogóle tu nie dochodzi, bo `LangGate` już odesłał na `/pl`. Problem, który zamyka, to pusty ekran bez rozróżnienia „nie ma” i „nie udało się pobrać”. Odpada jeden komunikat na oba.

Home składa skrót CV i kilka notatek. Nie jest osobnym dokumentem w bazie.

### Formularz rekrutera

`/:lang/contact` wysyła `POST` rekrutera z `Idempotency-Key` wygenerowanym na próbę wysłania. Sukces pokazuje `contact.recruiter.success`. Nie ma przełącznika roli. Problem, który zamyka, to drugi przycisk „jestem studentem”. Odpada ten przycisk.

### Formularz studenta

`StudentRoute` wymaga roli `student`. Gość idzie na `/login` z `next`. Zalogowany `owner` wraca na `/pl`. Wysłanie idzie na endpoint studenta. Problem, który zamyka, to formularz studenta na stronie publicznej. Odpada ukryte pole roli.

### Ukończenie lekcji

Na lekcji, gdy rola to `student`, jest przycisk `lesson.complete`. Woła `POST` ukończenia. Drugi raz nie jest błędem w UI: dostaje `200` i zostaje wciśnięty. Gość nie widzi przycisku.

### Wersje usług

Stopka pod numerem webu woła `GET /versions` i rysuje sześć wierszy w kolejności ze słownika. Brak pola albo pusty napis to `version.missing`. Siódma usługa nie jest dopisywana.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| `TrustedHtml` | jedyne miejsce HTML z API w DOM |
| `StudentRoute` | wpuszcza rolę `student` |
| idempotency w formularzu | jeden klucz na jedną próbę wysłania |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/clients/web/src/TrustedHtml.tsx` | DOMPurify i inner HTML |
| `src/clients/web/src/screens/` | ekrany ze słownika |
| `src/clients/web/src/StudentRoute.tsx` | strażnik |
| `src/clients/web/src/useVersions.ts` | sześć wierszy |
| `src/clients/web/src/TrustedHtml.test.tsx` | `script` nie wchodzi |

## Plan

### 1. HTML

Test: wejście z `<script>` daje kontener bez elementu script. Wejście z `<p>` zostaje akapitem.

### 2. Trasy danych

Każdy ekran publiczny z tabeli słownika ma test, który przy podstawionym kliencie pokazuje tytuł z odpowiedzi albo klucz pustej listy. Nie trzeba sześciu integracji z prawdziwym API. Wystarczy jeden reprezentatywny i formularz.

### 3. Formularze

Rekruter nie wysyła nagłówka `Authorization`, nawet jeśli właściciel jest zalogowany na tej samej przeglądarce i patrzy na stronę publiczną. Świadomie: formularz publiczny jest zawsze `RecruiterInquiry`. Właściciel, który chce napisać jako student, używa konta studenta. To jest ostre i zgodne z API.

### Co jest do czego

| Ekran | Źródło |
| --- | --- |
| home | CV i lista notatek |
| lekcja | `html` z API |
| kontakt publiczny | `POST` rekrutera |
| kontakt studenta | `POST` studenta |

## Testy

Nazwa: `trusted_html_strips_script`.

| Arrange | Act | Assert |
| --- | --- | --- |
| HTML z `script` | render `TrustedHtml` | brak elementu `script` |

Nazwa: `public_contact_posts_recruiter_inquiry`.

```bash
cd src/clients/web && pnpm exec vitest run
```

## Checkpoint

```bash
cd src/clients/web && pnpm exec vitest run
```

Na `aspire run` i `pnpm dev` da się otworzyć `/pl/cv`, lekcję publiczną i wysłać formularz rekrutera. W kodzie ekranów jest jedno `dangerouslySetInnerHTML`, w `TrustedHtml`. Stopka pokazuje sześć nazw.

## Umiesz wyjaśnić

Dlaczego czyścimy drugi raz?

Bo błąd w białej liście serwera nie powinien stać się skryptem w przeglądarce. Drugie czyszczenie jest tanie. Jedno miejsce pilnuje, że nikt nie wstawi HTML obok.

Dlaczego formularz publiczny nie umie być studencki?

Bo rola studenta jest w tokenie, a publiczny formularz jest bez konta. Przełącznik „jestem studentem” nadawałby rolę samą deklaracją. Ścieżka studenta jest za logowaniem.

Dlaczego `404` i błąd sieci to dwa klucze?

Bo „nie ma takiej lekcji” i „nie pobrało się” prowadzą do innej reakcji. Jeden napis uczy klikać odśwież na martwym slugu albo iść dalej przy awarii.

Skąd sześć wierszy, skoro część usług może nie istnieć?

Z `GET /versions`, które zawsze ma sześć pól. Puste pole to `version.missing`, nie dziura w stopce.

Dlaczego właściciel na stronie `/contact` wysyła zapytanie rekrutera?

Bo ten adres jest publiczny i znaczy „rekruter”. Właściciel nie jest tu nadawcą studenckim. Inny adres jest dla studenta.

## Zasady

- `dangerouslySetInnerHTML` występuje raz.
- Publiczny formularz nie tworzy `StudentInquiry`.
- Gość nie widzi przycisku ukończenia.
- Pusta lista i błąd odczytu mają różne klucze.
- Stopka ma sześć wierszy w stałej kolejności.
- Ekran nie woła portu usługi.

## Poza zakresem

- Edycja jest w [06-intranet.md](06-intranet.md).
- Ruch jest poza tym numerem; nie dodajemy biblioteki animacji tutaj.
- Playwright jest w [07-playwright.md](07-playwright.md).

## Źródła

- [DOMPurify](https://github.com/cure53/DOMPurify). Wiążąca jest sanitacja przed wstawieniem HTML. Dotyczy `TrustedHtml`. Ze źródła nie bierzemy wyłączenia w Development.
- [Trusted Types](https://developer.mozilla.org/en-US/docs/Web/API/Trusted_Types_API). Wiążące jest jedno miejsce, w którym HTML staje się węzłem. Dotyczy zakazu inner HTML poza komponentem. Ze źródła nie bierzemy polityki przeglądarki jako jedynej ochrony: DOMPurify jest w kodzie.
- Słownik, mapa ekranów. Wiążące są adresy i podział formularzy. Dotyczy tras. Ze słownika nie bierzemy edytora na stronie publicznej.

## Gotowe gdy

- Test `script` przechodzi.
- Formularz `/pl/contact` woła endpoint rekrutera.
- Student bez roli nie zostaje na formularzu studenta.
- Stopka rysuje sześć wierszy.
- W ekranach jest jedno `dangerouslySetInnerHTML`.
