# Użytkownicy i powiadomienia

Dwie usługi, które tylko konsumują zdarzenia. `Users.API` buduje profil użytkownika z `UserRegisteredIntegrationEvent` i wystawia `GET /api/users/me`. `Notifications.Worker` reaguje na nowe konto i nowy artykuł, wysyła powiadomienie i zapisuje, że to zrobił. Oba używają inboxa z [12-outbox-inbox.md](12-outbox-inbox.md) i konsumenta Kafki z [13-kafka.md](13-kafka.md). `tests/JJDevHub.Contracts.Tests` pilnuje, żeby kontrakty z `Identity.Contracts` i `News.Contracts` nie zmieniły się po cichu. Publikujący są w [15-identity.md](15-identity.md) i [16-usluga-news.md](16-usluga-news.md).

## Po co

Te usługi pokazują drugą stronę szyny. Odbiorca nie woła Identity po dane i nie czyta bazy `jjdevhub`. Buduje własny model z faktów, które dostał. Jeśli Users leży przez godzinę, rejestracje dalej działają. Po restarcie konsument dojdzie do bieżącego offsetu.

Users to świadomie zwykły CRUD na EF, bez event sourcingu. eShop robi tak z `Catalog.API`. Profil nie ma historii, którą warto odtwarzać, a jedna warstwa w jednym projekcie wystarcza. Value object `Email` i encja z regułą zostają, bo DDD nie oznacza event sourcingu.

Kontrakt integracyjny to API między zespołami, nawet jeśli zespół jest jeden. Zmiana pola w `UserRegisteredIntegrationEvent` łamie Users bez błędu kompilacji, jeśli Users ma starą wersję paczki. Test snapshotu łapie taką zmianę w PR.

## Dlaczego ta technologia

### Users jako CRUD na EF bez event sourcingu

Profil ma nazwę wyświetlaną i e-mail. Nikt nie zapyta „jak brzmiała nazwa użytkownika trzy zmiany temu”. Event sourcing dałby tu dwa modele, projekcję i ostateczną spójność bez żadnej korzyści. Encja `User` z value objectami `Email` i `DisplayName`, zapisywana przez EF, to DDD bez event sourcingu: reguły są w domenie, stan w tabeli. Jeden projekt zamiast czterech warstw, bo logiki jest mało. eShop robi tak z `Catalog.API`. Ten numer pokazuje, że wybór stylu jest per usługa, a nie per system.

### Users jako osobna usługa od Identity

Identity odpowiada za uwierzytelnienie: hasło, token, blokady. Users za to, kim jest osoba w systemie: nazwa, preferencje, w przyszłości avatar i bio. Gdyby profil leżał w `AspNetUsers`, każda zmiana profilu dotykałaby usługi bezpieczeństwa, a wymiana Identity na Keycloak albo OpenIddict wymagałaby migracji profilu. Rozdzielenie kosztuje ostateczną spójność: przez ułamek sekundy po rejestracji profilu jeszcze nie ma (`404`), a test E2E ponawia odczyt.

### Notifications jako worker z samymi ścieżkami technicznymi

Powiadomienia reagują na zdarzenia i nikt ich nie wywołuje. Logika to handlery i `BackgroundService`, bez endpointów biznesowych. Proces potrzebuje jednak HTTP do dwóch rzeczy: Prometheus ściąga `/metrics` ([docs/CICD/07-prometheus.md](../CICD/07-prometheus.md)), a Compose i Aspire sprawdzają `/health`. Czysty szablon `worker` (`Host.CreateApplicationBuilder`) nie ma Kestrela, więc `MapDefaultEndpoints` i `MapPrometheusScrapingEndpoint` nie miałyby gdzie działać. Dlatego worker startuje z `WebApplication.CreateSlimBuilder`: Kestrel bez MVC, routingu kontrolerów i plików statycznych, z trzema ścieżkami technicznymi. Alternatywa to wypychanie metryk przez OTLP do kolektora, ale na VM kolektora nie ma, a Jaeger metryk nie przyjmuje.

### `INotificationSender` z implementacją logującą

Prawdziwy e-mail wymaga dostawcy (SMTP, Cloudflare Email, SendGrid), sekretów i domeny z DKIM. To osobna decyzja. Interfejs z implementacją, która tylko loguje, pozwala zbudować i przetestować cały przepływ (zdarzenie → inbox → wysyłka → `SentNotifications`) bez dostawcy. Podmiana to jedna rejestracja w DI.

### Testy kontraktów jako snapshoty JSON

Zdarzenie integracyjne to publiczne API usługi. Zmiana nazwy pola w `UserRegisteredIntegrationEvent` łamie Users i Notifications, a kompilator tego nie zobaczy, bo każda usługa deserializuje JSON. Test serializuje przykładowe zdarzenie i porównuje z zatwierdzonym plikiem `Snapshots/{nazwa}.json`. Zmiana kształtu jest widoczna w PR jako zmiana pliku.

Alternatywy:

- **Pact** (PactNet, MIT) — kontrakty sterowane przez konsumenta, z brokerem kontraktów. Potężne przy wielu zespołach. Przy jednym repo i jednym autorze broker to zbędny serwer.
- **Verify** (MIT) — biblioteka snapshotów z narzędziami do porównywania i zatwierdzania. Dozwolona i wygodniejsza przy wielu snapshotach. Przy kilku plikach `File.ReadAllText` i porównanie stringów wystarcza, a przejście na Verify to zamiana jednej metody pomocniczej.

## Klasy i ścieżki

| Plik | Odpowiedzialność |
| --- | --- |
| `src/services/Users/Users.API/Users.API.csproj` | Web SDK, ServiceDefaults, `Identity.Contracts`, `JJDevHub.IntegrationEventLog`, `JJDevHub.EventBus.Kafka` |
| `src/services/Users/Users.API/Domain/Email.cs` | value object e-maila |
| `src/services/Users/Users.API/Domain/DisplayName.cs` | 2–50 znaków, przycięte |
| `src/services/Users/Users.API/Domain/User.cs` | `Entity`, `IAggregateRoot`: `IdentityGuid`, `Email`, `DisplayName` |
| `src/services/Users/Users.API/Data/UsersContext.cs` | baza `users`, `ApplyIntegrationEventLog` |
| `src/services/Users/Users.API/IntegrationEvents/UserRegisteredHandler.cs` | upsert profilu |
| `src/services/Users/Users.API/Apis/UserEndpoints.cs` | `GET` i `PUT /api/users/me` |
| `src/services/Notifications/Notifications.Worker/Notifications.Worker.csproj` | Web SDK (Kestrel tylko na ścieżki techniczne), ServiceDefaults, oba `*.Contracts` |
| `src/services/Notifications/Notifications.Worker/Data/NotificationsContext.cs` | baza `notifications` |
| `src/services/Notifications/Notifications.Worker/Data/SentNotification.cs` | ślad wysyłki |
| `src/services/Notifications/Notifications.Worker/Sending/INotificationSender.cs` | port wysyłki |
| `src/services/Notifications/Notifications.Worker/Sending/LoggingNotificationSender.cs` | wysyłka do logu |
| `src/services/Notifications/Notifications.Worker/IntegrationEvents/WelcomeUserHandler.cs` | powitanie nowego konta |
| `src/services/Notifications/Notifications.Worker/IntegrationEvents/ArticleCreatedHandler.cs` | informacja o nowym artykule |
| `tests/Users.API.Tests/`, `tests/Notifications.Worker.Tests/` | konsumpcja i idempotencja |
| `tests/JJDevHub.Contracts.Tests/` | snapshoty kontraktów |

## Plan

### 1. Projekty

```bash
dotnet new web -n Users.API -o src/services/Users/Users.API -f net11.0
dotnet new web -n Notifications.Worker -o src/services/Notifications/Notifications.Worker -f net11.0
dotnet new xunit -n Users.API.Tests -o tests/Users.API.Tests -f net11.0
dotnet new xunit -n Notifications.Worker.Tests -o tests/Notifications.Worker.Tests -f net11.0
dotnet new xunit -n JJDevHub.Contracts.Tests -o tests/JJDevHub.Contracts.Tests -f net11.0
dotnet sln add src/services/Users/Users.API src/services/Notifications/Notifications.Worker tests/Users.API.Tests tests/Notifications.Worker.Tests tests/JJDevHub.Contracts.Tests
dotnet add src/services/Users/Users.API reference src/aspire/JJDevHub.ServiceDefaults src/services/Identity/Identity.Contracts src/building-blocks/JJDevHub.IntegrationEventLog src/building-blocks/JJDevHub.EventBus.Kafka
dotnet add src/services/Notifications/Notifications.Worker reference src/aspire/JJDevHub.ServiceDefaults src/services/Identity/Identity.Contracts src/services/News/News.Contracts src/building-blocks/JJDevHub.IntegrationEventLog src/building-blocks/JJDevHub.EventBus.Kafka
dotnet add tests/JJDevHub.Contracts.Tests reference src/services/Identity/Identity.Contracts src/services/News/News.Contracts
```

Users referencjonuje `Identity.Contracts`, nie `Identity.API`. Notifications nie zna `News.Domain`.

### 2. Domena Users

`Email : ValueObject` według wzoru `ArticleTitle` z [02-value-object.md](02-value-object.md): `Create(string?)` przycina, zamienia na małe litery, wymaga dokładnie jednego `@` i do 254 znaków.

`DisplayName : ValueObject`: przycięte, od 2 do 50 znaków. `DisplayName.FromEmail(Email)` bierze część przed `@`, a za krótką uzupełnia do `user-xxxx` z pierwszych znaków `IdentityGuid`.

`User : Entity, IAggregateRoot` jest `sealed`, z prywatnym konstruktorem dla EF. `Id` to `Guid` sparsowany z `IdentityGuid`. Metody:

- `static Result<User> Register(string identityGuid, string email)` — `Invalid` dla nie-`Guid` albo złego e-maila,
- `Result<User> Rename(DisplayName name)`,
- `void SyncEmail(Email email)` — nadpisuje e-mail, gdy zdarzenie przyjdzie ponownie.

Bez `AggregateRoot`, bez `Apply`, bez strumienia. Stan zapisuje EF.

### 3. Baza Users

`UsersContext : DbContext`, `DbSet<User> Users`. `Email` i `DisplayName` mapowane `ComplexProperty` albo konwerterem na kolumny `Email` i `DisplayName`. Unikalny indeks na `Email`. `OnModelCreating` woła `ApplyIntegrationEventLog()`. Migracja `UsersInitial`.

### 4. Konsumpcja w Users

`UserRegisteredHandler(UsersContext db) : IIntegrationEventHandler<UserRegisteredIntegrationEvent>`:

- użytkownik o tym `Id` istnieje → `SyncEmail`,
- nie istnieje → `User.Register`; `Invalid` → log `Error` i koniec; złe dane nie zatrzymują partycji,
- `Add`. Zapis robi `InboxReceiver` razem z wpisem inboxa.

Handler jest idempotentny sam z siebie (upsert), a inbox dodatkowo nie wpuści drugi raz tego samego `EventId`.

### 5. Endpointy Users

| Metoda i ścieżka | Autoryzacja | Wynik |
| --- | --- | --- |
| `GET /api/users/me` | `RequireAuthorization()` | `200` z `IdentityGuid`, `Email`, `DisplayName`; `404`, gdy profil jeszcze nie przyszedł |
| `PUT /api/users/me` | `RequireAuthorization()` | body `{ "displayName": "…" }` → `200`, `400` |

Usługa jest mała, więc endpoint woła `UsersContext` wprost, bez mediatora. To ten sam pragmatyzm co w `Catalog.API` eShop. `404` po świeżej rejestracji to spójność ostateczna: klient ponawia.

### 6. Skład Users

```csharp
builder.AddServiceDefaults();
builder.AddDefaultAuthentication();
builder.AddNpgsqlDbContext<UsersContext>(ResourceNames.Users, s => s.DisableRetry = true);
builder.Services
    .AddIntegrationEventLog<UsersContext>(
        typeof(UserRegisteredIntegrationEvent).Assembly,
        options => options.PublishOutbox = false)
    .AddIntegrationEventHandlers(typeof(UserRegisteredHandler).Assembly)
    .AddKafkaConsumer(builder.Configuration, options =>
    {
        options.ConsumerGroup = "users";
        options.SubscribedTopics = ["identity.integration"];
    });
```

`PublishOutbox = false` nie rejestruje `OutboxDispatcher`. Users niczego nie publikuje. Opcja dochodzi do `AddIntegrationEventLog` z [12-outbox-inbox.md](12-outbox-inbox.md) w tym numerze.

### 7. Notifications

`NotificationsContext : DbContext` z `DbSet<SentNotification>` i `ApplyIntegrationEventLog()`. `SentNotification` (`sealed`): `Id`, `EventId`, `Kind` (`welcome`, `article-created`), `Recipient` (`string?`), `CreatedOn`. Unikalny indeks na `EventId` + `Kind`.

```csharp
namespace JJDevHub.Notifications.Worker.Sending;

public interface INotificationSender
{
    Task SendAsync(string kind, string? recipient, string text, CancellationToken cancellationToken);
}
```

`LoggingNotificationSender` zapisuje powiadomienie do logu (`Information`). Prawdziwy kanał (e-mail, webhook Discorda) to druga implementacja tego interfejsu, poza tym numerem.

Handlery:

- `WelcomeUserHandler` — `UserRegisteredIntegrationEvent` → `SendAsync("welcome", email, …)` i `SentNotification`,
- `ArticleCreatedHandler` — `ArticleCreatedIntegrationEvent` → `SendAsync("article-created", null, title)` i `SentNotification`.

Kolejność jest celowa: najpierw wysyłka, potem zapis w tej samej transakcji co inbox. Awaria po wysyłce, a przed commitem, wyśle powiadomienie drugi raz. Dla powiadomienia to akceptowalne. Odwrotna kolejność mogłaby je zgubić.

Skład w `Program.cs` workera: `WebApplication.CreateSlimBuilder(args)`, `AddServiceDefaults`, `AddNpgsqlDbContext<NotificationsContext>` z `DisableRetry = true` (powód w [14-aspire.md](14-aspire.md)), `AddIntegrationEventLog` z oboma zestawami kontraktów i `PublishOutbox = false`, `AddKafkaConsumer` z grupą `notifications` i tematami `identity.integration` oraz `news.integration`. Po `Build()` tylko `app.MapDefaultEndpoints()`: `/health`, `/alive`, `/metrics`. Innych tras worker nie ma.

### 8. Kontrakty pod testem

Projekt `tests/JJDevHub.Contracts.Tests`:

- `ContractCatalog` zbiera wszystkie typy `IIntegrationEvent` z `Identity.Contracts` i `News.Contracts`,
- `ContractSamples` ma dla każdego typu przykładową instancję ze stałymi wartościami (`EventId` = `00000000-0000-0000-0000-000000000001`, stała data),
- snapshot to plik `Snapshots/{event-name}.json` z wynikiem `IntegrationEventSerializer.ToEnvelope(sample).Payload`, sformatowanym z wcięciami.

Zmiana kształtu kontraktu psuje test. Poprawka jest jedna z dwóch: nowa wersja nazwy (`.v2`) obok starej, albo świadoma aktualizacja snapshotu w PR, jeśli pole jest tylko dodane i nullable.

### 9. AppHost i gateway

```csharp
var usersDb = postgres.AddDatabase(ResourceNames.Users, "users");
var notificationsDb = postgres.AddDatabase(ResourceNames.Notifications, "notifications");

var usersApi = builder.AddProject<Projects.Users_API>(ResourceNames.UsersApi)
    .WithReference(usersDb).WaitFor(usersDb)
    .WithReference(kafka).WaitFor(kafka)
    .WithEnvironment("Identity__Url", identityApi.GetEndpoint("http"));

builder.AddProject<Projects.Notifications_Worker>(ResourceNames.NotificationsWorker)
    .WithReference(notificationsDb).WaitFor(notificationsDb)
    .WithReference(kafka).WaitFor(kafka);
```

`ResourceNames` dostaje `UsersApi = "users-api"` i `NotificationsWorker = "notifications-worker"`. Gateway dostaje `WithReference(usersApi)` i trasę `/api/users/{**catch-all}` → klaster `users` (`http://users-api`).

### Co jest do czego

- `Users.API` — właściciel profilu: nazwa wyświetlana, e-mail do kontaktu. Hasło zostaje w Identity.
- `User` — encja z regułami w value objectach, zapisywana przez EF. Przykład DDD bez event sourcingu.
- `UserRegisteredHandler` — tłumaczy fakt z Identity na profil. Upsert plus inbox dają dwie warstwy idempotencji.
- `GET /api/users/me` — profil zalogowanego. `404` tuż po rejestracji to normalny stan przejściowy.
- `Notifications.Worker` — usługa bez endpointów biznesowych i bez publikacji. Tylko reaguje. HTTP ma wyłącznie dla zdrowia i metryk.
- `INotificationSender` — port kanału. Log dziś, e-mail jutro, bez zmiany handlerów.
- `SentNotification` — ślad, co i komu wysłano. Pomaga przy reklamacji „nie dostałem”.
- `JJDevHub.Contracts.Tests` — strażnik kontraktów między usługami.

## Testy

Projekt `tests/Users.API.Tests`. `UsersApiFactory` z kontenerem Postgresa (baza `users`), konsument Kafki wyłączony opcją, testowe tokeny jak w [16-usluga-news.md](16-usluga-news.md). Wiadomości podaje test przez `IIntegrationEventReceiver.ReceiveAsync` z kopertą z `IntegrationEventSerializer`.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Receive_creates_profile` | koperta rejestracji `a` | `ReceiveAsync` | jeden `User` z `IdentityGuid` `a` |
| `Second_receive_keeps_one_profile` | koperta już przyjęta | drugie `ReceiveAsync` | `Duplicate`, jeden `User` |
| `Invalid_email_does_not_throw` | koperta z e-mailem `x` | `ReceiveAsync` | `Handled`, zero `User` |
| `Me_returns_profile` | profil `a` | `GET /api/users/me` z tokenem `a` | `200`, e-mail |
| `Me_without_profile_returns_404` | pusta baza | `GET /api/users/me` | `404` |
| `Rename_too_short_returns_400` | profil `a` | `PUT` z `"x"` | `400` |

Projekt `tests/Notifications.Worker.Tests`. Host z `Host.CreateApplicationBuilder`, kontener Postgresa (baza `notifications`), `RecordingNotificationSender`.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Article_created_sends_once` | koperta artykułu | `ReceiveAsync` | jedno wywołanie nadawcy, jeden `SentNotification` |
| `Duplicate_article_sends_nothing` | koperta już przyjęta | `ReceiveAsync` | nadawca nadal raz |
| `Registration_sends_welcome_to_email` | koperta rejestracji | `ReceiveAsync` | `Recipient` równy e-mailowi |
| `Unknown_event_is_ignored` | koperta obcego typu | `ReceiveAsync` | `Ignored`, nadawca nie wywołany |

Projekt `tests/JJDevHub.Contracts.Tests`, klasa `ContractSnapshotTests`.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Every_contract_has_sample` | katalog typów | porównanie z `ContractSamples` | brak typu bez próbki |
| `Every_name_has_integration_segment_and_version` | katalog typów | odczyt `[EventName]` | wzór `^[a-z]+\.integration\.[a-z-]+\.v\d+$` |
| `Payload_matches_snapshot` | `[Theory]` po typach | `ToEnvelope(sample).Payload` | równe plikowi snapshotu |
| `Registry_builds_without_duplicates` | oba zestawy | `EventTypeRegistry.FromAssemblies` | bez wyjątku |

Klasa `AppHostTests` z [14-aspire.md](14-aspire.md) dostaje test od końca do końca:

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Registration_reaches_users_service` | aplikacja uruchomiona | `POST /api/auth/register` przez gateway, potem `GET /api/users/me` z tokenem, ponawiane do 30 s | `200` z tym e-mailem |

`dotnet test tests/Users.API.Tests`, `dotnet test tests/Notifications.Worker.Tests`, `dotnet test tests/JJDevHub.Contracts.Tests` i `dotnet test tests/JJDevHub.AppHost.Tests`.

## Zasady

- Konsument buduje własny model z faktów. Nie woła HTTP usługi-nadawcy i nie czyta jej bazy.
- Handler zdarzenia jest idempotentny, nawet z inboxem.
- Złe dane w zdarzeniu kończą się logiem, nie wyjątkiem, który blokuje partycję.
- Usługa bez publikacji ma `PublishOutbox = false`.
- Zmiana kontraktu integracyjnego to nowa wersja nazwy albo świadoma zmiana snapshotu w PR.
- `Users.API` nie ma tabeli haseł ani kopii `AspNetUsers`.

## Poza zakresem

- Prawdziwy kanał powiadomień (SMTP, Discord). Druga implementacja `INotificationSender`.
- Preferencje powiadomień użytkownika.
- `UserDeletedIntegrationEvent` i usuwanie profilu.
- Przypisanie autora do profilu w widoku artykułu. Widok News może kiedyś konsumować `identity.integration`, żeby pokazać `DisplayName`.

## Źródła

- [Implementing event-based communication between microservices](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/integration-event-based-microservice-communications) — subskrypcja, handler zdarzenia integracyjnego, idempotencja.
- [Creating a simple data-driven CRUD microservice](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/data-driven-crud-microservice) — `Catalog.API` jako wzór prostej usługi.
- [dotnet/eShop — Catalog.API](https://github.com/dotnet/eShop/tree/main/src/Catalog.API) i [OrderProcessor](https://github.com/dotnet/eShop/tree/main/src/OrderProcessor) — usługa CRUD i worker bez HTTP.
- [Worker services in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers) — szablon `worker` i `BackgroundService`.
- [Consumer-Driven Contracts](https://martinfowler.com/articles/consumerDrivenContracts.html) — Ian Robinson: kontrakt pilnowany testem.
- [Unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices) — test deterministyczny ze stałymi danymi, jak próbki kontraktów.
- [Idempotent Consumer](https://microservices.io/patterns/communication-style/idempotent-consumer.html) — Chris Richardson.

## Gotowe gdy

- Rejestracja przez gateway kończy się profilem w `users` i powitaniem w `notifications`.
- Nowy artykuł zostawia jeden `SentNotification` z rodzajem `article-created`.
- Powtórzona wiadomość nie tworzy drugiego profilu ani drugiego powiadomienia.
- Zmiana pola w kontrakcie bez nowej wersji psuje `ContractSnapshotTests`.
- `dotnet test` dla czterech projektów z sekcji Testy kończy się kodem 0.
