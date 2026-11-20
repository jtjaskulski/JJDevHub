# 04. Audyt

Poprzedni numer to [JWT](03-jwt-i-policies.md). Następny to [saga rejestracji](05-saga-rejestracji.md).

Ten numer zapisuje audyt zmian właściciela: kto, co, kiedy, jak wyglądało przed i po, oraz hash poprzedniego wpisu. Wpis jest append-only. Da się przeliczyć łańcuch i dostać pierwszy zły rekord. Audyt nie zastępuje logu aplikacji i nie jedzie do klienta publicznego.

## Po co

Log tekstowy da się skasować razem z kontenerem i nie wiąże dwóch zmian tej samej lekcji. Tabela, do której `UPDATE` nie przechodzi, zamyka „kto przestawił widoczność”. Hash chain zamyka ciche wyjęcie wiersza ze środka: przeliczenie się nie zgadza. To nie jest dowód przeciw administratorowi bazy, który podmieni całą tabelę i skróty naraz. Jest dowodem przeciw pomyłce aplikacji i przeciw zwykłemu `UPDATE`.

## Dlaczego ta technologia

### Osobna tabela, nie log

`audit_entries` w bazie zapisu usługi, która zmienia stan. Na tym numerze tabela powstaje w Identity i w Learning, bo obie już zmieniają coś, co ma właściciela. Problem, który zamyka, to jeden wspólny audyt w bazie, do której obie usługi mają login. Każda ma swoją tabelę. Łańcuch jest per `(entity_type, entity_id)` w tej bazie. Odpada jeden globalny łańcuch na całą firmę: psuje się przy dwóch transakcjach naraz i łączy usługi bazą. Koszt: dwa miejsca do sprawdzenia. Endpoint weryfikacji jest w każdej usłudze, pod tym samym kształtem.

### Osiem pól

`occurred_at`, `actor` (`sub` albo `system:seed`), `action`, `entity_type`, `entity_id`, `before` i `after` jako JSON albo null, `correlation_id`, `previous_hash`, `hash`. Problem, który zamyka, to wpis „coś się stało” bez przedmiotu. Odpada audyt bez `before` i `after` przy zmianie. Przy tworzeniu `before` jest null. Przy tworzeniu konta `after` nie zawiera skrótu hasła ani hasła. Zawiera e-mail i rolę. Koszt: trzeba świadomie wyciąć pole. Test padnie, jeśli w `after` jest klucz `passwordHash`.

### Hash

`hash` to SHA-256 z kanonicznego napisu pól i `previous_hash`. Pierwszy wpis encji ma `previous_hash` równy 64 zerom. Problem, który zamyka, to skrót tylko z bieżącego wiersza, który nie zauważy usunięcia poprzedniego. Odpada HMAC z kluczem w bazie: klucz obok danych nie dodaje wiele, a dokłada sekret. Koszt: administrator bazy nadal może przepisać cały łańcuch. Akceptujemy to i nie nazywamy tabeli dziennikiem niepodważalnym.

### Append-only

Interceptor EF rzuca przy stanie `Modified` albo `Deleted` na `AuditEntry`. Trigger w Postgresie robi to samo na `UPDATE` i `DELETE`. Dwa miejsca, bo samo EF da się obejść SQL-em, a sam trigger da się obejść inną aplikacją tylko jeśli ta aplikacja ma login. Login usługi jest właścicielem, więc trigger jest w tej samej bazie i właściciel go nie omija, jeśli trigger jest `BEFORE UPDATE` i rzuca. Właściciel może wyłączyć trigger. Dlatego zostaje też weryfikacja łańcucha, którą woła właściciel produktu, nie baza. Problem, który zamyka, to `SaveChanges` poprawiający historię przez pomyłkę. Odpada audyt w tej samej tabeli co lekcja, aktualizowany w miejscu.

### Weryfikacja

`GET /audit/verify` wymaga `owner`. Zwraca `isValid`, `checkedCount`, `firstBrokenId`. Nie zwraca treści `before` i `after` w tym endpoincie. Lista wpisów `GET /audit` też jest `owner` i jest w usłudze, nie w bramie. Brama tylko przekazuje.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| append-only | brak poprawnej ścieżki `UPDATE` i `DELETE` |
| hash chain | skrót zawiera skrót poprzedniego wpisu tej encji |
| `before` / `after` | stan bez sekretu |
| weryfikacja | przeliczenie łańcucha, nie zaufanie do kolumny `hash` w ciemno |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/shared/JJDevHub.Audit/AuditEntry.cs` | liczenie skrótu, biblioteka, gdy druga usługa już jest |
| `Learning.Infrastructure/Persistence/Interceptors/AuditAppendOnlyInterceptor.cs` | odmowa zmiany |
| `Identity.Infrastructure` | to samo |
| migracje | tabela i trigger `audit_reject_mutation` |
| `Learning.Api/Endpoints/AuditEndpoints.cs` | lista i weryfikacja |
| `tests/JJDevHub.Audit.Tests/ChainTests.cs` | zły środkowy wpis |

Biblioteka `JJDevHub.Audit` powstaje w tym numerze, bo Identity i Learning potrzebują jej razem. Nie wcześniej.

## Plan

### 1. Zapis w tej samej transakcji

Handler rewizji lekcji dopisuje `AuditEntry` przed `SaveChanges`, w tej samej jednostce pracy. Aktor z `ICurrentActor`. Gdy aktora nie ma, a komenda i tak przeszła filtr, handler nie zgaduje „system”. Kończy się błędem `identity.actor.missing` albo `learning.actor.missing`. Seed właściciela zapisuje aktora `system:seed`.

### 2. Trigger

```sql
CREATE OR REPLACE FUNCTION audit_reject_mutation()
RETURNS trigger AS $$
BEGIN
  RAISE EXCEPTION 'audit is append-only';
END;
$$ LANGUAGE plpgsql;
```

Trigger na `UPDATE` i `DELETE`. Test integracyjny wykonuje `DELETE` jako login usługi i oczekuje wyjątku.

### 3. Weryfikacja

Metoda `AuditChain.Verify` grupuje po encji, sortuje po `id`, przelicza. Pierwszy zły `id` wraca w wyniku. Test podmienia `hash` w pamięci, nie w bazie: funkcja jest czysta na liście. Osobny test integracyjny czyta z bazy po dwóch zmianach i oczekuje `isValid` true.

### Co jest do czego

| Pole | Czego nie niesie |
| --- | --- |
| `after` konta | hasła i skrótu |
| `after` lekcji | tokenu sesji |
| `actor` | e-maila zamiast `sub`, poza seedem `system:seed` |

## Testy

Nazwa: `Chain_detects_tampered_middle_hash`.

| Arrange | Act | Assert |
| --- | --- | --- |
| trzy wpisy, środkowy `hash` podmieniony | `Verify` | `isValid` false, `firstBrokenId` równe środkowemu |

Nazwa: `Account_audit_json_has_no_password_hash`.

```bash
dotnet test tests/JJDevHub.Audit.Tests --nologo
```

## Checkpoint

```bash
dotnet test tests/JJDevHub.Audit.Tests tests/Learning.Infrastructure.Tests --nologo --filter "FullyQualifiedName~Chain_detects|FullyQualifiedName~audit_reject"
```

Po rewizji lekcji jako właściciel `GET /learning/audit` zwraca wpis z `sub`. `DELETE FROM audit_entries` w psql loginem usługi pada.

## Umiesz wyjaśnić

Dlaczego łańcuch nie jest dowodem przeciw administratorowi?

Bo kto ma prawo wyłączyć trigger i przepisać tabelę, przepisać też skróty. Łańcuch łapie zmianę, która nie przeliczyła całości, i łapie błąd aplikacji. Nie zastępuje kopii poza zasięgiem tego loginu.

Dlaczego nie jeden audyt na wszystkie usługi?

Bo jedna tabela wymaga jednego loginu albo federacji, której nie mamy. Dwie tabele zachowują granicę baz. Weryfikacja jest wołana per usługa.

Czemu hasło nie wchodzi do `after`?

Bo audyt czyta właściciel i kopia zapasowa bazy. Skrót hasła w kolejnej tabeli poszerza wyciek. Rola i e-mail wystarczą, żeby zobaczyć, że seed utworzył właściciela.

Dlaczego interceptor i trigger?

Interceptor łapie `SaveChanges` zanim SQL wyjdzie, czytelnym wyjątkiem. Trigger łapie SQL puszczony obok EF. Sam interceptor nie wystarczy. Sam trigger wystarczyłby na bazie, ale wyjątek z bazy jest brzydszy w teście jednostkowym łańcucha, który i tak nie dotyka bazy.

Czym audyt różni się od logu z numeru śladów?

Log jest operacyjny, znika z retencją Loki i nie ma łańcucha. Audyt jest wierszem biznesowym „właściciel zmienił widoczność” i zostaje w Postgresie. Ślad pomaga znaleźć, który request to zrobił, przez `correlation_id`.

## Zasady

- `UPDATE` i `DELETE` na `audit_entries` padają.
- `passwordHash` nie wchodzi do JSON audytu.
- Łańcuch jest per encja, nie jeden na całą tabelę globalnie: per `(entity_type, entity_id)`.
- Weryfikacja wymaga `owner`.
- Audyt commituje się z zmianą, nie po niej w osobnym połączeniu.
- Usługa nie czyta tabeli audytu sąsiada.

## Poza zakresem

- Eksport audytu na zewnątrz, na `audit.events`, może dopisać relay tej usługi później. Temat już istnieje. Ten numer nie publikuje na niego, żeby nie mieszać łańcucha z projekcją. Publikacja skrótu wpisu, bez `before` i `after`, jest dozwolona później i nie jest warunkiem „gotowe”.
- Panel do przeglądania poza `GET` właściciela nie powstaje.

## Źródła

- [PostgreSQL triggers](https://www.postgresql.org/docs/current/trigger-definition.html). Wiążące jest `BEFORE UPDATE` i wyjątek. Dotyczy append-only. Ze źródła nie bierzemy triggera, który po cichu wraca `NULL` i udaje, że skasował.
- [SHA-256](https://csrc.nist.gov/pubs/fips/180-4/upd1/final). Wiążący jest skrót treści kanonicznej, nie „jakiś hash”. Dotyczy `ComputeHash`. Ze źródła nie bierzemy MD5.
- Numer outboxa. Wiążący jest jeden commit z danymi. Dotyczy audytu w tej samej transakcji co lekcja. Ze źródła nie bierzemy audytu wysyłanego tylko na Kafkę bez wiersza lokalnego.

## Gotowe gdy

- Test wykrywa podmieniony środkowy skrót.
- `DELETE` SQL-em pada.
- JSON utworzenia konta nie zawiera `passwordHash`.
- `GET /audit/verify` jako student zwraca `403`.
- Dwie usługi mają własne tabele, nie jedną wspólną.
