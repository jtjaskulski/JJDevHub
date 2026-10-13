# Identity

Obecne API z [src/api](../../src/api) staje się usługą `Identity.API`. Logowanie zostaje na ASP.NET Identity. Token podpisuje klucz RSA, a klucz publiczny jest wystawiony jako JWKS, więc pozostałe usługi walidują token bez wspólnego sekretu. Rejestracja zapisuje użytkownika i `UserRegisteredIntegrationEvent` w jednej transakcji, przez outbox z [12-outbox-inbox.md](12-outbox-inbox.md). Konsumentem jest Users w [17-uzytkownicy-i-powiadomienia.md](17-uzytkownicy-i-powiadomienia.md). AppHost z [14-aspire.md](14-aspire.md) dostaje nowy projekt w miejsce `api`.

## Po co

W eShop `Identity.API` jest osobną usługą, a reszta tylko sprawdza tokeny. Dziś klucz JWT jest symetryczny (`JWT_KEY`). Żeby News i Users mogły zwalidować token, musiałyby znać ten sam sekret, a wtedy mogłyby też tokeny wystawiać. Para kluczy RSA rozdziela role: Identity ma klucz prywatny i podpisuje, reszta pobiera publiczny z `/.well-known/jwks.json` i tylko sprawdza.

Wcześniejsza wersja tej specyfikacji tworzyła agregat użytkownika drugim wywołaniem po `UserManager.CreateAsync`. Gdy drugie wywołanie padało, konto istniało bez profilu. Teraz `CreateAsync` i wiersz outboxa wchodzą jedną transakcją. Profil zbuduje Users ze zdarzenia, a jeśli Kafka akurat leży, dispatcher wyśle zdarzenie później.

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
| `src/aspire/JJDevHub.AppHost/AppHost.cs` | zasób `identity-api` w miejsce `api` |
| `tests/Identity.API.Tests/` | przeniesiony `tests/JJDevHub.Api.Tests` |

## Tutorial

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

Na VM klucz leży w `/etc/jjdevhub/identity-signing.pem` (`chmod 600`, właściciel z grupy `docker`), poza repozytorium. Generowanie: `openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out identity-signing.pem`. `JWT_KEY` znika z [infra/docker/.env.example](../../infra/docker/.env.example), a dochodzi `JWT_SIGNING_KEY_PATH`. Po wdrożeniu stare tokeny HS256 przestają działać i użytkownicy logują się ponownie.

`TokenService` podpisuje `SigningCredentials(new RsaSecurityKey(rsa) { KeyId = kid }, SecurityAlgorithms.RsaSha256)`. Issuer i audience zostają z obecnych `JwtOptions`. Własna walidacja tokenu w Identity używa tego samego klucza publicznego.

### 4. JWKS

`JwksEndpoints.MapJwks(this WebApplication app)`:

- `GET /.well-known/openid-configuration` → `{ "issuer": "...", "jwks_uri": "<baza>/.well-known/jwks.json" }`. Baza adresu bierze się z `Jwt:PublicUrl`, a w dev z adresu żądania,
- `GET /.well-known/jwks.json` → `{ "keys": [ { "kty": "RSA", "use": "sig", "alg": "RS256", "kid": "...", "n": "...", "e": "..." } ] }`, po jednym wpisie na klucz z providera,
- oba anonimowe, `Cache-Control: public, max-age=300`.

To minimalny dokument discovery. `JwtBearer` w innych usługach czyta z niego `jwks_uri` i sam odświeża klucze.

### 5. Walidacja w innych usługach

`AddDefaultAuthentication(this IHostApplicationBuilder builder)` w ServiceDefaults, wzorem eShop:

- sekcja `Identity` z `Url` i `Audience`; brak `Url` → metoda nic nie rejestruje (usługa bez HTTP, np. worker),
- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer`: `Authority = Identity:Url`, `Audience`, `RequireHttpsMetadata` tylko poza `Development`, `MapInboundClaims = false`, `NameClaimType = "sub"`,
- `AddAuthorization()`.

AppHost przekazuje adres jako `Identity__Url` z `identityApi.GetEndpoint("http")`. ServiceDefaults dostaje pakiet `Microsoft.AspNetCore.Authentication.JwtBearer` (jest już w repo).

### 6. Rejestracja z outboxem

```csharp
namespace JJDevHub.Identity.Contracts;

[EventName("identity.integration.user-registered.v1")]
public sealed record UserRegisteredIntegrationEvent(
    Guid EventId, DateTimeOffset OccurredOn, string IdentityGuid, string Email) : IIntegrationEvent
{
    [JsonIgnore]
    public string PartitionKey => IdentityGuid;
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

`signingKeyPath` to parametr Aspire. W dev jest pusty i działa klucz w pamięci. `ResourceNames` dostaje `IdentityApi = "identity-api"`.

### Co jest do czego

- `Identity.API` — jedyna usługa, która zna hasła i wystawia tokeny. Reszta zna tylko `IdentityGuid`.
- `SigningKeyProvider` — klucz prywatny z pliku poza repo i jego `kid`. Rotacja bez przestoju dzięki drugiemu kluczowi publicznemu.
- `JwksEndpoints` — publiczna część klucza dla innych usług. Bez niej każda usługa musiałaby znać sekret.
- `AddDefaultAuthentication` — trzy linie konfiguracji, które każda usługa HTTP dostaje z ServiceDefaults.
- `UserRegisteredIntegrationEvent` — fakt „powstało konto” dla reszty systemu. Bez hasła i bez skrótu hasła.
- transakcja w `register` — konto i zdarzenie powstają razem albo wcale.
- `Identity.Contracts` — kontrakt, który referencjonuje Users. Nie ciągnie ASP.NET Identity.

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

`dotnet test tests/Identity.API.Tests` i `dotnet test tests/JJDevHub.AppHost.Tests`.

## Zasady

- Klucz prywatny nie trafia do repozytorium, obrazu ani zmiennych workflowu. Leży w pliku na hoście.
- Tylko Identity podpisuje tokeny. Inne usługi mają wyłącznie klucz publiczny z JWKS.
- Rejestracja zapisuje konto i outbox w jednej transakcji.
- Zdarzenie integracyjne nie niesie hasła ani jego skrótu.
- `IdentityGuid` to `IdentityUser.Id`, napis w kształcie `Guid`.

## Poza zakresem

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

## Gotowe gdy

- `src/api` nie istnieje, a `Identity.API` buduje się i startuje z AppHost jako `identity-api`.
- Token ma `alg` `RS256` i `kid`, a klucz z `/.well-known/jwks.json` go waliduje.
- Rejestracja zostawia `AspNetUsers` i `OutboxMessages` z tego samego commitu. Nieudana rejestracja nie zostawia żadnego.
- `JWT_KEY` zniknął z `.env.example`, a klucz prywatny jest tylko w pliku na hoście.
- `dotnet test tests/Identity.API.Tests` i `dotnet test tests/JJDevHub.AppHost.Tests` kończą się kodem 0.
