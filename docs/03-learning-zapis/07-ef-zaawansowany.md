# 07. EF: konwertery, interceptory, dziedziczenie, scaffold

Poprzedni numer to [kurs i lekcja](06-kurs-i-lekcja.md). Następny folder zaczyna się od [kontraktów](../04-zdarzenia/01-kontrakty.md).

Ten numer dopina mapowanie, które w zapisie Learning jest prawdziwe, i oddziela od niego to, co jest tylko próbą. Konwertery `Slug` i `LocalizedText` są produktem. Interceptor wolnego polecenia i interceptor wersji są produktem. Dziedziczenie TPH jest próbą w teście, bo w Learning nie ma hierarchii, która chce jedną tabelę. Scaffold jest próbą na kontenerze i nie staje się modelem.

## Po co

Bez konwertera `Slug` EF albo nie startuje, albo zapisuje struct jako coś, czego nie da się odczytać. Bez interceptora każde `SaveChanges` w handlerze musi pamiętać o podbiciu `version`, i któreś nie pamięta. Bez jawnej decyzji o TPH ktoś złoży `Course` i `Lesson` w jedną tabelę „bo tak się da”. Bez jednego przebiegu scaffoldu database-first zostaje hasłem. Przebieg ma pokazać, że umiemy go zrobić, i że nie jest źródłem prawdy.

## Dlaczego ta technologia

### Konwerter i comparer dla `Slug`

`ValueConverter<Slug, string>` zapisuje `slug.Value`. `ValueComparer` porównuje wartością, nie referencją. Problem, który zamyka, to tracker, który uważa encję za brudną przy każdym odczycie, bo struct porównał się referencją. Odpada kolumna `text` mapowana na `string` w encji. Koszt: zapytanie LINQ po `Slug` musi być tłumaczone. Test sprawdza, że filtr `Where(x => x.Slug == slug)` wykonuje się na serwerze: log SQL zawiera warunek, a nie ściąga całej tabeli. Jeśli tłumaczenie nie działa, filtr zostaje po stronie SQL przez porównanie do `slug.Value` w konfiguracji, nie przez `ToList()` i filtr w pamięci.

`LocalizedText` ma konwerter na `jsonb` i comparer po zawartości mapy.

### Interceptor wolnego polecenia

`SlowCommandInterceptor` dziedziczy z `DbCommandInterceptor` i loguje polecenie trwające dłużej niż 200 ms. Nie rzuca. Problem, który zamyka, to milczące zapytanie, które przytrzymuje wątek. Odpada logowanie każdego `SELECT 1`. Koszt: próg 200 ms jest stałą w jednym miejscu. Zmiana progu jest zmianą kodu, nie kliknięciem.

### Interceptor wersji

`VersionInterceptor` przy `SaveChanges` podbija `version` encji `Modified`. Problem, który zamyka, to handler, który zapomina `entity.Version++` i token współbieżności stoi w miejscu, więc drugi zapis zawsze przechodzi. Odpada podbijanie w każdej metodzie domeny. Koszt: interceptor musi ruszać tylko encje, które mają wersję. `StudentProgress` jej nie ma i interceptor go pomija.

To nie zastępuje tokenu. Token sprawdza, że `UPDATE` widział starą wartość. Interceptor ustawia nową. Oba są potrzebne.

### TPH, TPT, TPC

TPH trzyma hierarchię w jednej tabeli z dyskryminatorem. TPT w tabeli na typ. TPC w tabeli na typ konkretny, bez tabeli bazowej. Problem, który zamyka zła decyzja, to `Course` i `Lesson` w jednej tabeli: mają inne kolumny i inną unikalność. Odpada TPH dla nich. Odpada też TPT, które dokłada złączenia bez wspólnego życia obiektu. Zostają osobne tabele, tak jak w migracji.

Żeby dyskryminator nie był tylko akapitem, test `Tph_probe_filters_by_discriminator` mapuje w kontekście testowym, nie produkcyjnym, dwie klasy `ProbeCourse` i `ProbeArticle` dziedziczące po `ProbeNode`. `HasDiscriminator<string>("kind")`. Test zapisuje obie i czyta jedną. Kontekst próby nie jest rejestrowany w `AddLearningPersistence`. Nie ma migracji próby w katalogu produktu.

### Scaffold

`dotnet ef dbcontext scaffold` przeciw kontenerowi, na którym właśnie przeszła migracja produktu, wypisuje kontekst do katalogu tymczasowego. Checkpoint sprawdza kod wyjścia zero i usuwa katalog. Plik ze scaffoldu nie wchodzi do solucji. Problem, który zamyka, to database-first jako drugie źródło modelu: jutrzejsza migracja i wczorajszy scaffold kłamią inaczej. Odpada zastąpienie `LearningDbContext` wynikiem scaffoldu. Koszt: numer uczy polecenia i każe je wyrzucić. To jest cel.

Scaffold używa `--table lessons` i connection stringa z kontenera testowego. Hasło kontenera nie wchodzi do git.

### Expression trees, jednym akapitem przy zapytaniu

Filtr LINQ, który trafia do SQL, jest drzewem wyrażeń. Filtr po `ToList()` jest delegatem w pamięci. Test konwertera sluga jest miejscem, w którym to widać: log SQL albo `ToQueryString()` zawiera kolumnę `slug`. Nie budujemy własnego `ExpressionVisitor` w produkcie. Własny visitor bez drugiego konsumenta jest opisem kompilatora, nie funkcją Learning.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| value converter | tłumaczenie typu CLR na kolumnę |
| comparer | jak tracker poznaje, że wartość się zmieniła |
| interceptor | kod wpięty w potok EF, nie w encję |
| TPH | jedna tabela, kolumna dyskryminatora |
| scaffold | wygenerowanie kontekstu z istniejącej bazy |
| expression tree | kształt LINQ, który dostawca może zamienić na SQL |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Learning.Infrastructure/Persistence/Converters/SlugConverter.cs` | `Slug` na `string` |
| `Learning.Infrastructure/Persistence/Converters/LocalizedTextConverter.cs` | mapa na `jsonb` |
| `Learning.Infrastructure/Persistence/Interceptors/SlowCommandInterceptor.cs` | log ponad 200 ms |
| `Learning.Infrastructure/Persistence/Interceptors/VersionInterceptor.cs` | podbicie `version` |
| `tests/Learning.Infrastructure.Tests/TphProbeTests.cs` | dziedziczenie poza produktem |
| `tests/Learning.Infrastructure.Tests/ScaffoldProbeTests.cs` | scaffold do katalogu tymczasowego, `Category=Integration` |

## Plan

### 1. Konwertery w `ConfigureConventions`

`LearningDbContext.ConfigureConventions` rejestruje konwerter i comparer `Slug` dla całej właściwości tego typu. Konfiguracja lekcji nie powtarza tego dla każdej kolumny sluga.

### 2. Interceptory przy `AddDbContext`

Oba interceptory są `Singleton` i wchodzą do `AddDbContext` przez `AddInterceptors`. `VersionInterceptor` w `SavingChanges` dla stanu `Modified` robi `version + 1`. Dla stanu `Added` zostawia `1`, jeśli fabryka ustawiła `1`, albo ustawia `1`, jeśli jest zero. Jedna z tych dwóch dróg jest w kodzie, nie obie naraz. Wybór: fabryka ustawia `1`, interceptor podbija tylko `Modified`.

### 3. Próba TPH

Kontekst w projekcie testowym. Migracja próby to `EnsureCreated` na sqlite w pamięci albo na schemacie w kontenerze, który i tak wstaje. Sqlite w pamięci wystarcza, bo próba nie sprawdza jsonb Postgresa. Produkcyjny kontekst nie dostaje `ProbeNode`.

### 4. Scaffold

Test integracyjny uruchamia proces `dotnet ef dbcontext scaffold` z argumentami do katalogu `Path.GetTempPath()`. Asercja: kod wyjścia `0` i plik `*.cs` w tym katalogu zawiera `lesson`. Potem `Directory.Delete`. Test nie zostawia pliku w repozytorium. Jeśli narzędzie `dotnet-ef` nie jest zainstalowane na runnerze, job instaluje je jako narzędzie lokalne z manifestu `dotnet-tools.json` w repozytorium. Wersja narzędzia jest przypięta i zgodna z EF 10.

### Co jest do czego

| Klasa | Czy jedzie na maszynę |
| --- | --- |
| konwertery i interceptory | tak |
| `ProbeNode` | nie, tylko test |
| wynik scaffoldu | nie, katalog tymczasowy |

## Testy

Nazwa: `Slug_filter_translates_to_sql`.

| Arrange | Act | Assert |
| --- | --- | --- |
| jedna technologia w kontenerze | zapytanie `Where` po `Slug` i `ToQueryString()` | SQL zawiera `slug` i nie wymaga `ToList` przed filtrem |

Nazwa próby: `Tph_probe_filters_by_discriminator`. Asercja: odczyt typu bazowego zwraca dwa wiersze, odczyt typu pochodnego jeden.

```bash
dotnet test tests/Learning.Infrastructure.Tests --nologo --filter "FullyQualifiedName~Slug_filter_translates|FullyQualifiedName~Tph_probe"
```

## Checkpoint

```bash
dotnet test tests/Learning.Infrastructure.Tests --nologo --filter "Category!=Performance"
git status --short
```

`git status` nie pokazuje plików ze scaffoldu. W `Learning.Infrastructure` nie ma klasy `ProbeNode`.

## Umiesz wyjaśnić

Dlaczego comparer jest osobno od konwertera?

Konwerter mówi, jak zapisać wartość w kolumnie. Comparer mówi, jak poznać, że właściwość się zmieniła w pamięci. Bez comparera tracker na struct potrafi uznać encję za zmienioną zaraz po odczycie i zrobić pusty `UPDATE`.

Co robi interceptor wersji, czego nie robi token?

Token pilnuje, że nikt nie zapisał wiersza między naszym odczytem a zapisem. Interceptor wylicza nowy numer. Sam token ze stałą wartością `1` na zawsze nie wykryje drugiego zapisu, jeśli oba czytają `1` i oba zapisują `1`.

Dlaczego `Course` i `Lesson` nie są TPH?

Bo nie mają wspólnego wiersza. Unikalność kolejności lekcji i unikalność sluga kursu to inne reguły. Jedna tabela z dyskryminatorem zostawia puste kolumny i gubi te reguły w `CHECK`, których nikt nie dopisze.

Czym scaffold różni się od migracji?

Migracja idzie od modelu w kodzie do bazy i jest w git. Scaffold idzie od bazy do kodu i u nas jest ćwiczeniem na kontenerze. Dwa źródła prawdy rozjadą się przy pierwszej ręcznej poprawce w bazie.

Gdzie tu jest expression tree?

W `IQueryable`. `Where` przed materiałem z bazy jest drzewem, które Npgsql zamienia na SQL. `Where` po `ToList` jest metodą w pamięci i ściąga tabelę. Test `ToQueryString` pilnuje, że nie zrobiliśmy drugiego.

## Zasady

- `ProbeNode` nie wchodzi do produktu i nie ma migracji w `Migrations/`.
- Wynik scaffoldu nie jest commitowany.
- Próg 200 ms jest w interceptorze, nie w każdej metodzie repozytorium.
- Wersję podbija interceptor, nie endpoint.
- Filtr po slugu nie ściąga całej tabeli do pamięci.

## Poza zakresem

- Outbox jako interceptor albo jako jawny zapis w handlerze rozstrzyga [02-outbox.md](../04-zdarzenia/02-outbox.md). Ten numer nie zapisuje zdarzeń.
- Audyt append-only ma własny interceptor w [04-audyt.md](../05-tozsamosc/04-audyt.md). Tu nie ma tabeli audytu.
- Odczyt Dapperem nie używa tego kontekstu.

## Źródła

- [Value conversions](https://learn.microsoft.com/ef/core/modeling/value-conversions). Wiążące są converter i comparer. Dotyczy `Slug` i `LocalizedText`. Ze źródła nie bierzemy konwertera, który łyka wyjątek i zapisuje pusty string.
- [Interceptors](https://learn.microsoft.com/ef/core/logging-events-diagnostics/interceptors). Wiążące jest wpięcie w `SaveChanges` i w polecenie, bez zmiany encji o EF. Dotyczy wersji i wolnego SQL. Ze źródła nie bierzemy interceptora, który otwiera drugie połączenie w `SavingChanges`.
- [Inheritance](https://learn.microsoft.com/ef/core/modeling/inheritance). Wiążące są TPH, TPT i TPC oraz to, że domyślne jest TPH. Dotyczy odmowy TPH dla kursu i lekcji oraz próby w teście. Ze źródła nie bierzemy TPH tylko dlatego, że jest domyślne.
- [Scaffolding](https://learn.microsoft.com/ef/core/managing-schemas/scaffolding/). Wiążące jest polecenie `dbcontext scaffold`. Dotyczy próby na kontenerze. Ze źródła nie bierzemy scaffoldu jako modelu produktu.
- [LINQ expression trees](https://learn.microsoft.com/dotnet/csharp/advanced-topics/expression-trees/). Wiążące jest rozróżnienie drzewa i delegata. Dotyczy filtra, który ma zostać w SQL. Ze źródła nie bierzemy ręcznie budowanego drzewa w endpoincie.

## Gotowe gdy

- Filtr po `Slug` jest widoczny w `ToQueryString()`.
- `VersionInterceptor` podbija `Modified` i test konfliktu z numeru EF nadal przechodzi.
- Polecenie powyżej 200 ms zostawia log, krótsze nie musi.
- Próba TPH przechodzi i nie ma jej klas w projekcie infrastruktury produktu.
- Scaffold wraca kodem zero i `git status` jest czysty.
- `dotnet-tools.json` przypina `dotnet-ef`.
