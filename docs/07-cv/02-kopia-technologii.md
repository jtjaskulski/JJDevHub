# 02. Kopia technologii

Poprzedni numer to [sekcje](01-sekcje.md). Następny to [strona publiczna](03-strona-publiczna.md).

Ten numer konsumuje `learning.integration` w Cv. W `cv_read` ląduje ostatnia nazwa technologii i znacznik usunięcia. Strona jeszcze się nie składa. Jest wiersz kopii. Cv nie woła Learning po HTTP i nie otwiera `learning_read`.

## Po co

Skill ma slug. Czytelnik ma zobaczyć nazwę. Dociągnięcie nazwy z Learning przy każdym `GET` strony zwiąże CV z dostępnością katalogu i z kształtem cudzej bazy. Kopia z tematu kompaktowanego zamyka to: po restarcie Cv czyta ostatni stan klucza, bez historii pośrednich nazw i bez logowania do Learning.

## Dlaczego ta technologia

### Temat już jest

`learning.integration` jest kompaktowany i niesie `IntegrationStateV1`. Cv dostaje grupę `cv-learning`. Problem, który zamyka, to drugi temat „specjalnie dla CV” z tą samą treścią. Odpada wołanie `GET /learning/technologies/{slug}` z projektora. Koszt: Cv zna rekord z `Learning.Contracts`. Wolno. Nie zna `Learning.Domain`.

### Klucz i compaction

Klucz wiadomości to slug. Compaction zostawia ostatnią wartość. Nowy projektor, który czyta od początku, dostaje aktualny stan, nie wszystkie historyczne nazwy. Problem, który zamyka, to odtwarzanie nazwy z całego `learning.catalog`. Odpada konsumpcja katalogu przez Cv. Katalog ma lekcje i rewizje, których CV nie potrzebuje. Koszt: Learning musi publikować stan przy każdej zmianie nazwy. To już robi handler technologii.

### Usunięcie

Gdy `deleted` jest true, kopia zostaje z tym znacznikiem. Skill w strumieniu nie znika. Strona w następnym numerze pokaże slug bez udawania, że technologia nadal jest w katalogu. Problem, który zamyka, to skasowanie kopii, po którym nie wiadomo, czy sluga nigdy nie było. Odpada `DELETE` wiersza kopii jako jedyna obsługa. Koszt: wiersz zostaje.

### Inbox

Ten sam wzorzec `(consumer, message_id)`. Compaction nie zastępuje inboxu: powtórzona ostatnia wiadomość po rebalansie nie może psuć znacznika. Upsert i tak jest idempotentny po slugu. Inbox zostaje, bo reguła jest jedna dla wszystkich konsumentów, nie dlatego że upsert by nie wystarczył. Przy samym upsertcie ostatni stan wygrywa. Inbox jest na wypadek skutku, który nie jest idempotentny. Tu skutek jest idempotentny. Zostawiamy inbox, żeby nie uczyć wyjątku.

### Kolejność

Wersja stanu z wiadomości jest w kopii. Starsza wersja nie nadpisuje nowszej, jeśli przyjdzie później. Porównanie `stateVersion` w wierszu. Problem, który zamyka, to wyścig dwóch wiadomości tego samego sluga przy dwóch partiach, którego compaction i jeden konsument w grupie i tak nie powinny zrobić na jednym kluczu. Klucz jest slugiem, więc jedna partycja. Zabezpieczenie jest tanie.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| kopia | wiersz w `cv_read`, nie źródło katalogu |
| compaction | na topicu zostaje ostatni rekord klucza |
| `deleted` | technologia zniknęła z katalogu, slug w skillu zostaje |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Cv.Projector/LearningIntegrationConsumer.cs` | grupa `cv-learning` |
| `Cv.Infrastructure/Read/TechnologyCopyStore.cs` | upsert do `technology_copy` |
| `tests/Cv.Projector.Tests/StaleVersionTests.cs` | starsza wersja nie nadpisuje |

Tabela `technology_copy`: `slug` klucz, `name_pl`, `name_en`, `deleted`, `state_version`.

## Plan

### 1. Konsument

Czyta tylko `learning.integration`. Ignoruje inne topiki. Deserializuje `IntegrationStateV1`. `kind` inny niż `technology` jest pomijany z commitem offsetu: stan kursu CV na tym numerze nie pokazuje, chyba że projekt w CV ma slug technologii, a nie slug kursu. Stan kursu wolno zapisać obok, w `course_copy`, albo pominąć. Bierzemy pominięcie kursu z commitem offsetu, bez błędu. CV nie renderuje listy kursów. Renderuje skill i projekt ze slugiem technologii.

### 2. Upsert

Jeśli `state_version` w wierszu jest większa niż w wiadomości, nie nadpisujemy. W przeciwnym razie nadpisujemy nazwę i `deleted`.

### 3. Brak HTTP

W projekcie projektora nie ma `HttpClient` do Learning. Test architektury szuka `AddHttpClient` w projekcie projektora CV i oczekuje braku.

### Co jest do czego

| Wiadomość | Skutek |
| --- | --- |
| stan technologii | upsert `technology_copy` |
| stan kursu | offset schodzi, brak wiersza |
| starsza wersja | brak zmiany wiersza |
| `deleted=true` | wiersz zostaje, flaga true |

## Testy

Nazwa: `Older_state_does_not_overwrite_newer_copy`.

| Arrange | Act | Assert |
| --- | --- | --- |
| wiersz z `state_version` 5 | wiadomość z wersją 4 | nazwa w wierszu bez zmian |

```bash
dotnet test tests/Cv.Projector.Tests --nologo --filter "FullyQualifiedName~Older_state"
```

## Checkpoint

```bash
dotnet test tests/Cv.Projector.Tests --nologo
aspire run
```

Zmiana nazwy technologii w Learning po chwili jest widoczna w `technology_copy`. Zatrzymany Learning nie przeszkadza `SELECT` kopii. W logu projektora CV nie ma żądania HTTP do Learning.

## Umiesz wyjaśnić

Dlaczego nie `GET` do Learning?

Bo publiczne CV zależałoby od tego, czy katalog stoi, i od kształtu jego API. Kopia jest w bazie CV. `GET` strony czyta jedną bazę.

Co compaction daje przy restarcie?

Konsument od offsetu zero dostaje ostatnią wartość każdego sluga, nie całą historię nazw. Mniej wiadomości, ten sam wynik upsertu.

Dlaczego skill nie znika, gdy technologia jest `deleted`?

Bo w dniu publikacji CV slug był prawdziwy. Strona ma pokazać, że celu już nie ma, a nie udawać, że skillu nie było. Usunięcie skillu jest osobną decyzją właściciela.

Czemu starsza wersja nie wygrywa?

Bo kolejność dostawy przy błędzie producenta albo przy powtórce nie może cofnąć nazwy, którą już przyjęliśmy z wyższą wersją. Wersja jest w stanie publikowanym przez Learning.

Dlaczego inbox zostaje przy idempotentnym upsertcie?

Bo każdy konsument w produkcie ma inbox. Wyjątek „tu nie trzeba” jest pierwszym miejscem, w którym ktoś doda skutek uboczny i zapomni. Koszt wiersza jest mniejszy niż wyjątek w regule.

## Zasady

- Cv nie ma connection stringa Learning.
- Projektor CV nie ma klienta HTTP do Learning.
- `deleted` nie kasuje wiersza kopii.
- Starsza `state_version` nie nadpisuje.
- Referencja do `Learning.Contracts` jest dozwolona. Do `Learning.Domain` nie.
- Skill w `cv_write` nie jest aktualizowany automatycznie, gdy zmieni się nazwa. Aktualizuje się kopia nazwy.

## Poza zakresem

- Skład strony z sekcji i kopii jest w [03-strona-publiczna.md](03-strona-publiczna.md).
- Ekran technologii w katalogu zostaje w Learning.

## Źródła

- [Log compaction](https://kafka.apache.org/documentation/#compaction). Wiążące jest zostawienie ostatniej wartości klucza. Dotyczy `learning.integration`. Ze źródła nie bierzemy kompaktowania `cv.sections`.
- Numer kontraktów Learning. Wiążący jest `IntegrationStateV1`. Dotyczy konsumenta CV. Ze źródła nie bierzemy lekcji na tym topicu.
- [Database per service](https://microservices.io/patterns/data/database-per-service.html). Wiążące jest nieczytanie cudzej bazy. Dotyczy zakazu `learning_read` w Cv. Ze źródła nie bierzemy synchronicznego HTTP jako zamiennika kopii na ścieżce strony.

## Gotowe gdy

- Test starszej wersji nie zmienia nazwy.
- Po zmianie technologii w Development kopia w `cv_read` się zmienia.
- W projektorze CV nie ma `HttpClient` do Learning.
- Wiersz z `deleted` zostaje.
- Cv nie ma connection stringa do baz Learning.
