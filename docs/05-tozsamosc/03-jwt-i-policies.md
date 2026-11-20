# 03. JWT i polityki

Poprzedni numer to [OpenIddict](02-openiddict.md). Następny to [audyt](04-audyt.md).

Ten numer włącza walidację JWT w bramie i w Learning. Polityka `owner` wpuszcza zapis katalogu. Polityka `student` wpuszcza ukończenie lekcji i widoczność `students`. Gość widzi tylko `public`. Flaga `WritesEnabled=false` schodzi z maszyny, bo rolę da się już sprawdzić. Klucz prywatny nie jest kopiowany do bramy.

## Po co

Sama flaga wyłącza zapis dla wszystkich, także dla właściciela. Token z rolą zamyka właściwą granicę: właściciel pisze, student kończy lekcję, gość czyta publiczne. Brama odrzuca zły podpis zanim żądanie dojdzie do usługi. Usługa i tak sprawdza podpis drugi raz. Jedna warstwa da się pominąć złą trasą. Dwie warstwy nie polegają na tym, że nikt nie pominie bramy w sieci compose.

## Dlaczego ta technologia

### `JwtBearer` i JWKS

`Microsoft.AspNetCore.Authentication.JwtBearer` bierze klucze z adresu odkrytego w konfiguracji, czyli z Identity. Problem, który zamyka, to współdzielony sekret HMAC w każdej usłudze: kto umie sprawdzić, ten umie podpisać. Odpada wklejenie PEM prywatnego do appsettings bramy. Koszt: usługi muszą widzieć Identity w sieci wewnętrznej. Widzą. Nie widzą jej z internetu osobną nazwą. Ruch idzie `http://identity` w compose.

Parametry walidacji: issuer, audience `jjdevhub`, czas życia, podpis. `ClockSkew` to jedna minuta, nie pięć domyślnych, jeśli test na to pozwala bez flaky. Jedna minuta jest w opcjach jawnie.

### Polityki

`owner` wymaga claimu `role` równego `owner`. `student` wymaga `student`. Nie ma polityki „zalogowany cokolwiek” na zapisie katalogu. Problem, który zamyka, to `RequireAuthenticatedUser` na `POST /courses`, które wpuszcza studenta. Odpada jedna polityka na wszystko. Koszt: każdy endpoint ma nazwaną politykę albo jest jawnie anonimowy. Endpoint bez `AllowAnonymous` i bez polityki nie przechodzi testu, który skanuje mapę endpointów. Test jest w projekcie Learning i w bramie.

### `ICurrentActor`

Port w aplikacji Learning: `Name` to `sub`, `IsOwner`, `IsStudent`. Implementacja czyta `ClaimsPrincipal`. Handler publikacji kursu na początku zwraca `learning.owner.required` i `403`, gdy aktor nie jest właścicielem. Filtr endpointu i tak nie wpuści studenta. Handler nie ufa samemu filtrowi. Problem, który zamyka, to reguła tylko w atrybucie, którą drugi host zapomni. Odpada samo `[Authorize]` bez sprawdzenia w handlerze.

`CompleteLesson` bierze `sub` z aktora. Ciało z `StudentId` z numeru lekcji znika. Student nie podaje, kim jest.

### Widoczność

`GET` lekcji: brak tokenu i `public` to `200`. Brak tokenu i `students` albo `private` to `404`. Token `student` i `students` to `200`. Token `student` i `private` to `404`. Token `owner` widzi wszystkie. Ten sam `type` `learning.lesson.not_found` dla braku i dla ukrycia przed tą rolą.

### Brama

YARP uwierzytelnia, gdy nagłówek `Authorization` jest. Gdy go nie ma, puszcza żądanie dalej na ścieżki `GET` publiczne. Na ścieżki zapisu brak tokenu brama zamienia na `401`. Usługa powtórzy odmowę, jeśli brama przepuści. Ścieżki zapisu są listą prefiksów w konfiguracji bramy: `POST` i `PUT` pod `/learning`, poza `POST /learning/lessons/*/completion`, które wymaga `student` i które i tak oceni usługa. Brama nie musi znać każdej roli. Musi odrzucić zły podpis zawsze, także na `GET`. Zły podpis to `401`, nie „traktuj jak gościa”. Inaczej ktoś z uszkodzonym tokenem zobaczy treść publiczną i pomyśli, że jest zalogowany. Klient przy `401` czyści sesję.

### Zapis na maszynie

`WritesEnabled` przestaje być wyłącznikiem globalnym. Znika albo zostaje ignorowane. Źródłem prawdy jest polityka. Deploy Learning i Identity jest warunkiem włączenia tras zapisu. Jeśli Identity nie odpowiada na JWKS, walidacja pada i zapis dostaje `401`, nie otwarte drzwi. To jest właściwy kierunek awarii.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| audience | `jjdevhub`, odbiorca tokenu |
| polityka | nazwany zestaw wymagań na claim |
| `ICurrentActor` | port, `sub` i rola, bez `HttpContext` w handlerze |
| JWKS | jedyne miejsce, z którego usługa bierze klucz publiczny |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/service-defaults` albo `Identity.Contracts` | stałe nazw polityk `owner` i `student` |
| `Learning.Api/Auth/LearningAuth.cs` | `AddJwtBearer` i polityki |
| `Learning.Application/Ports/ICurrentActor.cs` | port |
| `Learning.Api/Auth/HttpCurrentActor.cs` | implementacja |
| `src/gateway/JJDevHub.Gateway/Auth/GatewayAuth.cs` | walidacja podpisu |
| `tests/Learning.Api.Tests/AuthorizationTests.cs` | role na endpoincie |

Nazwy polityk żyją w jednym projekcie `JJDevHub.ServiceDefaults` albo w małym `src/shared/JJDevHub.Auth`, jeśli ServiceDefaults nie powinno znać ról produktu. ServiceDefaults jest o hoście, nie o rolach. Dlatego stałe są w `src/shared/JJDevHub.Auth`, bez EF. To jest druga biblioteka współdzielona i ma dwóch konsumentów od razu: bramę i Learning. Powstaje teraz, nie wcześniej.

## Plan

### 1. Walidacja

Authority to adres Identity w sieci wewnętrznej. `RequireHttpsMetadata` jest `false` w tej sieci, bo ruch między kontenerami jest HTTP. Z internetu i tak wchodzi się tylko na bramę, po HTTPS tunelu. Audience `jjdevhub`.

### 2. Endpointy Learning

`POST` i `PUT` katalogu: polityka `owner`. `POST .../completion`: polityka `student`. `GET`: anonimowy, a widoczność w zapytaniu odczytu dostaje zakres z aktora, jeśli token jest. Brak tokenu to zakres `Public`.

### 3. Usunięcie flagi

`Learning:WritesEnabled` znika z konfiguracji i z testów. Testy, które liczyły na `404` z flagi, liczą teraz na `401` bez tokenu.

### 4. Test bramy

Żądanie z tokenem podpisanym innym kluczem dostaje `401` i nie dochodzi do Learning. Test podstawia dwa JWKS.

### Co jest do czego

| Rola | Zapis katalogu | Ukończenie | Odczyt `students` | Odczyt `private` |
| --- | --- | --- | --- | --- |
| brak | `401` | `401` | `404` | `404` |
| `student` | `403` | `200` | `200` | `404` |
| `owner` | `200` | `403` | `200` | `200` |

Właściciel nie kończy lekcji jako student. Nie ma tej potrzeby w produkcie. Jeśli testowy właściciel chce ukończyć, zakłada konto studenta. Nie poszerzamy polityki ukończenia „dla wygody”.

## Testy

Nazwa: `Student_cannot_publish_course`.

| Arrange | Act | Assert |
| --- | --- | --- |
| token `student` | `POST /courses` | `403` |
| token `owner` | `POST /courses` | nie `401` i nie `403`; dalej walidacja ciała |

Nazwa: `Tampered_token_is_rejected_at_gateway`.

```bash
dotnet test tests/Learning.Api.Tests tests/JJDevHub.Gateway.Tests --nologo --filter "FullyQualifiedName~Student_cannot_publish|FullyQualifiedName~Tampered_token"
```

## Checkpoint

```bash
aspire run
```

Token właściciela z przepływu PKCE publikuje technologię przez bramę. Token studenta dostaje `403`. Gość czyta lekcję publiczną i dostaje `404` na prywatną. `WritesEnabled` nie występuje w `appsettings.json`.

## Umiesz wyjaśnić

Dlaczego usługa sprawdza podpis, skoro sprawdza go brama?

Bo w sieci compose da się zawołać Learning z pominięciem bramy. Drugie sprawdzenie jest tanie. Jedno sprawdzenie jest założeniem, że nikt nie pomyli portu.

Dlaczego zły podpis nie jest traktowany jak gość?

Bo klient z zepsutym tokenem pokazałby treść publiczną i schował błąd sesji. `401` każe wylogować. Gość w ogóle nie przysyła nagłówka.

Dlaczego HMAC odpada?

Bo sekret sprawdzający jest sekretem podpisującym. Każda usługa mogłaby wystawić token `owner`. Klucz prywatny zostaje w Identity. Reszta ma klucz publiczny z JWKS.

Czemu `student` nie widzi `private` nawet jako `404` inne niż brak?

Widzi `404` z tym samym kodem co brak. Nie ma osobnego kodu „to istnieje, ale jest prywatne”.

Dlaczego właściciel nie ma polityki ukończenia?

Bo ukończenie jest stanem studenta. Właściciel nie jest studentem swojego kursu w tym produkcie. Mieszanie ról w jednym koncie skończy się `if` w każdym handlerze.

## Zasady

- Polityka zapisu katalogu to `owner`.
- Identyfikator studenta bierze się z `sub`, nie z ciała.
- Klucz prywatny nie wchodzi do bramy ani do Learning.
- Zły podpis to `401` także na `GET`.
- `WritesEnabled` nie jest już wyłącznikiem.
- Handler publikacji sprawdza aktora sam.

## Poza zakresem

- Ekrany logowania są we frontcie.
- Audyt „kto zmienił lekcję” jest w [04-audyt.md](04-audyt.md).
- Rejestracja z zaproszenia jest w [05-saga-rejestracji.md](05-saga-rejestracji.md).
- Twardnienie nagłówków jest w [06-twardnienie-api.md](06-twardnienie-api.md).

## Źródła

- [JWT bearer](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication). Wiążące są authority, audience i walidacja czasu. Dotyczy Learning i bramy. Ze źródła nie bierzemy wyłączenia walidacji podpisu w Development.
- [Authorization policies](https://learn.microsoft.com/aspnet/core/security/authorization/policies). Wiążące są nazwane polityki. Dotyczy `owner` i `student`. Ze źródła nie bierzemy samego `RequireAuthenticatedUser` na zapisie.
- [RFC 7519](https://www.rfc-editor.org/rfc/rfc7519). Wiążące są `sub`, `exp` i podpis. Dotyczy odrzucenia tokenu bez nich. Ze źródła nie bierzemy roli wpisanej przez klienta poza serwerem, który zna wiersz konta.

## Gotowe gdy

- Student dostaje `403` na publikacji kursu, właściciel przechodzi dalej.
- Zły podpis pada na bramie.
- Ukończenie nie czyta `StudentId` z ciała.
- Gość nie rozróżnia po kodzie błędu braku lekcji i lekcji prywatnej.
- W drzewie nie ma `WritesEnabled`.
