# 04. Logowanie w przeglądarce

Poprzedni numer to [klient OpenAPI](03-klient-openapi.md). Następny to [treść publiczna](05-tresc-publiczna.md).

Ten numer podpina authorization code z PKCE. Przeglądarka jest klientem publicznym `jjdevhub-web`. Nie ma client secret w bundle. Access token żyje w pamięci. Odświeżenie używa refresh tokenu trzymanego tak, żeby przeładowanie strony nie wylogowywało od razu, bez wkładania access tokenu do `localStorage`.

## Po co

API właściciela bez logowania jest zamknięte, a formularz studenta też. PKCE zamyka wymianę kodu. Trzymanie access tokenu w `localStorage` zamyka się tym, że dowolny skrypt na stronie go czyta. Pamięć i krótkie życie access tokenu ograniczają okno. Refresh pozwala zostać zalogowanym.

## Dlaczego ta technologia

### PKCE w aplikacji

Biblioteka, jeśli jest, ma być mała i na MIT. Wolno napisać przepływ wprost: `code_verifier`, `S256`, przekierowanie na `/connect/authorize`, powrót na `/callback`, wymiana na `/connect/token`. Problem, który zamyka, to grant hasła z pola do token endpointu. Odpada wklejenie tokenu w ustawieniach. Koszt: przekierowanie miga. To jest model z numeru OpenIddict.

Redirect to `http://localhost:5173/callback` w Development i `https://<hostname>/callback` w obrazie. Oba są w seedzie Identity. Nowy hostname jest zmianą seeda i tej zmiennej razem.

### Gdzie leży token

Access token jest w zmiennej modułu albo w kontekście React, nie w `localStorage`. Refresh token jest w `sessionStorage` albo w pamięci, jeśli akceptujemy wylogowanie przy zamknięciu karty. Ten numer wybiera pamięć na access i `sessionStorage` na refresh. Problem, który zamyka, to access token przeżywający XSS w magazynie trwałym. Odpada `localStorage` na access. Koszt: odświeżenie karty woła refresh. Zamknięcie karty wylogowuje. To jest akceptowane.

### `401`

Odpowiedź `401` czyści stan i prowadzi na `/login` z `next` równym ścieżce, jeśli ścieżka jest względna i zaczyna się od `/`, i nie jest adresem zewnętrznym. Problem, który zamyka, to otwarte przekierowanie na `https://evil`. Odpada `next` równy dowolnemu URL. Koszt: po logowaniu wraca się tylko w obręb aplikacji.

### Rejestracja

`/register` zbiera e-mail, hasło i kod zaproszenia, woła `POST /identity/register` i pokazuje stan sagi, dopóki nie jest `Completed`. Potem prowadzi do logowania. Nie trzyma hasła w query. Problem, który zamyka, to konto bez zaproszenia. Odpada rejestracja właściciela. Właściciel powstaje z `seed-owner`.

### Wylogowanie

Czyści pamięć i `sessionStorage`, woła koniec sesji Identity, jeśli endpoint jest w odkrywaniu. Nie zostawia refresh.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| `code_verifier` | sekret PKCE trzymany w sesji przeglądarki na czas przekierowania |
| access token | krótki, w pamięci |
| refresh token | w `sessionStorage` |
| `next` | względna ścieżka powrotu |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/clients/web/src/auth/pkce.ts` | verifier i challenge |
| `src/clients/web/src/auth/callback.tsx` | wymiana kodu |
| `src/clients/web/src/auth/session.ts` | pamięć i refresh |
| `src/clients/web/src/routes/Login.tsx` | start przepływu |
| `src/clients/web/src/routes/Register.tsx` | saga |
| `src/clients/web/src/auth/pkce.test.ts` | challenge i `next` |

`code_verifier` na czas przekierowania może być w `sessionStorage`, bo bez tego powrót z Identity go nie zobaczy. Po wymianie jest kasowany. To nie jest access token.

## Plan

### 1. Start

`/login` generuje verifier, zapisuje go, przekierowuje na authorize z `client_id=jjdevhub-web` i `code_challenge`.

### 2. Callback

`/callback` czyta kod, wymienia, kasuje verifier, wkłada tokeny, idzie na `next` albo `/pl`.

### 3. Klient HTTP

`client.ts` dostaje nagłówek `Authorization` z pamięci, jeśli token jest. Nie czyta `localStorage`.

### 4. Test `next`

`next=https://evil.example` jest odrzucany. `next=/pl/courses` jest przyjmowany.

### Co jest do czego

| Miejsce | Co trzyma |
| --- | --- |
| pamięć | access token |
| `sessionStorage` | refresh i, chwilowo, verifier |
| `localStorage` | nic z tokenów |
| bundle | nic z sekretu klienta, bo go nie ma |

## Testy

Nazwa: `external_next_is_rejected`.

| Arrange | Act | Assert |
| --- | --- | --- |
| `next` z innym hostem | funkcja powrotu | wynik `/pl` |

Nazwa: `access_token_is_not_written_to_local_storage`. Po udanej wymianie w teście z podstawionym `localStorage` długość zapisów tokenu jest zero.

```bash
cd src/clients/web && pnpm exec vitest run src/auth
```

## Checkpoint

```bash
cd src/clients/web && pnpm exec vitest run
```

W Development da się przejść `/login`, zalogować na Identity i wrócić z tokenem. W źródłach webu nie ma `client_secret`. `localStorage` w teście nie dostał access tokenu.

## Umiesz wyjaśnić

Dlaczego nie ma sekretu klienta?

Bo bundle przeglądarki da się przeczytać. Publiczny klient i PKCE są modelem z serwera. Sekret w zmiennej `VITE_` byłby publiczny.

Dlaczego access token nie leży w `localStorage`?

Bo zostaje po zamknięciu karty i czyta go każdy skrypt na originie. Pamięć znika z odświeżeniem procesu. Refresh w `sessionStorage` przeżywa odświeżenie karty i znika z zamknięciem.

Czemu `next` musi być względny?

Bo inaczej po logowaniu wyślemy człowieka na cudzą stronę, a token zostanie u nas, ale zaufanie do przekierowania już nie. Ścieżka `/pl/...` wystarcza.

Gdzie jest hasło?

Tylko w formularzu Identity po przekierowaniu, albo w formularzu rejestracji wysłanym na `POST` rejestracji, nie w adresie. Rejestracja jest na naszej stronie i woła API. Hasło nie trafia do query i nie jest logowane.

Dlaczego rejestracja pokazuje stan sagi?

Bo `202` nie znaczy, że konto już się loguje. `WaitingForLearning` jeszcze nie aktywowało konta. Ekran odpytuje, aż będzie `Completed` albo stan końcowy z błędem.

## Zasady

- Brak `client_secret` w `src/clients`.
- Access token nie jest zapisywany do `localStorage`.
- `next` zewnętrzne jest odrzucane.
- Verifier jest kasowany po wymianie.
- `401` czyści sesję.
- Hasło nie występuje w adresie.

## Poza zakresem

- Ekrany kursów są w [05-tresc-publiczna.md](05-tresc-publiczna.md).
- Intranet jest w [06-intranet.md](06-intranet.md).
- Ten sam przepływ na telefonie jest w [02-logowanie.md](../12-mobile/02-logowanie.md).

## Źródła

- [RFC 7636](https://www.rfc-editor.org/rfc/rfc7636). Wiążące jest S256 i trzymanie verifera po stronie klienta, który zaczął. Dotyczy `pkce.ts`. Ze źródła nie bierzemy metody `plain`.
- Numer OpenIddict. Wiążące są `jjdevhub-web` i brak sekretu. Dotyczy przekierowania. Ze źródła nie bierzemy grantu hasła.
- [OWASP session](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html). Wiążące jest ograniczenie miejsca, w którym leży token. Dotyczy odmowy `localStorage` na access token. Ze ściągi nie bierzemy cookie, którego API bearer i tak nie czyta, jako jedynego magazynu, jeśli przekierowanie PKCE wymaga odczytu w JavaScript. Refresh w `sessionStorage` jest świadomym kompromisem.

## Gotowe gdy

- Test odrzuca zewnętrzne `next`.
- Test nie widzi access tokenu w `localStorage`.
- W drzewie webu nie ma `client_secret`.
- Callback kasuje verifier.
- Rejestracja nie wkłada hasła do query.
