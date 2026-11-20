# 06. Twardnienie API

Poprzedni numer to [saga](05-saga-rejestracji.md). Następny folder zaczyna się od [agregatu notatki](../06-news/01-agregat-i-zdarzenie.md).

Ten numer dokłada limit prób, nagłówki odpowiedzi i CORS do bramy. Zamyka rejestrację i logowanie przed zgadywaniem kodu oraz przed wołaniem API z obcej strony. Nie dodaje nowej funkcji produktu.

## Po co

Endpoint zaproszenia bez limitu pozwala sprawdzać kody tak szybko, jak sieć pozwoli. Przeglądarka z obcej domeny nie powinna dostać odpowiedzi z tokenem, nawet jeśli token i tak jest w nagłówku, którego tamta strona nie powinna widzieć. Nagłówki `Server` z wersją i brak `nosniff` nie są dziurą same w sobie. Są informacją, której nie chcemy oddawać, i klasą plików, której nie chcemy wąchać jako HTML.

## Dlaczego ta technologia

### Limit na bramie

Stały limit: 30 żądań na 10 sekund na adres IP na ścieżki `/connect/` i `/identity/register`, osobno od reszty. Powyżej `429` i `Retry-After`. Problem, który zamyka, to brak hamulca na authorize i na kod zaproszenia. Odpada limit tylko w aplikacji mobilnej. Koszt: wspólny NAT kilku osób dostaje jeden kubełek. Próg 30 na 10 sekund jest na tyle wysoki, że zwykłe przekierowanie PKCE się mieści, a pętla zgadywania nie. Liczby są w konfiguracji bramy.

Implementacja to wbudowany rate limiter ASP.NET, `PartitionedRateLimiter` po IP z `X-Forwarded-For` tylko wtedy, gdy połączenie przyszło z `cloudflared` na localhost. Inny `X-Forwarded-For` jest ignorowany, żeby klient nie podał sobie innego IP. Problem, który zamyka, to limit liczony z nagłówka, który atakujący ustawia. Odpada ufanie nagłówkowi z internetu wprost do Kestrelu, jeśli Kestrel kiedykolwiek słucha szerzej niż localhost. U nas słucha na localhost, a przed nim stoi `cloudflared`. Konfiguracja wymienia zaufany proxy jako loopback.

### CORS

Brama zezwala na jedną origin przeglądarki, tę z hostname produktu i `http://localhost:5173` w Development. Metody, których web używa. Nagłówki `Authorization`, `Content-Type`, `Idempotency-Key`, `If-Match`, `Accept-Language`. `AllowCredentials` tylko jeśli cookie sesji logowania jest na tej samej origin co API. Przepływ PKCE trzyma cookie na Identity, a API produktu jest na tym samym hostname przez bramę, więc origin jest jedna. Telefon nie używa CORS. Problem, który zamyka, to `Access-Control-Allow-Origin: *` razem z nagłówkiem autoryzacji. Odpada gwiazdka. Koszt: nowy hostname frontu jest zmianą konfiguracji.

### Nagłówki

Brama ustawia `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`, `X-Frame-Options: DENY`. Nie ustawia CORS na `*`. Problem, który zamyka, to osadzenie API w ramce i wąchanie typu. Odpada HSTS na samym Kestrelu jako jedyne miejsce: TLS kończy Cloudflare. HSTS ustawia się na strefie, nie w aplikacji, albo aplikacja i tak go wysyła, bo nie szkodzi przy pełnym HTTPS. Ten numer wysyła `Strict-Transport-Security` z bramy z `max-age` 180 dni. Przy samym `localhost` bez TLS przeglądarka deweloperska i tak go zignoruje na HTTP.

Ukrycie nagłówka `Server`, na ile Kestrel pozwala przez `AddServerHeader = false`.

### Treść błędu rejestracji

Zły kod i zły e-mail przy logowaniu dostają ten sam kształt `401` albo `422` bez wskazania, które pole jest prawdziwe, tam gdzie to zdradza konto. Dla logowania: jeden kod `identity.credentials.invalid`, zawsze, czy e-mail nie istnieje, czy hasło złe. Dla zaproszenia: kody domeny zostają, bo kod zaproszenia nie jest e-mailem i właściciel i tak zna kody, które wydał. Limit jest tym, co hamuje zgadywanie. Nie udajemy, że wygasły kod jest „złymi danymi logowania”.

### Rozmiar

Limit ciała żądania na bramie to 1 MB. Lekcja mieści się w 200 tysiącach znaków na język. Większe ciało dostaje `413`. Problem, który zamyka, to `POST` bez końca. Odpada brak limitu Kestrela.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| `429` | za dużo żądań |
| CORS | które origin wolno przeglądarce czytać |
| `nosniff` | przeglądarka nie zgaduje typu treści |
| zaufany proxy | kto wolno nadpisać adres klienta |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/gateway/JJDevHub.Gateway/Security/GatewayHardening.cs` | limiter, nagłówki, CORS, limit ciała |
| `Identity.Api` | jeden kod błędu logowania |
| `tests/JJDevHub.Gateway.Tests/RateLimitTests.cs` | 31. żądanie |
| `deploy/cloudflare/README.md` | dopisek o HSTS na strefie, jeśli brama go już wysyła |

## Plan

### 1. Limiter

Polityka `auth` na ścieżkach logowania i rejestracji. Test woła endpoint 31 razy z tego samego IP i oczekuje `429` oraz `Retry-After`. Test nie idzie w sieć. Host w pamięci.

### 2. CORS

Polityka nazwana `web`. Brak origin na liście nie dostaje `Access-Control-Allow-Origin`. Test z nagłówkiem `Origin: https://evil.example` na `GET /versions` nie widzi tego origin w odpowiedzi.

### 3. Logowanie

Nieudane logowanie, niezależnie od powodu „nie ma konta” albo „złe hasło”, zwraca `identity.credentials.invalid`. Test ma dwa arrange i ten sam `type`.

### Co jest do czego

| Środek | Gdzie działa |
| --- | --- |
| limiter | brama, ścieżki tożsamości |
| CORS | brama |
| jeden kod logowania | Identity |
| `413` | brama, całe API |

## Testy

Nazwa: `Thirty_first_auth_request_returns_429`.

| Arrange | Act | Assert |
| --- | --- | --- |
| 30 żądań pod limit | 31. na `/identity/register` | `429`, jest `Retry-After` |

Nazwa: `Unknown_origin_gets_no_allow_header`.

```bash
dotnet test tests/JJDevHub.Gateway.Tests tests/Identity.Api.Tests --nologo --filter "FullyQualifiedName~Thirty_first|FullyQualifiedName~Unknown_origin|FullyQualifiedName~credentials_invalid"
```

## Checkpoint

```bash
dotnet test tests/JJDevHub.Gateway.Tests --nologo --filter "FullyQualifiedName~Thirty_first"
curl -sI http://127.0.0.1:8080/versions | tr -d '\r' | grep -i -E 'x-content-type-options|x-frame-options'
```

Odpowiedź zawiera `nosniff` i `DENY`. `GET /versions` spoza origin z listy nie dostaje `Access-Control-Allow-Origin` równego obcej stronie.

## Umiesz wyjaśnić

Dlaczego limit jest na bramie, a nie tylko w Identity?

Bo brama jest jedynym wejściem z internetu. Limit w samej usłudze da się ominąć tylko z sieci wewnętrznej, której i tak nie publikujemy. Na wejściu jest jedno miejsce i jedna liczba.

Dlaczego nie ufać każdemu `X-Forwarded-For`?

Bo klient dopisze nagłówek i dostanie świeży kubełek. Ufamy nagłówkowi tylko od loopbacka, gdzie stoi `cloudflared`.

Czemu CORS `*` odpada przy nagłówku autoryzacji?

Bo przeglądarka nie pozwala mieszać gwiazdki z poświadczeniami, a gdyby pozwoliła, obca strona czytałaby odpowiedź API zalogowanego człowieka. Lista origin jest krótka.

Dlaczego logowanie ma jeden kod błędu, a zaproszenie nie?

Logowanie zdradza, czy e-mail jest w bazie, jeśli komunikaty się różnią. Zaproszenie jest sekretem losowym, a właściciel zna wydane kody. Różne kody zaproszenia pomagają mu zobaczyć, że kod wygasł. Hamulcem jest limit, nie udawanie, że kod jest hasłem.

Co daje `nosniff`?

Przeglądarka nie potraktuje odpowiedzi JSON jako HTML albo obrazka. Przy API publicznym to mała rzecz. I tak ją ustawiamy, bo nic nie kosztuje.

## Zasady

- Brak `Access-Control-Allow-Origin: *`.
- Limit auth jest w konfiguracji, z testem na 31. żądanie.
- `X-Forwarded-For` spoza loopbacka nie wybiera kubełka.
- Ciało ponad 1 MB dostaje `413`.
- Nieudane logowanie ma jeden `type`.
- Nagłówki twardnienia ustawia brama, nie każdy projekt osobno z inną wartością.

## Poza zakresem

- Treść stron błędów przeglądarki jest we frontcie.
- WAF Cloudflare poza tunelem nie jest konfigurowany w tym numerze poza trybem SSL, który już jest.
- Rotacja klucza podpisu nie jest tym numerem.

## Źródła

- [Rate limiting in ASP.NET](https://learn.microsoft.com/aspnet/core/performance/rate-limit). Wiążący jest limiter partycjonowany. Dotyczy ścieżek tożsamości. Ze źródła nie bierzemy wyłączenia limitu w Development na hoście, który i tak jest tym samym kodem co maszyna. W Development próg może być wyższy, jawnie, nie wyłączony.
- [Fetch CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS). Wiążące jest to, że gwiazdka nie idzie z credentials. Dotyczy listy origin. Ze źródła nie bierzemy odbijania dowolnego `Origin`.
- [OWASP API Security](https://owasp.org/API-Security/). Wiążące są ograniczenie liczby prób i brak nadmiarowej informacji przy logowaniu. Dotyczy `429` i jednego kodu błędu. Ze ściągi nie bierzemy punktów, które wymagają bramy API w chmurze: bramą zostaje YARP.

## Gotowe gdy

- 31. żądanie rejestracji w teście zwraca `429`.
- Obce origin nie dostaje pozwolenia CORS.
- Dwa powody złego logowania mają ten sam `type`.
- Odpowiedź bramy ma `nosniff` i `X-Frame-Options: DENY`.
- Ciało 2 MB dostaje `413`.
