# Identity

Obecne API z [src/api](../../src/api) staje się usługą `Identity.API`. Logowanie zostaje na ASP.NET Identity. Token podpisuje klucz RSA, a klucz publiczny jest wystawiony jako JWKS, więc pozostałe usługi walidują token bez wspólnego sekretu. Rejestracja zapisuje użytkownika i `UserRegisteredIntegrationEvent` w jednej transakcji, przez outbox z [12-outbox-inbox.md](12-outbox-inbox.md). Konsumentem jest Users w [17-uzytkownicy-i-powiadomienia.md](17-uzytkownicy-i-powiadomienia.md). Token niesie rolę: `owner` dla autora treści i `student` dla osoby, która przyszła z kodem zaproszenia. Polityki `Owner` i `Student` z ServiceDefaults czytają tę rolę w każdej usłudze. AppHost z [14-aspire.md](14-aspire.md) dostaje nowy projekt w miejsce `api`.

## Po co

W eShop `Identity.API` jest osobną usługą, a reszta tylko sprawdza tokeny. Dziś klucz JWT jest symetryczny (`JWT_KEY`). Żeby News i Users mogły zwalidować token, musiałyby znać ten sam sekret, a wtedy mogłyby też tokeny wystawiać. Para kluczy RSA rozdziela role: Identity ma klucz prywatny i podpisuje, reszta pobiera publiczny z `/.well-known/jwks.json` i tylko sprawdza.

Wcześniejsza wersja tej specyfikacji tworzyła agregat użytkownika drugim wywołaniem po `UserManager.CreateAsync`. Gdy drugie wywołanie padało, konto istniało bez profilu. Teraz `CreateAsync` i wiersz outboxa wchodzą jedną transakcją. Profil zbuduje Users ze zdarzenia, a jeśli Kafka akurat leży, dispatcher wyśle zdarzenie później.

Usługi treści z [19-learning.md](19-learning.md), [21-notatki.md](21-notatki.md) i [22-cv.md](22-cv.md) potrzebują trzech poziomów dostępu: gość czyta treść publiczną, student czyta też materiały dla studentów, a owner pisze wszystko i ma prywatną bazę wiedzy. Rola jest faktem o koncie, więc należy do Identity i jedzie w tokenie. Usługa treści nie pyta nikogo o uprawnienia, tylko czyta claim.

## Dlaczego ta technologia

### ASP.NET Core Identity zostaje

Identity już działa w projekcie: hashowanie haseł (PBKDF2 z solą), blokada po nieudanych próbach, tabele `AspNetUsers`, migracja. Przepisanie tego na własne to proszenie się o błąd bezpieczeństwa. Identity jest częścią platformy, darmowe i utrzymywane przez Microsoft.

### Własne tokeny JWT zamiast serwera OIDC

Pełny serwer OpenID Connect daje przepływ authorization code z PKCE, odświeżanie tokenów, zgody, logowanie zewnętrzne i standardowy discovery. Opcje:

- **Keycloak** (Apache 2.0) — kompletny serwer tożsamości w Javie. Projekt świadomie trzyma go poza drzewem: to osobny, ciężki proces z własną bazą i panelem.
- **Duende IdentityServer** — płatna licencja dla firm powyżej progu przychodu, odpada według reguły licencji.
- **OpenIddict** (Apache 2.0) — darmowy serwer OIDC jako biblioteka ASP.NET Core, na EF Core. Najlepszy kandydat, gdy przyjdzie potrzeba logowania zewnętrznego albo aplikacji mobilnej z PKCE.

Dziś mamy jednego klienta (web) i logowanie hasłem. Własny endpoint wydający JWT plus JWKS to kilkadziesiąt linii, które student rozumie w całości. Ścieżka wyjścia jest prosta: OpenIddict wystawia ten sam `/.well-known/openid-configuration` i `jwks.json`, więc pozostałe usługi nie zauważą zmiany.

### RS256 i JWKS zamiast wspólnego sekretu HS256

Przy HS256 ten sam klucz podpisuje i weryfikuje. Każda usługa, która sprawdza tokeny, mogłaby je też wystawiać, a wyciek klucza z dowolnej usługi kompromituje wszystkie. RS256 używa pary kluczy: prywatny tylko w Identity (plik PEM podany jako sekret Compose), publiczny dla wszystkich pod `/.well-known/jwks.json`. `JwtBearer` w ASP.NET Core umie pobrać JWKS sam z `Authority` i odświeżać go, więc usługi nie mają w konfiguracji żadnego sekretu. Rotacja: nowy klucz podpisuje, stary (`PreviousSigningKeyPath`) zostaje w JWKS, aż wygasną wydane nim tokeny.

ES256 (krzywe eliptyczne) dałby krótsze podpisy. RS256 wygrywa kompatybilnością: obsługuje go każda biblioteka JWT i każdy gateway.

### Outbox w transakcji z `UserManager`

`UserManager.CreateAsync` zapisuje przez `AppDbContext`. Outbox z [12-outbox-inbox.md](12-outbox-inbox.md) używa tego samego kontekstu. Jedna transakcja obejmuje oba zapisy: albo jest konto i zdarzenie, albo nic. To naprawia stary błąd, gdzie profil tworzył się drugim, niezależnym wywołaniem.

### Role ASP.NET Identity w tokenie

`IdentityDbContext` ma tabele `AspNetRoles` i `AspNetUserRoles` od pierwszej migracji. Wystarczy `AddRoles<IdentityRole>()` przy `AddIdentityCore`, a `UserManager.GetRolesAsync` daje listę ról do tokenu. Claim `role` jest standardowy w JWT i `JwtBearer` mapuje go na `ClaimsPrincipal.IsInRole` przez `RoleClaimType`.

Alternatywy:

- **Uprawnienia jako osobne claimy** (`content:write`, `notes:read-students`) — drobniejsze, ale przy dwóch rolach to lista, którą trzeba utrzymywać bez żadnej decyzji, którą by umożliwiała.
- **Autoryzacja relacyjna** (OpenFGA, SpiceDB, model Zanzibar) — odpowiada na pytanie „czy ten student należy do tego wykładu”. To przyszła potrzeba zapisów na kursy, a nie dzisiejsza. Osobny serwer i osobny model danych.
- **Role w usłudze Users** — Users nie wystawia tokenu, więc każda usługa treści musiałaby pytać Users przy każdym żądaniu albo trzymać kopię ról z szyny.

Koszt: rola w tokenie żyje tyle, co token. Odebranie roli `student` działa od następnego logowania, najpóźniej po `Jwt:ExpiryMinutes`. Przy materiałach dydaktycznych to akceptowalne.

### Kody zaproszeń dla studentów

Prowadzący ma grupę na zajęciach. Nadawanie roli trzydziestu osobom po kolei w intranecie to trzydzieści kliknięć i trzydzieści adresów do przepisania. Kod zaproszenia z datą ważności i limitem użyć prowadzący pokazuje na slajdzie, a student wpisuje go przy rejestracji albo w profilu.

Kod to 10 znaków z alfabetu Crockford Base32 (bez `I`, `L`, `O`, `U`), losowany przez `RandomNumberGenerator`. W bazie leży jego SHA-256, a nie sam kod. Wyciek bazy nie daje aktywnych kodów. Kod widać raz, w odpowiedzi na utworzenie. Limit użyć pilnuje jedno `UPDATE ... SET "UsedCount" = "UsedCount" + 1 WHERE "UsedCount" < "MaxUses"` przez `ExecuteUpdateAsync` w transakcji rejestracji, więc dwie równoczesne rejestracje nie przekroczą limitu.

Alternatywy:

- **Domena e-maila uczelni** (`@student.uczelnia.pl`) — każdy student uczelni, nie tylko z grupy. Wymaga potwierdzenia e-maila, którego dziś nie ma.
- **Logowanie przez system uczelni** (SAML, OIDC) — właściwa droga przy wdrożeniu instytucjonalnym, poza projektem.
- **Ręczne nadanie roli** — zostaje jako uzupełnienie dla pojedynczych osób.

## Klasy i ścieżki

| Plik | Odpowiedzialność |
| --- | --- |
| `src/services/Identity/Identity.API/` | przeniesiony `src/api` (`git mv`), projekt `Identity.API.csproj` |
| `src/services/Identity/Identity.API/Auth/SigningKeyProvider.cs` | ładuje klucz RSA z PEM, `kid` |
| `src/services/Identity/Identity.API/Auth/TokenService.cs` | podpis `RS256` zamiast `HS256` |
| `src/services/Identity/Identity.API/Auth/JwksEndpoints.cs` | `/.well-known/openid-configuration`, `/.well-known/jwks.json` |
| `src/services/Identity/Identity.API/Auth/AuthEndpoints.cs` | rejestracja w transakcji z outboxem |
| `src/services/Identity/Identity.API/Data/AppDbContext.cs` | `ApplyIntegrationEventLog` |
| `src/services/Identity/Identity.API/Data/Migrations/` | dochodzi `IdentityOutbox` |
| `src/services/Identity/Identity.Contracts/UserRegisteredIntegrationEvent.cs` | kontrakt na `identity.integration` |
| `src/aspire/JJDevHub.ServiceDefaults/AuthenticationExtensions.cs` | `AddDefaultAuthentication()` dla usług-konsumentów tokenu |
| `src/aspire/JJDevHub.ServiceDefaults/Roles.cs` | stałe `owner`, `student` |
| `src/aspire/JJDevHub.ServiceDefaults/Policies.cs` | nazwy polityk `Owner`, `Student` |
| `src/services/Identity/Identity.API/Auth/OwnerSeeder.cs` | role w bazie, `owner` dla `Identity:OwnerEmail` przy starcie |
| `src/services/Identity/Identity.API/Invites/StudentInvite.cs` | encja: skrót kodu, etykieta, ważność, limit, licznik |
| `src/services/Identity/Identity.API/Invites/InviteCode.cs` | losowanie kodu i SHA-256 |
| `src/services/Identity/Identity.API/Invites/InviteEndpoints.cs` | tworzenie, lista, unieważnienie, wykorzystanie przez zalogowanego |
| `src/services/Identity/Identity.API/Admin/UserAdminEndpoints.cs` | lista studentów, nadanie i odebranie roli |
| `src/services/Identity/Identity.API/Data/Migrations/` | dochodzi `IdentityRolesAndInvites` |
| `src/aspire/JJDevHub.AppHost/AppHost.cs` | zasób `identity-api` w miejsce `api` |
| `tests/Identity.API.Tests/` | przeniesiony `tests/JJDevHub.Api.Tests` |

## Plan

### 1. Przeniesienie

```bash
git mv src/api src/services/Identity/Identity.API
git mv src/services/Identity/Identity.API/JJDevHub.Api.csproj src/services/Identity/Identity.API/Identity.API.csproj
git mv tests/JJDevHub.Api.Tests tests/Identity.API.Tests
git mv tests/Identity.API.Tests/JJDevHub.Api.Tests.csproj tests/Identity.API.Tests/Identity.API.Tests.csproj
dotnet new classlib -n Identity.Contracts -o src/services/Identity/Identity.Contracts -f net11.0
dotnet sln add src/services/Identity/Identity.Contracts
dotnet add src/services/Identity/Identity.Contracts reference src/building-blocks/JJDevHub.EventBus
dotnet add src/services/Identity/Identity.API reference src/services/Identity/Identity.Contracts src/building-blocks/JJDevHub.IntegrationEventLog src/building-blocks/JJDevHub.EventBus.Kafka
```

Wpisy w [JJDevHub.sln](../../JJDevHub.sln) idą za nowymi ścieżkami. `RootNamespace` zostaje `JJDevHub.Api`, żeby nie przepisywać namespace'ów w tym numerze. Zmiana na `JJDevHub.Identity.Api` to osobny, mechaniczny commit.

Ścieżki do poprawienia w tym samym numerze:

- [infra/docker/Dockerfile](../../infra/docker/Dockerfile): `src/api` → `src/services/Identity/Identity.API`,
- [.github/workflows/api.yml](../../.github/workflows/api.yml): filtr `paths` i ścieżki projektów; pełny podział CI jest w [18-ci-uslug.md](18-ci-uslug.md),
- [src/Clients/web](../../src/Clients/web): proxy Vite na `identity-api` do czasu gatewaya z `16`.

### 2. Baza

Baza fizycznie zostaje `jjdevhub`, bo na VM leżą w niej konta. Zasób Aspire nazywa się `identity`:

```csharp
var identityDb = postgres.AddDatabase(ResourceNames.Identity, "jjdevhub");
```

`Program.cs` czyta `GetConnectionString("identity")` zamiast `DefaultConnection`. W `appsettings.json` klucz zmienia nazwę. Migracja danych nie jest potrzebna.

### 3. Klucz podpisu

`SigningKeyProvider` jest singletonem. Czyta `Jwt:SigningKeyPath` (PEM z kluczem prywatnym RSA 2048):

- plik jest → `RSA.ImportFromPem`, `kid` to pierwsze 16 znaków SHA-256 z klucza publicznego w Base64Url,
- pliku nie ma i środowisko to `Development` → klucz generowany w pamięci, log `Warning`; po restarcie stare tokeny tracą ważność,
- pliku nie ma poza `Development` → `InvalidOperationException` przy starcie.

Opcjonalne `Jwt:PreviousSigningKeyPath` dodaje do JWKS drugi klucz publiczny bez podpisywania nim. Rotacja: nowy klucz w `SigningKeyPath`, stary w `PreviousSigningKeyPath` na czas życia tokenu, potem usunięcie.

Na VM klucz przychodzi jako sekret Compose `/run/secrets/identity-signing.pem`, odszyfrowany z `infra/secrets/identity-signing.sops.pem` ([docs/CICD/12-sekrety.md](../CICD/12-sekrety.md), [18-ci-uslug.md](18-ci-uslug.md)). Jawna kopia nie leży na dysku VM ani w repozytorium. Generowanie i zaszyfrowanie: `openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 | sops encrypt --input-type binary --output-type binary --filename-override infra/secrets/identity-signing.sops.pem /dev/stdin > infra/secrets/identity-signing.sops.pem`. `JWT_KEY` znika z [infra/docker/.env.example](../../infra/docker/.env.example), a dochodzi `JWT_SIGNING_KEY_PATH`. Po wdrożeniu stare tokeny HS256 przestają działać i użytkownicy logują się ponownie.

`TokenService` podpisuje `SigningCredentials(new RsaSecurityKey(rsa) { KeyId = kid }, SecurityAlgorithms.RsaSha256)`. Issuer i audience zostają z obecnych `JwtOptions`. Własna walidacja tokenu w Identity używa tego samego klucza publicznego.

### 4. JWKS

`JwksEndpoints.MapJwks(this WebApplication app)`:

- `GET /.well-known/openid-configuration` → `{ "issuer": "...", "jwks_uri": "<baza>/.well-known/jwks.json" }`. Baza adresu bierze się z `Jwt:PublicUrl`, a w dev z adresu żądania,
- `GET /.well-known/jwks.json` → `{ "keys": [ { "kty": "RSA", "use": "sig", "alg": "RS256", "kid": "...", "n": "...", "e": "..." } ] }`, po jednym wpisie na klucz z providera,
- oba anonimowe, `Cache-Control: public, max-age=300`.

To minimalny dokument discovery. `JwtBearer` w innych usługach czyta z niego `jwks_uri` i sam odświeża klucze.

### 5. Walidacja w innych usługach

`AddDefaultAuthentication(this IHostApplicationBuilder builder)` w ServiceDefaults, wzorem eShop:

- sekcja `Identity` z `Url` i `Audience`; brak `Url` → metoda nic nie rejestruje (usługa bez chronionych endpointów, np. worker),
- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer`: `Authority = Identity:Url`, `Audience`, `RequireHttpsMetadata` tylko poza `Development`, `MapInboundClaims = false`, `NameClaimType = "sub"`, `RoleClaimType = "role"`,
- `AddAuthorizationBuilder()` z politykami z kroku 10.

AppHost przekazuje adres jako `Identity__Url` z `identityApi.GetEndpoint("http")`. ServiceDefaults dostaje pakiet `Microsoft.AspNetCore.Authentication.JwtBearer` (jest już w repo).

### 6. Rejestracja z outboxem

```csharp
namespace JJDevHub.Identity.Contracts
{
    [EventName("identity.integration.user-registered.v1")]
    public sealed record UserRegisteredIntegrationEvent(
        Guid EventId, DateTimeOffset OccurredOn, string IdentityGuid, string Email) : IIntegrationEvent
    {
        [JsonIgnore]
        public string PartitionKey => IdentityGuid;
    }
}
```

`AppDbContext.OnModelCreating` woła `base.OnModelCreating` i `ApplyIntegrationEventLog()`. Migracja `IdentityOutbox` dodaje `OutboxMessages` i `InboxMessages`.

Endpoint `register` w [AuthEndpoints.cs](../../src/api/Auth/AuthEndpoints.cs):

- `await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken)`,
- `userManager.CreateAsync(user, password)`; porażka → `ValidationProblem`, transakcja wycofuje się przy `Dispose`,
- `outbox.AddAsync(new UserRegisteredIntegrationEvent(Guid.NewGuid(), time.GetUtcNow(), user.Id, user.Email), cancellationToken)`,
- `transaction.CommitAsync(cancellationToken)`, potem token jak dziś.

`UserManager` i outbox używają tego samego scoped `AppDbContext`, więc tego samego połączenia i tej samej transakcji.

Skład w `Program.cs`:

```csharp
builder.Services.AddIntegrationEventLog<AppDbContext>(typeof(UserRegisteredIntegrationEvent).Assembly);
builder.Services.AddKafkaEventBus(builder.Configuration, options => options.PublishedTopics = ["identity.integration"]);
```

### 7. AppHost

Zasób `api` znika. Dochodzi:

```csharp
var identityApi = builder.AddProject<Projects.Identity_API>(ResourceNames.IdentityApi)
    .WithReference(identityDb).WaitFor(identityDb)
    .WithReference(kafka).WaitFor(kafka)
    .WithEnvironment("Jwt__SigningKeyPath", signingKeyPath);
```

`signingKeyPath` to parametr Aspire. W dev jest pusty i działa klucz w pamięci. `ResourceNames` dostaje `IdentityApi = "identity-api"`. Drugi parametr, `owner-email`, trafia do `Identity__OwnerEmail`.

### 8. Role i owner

`AddIdentityCore<IdentityUser>(...).AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>()`. `Roles.Owner = "owner"` i `Roles.Student = "student"` leżą w ServiceDefaults, bo czytają je wszystkie usługi.

`OwnerSeeder : IHostedService` w `StartAsync`:

- tworzy role `owner` i `student`, jeśli ich nie ma,
- gdy `Identity:OwnerEmail` jest ustawione i konto z tym adresem istnieje, dodaje mu rolę `owner`.

Na VM konto autora już istnieje, więc rolę dostaje przy pierwszym starcie po wdrożeniu, bez ręcznego SQL. Rejestracja na adres z `Identity:OwnerEmail` dostaje rolę `owner` w tej samej transakcji co konto. `OwnerEmail` jest jeden. Drugiego ownera nadaje się w bazie, świadomie.

`TokenService.CreateToken(IdentityUser user, IReadOnlyList<string> roles)` dopisuje po jednym claimie `role` na rolę. Endpointy `register` i `login` biorą role z `UserManager.GetRolesAsync`. `MeResponse` dostaje `Roles`, żeby klient web wiedział, czy pokazać wejście do intranetu.

### 9. Zaproszenia

`StudentInvite` (`sealed`, EF): `Id`, `CodeHash` (unikalny indeks), `Label` (np. „PMAB zaoczni 2026”), `ExpiresOn`, `MaxUses`, `UsedCount`, `RevokedOn`, `CreatedOn`. Migracja `IdentityRolesAndInvites` dodaje tabelę `StudentInvites`.

| Metoda i ścieżka | Polityka | Wynik |
| --- | --- | --- |
| `POST /api/auth/invites` | `Owner` | body `{ label, expiresOn, maxUses }` → `201` z `code` (jedyny raz, kiedy kod jest widoczny) |
| `GET /api/auth/invites` | `Owner` | lista bez kodów: etykieta, ważność, `usedCount`/`maxUses`, stan |
| `DELETE /api/auth/invites/{id}` | `Owner` | `204`, ustawia `RevokedOn` |
| `POST /api/auth/invites/redeem` | zalogowany | body `{ code }` → `200` z nowym tokenem z rolą `student`, `400` przy złym kodzie |

`RegisterRequest` dostaje opcjonalne `InviteCode`. Rejestracja z kodem w jednej transakcji: konto, `UPDATE` licznika z warunkiem, rola `student`, wiersz outboxa. Kod nieznany, wygasły, unieważniony albo wyczerpany daje `400` z błędem pola `inviteCode` i nie zostawia konta. Odpowiedź nie mówi, który z czterech powodów zaszedł.

`InviteCode.Hash(string code)` normalizuje kod (wielkie litery, bez myślników i spacji) przed SHA-256, więc `abcd-efgh-jk` i `ABCDEFGHJK` to ten sam kod.

### 10. Polityki i administracja

`AddDefaultAuthentication` rejestruje polityki:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Owner, policy => policy.RequireRole(Roles.Owner))
    .AddPolicy(Policies.Student, policy => policy.RequireRole(Roles.Student, Roles.Owner));
```

Owner spełnia politykę `Student`, bo widzi wszystko, co student. Usługa treści pisze `RequireAuthorization(Policies.Owner)` na endpointach zapisu, a widoczność `Students` sprawdza przez `IAuthorizationService` w zapytaniu.

`UserAdminEndpoints` (wszystkie z polityką `Owner`):

- `GET /api/auth/users?role=student` → `IdentityGuid`, e-mail, role, data utworzenia,
- `PUT /api/auth/users/{id}/roles/student` → `204`, idempotentnie,
- `DELETE /api/auth/users/{id}/roles/student` → `204`.

Gateway nie potrzebuje nowej trasy, bo wszystko jest pod `/api/auth/`.

### Co jest do czego

- `Identity.API` — jedyna usługa, która zna hasła i wystawia tokeny. Reszta zna tylko `IdentityGuid`.
- `SigningKeyProvider` — klucz prywatny z pliku poza repo i jego `kid`. Rotacja bez przestoju dzięki drugiemu kluczowi publicznemu.
- `JwksEndpoints` — publiczna część klucza dla innych usług. Bez niej każda usługa musiałaby znać sekret.
- `AddDefaultAuthentication` — trzy linie konfiguracji, które każda usługa HTTP dostaje z ServiceDefaults.
- `UserRegisteredIntegrationEvent` — fakt „powstało konto” dla reszty systemu. Bez hasła i bez skrótu hasła.
- transakcja w `register` — konto i zdarzenie powstają razem albo wcale.
- `Identity.Contracts` — kontrakt, który referencjonuje Users. Nie ciągnie ASP.NET Identity.
- claim `role` i polityki `Owner`/`Student` — jedno źródło uprawnień dla wszystkich usług. Usługa treści nie zna haseł ani tabel ról, tylko czyta token.
- `OwnerSeeder` — autor treści dostaje rolę bez ręcznej zmiany w bazie produkcyjnej.
- `StudentInvite` — grupa studentów dostaje dostęp jednym kodem z zajęć. Skrót w bazie zamiast kodu, limit pilnowany w SQL.
- `UserAdminEndpoints` — lista i korekta ról z intranetu ([docs/frontend/intranet/01-dostep-i-trasy.md](../frontend/intranet/01-dostep-i-trasy.md)).

## Testy

Projekt `tests/Identity.API.Tests` (przeniesiony). `ApiFactory` z kontenerem Postgresa podmienia `IEventBus` na `RecordingEventBus`, a `Jwt:SigningKeyPath` na plik PEM wygenerowany w `InitializeAsync`. Kafka nie jest potrzebna. Dispatcher outboxa jest wyłączony opcją, a test woła `OutboxProcessor` wprost.

Klasa `AuthAndHealthTests` zostaje. Dochodzą:

Klasa `RegistrationOutboxTests`.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Register_creates_identity_user` | host z fabryki | `POST /api/auth/register` | `AspNetUsers` ma ten e-mail |
| `Register_writes_outbox_row` | ten sam rejestr | odczyt `OutboxMessages` | jeden wiersz `identity.integration.user-registered.v1` |
| `Outbox_payload_has_no_password` | ten sam rejestr | odczyt `Payload` | brak hasła i pola `passwordHash` |
| `Duplicate_email_writes_no_outbox_row` | konto już istnieje | drugi `register` tym e-mailem | nadal jeden wiersz outboxa |
| `Processor_publishes_registration` | rejestracja | `OutboxProcessor.RunOnceAsync` | `RecordingEventBus` ma kopertę z kluczem równym `IdentityUser.Id` |

Klasa `JwksTests`.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Discovery_points_to_jwks` | host | `GET /.well-known/openid-configuration` | `jwks_uri` kończy się na `/.well-known/jwks.json` |
| `Jwks_exposes_signing_key` | host | `GET /.well-known/jwks.json` | jeden klucz `RSA` z `kid` |
| `Token_validates_with_jwks_key` | token z `login` | walidacja `JsonWebTokenHandler` kluczem z JWKS | `IsValid` |
| `Token_uses_rs256` | token z `login` | odczyt nagłówka | `alg` równe `RS256` |

Klasa `RolesAndInvitesTests`. Fabryka ustawia `Identity:OwnerEmail` na `owner@test.local`.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Owner_email_gets_owner_role` | host | `register` z `owner@test.local` | token ma `role` `owner` |
| `Seeder_grants_owner_to_existing_account` | konto `owner@test.local` bez roli | `OwnerSeeder.StartAsync` | `GetRolesAsync` zawiera `owner` |
| `Plain_registration_has_no_role` | host | `register` bez kodu | token bez claimu `role` |
| `Invite_registration_gets_student_role` | zaproszenie z limitem 2 | `register` z kodem | token ma `role` `student`, `UsedCount` 1 |
| `Invite_code_is_case_and_dash_insensitive` | kod `ABCDEFGHJK` | `register` z `abcd-efgh-jk` | `201` |
| `Expired_invite_returns_400_without_account` | zaproszenie z `ExpiresOn` w przeszłości (`FakeTimeProvider`) | `register` z kodem | `400` z polem `inviteCode`, brak wiersza w `AspNetUsers` |
| `Exhausted_invite_returns_400` | zaproszenie z limitem 1, już użyte | `register` z kodem | `400` |
| `Invite_is_stored_as_hash` | `POST /api/auth/invites` | odczyt `StudentInvites` | `CodeHash` różny od kodu z odpowiedzi, kodu nie ma w tabeli |
| `Student_cannot_create_invite` | token z rolą `student` | `POST /api/auth/invites` | `403` |
| `Owner_grants_student_role` | konto `b` bez roli | `PUT /api/auth/users/{b}/roles/student`, potem `login` jako `b` | token `b` ma `role` `student` |

Klasa `PolicyTests` w `tests/JJDevHub.AppHost.Tests` sprawdza ServiceDefaults bez Identity: host z `AddDefaultAuthentication`, testowy klucz, endpointy z obiema politykami.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Owner_passes_student_policy` | token z `owner` | `GET` na endpoint `Student` | `200` |
| `Student_fails_owner_policy` | token z `student` | `GET` na endpoint `Owner` | `403` |

`dotnet test tests/Identity.API.Tests` i `dotnet test tests/JJDevHub.AppHost.Tests`.

## Zasady

- Klucz prywatny nie trafia do repozytorium, obrazu ani zmiennych workflowu. Leży w pliku na hoście.
- Tylko Identity podpisuje tokeny. Inne usługi mają wyłącznie klucz publiczny z JWKS.
- Rejestracja zapisuje konto i outbox w jednej transakcji.
- Zdarzenie integracyjne nie niesie hasła ani jego skrótu.
- `IdentityGuid` to `IdentityUser.Id`, napis w kształcie `Guid`.
- Rola jedzie w tokenie jako claim `role`. Usługi sprawdzają ją politykami z ServiceDefaults, a nazw ról nie przepisują w kodzie.
- Kod zaproszenia jest widoczny raz. W bazie leży jego SHA-256.
- Nieudane użycie kodu nie mówi, czy kod wygasł, został wyczerpany, unieważniony, czy nie istnieje.

## Poza zakresem

- Zapisy na konkretny wykład (student widzi tylko swoje kursy). Wymaga autoryzacji relacyjnej, a dziś rola `student` daje dostęp do wszystkich materiałów `Students`.
- Potwierdzenie e-maila i reset hasła.
- Pełny OpenID Connect, Duende IdentityServer, Keycloak. Minimalny discovery wystarcza walidacji JWT.
- Refresh tokeny i wylogowanie po stronie serwera.
- Usuwanie konta i `UserDeletedIntegrationEvent`. Ten sam wzór, osobny numer, gdy dojdzie ekran.
- Profil i widok użytkownika — [17-uzytkownicy-i-powiadomienia.md](17-uzytkownicy-i-powiadomienia.md).

## Źródła

- [Securing .NET microservices](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/secure-net-microservices-web-applications/) — usługa tożsamości i tokeny walidowane przez pozostałe usługi.
- [dotnet/eShop — Identity.API](https://github.com/dotnet/eShop/tree/main/src/Identity.API) i [AuthenticationExtensions](https://github.com/dotnet/eShop/blob/main/src/eShop.ServiceDefaults/AuthenticationExtensions.cs) — wzorzec `AddDefaultAuthentication`.
- [RFC 7517: JSON Web Key](https://www.rfc-editor.org/rfc/rfc7517) — format JWKS.
- [OpenID Connect Discovery](https://openid.net/specs/openid-connect-discovery-1_0.html) — `issuer` i `jwks_uri`.
- [Configure JWT bearer authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication) — `Authority`, `Audience`, metadane.
- [Introduction to Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity) — `UserManager` i `IdentityDbContext`.
- [Transactional outbox pattern](https://learn.microsoft.com/en-us/azure/architecture/databases/guide/transactional-outbox-cosmos) — konto i zdarzenie w jednej transakcji.
- [Role-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles) — `AddRoles`, `RequireRole`, `RoleClaimType`.
- [Policy-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies) — `AddAuthorizationBuilder` i polityki nazwane.
- [Crockford Base32](https://www.crockford.com/base32.html) — alfabet bez znaków mylonych przy przepisywaniu ze slajdu.

## Gotowe gdy

- `src/api` nie istnieje, a `Identity.API` buduje się i startuje z AppHost jako `identity-api`.
- Token ma `alg` `RS256` i `kid`, a klucz z `/.well-known/jwks.json` go waliduje.
- Rejestracja zostawia `AspNetUsers` i `OutboxMessages` z tego samego commitu. Nieudana rejestracja nie zostawia żadnego.
- `JWT_KEY` zniknął z `.env.example`, a klucz prywatny jest tylko w pliku na hoście.
- Konto z `Identity:OwnerEmail` ma w tokenie `role` `owner`, a rejestracja z ważnym kodem daje `student`.
- Wygasły albo wyczerpany kod kończy się `400` bez konta w bazie.
- `dotnet test tests/Identity.API.Tests` i `dotnet test tests/JJDevHub.AppHost.Tests` kończą się kodem 0.
