# 06. Redis

Poprzedni numer to [OpenBao](05-openbao.md). Następny to [skalowanie](07-skalowanie.md).

Ten numer dodaje Redis jako cache odczytu. Klucz zaczyna się od nazwy usługi. Pad Redisa nie psuje `GET`: zapytanie schodzi do Postgresa. Redis nie jest bazą zapisu i nie jest magazynem strumienia.

## Po co

Strona CV i katalog są czytane częściej, niż się zmieniają. Cache zamyka powtarzalny odczyt. Bez prefiksu klucz `page` w News nadpisze `page` w CV. Bez zejścia do bazy przy awarii cache strona leży, choć Postgres żyje.

## Dlaczego ta technologia

### Redis, nie pamięć procesu

`IMemoryCache` nie jest wspólny dla dwóch Podów. Problem, który zamyka, to drugi Pod ze starym cache albo z pustym. Odpada traktowanie pamięci procesu jako cache produktu, gdy replika może być większa niż jeden. Koszt: kolejny proces. Jest StatefulSet albo Deployment z wolumenem nieobowiązkowym: cache wolno zgubić. Nie stawiamy dysku jako warunku poprawności. Problem utraconych danych nie istnieje, bo danych źródłowych tu nie ma.

### Prefiks

Klucz `cv:page:public` i `learning:course:{slug}:{lang}`. Problem, który zamyka, to jedną bazę Redis bez przestrzeni nazw. Odpada osobny Redis na usługę. Koszt: dyscyplina prefiksu. Test wstawia dwa klucze i sprawdza, że nie są równe.

### Ominięcie przy błędzie

Klient ma krótki timeout. Wyjątek Redisa łapie odczyt i idzie do Dappera. `GET` zwraca `200`. Metryka `cache.miss` albo log, nie `500`. Problem, który zamyka, to `GET`, który jest tak zdrowy jak cache. Odpada cache jako zależność ready, jeśli ready ma ściągać Pod z ruchu, gdy tylko Redis mrugnie. Ready nie wymaga Redisa. Live też nie. Koszt: przy leżącym Redisie baza dostaje pełny ruch. To jest właśnie poprawne zachowanie.

### Unieważnienie

Po zdarzeniu, które zmienia dokument, projektor kasuje klucz albo ustawia krótkie TTL. TTL jest 60 sekund, jeśli projektor nie zdąży skasować. Problem, który zamyka, to cache bez wygaśnięcia, który serwuje starą notatkę godzinami po publikacji. Odpada TTL 24 godziny na treści, którą właściciel właśnie zmienił. Koszt: więcej odczytów bazy. 60 sekund okna po zmianie, gdy kasowanie klucza zawiedzie, jest akceptowane.

### AppHost

`AddRedis("cache")` z obrazem oficjalnym, nie z obrazem o spornej licencji. Pakiet `Aspire.Hosting.Redis` jest na MIT. Klient `Aspire.StackExchange.Redis` też. Problem, który zamyka, to Redis tylko na klastrze i inny laptop. Odpada brak Redisa w `aspire run`, gdy kod już go używa. Hasło dev jest w user secrets, nie w git.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| prefiks | nazwa usługi na początku klucza |
| TTL | czas, po którym klucz znika sam |
| ominięcie | błąd cache nie jest błędem `GET` |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/apphost` | zasób `cache` |
| `Learning.Infrastructure/Read/CachedReadQueries.cs` | dekorator nad Dapperem |
| `Cv.Infrastructure/Read/` | to samo dla strony |
| `deploy/k8s/base/redis/` | Deployment cache |
| `tests/Learning.Infrastructure.Tests/CacheBypassTests.cs` | wyjątek klienta idzie do bazy |

Dekorator jest w infrastrukturze. Aplikacja nadal widzi `IReadQueries`.

## Plan

### 1. Klucz

Funkcja `Key(service, parts)` odrzuca pusty segment. Test jednostkowy.

### 2. Dekorator

`Get` czyta cache. Brak klucza albo wyjątek: Dapper, potem zapis cache z TTL. Wyjątek zapisu cache jest ignorowany. `GET` i tak ma wynik z bazy.

### 3. Projektor

Po upsertcie dokumentu kasuje klucz sluga. Nie kasuje kluczy innej usługi.

### Co jest do czego

| Awaria | `GET` |
| --- | --- |
| Redis nie odpowiada | `200` z bazy |
| baza nie odpowiada | błąd, jak dziś |
| stary klucz | znika po TTL albo po kasowaniu |

## Testy

Nazwa: `Redis_failure_still_reads_database`.

| Arrange | Act | Assert |
| --- | --- | --- |
| klient Redisa, który rzuca, Dapper z dokumentem | `GET` lekcji | `200` i tytuł z bazy |

```bash
dotnet test tests/Learning.Infrastructure.Tests --nologo --filter "FullyQualifiedName~Redis_failure_still_reads"
```

## Checkpoint

```bash
dotnet test tests/Learning.Infrastructure.Tests --nologo --filter "FullyQualifiedName~Redis_failure"
aspire run
```

Przy działającym Redisie dwa `GET` z rzędu: drugi nie jest błędem. Po `docker stop` kontenera cache `GET` nadal `200`. W manifeście nie ma hasła Redisa. Ready Poda aplikacji jest zielony przy leżącym Redisie.

## Umiesz wyjaśnić

Dlaczego Redis nie trzyma strumienia?

Bo utrata Redisa jest normalna. Strumień i outbox są w Postgresie. Cache, który jest źródłem, nie jest cache.

Dlaczego prefiks?

Bo jedna instancja na klaster jest prostsza niż pięć. Prefiks jest granicą, skoro nie ma osobnej bazy Redis na usługę. Klucz bez prefiksu nadpisze sąsiada.

Czemu ready nie wymaga Redisa?

Bo zejście do bazy jest poprawną odpowiedzią. Ściąganie Poda z ruchu przy padzie cache zamienia przyspieszenie w awarię.

Co ogranicza starą treść?

Kasowanie klucza przez projektor i TTL 60 sekund. Nie ma nieskończonego cache.

Dlaczego dekorator, a nie cache w endpoincie?

Bo endpoint nie powinien wiedzieć o Redisie. `IReadQueries` zostaje portem. Drugi endpoint dostaje cache za darmo albo świadomie woła port bez cache, jeśli kiedyś będzie taki.

## Zasady

- Klucz zaczyna się od nazwy usługi.
- Wyjątek Redisa nie daje `500` na `GET`, jeśli baza odpowiedziała.
- TTL jest 60 sekund, o ile kasowanie nie nastąpi wcześniej.
- Hasło Redisa nie jest w git.
- Redis nie jest w liveness.
- Strumień nie jest zapisywany do Redisa.

## Poza zakresem

- HPA jest w [07-skalowanie.md](07-skalowanie.md). Redis nie dostaje HPA w tym numerze.
- Drugi poziom cache w przeglądarce zostaje przy TanStack Query. Ten numer go nie rusza.

## Źródła

- [Aspire Redis](https://aspire.dev/integrations/caching/redis/). Wiążące jest dodanie zasobu i connection string. Dotyczy AppHosta. Ze źródła nie bierzemy Redisa jako bazy dokumentu.
- [Redis key names](https://redis.io/docs/latest/develop/use/keyspace/). Wiążące jest świadome nazywanie kluczy. Dotyczy prefiksu. Ze źródła nie bierzemy jednego klucza `page` bez usługi.
- Numer odczytu. Wiążące jest to, że Postgres zostaje źródłem `GET`. Dotyczy ominięcia cache. Ze źródła nie bierzemy `500`, gdy cache leży.

## Gotowe gdy

- Test z rzucającym klientem zwraca treść z bazy.
- Dwa klucze różnych usług nie są tym samym stringiem.
- Po zatrzymaniu Redisa `GET` publicznej lekcji jest `200`.
- W manifeście nie ma hasła.
- Ready aplikacji nie zależy od Redisa.
