# 04. EF Core dla zapisu Learning

Poprzedni numer to [Postgres](03-postgres.md). Następny to [REST](05-rest.md).

Ten numer stawia `Learning.Infrastructure` i `LearningDbContext` na `learning_write`. Adaptery implementują porty z aplikacji. Migracja tworzy tabele. Domena nadal nie wie, że istnieje EF. Odczyt Dapperem jeszcze nie istnieje: `learning_read` zostaje pusta.

## Po co

Port bez adaptera nie zapisuje kursu po restarcie procesu. EF zamyka mapowanie encji na tabele i transakcję, którą `IUnitOfWork` commituje. Bez migracji w git schemat żyje tylko na jednym laptopie i nie da się go postawić na maszynie drugi raz tak samo.

Jednocześnie EF na ścieżce odczytu listy kusi `Include` i śledzeniem zmian, których odczyt nie potrzebuje. Dlatego ten numer jest tylko zapisem. Odczyt dostanie Dappera.

## Dlaczego ta technologia

### EF Core 10 i Npgsql

`UseNpgsql` oraz `UseSnakeCaseNamingConvention`. Problem, który zamyka, to kolumny `TechnologySlug` w bazie, która wszędzie indziej ma `technology_slug`. Odpada ręczne nazywanie każdej kolumny, poza miejscem, gdzie konwencja nie wystarcza. Koszt: migracja jest w repozytorium i jest jedyną drogą zmiany tabeli. Ręczny `ALTER` na maszynie bez migracji jest błędem operacyjnym.

### `int Version` jako token współbieżności

Kolumna `version` jest concurrency token. Zapis z nieaktualną wersją rzuca `DbUpdateConcurrencyException`, a adapter zamienia go na `Result` z kodem `learning.version.conflict` i rodzajem `Conflict`. Problem, który zamyka, to dwa okna, które cicho nadpisują lekcję. Odpada brak tokenu. Koszt: każda komenda zmiany niesie oczekiwaną wersję. Na tym numerze wersję niesie `ReviseLessonCommand`. HTTP i `ETag` dojdą w następnym numerze; tu jest już odmowa w handlerze, gdy wersja w bazie jest inna.

### Konfiguracja w klasach `IEntityTypeConfiguration`

`OnModelCreating` woła `ApplyConfigurationsFromAssembly`. Problem, który zamyka, to jeden `OnModelCreating` na kilkaset linii. Odpada dopisywanie mapowania w encji atrybutem: encja zostałaby zależna od EF. Koszt: nowa encja bez konfiguracji wywali migrację albo wpadnie w konwencję, której nie chcemy. Test sprawdza, że `Lesson` ma tabelę `lessons`.

### Migracja w repozytorium

`dotnet ef migrations add` z projektu infrastruktury, z projektem startowym, który istnieje tylko po to, żeby narzędzie miało hosta, albo z `Learning.Api`, jeśli ten numer doda pusty host. Żeby nie wyprzedzać REST-a, projekt startowy narzędzia to `Learning.Infrastructure` z fabryką `IDesignTimeDbContextFactory`. Fabryka czyta connection string ze zmiennej środowiska i nie ma hasła w kodzie.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| migracja | plik w git, który doprowadza bazę do modelu |
| concurrency token | kolumna, której `WHERE` przy `UPDATE` pilnuje, że nikt nie wszedł wcześniej |
| snake_case | `technology_slug` zamiast `TechnologySlug` |
| tracker | EF śledzi encje w kontekście; zapis tego potrzebuje, odczyt listy nie |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Learning.Infrastructure/Persistence/LearningDbContext.cs` | `DbSet` zapisu |
| `Learning.Infrastructure/Persistence/Configurations/*.cs` | mapowanie |
| `Learning.Infrastructure/Persistence/LearningDbContextFactory.cs` | czas projektowania migracji |
| `Learning.Infrastructure/Persistence/Migrations/` | wygenerowane migracje |
| `Learning.Infrastructure/Adapters/EfCourseRepository.cs` i sąsiedzi | porty |
| `Learning.Infrastructure/Adapters/EfUnitOfWork.cs` | `SaveChangesAsync` i tłumaczenie konfliktu |
| `tests/Learning.Infrastructure.Tests/` | test migracji na Testcontainers, `Category=Integration` |

`DbSet` obejmuje `Category`, `Technology`, `Course`, `Lesson`, `StudentProgress`. Nie obejmuje tabeli odczytu.

## Plan

### 1. Model

Tabele: `categories`, `technologies`, `courses`, `lessons`, `student_progress`. Klucz główny `id uuid`. `slug` unikalny w swojej tabeli. `student_progress` ma unikalność `(student_id, lesson_slug)`. `version int not null` na tabelach, które da się zmieniać: technologia, kurs, lekcja, kategoria. Postęp nie ma wersji na tym numerze: ukończenie jest idempotentne w domenie.

`LocalizedText` na tym numerze jest kolumną `jsonb`. Konwerter dopina [numer zaawansowany](07-ef-zaawansowany.md). Tu wolno zapisać JSON przez konwerter od razu, jeśli bez niego model się nie kompiluje: `LocalizedText` nie jest typem, który EF mapuje sam. Więc konwerter `Slug` i `LocalizedText` powstaje w tym numerze w minimalnej formie, a numer zaawansowany dopisuje interceptory, porównanie value convertera i próbę TPH. Nie zostawiamy encji niezapisywalnej.

Indeks unikalny na `slug`. Klucz obcy z `courses.technology_slug` nie wskazuje `technologies.id`. Wskazuje slug albo nie wskazuje nic: trzymamy `technology_slug` jako kolumnę bez FK do cudzej tabeli wewnątrz tej samej bazy tylko wtedy, gdy slug jest stabilny. W obrębie Learning wolno dać FK do `technologies.slug`, bo to ta sama usługa i ta sama baza zapisu. Nie wolno dać FK do przyszłej bazy Cv. Numer Cv i tak nie sięga tej tabeli.

### 2. Adapter

`EfCourseRepository.GetBySlugAsync` używa `SingleOrDefaultAsync`. `AddAsync` woła `DbSet.Add`. `EfUnitOfWork.SaveChangesAsync` łapie `DbUpdateConcurrencyException` i rzuca `ConcurrencyConflictException` z aplikacji. Handler `ReviseLesson` łapie ten wyjątek i zwraca `Result` z `learning.version.conflict`. Wyjątek nie wychodzi do przyszłego HTTP jako `500`. Typ wyjątku żyje w aplikacji, nie w EF, żeby handler nie łapał typu z infrastruktury. Adapter tłumaczy.

To jest jedyny wyjątek użyty jako sygnał między adapterem a handlerem. Reszta odmów jest `Result`.

### 3. Migracja

`dotnet ef migrations add InitialLearningWrite`. Wygenerowany plik jest w git. `Designer` jest w git i jest wyłączony z limitu `pr-size`. Aktualizacja bazy w teście: `MigrateAsync` na kontenerze.

### 4. AppHost

Projekt infrastruktury nie jest procesem. Do AppHosta wchodzi projekt, który umie otworzyć bazę. Na tym numerze jest to worker albo testowy host `Learning.Migrator`, uruchamiany ręcznie, nie jako usługa publiczna. Prostsza droga: migrację odpala przyszły `Learning.Api` przy starcie w Development i job deployu odpala `dotnet ef database update` jako krok. Żeby nie czekać na REST, ten numer dodaje `Learning.Api` jako pusty host z samym `MigrateAsync` przy starcie i health. REST dopnie endpointy w następnym numerze do tego samego projektu. To jest świadome wyprzedzenie o jeden projekt, nie o endpointy produktu.

Pusty host woła `AddServiceDefaults`, `AddLearningApplication`, `AddLearningPersistence` i migruje w Development. Poza Development nie migruje sam. Na maszynie migrację puszcza krok deployu z katalogu artefaktu, albo jednorazowy kontener. Ten numer wybiera jednorazowy kontener `learning-migrate` w compose, ten sam obraz co api, z argumentem polecenia `migrate`. Obraz jeszcze nie rozróżnia polecenia: `Learning.Api` czyta argument `migrate` i kończy proces po `MigrateAsync`. Bez argumentu jest hostem HTTP. Następny numer dodaje endpointy do tej drugiej ścieżki.

### Co jest do czego

| Klasa | Kto ją woła |
| --- | --- |
| `LearningDbContext` | adaptery, nikt z domeny |
| `EfUnitOfWork` | handler przez port |
| migracja | test, kontener `migrate`, nie każde żądanie HTTP |
| `ConcurrencyConflictException` | handler rewizji |

## Testy

Nazwa: `Migrate_then_publish_technology_roundtrips_slug`.

| Arrange | Act | Assert |
| --- | --- | --- |
| pusty `postgres:18` z bazą `learning_write`, po `MigrateAsync` | handler publikacji przez porty EF | ponowne pobranie po slugu zwraca tę samą nazwę `pl` |

Cecha `Category=Integration`. Test jednostkowy tego numeru nie istnieje osobno: mapowanie bez bazy sprawdza kompilacja. Konflikt wersji jest testem integracyjnym `Revise_with_stale_version_returns_conflict`: dwa konteksty, pierwszy zapisuje, drugi zapisuje ze starą wersją.

```bash
dotnet test tests/Learning.Infrastructure.Tests --nologo --filter "Category=Integration"
```

## Checkpoint

```bash
dotnet ef migrations list --project src/services/Learning/Learning.Infrastructure
dotnet test tests/Learning.Infrastructure.Tests --nologo --filter "FullyQualifiedName~Migrate_then_publish"
```

Lista migracji pokazuje `InitialLearningWrite`. W `learning_read` nadal nie ma tabel produktu. Sprawdzenie: połączenie do `learning_read` i brak relacji `lessons` w `information_schema`.

## Umiesz wyjaśnić

Dlaczego domena nie ma atrybutu `[Table]`?

Bo wtedy nie kompiluje się bez EF i reguła zależy od frameworka. Mapowanie jest w konfiguracji infrastruktury.

Czym token wersji różni się od sluga?

Slug identyfikuje w adresie i nie rośnie. Wersja rośnie przy każdej zmianie i wykrywa dwa zapisy naraz. Zamiana jednego na drugie psuje albo adres, albo współbieżność.

Dlaczego migracja nie leci przy każdym starcie na maszynie?

Bo start wielu replik naraz na jednej bazie skończy się wyścigiem migracji. Na laptopie, w Development, jedna instancja `aspire run` migruje. Na maszynie migruje krok deployu, raz.

Dlaczego `LocalizedText` jest `jsonb`, a nie dwiema kolumnami?

Bo trzeci język miałby być kolejną kolumną albo kolejną migracją schematu. Mapa w JSON zostawia schemat. Indeks pod język dojdzie w odczycie, jeśli zapytanie tego wymaga.

Kto widzi `DbContext`?

Adapter w infrastrukturze. Handler widzi port. Endpoint, gdy powstanie, w ogóle nie zobaczy kontekstu.

## Zasady

- `Learning.Domain` nadal nie referencjonuje EF.
- Migracja jest w git. Ręczny `ALTER` na maszynie bez nowej migracji nie jest procedurą.
- Poza Development proces HTTP nie woła `Migrate` w `Program.cs`.
- Konflikt wersji wraca jako `Result`, nie jako `500`.
- `learning_read` nie ma tabel zapisu.

## Poza zakresem

- Endpointy, `ETag` i `ProblemDetails` są w [05-rest.md](05-rest.md).
- Interceptory i scaffold są w [07-ef-zaawansowany.md](07-ef-zaawansowany.md).
- Outbox w tej samej transakcji jest w [02-outbox.md](../04-zdarzenia/02-outbox.md).
- Odczyt jest w [06-odczyt-dapper.md](../04-zdarzenia/06-odczyt-dapper.md).

## Źródła

- [EF Core migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/). Wiążące jest trzymanie migracji w repozytorium i `database update` jako droga schematu. Dotyczy `InitialLearningWrite`. Ze źródła nie bierzemy `EnsureCreated` na maszynie.
- [Concurrency tokens](https://learn.microsoft.com/ef/core/saving/concurrency). Wiążący jest token, który wchodzi do `WHERE`. Dotyczy kolumny `version`. Ze źródła nie bierzemy cichego `ClientWins`.
- [PostgreSQL jsonb](https://www.postgresql.org/docs/current/datatype-json.html). Wiążące jest trzymanie dokumentu tam, gdzie kształt zależy od kultury, nie od relacji. Dotyczy `LocalizedText`. Ze źródła nie bierzemy całej bazy jako dokumentu.

## Gotowe gdy

- Migracja `InitialLearningWrite` jest w git i test na czystym Postgresie ją stosuje.
- Publikacja technologii wraca po slugu z tą samą nazwą.
- Zapis ze starą wersją zwraca `learning.version.conflict`.
- Domena nie zyskała referencji do EF.
- `learning_read` nie ma tabel `lessons`.
