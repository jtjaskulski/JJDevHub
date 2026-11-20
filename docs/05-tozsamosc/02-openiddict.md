# 02. OpenIddict

Poprzedni numer to [konta](01-konta-i-zaproszenia.md). Następny to [JWT i polityki](03-jwt-i-policies.md).

Ten numer stawia w Identity serwer OpenIddict 7.8.0. Przeglądarka i telefon dostaną authorization code z PKCE. Na tym numerze klientem testowym jest żądanie HTTP, nie jeszcze React. Token jest JWT. Klucz prywatny zostaje w Identity. Klucz publiczny jest na JWKS. Hasło użytkownika nie jest w treści tokenu.

## Po co

Własnoręcznie składany JWT da się podpisać źle: algorytm `none`, brak `exp`, sekret współdzielony w każdej usłudze. Serwer OIDC zamyka wydawanie i odkrywanie kluczy. PKCE zamyka kradzież kodu w kliencie publicznym, który nie ma sekretu. Przeglądarka i telefon są takimi klientami. Sekret w aplikacji mobilnej nie jest sekretem.

Do tej pory lokalny token mógł pochodzić z `dotnet user-jwts`. Od tego numeru Development też bierze token z Identity, żeby nie mieć dwóch sposobów podpisu. `user-jwts` odpada.

## Dlaczego ta technologia

### OpenIddict, nie Duende i nie Keycloak

OpenIddict jest na Apache-2.0 i siada na naszym `Account`. Problem, który zamyka, to drugi katalog użytkowników w Keycloak i licencja Duende poza listą. Odpada też ręczny `JwtSecurityTokenHandler` jako docelowy mechanizm wydawania. Koszt: trzeba zrozumieć authorization code, a nie tylko „wklejam bearer”. To jest koszt, który i tak ponosi klient.

### Authorization code i PKCE

Grant to `authorization_code` z `code_challenge` S256. Nie ma `password` grant. Nie ma implicit. Problem, który zamyka, to hasło wpisywane w aplikacji, która sama bierze token z endpointu tokenu, oraz kod bez PKCE, który da się wymienić z innego komputera. Odpada ROPC. Koszt: przekierowanie. Web i telefon je mają. Klient testowy w numerze używa przekierowania na `localhost`.

Klient publiczny `jjdevhub-web` i `jjdevhub-mobile` nie ma client secret. Zaufanie jest w PKCE i w zarejestrowanym `redirect_uri`. Inny redirect pada.

### Klucze

Certyfikat podpisu albo klucz RSA powstaje przy starcie Development i ląduje w katalogu danych poza gitem, albo jest w zmiennej środowiska na maszynie jako PEM. Ten numer wybiera PEM w `/etc/jjdevhub/identity-signing.pem` o uprawnieniu `600` oraz drugi plik szyfrowania. W Development `dotnet user-secrets` albo plik w katalogu użytkownika wskazany zmienną `IDENTITY_SIGNING_KEY_PATH`. W git jest przykład ścieżki, nie klucz. Problem, który zamyka, to klucz w `appsettings.json`. Odpada efemeryczny klucz deweloperski OpenIddict na maszynie: po restarcie wszystkie tokeny padają i nie da się tego odróżnić od awarii. W Development efemeryczny klucz jest dozwolony tylko wtedy, gdy test nie sprawdza restartu. Na maszynie plik jest obowiązkowy. Brak pliku na maszynie kończy start kodem niezerowym.

JWKS jest pod `/.well-known/jwks.json` zgodnie z odkrywaniem OpenIddict. Brama i usługi będą brać klucz stąd. Ten numer wystawia dokument. Następny go konsumuje.

### Zakresy i claim roli

Zakresy: `openid`, `profile`, `email`, `roles`. Rola idzie w claimie `role`. Jest jedna rola na konto, nie lista. `sub` jest identyfikatorem konta, nie e-mailem. E-mail jest osobnym claimem. Problem, który zamyka, to rola w `sub` i zmiana e-maila, która zmienia tożsamość. Odpada claim `role` wpisany przez klienta w żądaniu. Rola bierze się z wiersza `Account`.

### Odświeżanie

Refresh token jest włączony, rotacja włączona, stary refresh po użyciu odpada. Czas życia access tokenu to 15 minut. Refresh 14 dni. Liczby są w konfiguracji OpenIddict w kodzie, w jednym miejscu. Problem, który zamyka, to access token ważny rok. Odpada brak refresh i zmuszanie do hasła co 15 minut w sposób, którego jeszcze nie ma: klient numeru frontu schowa refresh. Tu jest serwer, który refresh wydaje.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| authorization code | krótki kod wymieniany na token, nie token w adresie na stałe |
| PKCE | `code_challenge` i `code_verifier`, bez sekretu klienta |
| JWKS | dokument kluczy publicznych |
| `sub` | identyfikator konta |
| refresh | token do wymiany, jednorazowy przy rotacji |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Identity.Infrastructure/OpenIddict/OpenIddictSetup.cs` | serwer, certyfikaty, scope |
| `Identity.Infrastructure/OpenIddict/AccountClaims.cs` | `sub`, `role`, `email` z wiersza |
| `Identity.Api/Program.cs` | mapowanie endpointów OpenIddict |
| `tests/Identity.Api.Tests/PkceTests.cs` | wymiana kodu |

Paczki `OpenIddict.AspNetCore` i `OpenIddict.EntityFrameworkCore` 7.8.0. Tabele OpenIddict są w `identity_write` obok `accounts`. To jest stan serwera, nie domena kursu. Migracja jest osobna.

## Plan

### 1. Aplikacje klienckie

Seed przy starcie, idempotentny, wstawia dwa client application: `jjdevhub-web` i `jjdevhub-mobile`, publiczne, grant authorization code, PKCE wymagane, redirecty `http://localhost:5173/callback` i schemat aplikacji mobilnej `jjdevhub://callback`. Redirect produkcyjny hostname dopisze numer frontu, gdy hostname jest znany. Na tym numerze localhost i schemat mobilny wystarczają. Dodanie redirectu jest zmianą seeda, nie kliknięciem w bazie.

### 2. Wymiana

Test podnosi host. Kroki: utworzyć konto w bazie testowej, pójść na authorize z `code_challenge`, zalogować się na endpoincie logowania testowego, wziąć kod, wymienić na token z `code_verifier`. Odpowiedź ma `access_token` i `refresh_token`. Drugie użycie tego samego refresh zwraca błąd.

Endpoint logowania formularzem jest częścią przepływu przekierowania. Przyjmuje e-mail i hasło, stawia cookie sesji logowania OpenIddict na czas przepływu i nie jest API „daj mi JWT za hasło” do wołania z JavaScript bez przekierowania. JavaScript dostaje kod przez redirect.

### 3. JWKS

`GET /.well-known/openid-configuration` zwraca adresy. `jwks_uri` odpowiada dokumentem z kluczem publicznym. W dokumencie nie ma klucza prywatnego. Test szuka pola `d` i oczekuje braku.

### Co jest do czego

| Element | Kto go woła |
| --- | --- |
| authorize | przeglądarka i telefon |
| token | klient po otrzymaniu kodu |
| JWKS | brama i usługi, w następnym numerze |
| `Account` | principal, przy budowie claimów |

## Testy

Nazwa: `Authorization_code_with_pkce_issues_role_from_account`.

| Arrange | Act | Assert |
| --- | --- | --- |
| konto `student`, challenge S256 | pełny przepływ do tokenu | JWT ma `role=student` i `sub` równe id konta, nie ma hasła |

Nazwa: `Token_response_has_no_private_key_material`. Dotyczy JWKS i ciała token response.

```bash
dotnet test tests/Identity.Api.Tests --nologo --filter "FullyQualifiedName~Authorization_code_with_pkce"
```

Test może iść na hoście w pamięci bez kontenera, jeśli OpenIddict i baza EF użyją sqlite w teście. Produkcja zostaje na Postgresie. Sqlite tylko w tym teście, żeby nie mylić go z modelem. Jeśli sqlite rozjedzie migrację, test jest integracyjny na Postgresie. Wolimy Postgresa, cecha `Category=Integration`, niż dwa dialekty. Decyzja: Testcontainers, jedna baza `identity_write`.

## Checkpoint

```bash
dotnet test tests/Identity.Api.Tests --nologo --filter "Category=Integration&FullyQualifiedName~Pkce"
curl -fsS http://127.0.0.1:<port-identity>/.well-known/openid-configuration
```

Przy `aspire run` konfiguracja odkrywania zwraca JSON. W JWKS nie ma prywatnego wykładnika.

## Umiesz wyjaśnić

Dlaczego nie ma grantu hasła?

Bo hasło zostałoby w kliencie, który sam woła token endpoint. Authorization code trzyma hasło na stronie Identity, w przekierowaniu, a do klienta wraca kod, potem token. PKCE wiąże kod z tym klientem, który zaczął przepływ.

Dlaczego klient nie ma sekretu?

Bo sekret w przeglądarce i w aplikacji na telefonie da się wyjąć. Publiczny klient plus PKCE jest modelem na ten przypadek. Sekret zostałby dla klienta serwerowego, którego nie mamy.

Czym `sub` różni się od e-maila?

E-mail się zmienia. `sub` jest identyfikatorem wiersza i nie zmienia się przy zmianie e-maila. Rola i tak jest osobnym claimem.

Dlaczego klucz nie jest efemeryczny na maszynie?

Bo restart unieważniłby wszystkie sesje i nie dałoby się podpisać tokenu spójnego z JWKS, które usługi zdążyły zbuforować. Plik poza gitem przeżywa restart.

Co zostało z `user-jwts`?

Nic w Development produktu. Narzędzie nie jest ścieżką logowania. Jeden podpis, ten z Identity.

## Zasady

- Grant `password` i `implicit` są wyłączone.
- Klient publiczny nie ma client secret w repozytorium.
- Klucz prywatny nie wchodzi do git, do logu i do JWKS.
- `role` pochodzi z wiersza konta.
- Access token żyje 15 minut.
- Refresh jest rotowany.
- Tabele OpenIddict są w `identity_write`.

## Poza zakresem

- Walidacja w Learning i w bramie jest w [03-jwt-i-policies.md](03-jwt-i-policies.md).
- Ekran logowania przeglądarki jest w [04-logowanie-pkce.md](../11-web/04-logowanie-pkce.md).
- Ekran telefonu jest w [02-logowanie.md](../12-mobile/02-logowanie.md).
- Saga rejestracji z zaproszenia jest w [05-saga-rejestracji.md](05-saga-rejestracji.md). Logowanie istniejącego konta nie jest sagą.

## Źródła

- [OpenIddict documentation](https://documentation.openiddict.com/). Wiążące są authorization code, PKCE i klucze podpisu. Dotyczy serwera w Identity. Ze źródła nie bierzemy grantu hasła.
- [RFC 7636](https://www.rfc-editor.org/rfc/rfc7636). Wiążące jest S256 i odmowa wymiany bez verifera. Dotyczy klientów publicznych. Ze źródła nie bierzemy `plain` jako metody challenge.
- [RFC 7517](https://www.rfc-editor.org/rfc/rfc7517). Wiążący jest dokument klucza publicznego. Dotyczy JWKS. Ze źródła nie bierzemy klucza prywatnego w tym dokumencie.
- [Apache-2.0 OpenIddict](https://github.com/openiddict/openiddict-core/blob/dev/LICENSE). Wiążąca jest licencja 7.8.0. Dotyczy wyboru zamiast Duende. Ze źródła nie bierzemy pakietów spoza tej licencji.

## Gotowe gdy

- Test PKCE dostaje token z rolą z bazy.
- Grant hasła jest wyłączony i test, który go wywoła, dostaje odmowę.
- JWKS nie zawiera pola klucza prywatnego.
- W git nie ma PEM.
- `user-jwts` nie jest w `Program.cs` żadnej usługi jako mechanizm Development.
