# 06. SonarQube

Poprzedni numer to [zależności](05-zaleznosci.md). Następny to [łańcuch dostaw](07-lancuch-dostaw.md).

Ten numer dodaje check `sonarqube`. Bramka jakości stoi na SonarQube Community Build, w kontenerze, który job podnosi sam. Wynik nie zależy od kliknięć w interfejsie. Próg dotyczy nowego kodu: zero nowych blockerów, zero nowych critical, sto procent przejrzanych hotspotów, duplikacja co najwyżej trzy procent, pokrycie co najmniej osiemdziesiąt procent, gdy są linie do pokrycia.

## Po co

CodeQL szuka wzorców bezpieczeństwa. Nie mierzy, czy nowa metoda ma test, ani czy ten sam blok wklejono trzeci raz. Bez progu pokrycia pierwszy projekt wchodzi bez testu i później próg jest „na potem”. Na potem nie następuje.

Serwer klikany ręcznie rozjeżdża się między przebiegami: ktoś zmieni próg w interfejsie i git o tym nie wie. Dlatego bramka jest plikiem w repozytorium, a kontener jest świeży.

## Dlaczego ta technologia

### Community Build w jobie

Obraz `sonarqube:community`. Licencja serwera jest LGPL i serwer nie jest linkowany do aplikacji. Problem, który zamyka, to brak bramki jakości na pustym koncie chmurowym. Odpada hostowany serwer jako jedyne źródło werdyktu, bo jego stan nie jest w git. Koszt: job jest cięższy. Czeka na health kontenera, ładuje bramkę z pliku i dopiero skanuje.

Na maszynie, od [numeru maszyny](../02-szkielet-i-deploy/04-maszyna.md), może stać ten sam obraz do ręcznego obejrzenia. Werdykt pull requestu i tak liczy kontener z joba. Adres na maszynie nie luzuje progu.

### Próg na nowym kodzie

Nowy kod to diff pull requestu. Problem, który zamyka, to dziedziczenie długu: stary plik bez testu nie blokuje poprawki literówki, ale nowa linia bez pokrycia blokuje. Odpada próg na całym repozytorium od pierwszego dnia, który zmusza do pisania testów do wygenerowanego kodu. Koszt: trzeba umieć pokazać skanerowi, co jest nowe. Robi to parametr pull requestu w wywołaniu skanera, nie ręczna lista plików.

Pokrycie liczy się, gdy są linie do pokrycia. Sam plik workflow albo sam markdown nie ma linii C# i nie oblewa progu osiemdziesięciu procent.

### Token

Token serwera powstaje w jobie, na świeżym kontenerze, i ginie z kontenerem. Nie ma sekretu `SONAR_TOKEN` w ustawieniach repozytorium dla tego przebiegu. Log nie drukuje tokenu. `set +x` wokół kroku, który go wystawia skanerowi. Po zapisie pliku krok dokłada `::add-mask::`, żeby runner zamazał wartość, gdyby jednak wpadła do logu.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| quality gate | zestaw warunków, po których skan jest czerwony albo zielony |
| nowy kod | linie dodane w pull requeście |
| hotspot | miejsce, które wymaga decyzji, nie jest od razu błędem |
| produkcja | `*.cs`, `*.ts` albo `*.tsx` poza katalogiem testów i poza plikiem `*.test.ts` |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `.github/workflows/sonarqube.yml` | job `sonarqube` |
| `.github/scripts/sonar-material.sh` | `material`, `no material` albo `no material: brak produkcji` |
| `.github/scripts/sonar-bootstrap.sh` | hasło, projekt `jjdevhub`, warunki bramki, token do pliku |
| `.github/scripts/sonar-coverage.sh` | artefakt `coverage` z joba `unit-tests` |
| `.github/scripts/sonar-scan.sh` | `dotnet-sonarscanner` `12.1.0` albo CLI z sumą SHA-256 |
| `deploy/sonar/quality-gate.json` | pięć progów, ładowane do kontenera |
| `sonar-project.properties` | klucz `jjdevhub`, katalogi źródeł i testów, wyłączenia |
| `tests/ci/sonarqube.sh` | suchy przebieg pliku bramki, materiału i pustego pokrycia, bez kontenera |

## Plan

### 1. Bramka w pliku

`deploy/sonar/quality-gate.json` ma dokładnie pięć pól. Inny zestaw kluczy `sonar-bootstrap.sh` odrzuca komunikatem `bramka w pliku jest niekompletna`.

```json
{
  "new_blocker": 0,
  "new_critical": 0,
  "new_hotspots_reviewed": 100,
  "new_duplicated_lines_density": 3,
  "new_coverage": 80
}
```

Bootstrap tłumaczy je na metryki serwera. Operator `GT` pada, gdy wartość jest większa od progu. Operator `LT` pada, gdy wartość jest mniejsza.

| Pole w pliku | Metryka | Operator |
| --- | --- | --- |
| `new_blocker` | `new_software_quality_blocker_issues` | `GT` `0` |
| `new_critical` | `new_software_quality_high_issues` | `GT` `0` |
| `new_hotspots_reviewed` | `new_security_hotspots_reviewed` | `LT` `100` |
| `new_duplicated_lines_density` | `new_duplicated_lines_density` | `GT` `3` |
| `new_coverage` | `new_coverage` | `LT` `80` |

Job tworzy projekt `jjdevhub` i bramkę o tej nazwie przy każdym przebiegu, potem przypina bramkę do projektu. Zmiana progu jest zmianą pliku.

Zakres skanu jest w `sonar-project.properties`:

```text
sonar.projectKey=jjdevhub
sonar.qualitygate.wait=true
sonar.qualitygate.timeout=300
sonar.sources=src
sonar.tests=tests
sonar.exclusions=**/bin/**,**/obj/**,**/node_modules/**,**/dist/**,**/Migrations/**,src/clients/shared/api-client/generated/**
sonar.cpd.exclusions=**/Migrations/**,src/clients/shared/api-client/generated/**
```

`sonar.qualitygate.wait=true` sprawia, że skaner czeka na werdykt bramki i kończy się kodem różnym od zera, gdy bramka jest czerwona. Migracje i wygenerowany klient nie wchodzą do duplikacji.

### 2. Czy w ogóle stawiać kontener

`.github/scripts/sonar-material.sh` szuka `*.cs`, `*.ts` i `*.tsx`. Pomija `.git`, `bin`, `obj`, `node_modules` i `dist`. Plik testowy to ścieżka pod `tests/`, katalog `Tests` albo nazwa `*.test.ts`, `*.test.tsx`, `*.spec.ts`, `*.spec.tsx`.

| Co jest w drzewie | Linia |
| --- | --- |
| brak tych plików | `no material` |
| same pliki testowe | `no material: brak produkcji` |
| choć jeden plik produkcji | `material` |

W `.github/workflows/sonarqube.yml` obie linie zaczynające się od `no material` ustawiają `skip=true`. Kontener wtedy nie wstaje. Sama produkcja ustawia `skip=false`. Inna linia kończy krok kodem `1`.

Na dzisiejszym drzewie jest kod C# w `src/`, więc żywy job idzie w kontener. Puste gałęzie sprawdza suchy przebieg na katalogach tymczasowych.

### 3. Job i kontener

`runs-on: ubuntu-latest`, `timeout-minutes: 40`. Uprawnienia: `contents: read` i `actions: read`. Drugie jest po to, żeby pobrać artefakt przebiegu `unit-tests`. Sekretu repozytorium nie ma.

Zmienne joba:

```text
SONAR_HOST_URL=http://127.0.0.1:9000
SONAR_HEAD_SHA=<sha głowy pull requestu>
SONAR_PR_KEY=<numer pull requestu>
SONAR_PR_BRANCH=<głowa>
SONAR_PR_BASE=<baza>
```

Kolejność, gdy materiał jest:

1. `sudo sysctl -w vm.max_map_count=524288` i `fs.file-max=131072`
2. `docker rm -f jjdevhub-sonarqube`, potem `docker run -d --name jjdevhub-sonarqube --publish 127.0.0.1:9000:9000 sonarqube:community`
3. `.github/scripts/sonar-coverage.sh artifacts/coverage`
4. bootstrap: `openssl rand -base64 24` jako hasło, `set +x`, `bash .github/scripts/sonar-bootstrap.sh`
5. odczyt tokenu z pliku i `::add-mask::`
6. setup SDK, gdy jest `*.csproj`
7. `.github/scripts/sonar-scan.sh`
8. `docker rm -f jjdevhub-sonarqube`, także gdy wcześniejszy krok padł, o ile kontener został oznaczony jako uruchomiony

Bootstrap czeka do pięciu minut, aż `api/system/status` zwróci `UP`. Potem zmienia hasło `admin` z domyślnego na wylosowane, sprawdza logowanie, tworzy projekt i bramkę, dokłada pięć warunków i generuje token typu `PROJECT_ANALYSIS_TOKEN`. Token ląduje w pliku ze zmiennej `SONAR_TOKEN_FILE` (w jobie `${{ runner.temp }}/sonar-token`), uprawnienie `600`. Wartości tokenu w tym dokumencie nie ma. Hasło nie jest wypisywane: `set +x` jest przed `openssl`, a zmienna jest czyszczona po eksporcie do skryptu.

Poza Actions `sonar-coverage.sh` wypisuje `pokrycie puste` i kończy się kodem `0`. W Actions czeka do dwudziestu minut na przebieg workflow `unit-tests.yml` o tym samym SHA, potem `gh run download` artefaktu o nazwie `coverage`. Brak artefaktu też jest `pokrycie puste` i kodem `0`. Pusty raport nie jest tu błędem skryptu. Bramka `new_coverage` ocenia go dopiero w skanerze, gdy są linie do pokrycia. Artefakt powstaje w [jobie testów jednostkowych](03-build-testy-format.md), z retencją jednego dnia.

### 4. Skan

`.github/scripts/sonar-scan.sh` wymaga niepustego pliku tokenu, adresu serwera i katalogu `src`. Szuka `coverage.cobertura.xml` i `lcov.info` w `artifacts/coverage` i w `src`. Brak obu wypisuje `pokrycie puste` i idzie dalej.

Gdy w drzewie jest `*.csproj`, wymagany jest `global.json`. Skaner to globalne narzędzie `dotnet-sonarscanner` w wersji `12.1.0`. Kolejność: `begin`, `dotnet build JJDevHub.slnx -c Release --nologo`, `end`. Klucz projektu to `jjdevhub`. Parametry pull requestu, gdy `SONAR_PR_KEY` jest ustawione:

```text
sonar.pullrequest.key
sonar.pullrequest.branch
sonar.pullrequest.base
```

Ścieżki Cobertura idą jako `sonar.cs.cobertura.reportsPaths`. `lcov.info` idzie jako `sonar.javascript.lcov.reportPaths`.

Gdy projektu nie ma, a materiał jednak jest (sam TypeScript produkcji), skrypt bierze CLI `sonar-scanner-cli` w wersji `8.5.0.16571`. Archiwum jest sprawdzane sumą SHA-256 `62a200079f5ed38682e59ce5fb507f75b7a3c8dd672cfaf647be15a2406422cd`. Zła suma kończy krok, zanim skaner ruszy. Na dzisiejszym drzewie jest `.csproj`, więc żywy job idzie ścieżką `dotnet sonarscanner`.

Po `end` albo po CLI skrypt robi `unset SONAR_TOKEN`. `set +x` obejmuje odczyt pliku.

### 5. Suchy przebieg

`tests/ci/sonarqube.sh` nie podnosi Dockera. Sprawdza:

- JSON bramki jest równy pięciu liczbom z kroku 1
- `sonar-project.properties` zawiera klucz `jjdevhub`, `**/Migrations/**`, ścieżkę wygenerowanego klienta i `sonar.qualitygate.wait=true`
- pięć metryk z tabeli występuje w `sonar-bootstrap.sh`
- pusty katalog daje `no material`
- katalog z samymi testami daje `no material: brak produkcji`
- katalog z `src/A.cs` daje `material`
- `sonar-coverage.sh` bez `GITHUB_ACTIONS` daje kod `0` i log `pokrycie puste`

```bash
bash tests/ci/sonarqube.sh
```

Zielony przebieg milczy.

Pełny kontener jest checkiem na pull requeście. Lokalnie suchy przebieg zamyka próg i rozróżnienie materiału. Obraz ściąga się przy pierwszym jobie, który zobaczy plik produkcji.

### 6. Ruleset

Nazwa `sonarqube` wchodzi do trzech rulesetów.

```bash
python3 -c 'import json,pathlib
for path in pathlib.Path(".github/rulesets").glob("*.json"):
    data=json.load(path.open())
    checks={item["context"] for item in data["rules"][-1]["parameters"]["required_status_checks"]}
    assert "sonarqube" in checks, path
'
```

### Co jest do czego

| Plik | Kto go woła |
| --- | --- |
| `quality-gate.json` | `sonar-bootstrap.sh`, przy starcie kontenera |
| `sonar-project.properties` | skaner, w `begin` albo w `-Dproject.settings` |
| artefakt `coverage` | `sonar-coverage.sh`, z joba `unit-tests` |
| `sonar-material.sh` | pierwszy warunek joba, zanim wstanie JVM |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| `deploy/sonar/quality-gate.json` | porównanie z pięcioma liczbami | równość, inaczej suchy przebieg pada |
| katalog bez źródeł | `sonar-material.sh` | `no material`, kod `0` |
| same pliki testowe | `sonar-material.sh` | `no material: brak produkcji` |
| `src/A.cs` | `sonar-material.sh` | `material` |
| brak `GITHUB_ACTIONS` | `sonar-coverage.sh` | `pokrycie puste`, kod `0` |

Pełny przebieg kontenera jest checkiem na pull requeście, nie suchym testem lokalnym.

## Checkpoint

```bash
test -f .github/workflows/sonarqube.yml
test -f deploy/sonar/quality-gate.json
test -f sonar-project.properties
python3 -c 'import json; g=json.load(open("deploy/sonar/quality-gate.json")); assert g["new_blocker"]==0 and g["new_coverage"]==80 and g["new_hotspots_reviewed"]==100'
bash tests/ci/sonarqube.sh
```

Suchy przebieg zwraca kod zero. `actionlint .github/workflows/sonarqube.yml` zwraca kod zero. W pliku workflow nie ma wartości tokenu.

## Umiesz wyjaśnić

Dlaczego kontener jest świeży, a nie jeden serwer na lata?

Bo próg ma być w git. Serwer, w którym ktoś kliknął inny próg, okłamałby check. Świeży kontener ładuje `quality-gate.json` przy każdym przebiegu i jest usuwany na końcu joba.

Czemu pokrycie jest na nowym kodzie, nie na całym?

Żeby stary dług nie blokował każdej poprawki i żeby nowa linia nie wchodziła bez testu. Osiemdziesiąt procent dotyczy linii, które ten pull request dodał. Parametry `sonar.pullrequest.*` biorą się z numeru, gałęzi i bazy zdarzenia.

Co z markdownem tej specyfikacji?

Nie ma linii do pokrycia. `sonar-material.sh` w ogóle nie widzi `*.md`. Sam markdown daje `no material` i kontener nie wstaje. Próg pokrycia się nie włącza.

Czym `no material` różni się od `no material: brak produkcji`?

Pierwsze to brak `*.cs`, `*.ts` i `*.tsx`. Drugie to same testy. Oba pomijają kontener. Dopiero plik produkcji daje `material` i stawia serwer.

Gdzie jest token?

Powstaje w bootstrapie jako token analizy projektu, ląduje w pliku tymczasowym runnera o uprawnieniu `600` i jest maskowany. Nie ma go w ustawieniach repozytorium i nie ma go w tym pliku. Kontener jest kasowany razem z hasłem, które wylosował `openssl`.

Dlaczego to nie zastępuje CodeQL?

Inny rodzaj znaleziska. Skan bezpieczeństwa kodu zostaje. Tu jest pokrycie, duplikacja, blocker i hotspot.

Dlaczego pusty artefakt pokrycia nie kończy `sonar-coverage.sh` kodem `1`?

Bo brak pliku i brak przebiegu testów mają własny log `pokrycie puste`. Werdykt „źródło bez testu” należy do bramki `new_coverage`, gdy skaner widzi linie. Skrypt pobierania nie zgaduje, czy linie są.

## Zasady

- Próg nie jest luzowany w interfejsie. Pięć kluczy w JSON jest zamknięte: dodatkowe pole psuje bootstrap.
- Obraz to `sonarqube:community`, bez edycji płatnej.
- Token nie wchodzi do logu ani do git. Hasło administratora jest losowane w przebiegu.
- Job pull requestu nie używa runnera `jjdevhub`.
- Wyłączenie reguły w kodzie komentarzem nie zastępuje zmiany `quality-gate.json`.
- `dotnet-sonarscanner` stoi na `12.1.0`. CLI, gdy nie ma projektu, stoi na `8.5.0.16571` i na sumie z skryptu.

## Poza zakresem

- Miejsce kontenera na maszynie do oglądania jest w [04-maszyna.md](../02-szkielet-i-deploy/04-maszyna.md). Werdykt zostaje w tym jobie.
- Testy, które dają pokrycie, dochodzą z numerami kodu. Piramida jest w [10-testy](../10-testy/01-integracyjne.md).
- Sam artefakt Cobertura składa [job testów jednostkowych](03-build-testy-format.md).

## Źródła

- [Quality gates](https://docs.sonarsource.com/sonarqube-community-build/quality-standards-administration/managing-quality-gates/). Wiążące są warunki na nowym kodzie. Dotyczy pięciu progów z tego numeru. Ze źródła nie bierzemy bramki wyklikanej i niezacommitowanej.
- [SonarScanner for .NET](https://docs.sonarsource.com/sonarqube-community-build/analyzing-source-code/scanners/dotnet/sonarqube-scanner-for-dotnet). Wiążące jest rozpoczęcie skanu przed buildem i zakończenie po teście, oraz parametry pull requestu. Dotyczy joba, gdy są projekty. Ze źródła nie bierzemy tokenu trzymanego w sekrecie repozytorium, skoro kontener jest świeży i token może powstać w przebiegu.
- [Analysis scope](https://docs.sonarsource.com/sonarqube-community-build/project-administration/setting-analysis-scope/excluding-files-based-on-path-patterns/). Wiążące jest wyłączenie `**/Migrations/**` i wygenerowanego klienta. Dotyczy tego, żeby generator nie zjadał progu duplikacji. Ze źródła nie bierzemy wyłączenia `src/services`.

## Gotowe gdy

- `deploy/sonar/quality-gate.json` ma pięć progów z tego numeru, a `tests/ci/sonarqube.sh` zwraca kod zero.
- Katalog bez źródeł daje `no material`. Same testy dają `no material: brak produkcji`. Plik produkcji daje `material`.
- Trzy rulesety wymieniają `sonarqube`.
- W workflow nie ma wartości tokenu.
- Katalogi migracji i wygenerowanego klienta są poza liczeniem duplikacji.
