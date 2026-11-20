# 01. Sekcje CV

Poprzedni numer to [odczyt notatki](../06-news/04-odczyt-markdown.md). Następny to [kopia technologii](02-kopia-technologii.md).

Ten numer stawia sekcje CV jako osobne strumienie: `Profile`, `Position`, `Project`, `Education`, `Activity`, `Skill`, `Language`. Nie ma klasy `Cv`. `Skill` trzyma slug technologii, nie klucz z bazy Learning. Zapis jest tym samym portem `IEventStore` co notatka, w bazie `cv_write`.

## Po co

Jedna tabela „cv” z JSON całej strony gubi historię stanowiska i miesza siedem reguł w jednym dokumencie. Osobny strumień na sekcję zostawia brzmienie stanowiska i pozwala poprawić jedno miejsce bez przepisywania reszty. Klasa `Cv` w namespace `Cv` zepsułaby kompilator i model: CV nie jest jednym agregatem, jest stroną złożoną z sekcji.

## Dlaczego ta technologia

### Sześć strumieni plus profil

Siedem typów, każdy ze swoim `stream_id`: `profile/{id}`, `position/{id}` i dalej. Problem, który zamyka, to jeden strumień na całe CV, w którym wersja stanowiska blokuje zapis języka. Odpada jeden agregat `Cv`. Koszt: strona publiczna musi złożyć siedem dokumentów. Złoży je odczyt, nie `GET` wołający siedem razy `Replay`.

### Slug technologii w `Skill`

Pole `TechnologySlug` jest `Slug`. Brak FK do `learning_write`. Problem, który zamyka, to złączenie do cudzej bazy. Odpada identyfikator `Guid` technologii z Learning. Koszt: nazwa wyświetlana może być nieaktualna, dopóki nie ma kopii z tematu kompaktowanego. Kopia jest następnym numerem. Tu wolno zapisać skill, nawet jeśli Learning jeszcze nie potwierdzi sluga. Sprawdzenie „czy slug istnieje” przez HTTP do Learning nie wchodzi. Strona pokaże slug, a nazwę, gdy kopia dojdzie.

### Ten sam magazyn

`JJDevHub.EventStore.Postgres` dostaje connection string `cv_write`. Nie ma drugiej implementacji SQL. Problem, który zamyka, to skopiowany `INSERT` w Cv. Odpada własna tabela „bo CV jest inne”. Tabela ma ten sam kształt. Inna jest tylko baza i typy zdarzeń. Koszt: migracja tworzy `stream_events` w `cv_write` tym samym skryptem schematu, sparametryzowanym bazą, nie wklejonym drugi raz z inną kolumną.

### Zdarzenia

Każda sekcja ma utworzenie i rewizję. Nazwy: `cv.position-created.v1`, `cv.position-revised.v1` i analogicznie. Atrybut na zdarzeniu. `Skill` nie zmienia sluga technologii w rewizji: zmiana technologii to nowy skill albo jawne pole, które wolno zmienić, bo to nie jest adres URL skillu. Adresem publicznym skillu jest slug technologii. Zmiana slugu w skillu zmienia miejsce na stronie. Dlatego slug w `Skill` jest zamrożony jak slug artykułu. Pomyłka poprawia się usunięciem. Usunięcie jest zdarzeniem `cv.skill-removed.v1`, nie `DELETE` wierszy strumienia. Strumień zostaje.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| sekcja | jeden agregat CV, jeden strumień |
| `Skill` | umiejętność wskazująca slug technologii |
| usunięcie | zdarzenie, nie skasowanie historii |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/services/Cv/Cv.Domain/Profile.cs` i pozostałe sekcje | reguły i `Apply` |
| `src/services/Cv/Cv.Domain/Skill.cs` | slug zamrożony |
| `Cv.Application/UseCases/` | handler na komendę |
| `Cv.Api/Endpoints/` | `owner`, `If-Match` przy rewizji |
| `tests/Cv.Domain.Tests/SkillTests.cs` | zamrożenie sluga |

Nie ma pliku `Cv.cs` w domenie.

## Plan

### 1. Profil

Jeden profil właściciela. Drugi `Create` zwraca `cv.profile.exists`. Nie ma multi-CV. Identyfikator profilu jest stały po pierwszym utworzeniu.

### 2. Skill

`Create` przyjmuje slug i poziom jako `LocalizedText` albo krótki kod `level`, nie HTML. `Revise` nie przyjmuje nowego sluga. Test próbuje zmienić slug i dostaje `cv.skill.slug_frozen`.

### 3. Zapis

Ta sama kolejność co notatka: `Read`, `Replay`, metoda, `Append`, outbox na `cv.sections` albo na temat, który projektor CV czyta. Temat `cv.sections` dopisujemy do `create-topics.sh`, 3 partycje, bez kompaktowania. Słownik warto uzupełnić w tym samym scaleniu, jeśli tematu tam nie było. Był `learning.integration` dla kopii, nie było tematu sekcji. Dopisujemy `cv.sections` do słownika w tym numerze.

### Co jest do czego

| Sekcja | Stream id | Czy niesie slug technologii |
| --- | --- | --- |
| `Profile` | `profile/{id}` | nie |
| `Position`, `Project`, `Education`, `Activity`, `Language` | własny prefiks | nie, poza `Project`, który wolno ma listę slugów technologii jako wartości |
| `Skill` | `skill/{id}` | tak, zamrożony |

`Project` trzyma listę slugów, nie nazw. Nazwy dojdą z kopii.

## Testy

Nazwa: `Revise_skill_slug_returns_frozen`.

| Arrange | Act | Assert |
| --- | --- | --- |
| skill ze slugiem `dotnet` | rewizja z slugiem `java` | `cv.skill.slug_frozen`, w zdarzeniach nadal `dotnet` |

```bash
dotnet test tests/Cv.Domain.Tests --nologo
```

## Checkpoint

```bash
dotnet test tests/Cv.Domain.Tests --nologo
test ! -f src/services/Cv/Cv.Domain/Cv.cs
```

Plik `Cv.cs` nie istnieje. Test zamrożenia przechodzi bez kontenera.

## Umiesz wyjaśnić

Dlaczego nie ma klasy `Cv`?

Bo strona CV nie jest jedną granicą zapisu. Stanowisko i język nie muszą mieć wspólnej wersji. Klasa o nazwie segmentu namespace też myli kompilator. Stronę złoży odczyt.

Dlaczego skill nie trzyma `Guid` z Learning?

Bo to klucz cudzej bazy. Slug jest adresem, który przeżyje przebudowę identyfikatorów. Nazwa wyświetlana przyjdzie kopią, nie złączeniem.

Dlaczego usunięcie nie kasuje wierszy strumienia?

Bo wtedy historia skillu znika i odtworzenie nie umie pokazać, że skill kiedyś był. Zdarzenie usunięcia jest faktem. Projekcja przestanie go pokazywać.

Czym profil różni się od reszty?

Jest jeden. Drugi profil nie jest funkcją produktu. Osobne strumienie stanowisk może być wiele.

Dlaczego temat sekcji nie jest kompaktowany?

Bo fakt „stanowisko zrewidowano” nie jest ostatnim stanem klucza dla obcej usługi w tym numerze. Ostatni stan technologii dla CV jedzie osobnym, kompaktowanym tematem z Learning. Sekcje czyta własny projektor CV i chce kolejne wersje, a i tak zapisuje dokument bieżący. Compaction na sekcjach zgubiłaby zdarzenia, których projektor jeszcze nie widział, jeśli czyta od zera po awarii i liczy na pełną historię. Projektor sekcji może czytać strumień. Na szynę i tak wysyłamy fakt, jak w News, i projektor buduje dokument z faktów, nie z pełnego replay w `GET`. Compaction by została tylko gdyby na temat wchodził cały stan. Nie wchodzi. Dlatego temat nie jest kompaktowany.

## Zasady

- Brak klasy `Cv` w domenie.
- Brak connection stringa do `learning_write`.
- Slug w `Skill` nie zmienia się w rewizji.
- Usunięcie jest zdarzeniem.
- Jeden profil.
- Magazyn to `IEventStore` na `cv_write`.

## Poza zakresem

- Kopia nazwy technologii jest w [02-kopia-technologii.md](02-kopia-technologii.md).
- Złożenie strony jest w [03-strona-publiczna.md](03-strona-publiczna.md).
- Ekran edytora jest w [06-intranet.md](../11-web/06-intranet.md).

## Źródła

- Numer strumienia News. Wiążący jest ten sam `IEventStore` i konflikt wersji. Dotyczy sekcji. Ze źródła nie bierzemy tabeli innej niż `stream_events` dla historii sekcji.
- Numer Learning o slugu. Wiążące jest zamrożenie adresu. Dotyczy skillu. Ze źródła nie bierzemy FK.
- Eric Evans, *Domain-Driven Design*. Wiążące jest to, że agregat jest granicą spójności, nie nazwą produktu. Dotyczy braku klasy `Cv`. Ze źródła nie bierzemy jednego agregatu na całą stronę.

## Gotowe gdy

- Test zamrożenia sluga przechodzi.
- W domenie nie ma pliku klasy `Cv`.
- Drugi profil zwraca `cv.profile.exists`.
- Usunięcie skillu dopisuje zdarzenie i nie woła `DELETE` na `stream_events`.
- Temat `cv.sections` jest w skrypcie topiców i w słowniku.
