# 02. Logowanie na telefonie

Poprzedni numer to [szkielet Expo](01-szkielet-expo.md). Następny to [ekrany](03-ekrany.md).

Ten numer robi ten sam authorization code z PKCE dla klienta `jjdevhub-mobile`. Przekierowanie wraca schematem `jjdevhub://callback`. Access token jest w pamięci. W `localStorage` go nie ma, bo to nie jest przeglądarka; nie ma też zapisu access tokenu w `AsyncStorage`. Refresh może być w bezpiecznym magazynie systemu, jeśli Expo go daje bez dokładania sekretu do repozytorium.

## Po co

Telefon bez logowania nie ukończy lekcji i nie otworzy skrzynki właściciela. Skopiowanie grantu hasła byłoby osobnym, gorszym protokołem. Ten sam serwer i PKCE zamykają jeden model. Inny magazyn tokenu niż w webie jest wymuszony przez platformę, nie przez inny protokół.

## Dlaczego ta technologia

### `expo-auth-session` albo przepływ wprost

Jeśli pakiet Expo do sesji auth jest na MIT i umie PKCE oraz schemat, używamy go. Jeśli ciągnie grant, którego nie chcemy, zostaje cienki przepływ na `expo-linking`. Problem, który zamyka, to własny serwer OAuth w aplikacji. Odpada webview, które samo wpisuje hasło do naszej strony i czyta URL z tokenem w fragmencie. Kod wraca schematem, wymiana jest w aplikacji, tak jak w przeglądarce.

### Magazyn

Access token tylko w pamięci procesu. Refresh w `expo-secure-store`, które korzysta z keychain i keystore. Problem, który zamyka, to refresh w pliku JSON aplikacji. Odpada `AsyncStorage` na token. Koszt: po odinstalowaniu magazyn znika. Dobrze.

### `next`

Na telefonie nie ma otwartego przekierowania na cudzy URL. Po logowaniu stos wraca do ekranu, który o logowanie poprosił. Parametr jest nazwą ekranu z listy, nie dowolnym stringiem. Nieznana nazwa wraca do `HomeScreen`.

### Rejestracja

Ekran rejestracji zbiera te same pola co web i woła ten sam `POST`. Hasło nie trafia do parametrów nawigacji.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| schemat | `jjdevhub://callback` |
| secure store | magazyn systemu na refresh |
| nazwa ekranu | jedyny dozwolony „powrót” |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/clients/mobile/src/auth/pkce.ts` | verifier, może być współdzielony z webem, jeśli nie ciągnie DOM |
| `src/clients/mobile/src/auth/session.ts` | pamięć i secure store |
| `src/clients/mobile/src/screens/LoginScreen.tsx` | start |
| `src/clients/mobile/app.json` | schemat `jjdevhub` |

Wspólny `pkce.ts` w `shared`, jeśli obie strony użyją tych samych funkcji bez DOM. Webowy plik staje się reeksportem. Jedna implementacja challenge.

## Plan

### 1. Schemat

`app.json` ma `scheme: jjdevhub`. Zgadza się z seedem Identity.

### 2. Wymiana

Po powrocie aplikacja wymienia kod, kasuje verifier, trzyma access w pamięci, refresh w secure store.

### 3. Test

Fakt na funkcji powrotu: nazwa spoza listy daje `HomeScreen`. Fakt: moduł sesji nie woła API `AsyncStorage` dla klucza access. Test podstawia magazyny.

### Co jest do czego

| Magazyn | Token |
| --- | --- |
| pamięć | access |
| secure store | refresh |
| AsyncStorage | język, nie token |

## Testy

Nazwa: `unknown_return_screen_goes_home`.

| Arrange | Act | Assert |
| --- | --- | --- |
| nazwa `https://evil` | funkcja powrotu | `HomeScreen` |

Nazwa: `access_token_is_not_put_in_async_storage`.

```bash
cd src/clients/mobile && pnpm exec vitest run src/auth
```

## Checkpoint

```bash
cd src/clients/mobile && pnpm exec vitest run
```

W `app.json` jest schemat `jjdevhub`. W źródłach nie ma `client_secret`. Ręczne sprawdzenie na urządzeniu nie jest warunkiem CI.

## Umiesz wyjaśnić

Dlaczego ten sam PKCE?

Bo serwer ma jeden model klienta publicznego. Osobny grant dla telefonu to drugi zestaw dziur, w tym hasło w aplikacji.

Dlaczego refresh jest w secure store, a access w pamięci?

Access jest krótki i nie musi przeżyć zabicia procesu, jeśli refresh potrafi go odnowić. Refresh przeżywa, więc ma być w magazynie systemu, nie w pliku czytelnym dla kopii zapasowej aplikacji wprost.

Czemu powrót jest nazwą ekranu?

Bo string URL na telefonie łatwo staje się linkiem wychodzącym. Lista ekranów jest zamknięta.

Gdzie jest hasło przy rejestracji?

W ciele `POST`, nie w parametrach trasy i nie w logu. Tak samo jak w webie.

Dlaczego verifier znika po wymianie?

Bo kto go ma razem z kodem, ten wymieni kod. Po wymianie nie jest potrzebny. Zostawienie go poszerza okno.

## Zasady

- Klient to `jjdevhub-mobile`, bez sekretu.
- Access token nie wchodzi do AsyncStorage.
- Powrót tylko na znaną nazwę ekranu.
- Schemat zgadza się z Identity.
- Hasło nie jest w parametrach nawigacji.
- Wspólna funkcja challenge, jeśli da się ją wyjąć bez DOM.

## Poza zakresem

- Ekrany treści są w [03-ekrany.md](03-ekrany.md).
- Rejestracja tokenu push jest w [04-powiadomienia.md](04-powiadomienia.md) i woła się po zalogowaniu właściciela, nie tu.
- Edytory intranetu na telefonie nie powstają.

## Źródła

- [RFC 7636](https://www.rfc-editor.org/rfc/rfc7636). Wiążące jest PKCE dla klienta publicznego. Dotyczy telefonu tak samo jak webu. Ze źródła nie bierzemy `plain`.
- [Expo AuthSession](https://docs.expo.dev/versions/latest/sdk/auth-session/). Wiążące jest przekierowanie schematem. Dotyczy `jjdevhub://callback`. Ze źródła nie bierzemy grantu hasła, nawet jeśli przykład go pokazuje.
- [Expo SecureStore](https://docs.expo.dev/versions/latest/sdk/securestore/). Wiążące jest trzymanie sekretu w magazynie systemu. Dotyczy refresh. Ze źródła nie bierzemy zapisu access tokenu obok refresh „dla wygody”.

## Gotowe gdy

- Test nieznanego powrotu ląduje na `HomeScreen`.
- Access token nie jest zapisywany do AsyncStorage w teście sesji.
- `app.json` ma schemat `jjdevhub`.
- W drzewie mobile nie ma `client_secret`.
- Rejestracja używa tego samego `POST` co web.
