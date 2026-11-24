# 03. Build, testy i format

Poprzedni numer to [gałęzie](02-galezie.md). Następny to [CodeQL](04-codeql.md).

Ten numer dodaje checki `build`, `unit-tests`, `format`, `integration-tests` i `performance` oraz skrypt komponentów dotkniętych zmianą. Pięć jobów woła jeden skrypt, `.github/scripts/dotnet-gate.sh`. Na drzewie bez projektu cztery z nich kończą się jawnym brakiem materiału. Od pierwszego projektu ten sam skrypt kompiluje albo testuje solucję, a format sprawdza całe drzewo.

## Po co

Bez joba `build` ruleset nie ma czego wymagać, a pierwszy projekt wchodzi czerwony albo wcale nie jest kompilowany. Bez osobnego `unit-tests` i `integration-tests` ktoś wyłącza kontenery filtrem i nazywa to pełnym testem. Bez `format` spór o spacje jest recenzją. Bez `performance` test obciążenia nie ma gdzie żyć, gdy dojdzie w fazie testów.

Filtr `paths` na `on` pomija job, a pominięty job nie daje statusu. Dlatego każdy z tych workflow startuje zawsze i sam decyduje, czy ma materiał.

## Dlaczego ta technologia

### Jeden job, jedna nazwa

Nazwa joba jest nazwą checka. Problem, który zamyka, to workflow `ci` z jobem `test`, którego ruleset szuka pod inną nazwą. Odpada jeden plik YAML ze wszystkimi checkami, bo padnięcie formatu zlewa się z padnięciem testu w jednym statusie. Koszt: pięć plików i pięć wpisów w rulesecie. Logika i tak jest w jednym skrypcie, żeby próg `no material` nie rozjechał się między kopiami.

### `dotnet format` i `.editorconfig`

Format jest checkiem, nie sugestią IDE. Problem, który zamyka, to diff złożony ze zmiany stylu i zmiany reguły. Odpada samo ustawienie „formatuj przy zapisie” na laptopie, bo runner go nie ma. Koszt: `.editorconfig` z [numeru solucji](01-solucja.md) jest kontraktem. Zmiana reguły stylu formatuje całe drzewo osobnym pull requestem o typie `chore`.

### Komponenty dotknięte zmianą

Skrypt `.github/scripts/affected-components.sh` mówi, które komponenty ruszył diff. Problem, który zamyka, to przebudowa Identity przy zmianie w News. Odpada zawsze-wszystko, gdy solucja dorośnie do pięciu usług. Koszt: zmiana w `JJDevHub.Shared.Kernel` oznacza wszystkie komponenty, które mają do niego `ProjectReference`. Skrypt to liczy z grafu projektów, nie z listy w komentarzu.

Wywołanie bez ścieżek wypisuje pustą listę. To nie jest `no material` joba builda. Build ma własny warunek: brak `*.csproj`.

### xUnit jako jedyny runner

[Technologie](../00-fundamenty/technologie.md) przypinają xunit.v3. Ten numer jeszcze nie dodaje projektu testowego. Ustala, że job szuka testów komendą `dotnet test` i filtrem cechy. Drugi runner nie wchodzi. W `global.json` z numeru solucji dochodzi klucz, którego ten runner wymaga.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| `no material` | jawny log i kod `0`, gdy nie ma czego sprawdzić |
| cecha | `Category=Integration` albo `Category=Performance` na teście |
| komponent dotknięty | katalog usługi, którego diff albo zależność od zmienionego projektu dotyczy |
| suchy przebieg | `tests/ci/build-format.sh`, kopia skryptu w pustym katalogu i jeden celowo źle złożony plik |

## Klasy i ścieżki

| Ścieżka | Check albo rola |
| --- | --- |
| `.github/scripts/dotnet-gate.sh` | pięć gałęzi: `build`, `unit-tests`, `format`, `integration-tests`, `performance` |
| `.github/workflows/build.yml` | `build`; jedyny z tej piątki, który odpala suchy przebieg |
| `.github/workflows/unit-tests.yml` | `unit-tests` i artefakt `coverage` |
| `.github/workflows/format.yml` | `format` |
| `.github/workflows/integration-tests.yml` | `integration-tests` |
| `.github/workflows/performance.yml` | `performance` |
| `.github/scripts/affected-components.sh` | lista komponentów; woła ją też [skan obrazu](08-obrazy.md) |
| `tests/ci/build-format.sh` | pusty katalog, pusta lista komponentów, białe znaki w próbce |
| `.editorconfig` | reguły `dotnet format`, plik z [numeru solucji](01-solucja.md) |
| `global.json` | klucz `test.runner`, dopisany do pliku z numeru solucji |

Wszystkie joby: `runs-on: ubuntu-latest`, setup SDK z `global.json` tam, gdzie jest `dotnet`. `unit-tests` ma dodatkowo `actions: write`, bo wgrywa artefakt. Reszta ma `contents: read`.

## Plan

### 1. Jeden skrypt, pięć nazw

`.github/scripts/dotnet-gate.sh` bierze dokładnie jeden argument. Inna liczba argumentów kończy się kodem `2` i linią `użycie: dotnet-gate.sh <build|unit-tests|format|integration-tests|performance>`. Nieznana nazwa też kończy się kodem `2`.

Wspólna funkcja braku materiału:

```bash
no_material() {
  printf 'no material\n'
  exit 0
}
```

Szukanie plików omija `.git`, `bin`, `obj` i `node_modules`. Workflow nie kopiuje tego warunku do YAML. Każdy job woła skrypt i bierze jego kod wyjścia.

Sprawdzenie samej składni wywołania:

```bash
bash .github/scripts/dotnet-gate.sh; test $? -eq 2
```

### 2. Build

Gałąź `build` w skrypcie: gdy w drzewie nie ma `*.csproj`, log `no material` i kod `0`. Gdy jest, jedna komenda:

```bash
dotnet build JJDevHub.slnx -c Release --nologo
```

Ostrzeżenie jest błędem, bo tak mówi `Directory.Build.props` z [01-solucja.md](01-solucja.md).

`.github/workflows/build.yml` ma trzy kroki: checkout z `persist-credentials: false`, `actions/setup-dotnet` z `global-json-file: global.json`, suchy przebieg `bash tests/ci/build-format.sh`, potem `bash .github/scripts/dotnet-gate.sh build`. Suchego przebiegu nie ma w pozostałych czterech workflow tej piątki. Stoi przy buildzie, bo tam i tak jest SDK, a przykłady dotyczą całego skryptu.

Na dzisiejszym drzewie są projekty, więc żywy krok builda kompiluje solucję. Puste `no material` sprawdza suchy przebieg, na kopii skryptu w katalogu tymczasowym, nie na solucji.

### 3. Testy jednostkowe i pokrycie

Gałąź `unit-tests` najpierw szuka projektu testowego. Projektem testowym jest plik `*Tests.csproj` albo `.csproj`, który referencjonuje xunit albo ma `IsTestProject` równe `true`. Brak takiego projektu to `no material`.

Gdy projekt jest, skrypt dopisuje `~/.dotnet/tools` do `PATH` i wymaga `dotnet-coverage` w wersji `18.12.0`. Inna wersja jest odinstalowywana i wstawiana jest ta. Raport:

```bash
dotnet-coverage collect \
  --output artifacts/coverage/coverage.cobertura.xml \
  --output-format cobertura \
  --nologo \
  -- dotnet test --solution JJDevHub.slnx -c Release \
       --filter 'Category!=Integration&Category!=Performance'
```

Filtr trzyma kontenery i obciążenie poza tym jobem. Test bez cechy leci tutaj.

Runner jest w `global.json`, w kluczu, którego [numer solucji](01-solucja.md) nie uzasadnia:

```json
"test": {
  "runner": "Microsoft.Testing.Platform"
}
```

xunit.v3 idzie przez Microsoft.Testing.Platform. Bez tego klucza `dotnet test` szuka runnera z linii SDK, nie z paczki.

`.github/workflows/unit-tests.yml` nie odpala suchego przebiegu. Kroki: checkout, setup SDK, `dotnet-gate.sh unit-tests`, potem wgranie artefaktu, gdy plik powstał:

```yaml
if: hashFiles('artifacts/coverage/coverage.cobertura.xml') != ''
name: coverage
path: artifacts/coverage/coverage.cobertura.xml
retention-days: 1
if-no-files-found: error
```

Uprawnienie `actions: write` jest tylko na tym jobie. [SonarQube](06-sonarqube.md) czyta ten artefakt. Gdy testów nie ma, plik nie powstaje, warunek `hashFiles` jest pusty i krok wgrania jest pomijany. To nie jest błąd.

Na dzisiejszym drzewie projekty `tests/JJDevHub.Gateway.Tests` i `tests/JJDevHub.ServiceDefaults.Tests` są projektami testowymi, więc żywy job nie loguje `no material`. Pusty sukces sprawdza suchy przebieg.

### 4. Format

Gałąź `format`: brak `*.csproj` i brak `*.cs` to `no material`. Inaczej:

```bash
dotnet format JJDevHub.slnx --verify-no-changes --severity warn
```

Padnięcie wypisuje diagnostykę i nie commituje poprawki. Poprawkę wnosi się do tej samej gałęzi. `.github/workflows/format.yml` woła tylko tę gałąź, po setupie SDK.

Suchy przebieg nie woła tej linii na solucji. Składa tymczasowy `Sample.csproj` i `Sample.cs` z podwójną spacją w `int  value` i woła `dotnet format` na tym projekcie, bez `--severity warn`. Oczekuje kodu `2` i słowa `WHITESPACE` w logu. Kod `2` jest kodem narzędzia przy `--verify-no-changes`, gdy plik jest brudny. Na solucji ten sam skutek kończy job `format` kodem różnym od zera.

### 5. Integracja i obciążenie

Obie gałęzie idą przez `run_filtered_tests`. Filtry:

| Job | Filtr |
| --- | --- |
| `integration-tests` | `Category=Integration\|Category=Contract` |
| `performance` | `Category=Performance` |

Gdy nie ma projektu testowego, znowu `no material`. Gdy projekt jest, skrypt łapie log `dotnet test`. Zero testów albo kod wyjścia `8` (żaden test nie pasuje do filtra) to `no material` i kod `0`. Inny kod wychodzi dalej: padnięcie testu oblewa job. Log `no material` dopisuje się na końcu, po logu `dotnet test`.

Test integracyjny podnosi kontener sam. Job nie dostaje sekretu aplikacji i nie łączy się z maszyną. Workflow `integration-tests.yml` i `performance.yml` mają checkout, setup SDK i jedną gałąź skryptu. Suchego przebiegu tam nie ma.

### 6. Komponenty dotknięte zmianą

`.github/scripts/affected-components.sh` zna osiem korzeni:

| Nazwa | Katalog |
| --- | --- |
| `contact` | `src/services/Contact` |
| `cv` | `src/services/Cv` |
| `gateway` | `src/gateway` |
| `identity` | `src/services/Identity` |
| `learning` | `src/services/Learning` |
| `mobile` | `src/clients/mobile` |
| `news` | `src/services/News` |
| `web` | `src/clients/web` |

Tryb jest jeden. Domyślny wypisuje JSON. `--list-roots` wypisuje katalogi trafionych komponentów. `--list-projects` wypisuje ich `*.csproj`. `--list-packages` wypisuje ich `package.json`. Drugi tryb listy w jednym wywołaniu kończy się kodem `2`.

Ścieżki wchodzą na dwa sposoby. `--from-git` czyta `git diff --name-only origin/$BASE_REF...HEAD` i wymaga zmiennej `BASE_REF`. Bez tej flagi skrypt czyta ścieżki ze stdin, a na terminalu nie czyta nic.

Klasyfikacja jednej ścieżki:

- `*.sln`, `*.slnx`, `Directory.Packages.props` albo `global.json` oznacza całe repozytorium: do listy wchodzi każdy komponent, który ma `*.csproj`.
- Ścieżka w `src/shared/JJDevHub.Shared.Kernel` oznacza jądro. Skrypt dopisuje komponent, którego `.csproj` ma `ProjectReference` do `JJDevHub.Shared.Kernel`.
- Ścieżka pod korzeniem z tabeli dopisuje ten jeden komponent.

Wywołanie bez ścieżek, także na drzewie, w którym projekty już są, wypisuje:

```json
{"components":[],"kernel":false}
```

Pusta lista jest tu uczciwa: skrypt nie zgaduje diffu. [Skan obrazu](08-obrazy.md) woła go z `--from-git`. Suchy przebieg woła go bez argumentów i wymaga dokładnie tego JSON-a.

Sprawdzenie:

```bash
bash .github/scripts/affected-components.sh
printf '%s\n' 'src/services/News/X.cs' | bash .github/scripts/affected-components.sh
```

Pierwsza linia to pusty JSON i kod `0`. Druga to `{"components":["news"],"kernel":false}`.

### 7. Suchy przebieg

`tests/ci/build-format.sh` robi trzy rzeczy, po cichu, gdy wychodzą.

Kopiuje `dotnet-gate.sh` do pustego katalogu i dla `build`, `unit-tests`, `format`, `integration-tests` oraz `performance` wymaga kodu `0` i tekstu `no material`. Solucja produktu nie jest tym katalogiem.

Potem woła `affected-components.sh` bez argumentów i wymaga `{"components":[],"kernel":false}`.

Na końcu składa próbkę z podwójną spacją i wymaga, żeby `dotnet format --verify-no-changes` skończył się kodem `2` oraz żeby w logu było `WHITESPACE`. Katalog próbki jest pod `tests/ci/.format.*` i jest sprzątany.

Job `build` odpala ten plik przed kompilacją solucji. Cisza i kod `0` znaczą, że trzy asercje przeszły. Komunikat `no material` widać tylko wtedy, gdy asercja padnie: skrypt wypisuje go na stderr razem z powodem.

Sprawdzenie:

```bash
bash tests/ci/build-format.sh
```

Kod wyjścia `0`. Na stdout nie ma linii, gdy przebieg jest zielony.

### 8. Ruleset

Do trzech plików w `.github/rulesets/` dochodzi pięć kontekstów, obok trzech z [numeru gałęzi](02-galezie.md):

```text
build
unit-tests
format
integration-tests
performance
```

Nazwa jest ta sama co `name` joba. W drzewie, które czytasz, tablica ma też konteksty późniejszych numerów. Ten krok sprawdza tylko tę piątkę.

```bash
python3 -c 'import json,pathlib
need={"build","unit-tests","format","integration-tests","performance"}
for path in pathlib.Path(".github/rulesets").glob("*.json"):
    data=json.load(path.open())
    checks={item["context"] for item in data["rules"][-1]["parameters"]["required_status_checks"]}
    assert need <= checks, path
'
```

Kod zero znaczy, że trzy rulesety wymieniają pięć checków.

### Co jest do czego

| Job albo plik | Po co żyje |
| --- | --- |
| `build` | czy solucja w ogóle się kompiluje; tu też stoi suchy przebieg |
| `unit-tests` | reguły bez kontenera i plik Cobertura |
| `integration-tests` | baza, broker, kontrakt |
| `performance` | próg opóźnienia, gdy test już jest |
| `format` | styl poza recenzją |
| `affected-components.sh` | późniejsze skanowanie obrazu jednej usługi, nie wszystkich |
| artefakt `coverage` | wejście skanu SonarQube, retencja jeden dzień |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| kopia `dotnet-gate.sh` w katalogu bez `*.csproj` | każda z pięciu gałęzi | log zawiera `no material`, kod `0` |
| `affected-components.sh` bez ścieżek | jedno wywołanie | `{"components":[],"kernel":false}` |
| `Sample.cs` z podwójną spacją | `dotnet format --verify-no-changes` | kod `2`, w logu `WHITESPACE` |
| stdin `src/services/News/X.cs` | `affected-components.sh` | JSON z samym `news` |

Pierwsze trzy wiersze wykonuje `tests/ci/build-format.sh`. Czwarty jest ręcznym sprawdzeniem z kroku 6. Solucja produktu nie jest katalogiem pustym: projekty z Aspire i z bramy zostają.

## Checkpoint

```bash
test -f .github/workflows/build.yml
test -f .github/workflows/unit-tests.yml
test -f .github/workflows/format.yml
test -f .github/workflows/integration-tests.yml
test -f .github/workflows/performance.yml
bash .github/scripts/affected-components.sh
bash tests/ci/build-format.sh
python3 -c 'import json; t=json.load(open("global.json"))["test"]; assert t["runner"]=="Microsoft.Testing.Platform"'
```

Skrypt komponentów wypisuje `{"components":[],"kernel":false}` i kod `0`. Suchy przebieg milczy i zwraca kod `0`. Ostatnie polecenie potwierdza runner. `actionlint` na pięciu workflow milczy.

## Umiesz wyjaśnić

Czym różni się `no material` od joba pominiętego?

`no material` to zielony status z logiem. Job pominięty nie wysyła statusu, a ruleset wtedy czeka albo blokuje. Dlatego nie ma `paths` na `on`.

Dlaczego integracja nie leci w filtrze jednostkowym?

Bo kontener jest wolny i sprawdza co innego niż regułę domeny. Oba joby są wymagane. Żaden nie zastępuje drugiego. Kod `8` z `dotnet test`, gdy filtr nic nie złapał, skrypt zamienia na `no material`.

Co znaczy zmiana w jądrze dla skryptu komponentów?

Ścieżka pod `src/shared/JJDevHub.Shared.Kernel` ustawia `kernel` na `true`. Potem skrypt czyta `ProjectReference` i dopisuje komponenty, które jądro wciągają. Inaczej News zostałby na starym `Result`, a tag by tego nie pokazał.

Dlaczego puste wywołanie `affected-components.sh` nie wymienia bramy, skoro jej projekt już jest?

Bo skrypt nie skanuje drzewa sam z siebie. Czeka na ścieżki ze stdin albo z `--from-git`. Projekt bramy wejdzie na listę, gdy diff ruszy `src/gateway/` albo plik całego repozytorium, na przykład `JJDevHub.slnx`.

Dlaczego format nie poprawia plików na runnerze?

Runner nie commituje do gałęzi pull requestu. Pokazuje diagnostykę. Poprawka jest w gałęzi, którą widać. Suchy przebieg pilnuje, że brudna próbka daje kod `2`, nie cichy sukces.

Kiedy `performance` przestaje być pustym sukcesem?

Gdy w drzewie jest test z `Category=Performance` i `dotnet test` go wykona. Do tego czasu job istnieje, jest wymagany, a skrypt loguje `no material`, także gdy projekt testowy już jest, ale żaden test nie ma tej cechy.

Po co `test.runner` w `global.json`?

Żeby `dotnet test` w gałęzi `unit-tests` użył Microsoft.Testing.Platform. Plik założył [numer solucji](01-solucja.md). Ten numer mówi, kto ten klucz czyta.

Dlaczego suchy przebieg jest tylko w jobie `build`?

Bo jeden przebieg pokrywa pięć gałęzi skryptu i próbkę formatu. Cztery pozostałe joby i tak wołają swoją gałąź na solucji. Drugi suchy przebieg w każdym z nich powtarzałby ten sam katalog tymczasowy.

## Zasady

- `runs-on` tych jobów to `ubuntu-latest`. Etykieta `jjdevhub` tu nie występuje.
- `permissions` to `contents: read`, a `unit-tests` dokłada `actions: write` na czas wgrania Cobertury.
- Filtr `paths` na poziomie `on` jest zabroniony.
- Projekt produkcyjny, gdy już powstanie, ma projekt testowy. Wyjątkiem jest `JJDevHub.AppHost`.
- Test bez cechy nie otwiera połączenia sieciowego poza procesem.
- `dotnet-coverage` stoi na `18.12.0`. Inna wersja globalna jest wymieniana w przebiegu.

## Poza zakresem

- Treść testów usług jest w numerach, które dokładają kod, i domyka się w [folderze testów](../10-testy/01-integracyjne.md).
- Skan kodu jest w [04-codeql.md](04-codeql.md).
- Skan paczek jest w [05-zaleznosci.md](05-zaleznosci.md).
- Odczyt artefaktu `coverage` jest w [06-sonarqube.md](06-sonarqube.md).
- `--from-git` tego skryptu komponentów jest w [08-obrazy.md](08-obrazy.md).

## Źródła

- [dotnet test](https://learn.microsoft.com/dotnet/core/tools/dotnet-test). Wiążący jest `--filter`. Dotyczy rozdziału cech. Ze źródła nie bierzemy jednego joba bez filtra, który ciągnie kontenery przy każdej zmianie komentarza.
- [dotnet format](https://learn.microsoft.com/dotnet/core/tools/dotnet-format). Wiążące jest `--verify-no-changes`. Dotyczy checka `format`. Ze źródła nie bierzemy auto-commita z runnera.
- [EditorConfig](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/). Wiążące są reguły stylu w pliku, nie w ustawieniu IDE. Dotyczy `.editorconfig` w korzeniu. Ze źródła nie bierzemy drugiego pliku stylu na usługę.
- [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro). Wiążący jest `test.runner` w `global.json` dla xunit.v3. Dotyczy gałęzi `unit-tests`. Ze źródła nie bierzemy drugiego runnera obok tej platformy.

## Gotowe gdy

- Pięć workflow istnieje, a `tests/ci/build-format.sh` zwraca kod zero.
- Na kopii skryptu bez `.csproj` każda z pięciu gałęzi loguje `no material` i zwraca kod zero. To sprawdza suchy przebieg.
- Rulesety `develop`, `release` i `main` wymieniają `build`, `unit-tests`, `format`, `integration-tests` i `performance`.
- `affected-components.sh` bez ścieżek zwraca `{"components":[],"kernel":false}` i kod zero.
- `global.json` ma `test.runner` równe `Microsoft.Testing.Platform`.
- W workflow nie ma `pull_request_target` ani sekretu.
