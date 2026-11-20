# 03. Postgres i dziesięć baz

Poprzedni numer to [porty](02-use-case-i-porty.md). Następny to [EF Core](04-ef-core.md).

Ten numer stawia jeden PostgreSQL 18 i dziesięć pustych baz. Learning dostaje connection string wyłącznie do `learning_write` i, jako rezerwację nazwy, do `learning_read`. Tabel jeszcze nie ma. Jest łączność, konto na bazę i skrypt, który tworzy bazy przy starcie kontenera.

## Po co

Jedna baza na cały produkt pozwala usłudze News przeczytać tabelę kursu, bo ma login, który widzi wszystko. Osobna baza na usługę zamyka to uprawnieniem, nie umową w komentarzu. Dwie bazy na usługę, zapis i odczyt, zamykają drugi skrót: handler komendy z connection stringiem, który sięga także widoku.

Osobny serwer na każdą bazę nie wchodzi. Zapis i outbox mają mieć jeden commit, a pięć silników tego nie upraszcza. Jeden proces, dziesięć baz, dziesięć loginów.

## Dlaczego ta technologia

### PostgreSQL 18

Obraz `postgres:18`. Problem, który zamyka, to „jakaś baza w kontenerze” bez wersji. Odpada SQL Server, bo nie jest bazą tego produktu, i MongoDB, bo odczyt i tak jest dokumentem albo wierszem pod slug, a licencja serwera Mongo jest poza listą. Koszt: trzeba znać `psql`, role i `GRANT`, a nie tylko ORM.

Wersja 18 jest ta sama na laptopie, w teście i na maszynie. Testcontainers używa `postgres:18`, nie `latest`.

### Aspire `AddPostgres` z naszym obrazem

`builder.AddPostgres("postgres").WithImage("postgres", "18")` tworzy zasób. Bazy powstają z skryptu inicjalizacji zamontowanego do `docker-entrypoint-initdb.d`. Problem, który zamyka, to baza klikana w kliencie SQL. Odpada obraz domyślny hostingu, jeśli nie jest 18. Koszt: hasło kontenera deweloperskiego jest w user secrets AppHosta, nie w `appsettings.json`. Na maszynie hasło jest w `/etc/jjdevhub/compose.env`.

### Login na bazę, nie jeden superuser w aplikacji

Każda baza ma właściciela: `learning_write_owner` widzi tylko `learning_write`, `learning_read_owner` tylko `learning_read`. Aplikacja Learning dostaje dwa connection stringi. Nie dostaje loginu `postgres`. Problem, który zamyka, to superuser w usłudze, który omija `GRANT` jedną literówką w nazwie bazy. Odpada jeden użytkownik `jjdevhub` do wszystkich baz. Koszt: dziesięć haseł w env na maszynie. Nazwy są w przykładzie. Wartości nie są w git.

Skrypt tworzy też puste bazy pozostałych usług, żeby następny numer nie zmieniał decyzji „jednak jedna baza”. Login tamtej usługi powstaje w tym samym skrypcie. Proces tamtej usługi jeszcze nie istnieje i nie dostaje jej hasła w swoim env, bo nie ma procesu. Hasła są wygenerowane na maszynie i leżą w env. W AppHost deweloperskim hasła baz są generowane przez Aspire i wstrzykiwane do projektów, które istnieją. Projekt Learning, gdy dostanie infrastrukturę w następnym numerze, dostanie referencję tylko do swoich dwóch baz.

Na tym numerze projekt Learning jeszcze nie otwiera połączenia w procesie hosta. Robi to test.

### `EXPLAIN` zostaje narzędziem, nie bramką

Numer nie dodaje indeksu, bo nie ma tabeli. W checkpointcie jest `EXPLAIN` na `SELECT 1`, żeby narzędzie było użyte i było wiadomo, gdzie wróci przy odczycie. Optymalizacja zapytania katalogu jest w numerze Dappera, gdy jest zapytanie.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| baza | osobny katalog w jednym procesie Postgresa, osobne uprawnienia |
| login | rola, która loguje się do jednej bazy |
| initdb | skrypt wykonywany raz, na pustym wolumenie |
| `EXPLAIN` | plan zapytania, bez zmiany danych |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `deploy/postgres/001_databases.sql` | `CREATE DATABASE` i role dla dziesięciu baz |
| `src/apphost/JJDevHub.AppHost/AppHost.cs` | zasób `postgres` z obrazem `postgres:18` i montowaniem skryptu |
| `tests/JJDevHub.Integration.Tests/PostgresTenDatabasesTests.cs` | łączność, cecha `Category=Integration` |

Skrypt nie zawiera hasła produkcyjnego. W teście hasło jest losowe, z Testcontainers. W AppHost hasło jest w user secrets.

## Plan

### 1. Skrypt

Dla każdej pary z słownika: `CREATE DATABASE learning_write`, rola właściciela, `REVOKE ALL ON DATABASE learning_write FROM PUBLIC`, `GRANT` tylko właścicielowi. Powtórzone dla dziesięciu nazw. Skrypt jest idempotentny na tyle, na ile initdb w ogóle puszcza się raz. Nie jest wołany przy każdym starcie na istniejącym wolumenie. Zmiana skryptu na istniejącym wolumenie deweloperskim wymaga usunięcia wolumenu. README zasobu o tym mówi jednym zdaniem.

### 2. AppHost

`AddPostgres("postgres").WithImage("postgres", "18")` i bind mount skryptu do katalogu inicjalizacji obrazu. Brama nie dostaje `WithReference` do Postgresa. Nie ma jeszcze projektu Learning w AppHost poza tym, co ten numer doda jako test. Projekt infrastruktury dojdzie w następnym numerze i wtedy dostanie referencję.

Jeśli AppHost nie ma projektu, który konsumuje zasób, zasób i tak startuje przy `aspire run`. To wystarcza, żeby zobaczyć kontener.

### 3. Test łączności

Testcontainers podnosi `postgres:18`, wykonuje skrypt, łączy się `Npgsql` do `learning_write` i wykonuje `SELECT current_database()`. Drugie połączenie tym samym loginem do `news_write` ma się nie udać. To jest cały sens numeru.

Cecha: `Category=Integration`. Job `integration-tests` przestaje być pusty, gdy ten test wejdzie do solucji. To jest zamierzone.

### 4. Maszyna

Wygenerowany compose z `aspire publish` zawiera usługę Postgresa z wolumenem. Hasła baz na maszynie dopisuje się do `/etc/jjdevhub/compose.env`. Przykład w git ma puste wartości i wszystkie nazwy zmiennych. Deploy nie podmienia wolumenu przy każdym tagu bramy. Postgres nie jest komponentem tagowanym. Jest infrastrukturą. Zmiana skryptu baz na maszynie z istniejącym wolumenem jest operacją opisaną w tym numerze: nowa baza `CREATE` wykonywana ręcznie tym samym SQL, jeśli wolumen już żyje. Pierwszy start pustego wolumenu wykonuje skrypt sam.

### Co jest do czego

| Baza | Kto ma login |
| --- | --- |
| `learning_write` | proces Learning, gdy powstanie, i test tego numeru |
| `learning_read` | ten sam proces, inny connection string, od numeru odczytu |
| pozostałe osiem | nikt, dopóki nie ma usługi; login już jest, hasło leży w env |

## Testy

Nazwa: `Learning_login_can_select_own_database_and_not_news`.

| Arrange | Act | Assert |
| --- | --- | --- |
| kontener `postgres:18` po skrypcie | połączenie właściciela `learning_write` | `current_database()` równe `learning_write` |
| ten sam login | połączenie do `news_write` | wyjątek uwierzytelnienia albo odmowa, test oczekuje wyjątku `NpgsqlException` |

```bash
dotnet test tests/JJDevHub.Integration.Tests --nologo --filter "FullyQualifiedName~Learning_login_can_select"
```

## Checkpoint

```bash
dotnet test tests/JJDevHub.Integration.Tests --nologo --filter "Category=Integration"
aspire run
```

Przy `aspire run` `docker exec` do kontenera Postgresa i `\l` pokazuje dziesięć baz z słownika. Hasło nie pojawia się w logu testu. Testcontainers drukuje connection string; projekt testu nie loguje go przez `ILogger`. Jeśli biblioteka wypisze go w konsoli runnera, to zostaje w runnerze, nie w repozytorium.

## Umiesz wyjaśnić

Dlaczego nie dwa schematy w jednej bazie?

Bo jeden connection string sięga obu schematów. Handler, który „tylko na chwilę” przeczyta drugą, przejdzie. Osobna baza i osobny login nie przejdą.

Dlaczego nie dziesięć serwerów?

Bo outbox i stan mają być w jednej transakcji. Dwa serwery nie dają jednego commita. Jeden proces Postgresa daje dziesięć baz i jeden commit w obrębie bazy zapisu.

Czym initdb różni się od migracji EF?

Initdb tworzy puste bazy raz, na pustym wolumenie. Migracja tworzy tabele i jedzie z kodem. Mieszanie tabel do initdb rozjeżdża schemat względem gita.

Dlaczego brama nie ma connection stringa?

Bo nie ma domeny i nie czyta wersji z bazy. Wersja jest w zmiennej procesu i w `GET /version` usługi.

Co z hasłem superusera `postgres`?

Jest w user secrets AppHosta albo w env maszyny, do administracji. Żaden projekt usługi go nie dostaje. Test używa hasła kontenera testowego, nie hasła maszyny.

## Zasady

- Obraz to `postgres:18`, także w Testcontainers.
- Aplikacja nie łączy się jako superuser.
- Skrypt i `appsettings` nie zawierają hasła produkcyjnego.
- Jest dziesięć baz, nawet gdy działa tylko Learning.
- Brama nie dostaje referencji do Postgresa.
- Test ma cechę `Category=Integration`.

## Poza zakresem

- Tabele Learning są w [04-ef-core.md](04-ef-core.md).
- Baza odczytu dostaje tabele w [06-odczyt-dapper.md](../04-zdarzenia/06-odczyt-dapper.md). Ten numer tworzy pustą `learning_read`.
- Kafka nie korzysta z Postgresa jako magazynu Kafki.
- Backup wolumenu nie jest tym numerem. Jest sprawą dysku VM.

## Źródła

- [PostgreSQL roles](https://www.postgresql.org/docs/current/user-manag.html). Wiążące jest rozdzielenie roli logowania i właściciela bazy. Dotyczy dziesięciu loginów. Ze źródła nie bierzemy jednego superusera w aplikacji.
- [Database per service](https://microservices.io/patterns/data/database-per-service.html). Wiążące jest to, że usługa nie czyta tabel sąsiada. Dotyczy osobnej bazy, nie osobnego serwera. Ze źródła nie bierzemy dziesięciu silników.
- [Aspire PostgreSQL](https://aspire.dev/integrations/databases/postgresql/postgresql-host/). Wiążące jest `AddPostgres` i referencja connection stringa. Dotyczy zasobu `postgres`. Ze źródła nie bierzemy obrazu innego niż `postgres:18`, jeśli domyślny tag hostingu jest inny: ustawiamy go wprost.
- [EXPLAIN](https://www.postgresql.org/docs/current/sql-explain.html). Wiążące jest czytanie planu zamiast zgadywania indeksu. Dotyczy późniejszego odczytu. Ze źródła nie bierzemy indeksu na pustej bazie „na zapas”.

## Gotowe gdy

- Test integracyjny pokazuje, że login Learning czyta swoją bazę i nie wchodzi do `news_write`.
- `\l` w kontenerze z AppHosta pokazuje dziesięć nazw ze słownika.
- W git nie ma hasła.
- Brama nie ma connection stringa.
- Job `integration-tests` wykonuje ten test, a nie loguje już `no material`, jeśli to jedyny test integracyjny.
