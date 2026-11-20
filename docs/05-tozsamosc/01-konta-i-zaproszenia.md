# 01. Konta i zaproszenia

Poprzedni numer to [health](../04-zdarzenia/09-health-i-odpornosc.md). Następny to [OpenIddict](02-openiddict.md).

Ten numer stawia usługę Identity: `Account` i `Invite` jako wiersze EF w `identity_write`, nie jako strumień. Jest hasher hasła, seed właściciela i odmowa zużytego zaproszenia. Nie ma jeszcze serwera OIDC. Hasło nie wraca w żadnym DTO.

## Po co

Learning umie już wyłączyć zapis na maszynie, bo nie wie, kto woła. Bez konta nie ma kogo wpuścić jako `owner`. Zaproszenie zamyka rejestrację: nie każdy, kto zna hostname, zakłada konto studenta. Kod jednorazowy z datą ważności jest tą granicą. Właściciel powstaje z polecenia na maszynie, nie z formularza publicznego.

Event sourcing konta nie wchodzi. Historia hasła nie jest produktem. Produktem jest bieżący stan: konto jest albo go nie ma, zaproszenie jest zużyte albo nie.

## Dlaczego ta technologia

### Wiersz, nie strumień

`Account` ma identyfikator, e-mail, skrót hasła, rolę i znacznik czasu. Problem, który zamyka, to strumień konta, z którego da się odtworzyć kolejne hasła, nawet jako skróty, bez potrzeby produktu. Odpada event sourcing Identity. Koszt: nie ma historii „jak brzmiał e-mail rok temu”. Zmiana e-maila nadpisuje wiersz i zostawia ślad w audycie, gdy audyt dojdzie. Do tego czasu jest `version` jak w Learning.

### Hasher z platformy

`PasswordHasher<Account>` z `Microsoft.AspNetCore.Identity` liczy skrót. Pakiet jest częścią platformy ASP.NET, licencja MIT. Nie bierzemy całej maszyny Identity UI, ról z cookie i menedżera użytkowników, którego i tak zastąpi OpenIddict. Problem, który zamyka, to własny SHA-256 bez soli. Odpada trzymanie hasła w odwrotnym stanie. Koszt: weryfikacja idzie przez hasher, nie przez porównanie stringów w endpoincie.

### Zaproszenie

`Invite` ma kod, rolę docelową `student`, `ExpiresAt`, `ConsumedAt`. Kod jest losowy, co najmniej 32 bajty w zapisie base64url, nie krótki PIN. Problem, który zamyka, to rejestracja otwarta. Odpada konto `owner` z formularza. Koszt: właściciel musi mieć sposób wydać kod. Na tym numerze jest endpoint `POST /invites` wyłączony tą samą flagą co zapis Learning, dopóki nie ma tokenu. W Development endpoint działa i wymaga nagłówka, który numer JWT zamieni na politykę `owner`. Do tego czasu w Development wystarcza, że endpoint nie jest mapowany, gdy `WritesEnabled` jest false. Na maszynie jest false.

Właściciel nie potrzebuje zaproszenia. Powstaje z polecenia `seed-owner`.

### Seed

`dotnet run --project src/services/Identity/Identity.Api -- seed-owner` czyta `OWNER_EMAIL` i `OWNER_PASSWORD` ze środowiska. Gdy konto o roli `owner` już jest, proces kończy się kodem `0` i nic nie zmienia. Gdy zmiennych nie ma, kończy się kodem `1` i nie tworzy konta z hasłem domyślnym. Problem, który zamyka, to hasło `admin` w migracji. Odpada SQL z hasłem w git. Koszt: pierwsze uruchomienie maszyny wymaga jednorazowego podania zmiennych w sesji, nie w pliku repozytorium. Mogą trafić do env maszyny na czas polecenia i zostać usunięte z sesji. Skrót zostaje w bazie.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| skrót hasła | wynik haszera, nie hasło |
| zaproszenie | jednorazowy kod na rolę `student` |
| `seed-owner` | polecenie tworzące pierwsze konto właściciela |
| rola | `owner` albo `student`, kolumna, nie drugi system |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/services/Identity/Identity.Domain/Account.cs` | fabryka, bez hasła w `ToString` |
| `src/services/Identity/Identity.Domain/Invite.cs` | ważność i zużycie |
| `src/services/Identity/Identity.Application/UseCases/SeedOwner.cs` | idempotentny seed |
| `src/services/Identity/Identity.Application/UseCases/IssueInvite.cs` | wydaje kod |
| `src/services/Identity/Identity.Infrastructure/Persistence/IdentityDbContext.cs` | tabele `accounts`, `invites` |
| `src/services/Identity/Identity.Api/Program.cs` | host, migracja jak w Learning, argument `seed-owner` |

Projekty idą tym samym układem co Learning: Domain, Application, Infrastructure, Api, Contracts, Relay, Projector. Relay i Projector mogą być puste do sagi i do projekcji skrótu konta. Ten numer dodaje Api, Domain, Application, Infrastructure. Contracts dochodzi przy sadze, jeśli wcześniej nie jest potrzebny. Nie tworzymy pustych projektów „na przyszłość”. Relay powstanie, gdy będzie outbox. Na tym numerze outboxa nie ma.

Bazy `identity_write` i `identity_read` już są. Migracja dotyczy tylko zapisu. Odczyt skrótu konta dojdzie, gdy brama albo ktoś będzie potrzebował listy. Na tym numerze nikt nie listuje kont publicznie.

## Plan

### 1. Konto

`Account.Create(email, passwordHash, role, clock)` odrzuca pusty e-mail kodem `identity.email.blank` i rolę spoza dwóch wartości kodem `identity.role.unknown`. Nie przyjmuje hasła w jawnej postaci. Hasher woła application, zanim powstanie encja. Encja nie ma metody `SetPassword(string plain)`.

`ToString` i log nie zawierają skrótu. Skrót jest długi i wygląda jak sekret. Zasada: nie logować właściwości `PasswordHash`.

### 2. Zaproszenie

`Invite.Issue` ustawia ważność na 7 dni. `Invite.Consume` zwraca błąd `identity.invite.consumed`, `identity.invite.expired` albo sukces i ustawia `ConsumedAt`. Drugie zużycie jest błędem, nie sukcesem. To nie jest idempotencja kliknięcia: kod ma być jednorazowy. Idempotencja HTTP dojdzie na rejestracji, na poziomie odpowiedzi, zanim drugie żądanie w ogóle zobaczy zużyty kod, albo zobaczy go i dostanie tę samą odmowę. Szczegół jest w OpenIddict i w sadze. Tu domena jest twarda: drugi `Consume` pada.

### 3. Unikalność e-maila

Indeks unikalny na `email`. Wyścig dwóch seedów kończy się jednym kontem. Handler łapie naruszenie i zwraca sukces, jeśli istniejące konto jest `owner` i e-mail się zgadza, albo `identity.email.taken`, jeśli e-mail należy do kogoś innego.

### Co jest do czego

| Typ | Kto go woła |
| --- | --- |
| `PasswordHasher` | handler seeda i później rejestracja, nie encja |
| `Invite.Consume` | saga rejestracji, jeszcze nie ten numer |
| `seed-owner` | człowiek na maszynie, raz |

## Testy

Nazwa: `Consume_twice_returns_consumed`.

| Arrange | Act | Assert |
| --- | --- | --- |
| zaproszenie świeże | dwa razy `Consume` | pierwszy sukces, drugi kod `identity.invite.consumed` |

Nazwa: `Create_does_not_accept_plaintext_password_parameter`. To jest przegląd sygnatury w teście architektury albo zwykły fakt, że hasher jest poza domeną: projekt domeny nie referencjonuje Identity. Wystarczy `dotnet list package` pusty na domenie i fakt zużycia zaproszenia.

```bash
dotnet test tests/Identity.Domain.Tests --nologo
```

## Checkpoint

```bash
dotnet test tests/Identity.Domain.Tests --nologo
OWNER_EMAIL=owner@example.com OWNER_PASSWORD='zmien-mnie-lokalnie' dotnet run --project src/services/Identity/Identity.Api -- seed-owner
```

Hasło w checkpointcie jest przykładem lokalnym, nie hasłem maszyny. Drugi przebieg tego samego polecenia kończy się kodem `0` i jednym wierszem w `accounts`. W repozytorium nie ma tego hasła. Przykład w dokumentacji jest jasno lokalny.

`GET` czegokolwiek na Identity nie zwraca `passwordHash`. Na tym numerze nie ma `GET` konta.

## Umiesz wyjaśnić

Dlaczego konto nie jest strumieniem?

Bo nie odtwarzamy historii haseł jako funkcji produktu. Bieżący wiersz wystarcza. Strumień został przy artykule i przy sekcjach CV, gdzie kolejne brzmienie jest treścią.

Dlaczego hasher nie siedzi w encji?

Bo encja nie powinna zależeć od frameworka ASP.NET. Dostaje gotowy skrót. Dzięki temu test domeny nie liczy hasha i nie ciągnie paczki.

Dlaczego drugie zużycie zaproszenia jest błędem, a drugie ukończenie lekcji sukcesem?

Bo ukończenie lekcji jest powtórką tego samego faktu. Zaproszenie jest przepustką jednorazową. Druga rejestracja tym samym kodem ma nie przejść.

Skąd się bierze właściciel?

Z polecenia i ze zmiennych środowiska. Nie z formularza i nie z migracji z hasłem.

Co wolno zalogować?

E-mail i identyfikator. Nie hasło, nie skrót, nie kod zaproszenia w całości. Kod w logu jest sekretem do czasu zużycia.

## Zasady

- Domena Identity nie referencjonuje EF ani OpenIddict.
- W git nie ma hasła ani skrótu prawdziwego konta.
- `seed-owner` nie tworzy drugiego właściciela.
- Zaproszenie wydaje rolę `student`, nigdy `owner`.
- Brak `GET`, który zwraca skrót hasła.
- `identity_read` nadal bez tabel, dopóki nie ma projekcji do pokazania.

## Poza zakresem

- Wydawanie tokenu jest w [02-openiddict.md](02-openiddict.md).
- Sprawdzanie tokenu w Learning jest w [03-jwt-i-policies.md](03-jwt-i-policies.md).
- Saga zużycia zaproszenia i zapisu w Learning jest w [05-saga-rejestracji.md](05-saga-rejestracji.md).
- Audyt zmiany konta jest w [04-audyt.md](04-audyt.md).

## Źródła

- [PasswordHasher](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1). Wiążące jest to, że skrót ma sól i algorytm z platformy. Dotyczy handlera seeda. Ze źródła nie bierzemy własnego SHA-256.
- [OWASP password storage](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html). Wiążące jest nieforsowanie własnego algorytmu. Dotyczy braku hasła w bazie w jawnej postaci. Ze ściągi nie bierzemy parametrów, które hasher platformy już ustawia, chyba że dokumentacja platformy każe je nadpisać.
- Numer Learning o EF. Wiążący jest ten sam kształt migracji, token wersji i login tylko do własnej bazy. Dotyczy `identity_write`. Ze źródła nie bierzemy strumienia zdarzeń.

## Gotowe gdy

- Test podwójnego zużycia zaproszenia przechodzi.
- Domena Identity nie ma `PackageReference`.
- `seed-owner` drugi raz nie tworzy wiersza.
- W logu polecenia nie ma hasła.
- Nie da się wydać zaproszenia na rolę `owner`.
