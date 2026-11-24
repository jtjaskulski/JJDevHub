# 05. Zależności i Dependabot

Poprzedni numer to [CodeQL](04-codeql.md). Następny to [SonarQube](06-sonarqube.md).

Ten numer dodaje check `dependency-vulnerabilities` i konfigurację Dependabota. Check pada na podatności wysokiej i krytycznej w paczkach NuGet i npm, także wtedy, gdy nie ma jeszcze poprawki. Dependabot otwiera pull request z podbiciem, a ten sam check go ocenia.

## Po co

Skan kodu nie wie, że paczka na liście ma CVE. `dotnet build` też nie wie. Bez checka podatność wchodzi na `main` i zostaje do czasu, aż ktoś przeczyta zakładkę. Bez Dependabota check tylko blokuje, a nikt nie przynosi poprawki.

`package.json` bez lockfile'a skanuje inne drzewo niż to, które zainstaluje runner. Dlatego brak lockfile'a jest błędem checka, nie ostrzeżeniem.

## Dlaczego ta technologia

### `dotnet list package --vulnerable` i `npm audit`

Oba narzędzia czytają to, co jest przypięte: restore NuGet i lockfile npm. Problem, który zamyka, to „u nas w IDE jest zielono, a na stronie poradnika jest CVE”. Odpada ręczny przegląd `Directory.Packages.props`. Koszt: check potrzebuje sieci do źródła poradników w dniu przebiegu. Przebieg bez sieci, który udaje sukces, jest gorszy niż padnięcie.

Próg to wysoki i krytyczny. Średni i niski zostają w logu. Nie ma flagi, która wyłącza wynik „bo nie ma jeszcze poprawki”: taki wynik też kończy się kodem `1`. Inaczej CVE bez łatki mieszka w `main` miesiącami.

### Dependabot

`.github/dependabot.yml` raz w tygodniu, w poniedziałek, otwiera pull requesty dla `nuget`, `npm` i `github-actions`. Problem, który zamyka, to pin, który starzeje się w ciszy. Odpada ręczne podbijanie „kiedyś”. Koszt: pull requesty Dependabota przechodzą pełną listę checków. Nie scala się ich z pominięciem `codeql` ani testów.

Grupowanie: jedna zmiana na katalog ekosystemu tygodniowo dla `minor` i `patch`, nie osobny pull request na każdą paczkę łatkową. Major jest poza grupą, bo major czyta się inaczej niż łatka.

### Brak drugiego skanera paczek w tym numerze

GitHub dependency review na pull requeście jest włączony jako widok, nie jako drugi wymagany check. Werdykt jest jeden, z tego workflow. Dwa checki na ten sam CVE dają dwa miejsca, w których próg się rozjeżdża.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| advisory | opis podatności z identyfikatorem i wagą |
| lockfile | `pnpm-lock.yaml`, drzewo, które naprawdę się zainstaluje |
| major | podbicie, które może złamać API paczki |
| tłumienie | wiersz w `.github/advisories-allow.txt`: identyfikator, data, zdanie |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `.github/workflows/dependencies.yml` | job `dependency-vulnerabilities` |
| `.github/scripts/vulnerable.sh` | próg, lockfile, tłumienie, kod wyjścia |
| `.github/dependabot.yml` | harmonogram, grupy `minor` i `patch`, komentarze katalogów npm |
| `tests/ci/dependencies.sh` | puste drzewo, brak lockfile'a, gwiazdka, poprawny wiersz tłumienia |
| `.github/advisories-allow.txt` | powstaje, gdy jest konkretny identyfikator do zgłoszenia; na zapas go nie ma |

## Plan

### 1. Job

`.github/workflows/dependencies.yml` startuje na `pull_request` do `develop`, `release` i `main`. Filtra `paths` nie ma. Job nazywa się `dependency-vulnerabilities`, `timeout-minutes` to `20`, uprawnienie to `contents: read`.

Kolejność kroków:

1. checkout z `persist-credentials: false`
2. `bash tests/ci/dependencies.sh`
3. `actions/setup-dotnet` z `global.json`, tylko gdy `hashFiles('**/*.csproj')` coś znajduje
4. `corepack enable`, tylko gdy w drzewie jest `package.json`
5. `bash .github/scripts/vulnerable.sh` na korzeniu repozytorium

Krok 5 na dzisiejszym drzewie robi restore, bo projekty już są. Sieć do poradników NuGet jest wtedy potrzebna. Suchy przebieg z kroku 2 sieci nie potrzebuje: pracuje na katalogach tymczasowych.

### 2. Skan NuGet

`.github/scripts/vulnerable.sh` przyjmuje opcjonalny korzeń. Bez argumentu bierze korzeń repozytorium. Szuka `*.csproj` i `package.json`, omijając `.git`, `bin`, `obj` i `node_modules`.

Gdy nie ma ani projektu, ani `package.json`, skrypt wypisuje `no material` i kończy się kodem `0`. Gdy nie ma projektu, a front jest, dopisuje linię `no material: nuget`. Gdy jest projekt, a frontu nie ma, dopisuje `no material: npm`.

Projekt bez `global.json` w tym korzeniu kończy się kodem `1` i komunikatem `brak global.json przy projekcie`. Projekt bez `JJDevHub.slnx` kończy się kodem `1` i komunikatem `brak JJDevHub.slnx`. Inaczej:

```bash
dotnet restore JJDevHub.slnx
dotnet list JJDevHub.slnx package --vulnerable --include-transitive --format json
```

JSON czyta funkcja `gate_report` dla rodzaju `nuget`. Bierze paczki z `topLevelPackages` i z `transitivePackages`. Waga `High`, `Critical`, `high` albo `critical` podnosi kod. Inna waga jest wypisywana i nie podnosi kodu. Brak łatki nie jest osobną gałęzią: skrypt w ogóle nie czyta pola „czy jest poprawka”.

Identyfikator bierze się z `advisoryurl`. W logu przy blokadzie jest nazwa paczki, waga i identyfikator, na stderr.

### 3. Skan npm

Dla każdego `package.json` skrypt wymaga `pnpm-lock.yaml` w tym samym katalogu. Brak pliku wypisuje `brak pnpm-lock.yaml przy <ścieżka>` i kończy się kodem `1`, zanim ruszy audit. Katalog bez `package.json` nie jest błędem.

Gdy lockfile jest:

```bash
pnpm audit --audit-level high --json
```

`high` u npm obejmuje wysoki i krytyczny. Kod procesu większy niż `1` to awaria narzędzia, nie werdykt, i kończy się komunikatem `pnpm audit nie doszedł`. Pusty JSON przy kodzie zero jest pomijany. Raport idzie tą samą bramką co NuGet, rodzaj `npm`. Gdy `pnpm` zwróci kod inny niż zero, a w raporcie nie ma wagi blokującej, skrypt i tak kończy się kodem `1`: ciche niepowodzenie audytu nie jest sukcesem.

`pnpm` musi być na `PATH`. W jobie daje go `corepack enable`, i tylko wtedy, gdy `package.json` w ogóle jest.

### 4. Tłumienie

Plik `.github/advisories-allow.txt` jest czytany na początku, zanim skrypt zdecyduje o `no material`. Brak pliku jest poprawny. Wiersz ma trzy pola: identyfikator, datę `RRRR-MM-DD` i zdanie. Zdanie ma co najmniej dwa słowa i jedną literę.

```text
GHSA-xxxx-xxxx-xxxx 2026-11-20 powód tłumienia.
```

Gwiazdka w dowolnym miejscu wiersza kończy się komunikatem `tłumienie gwiazdką jest zakazane` i kodem `1`. Zła data albo brak zdania też kończą się kodem `1`. Identyfikator z listy, przy wadze blokującej, wypisuje `tłumienie <paczka> <waga> <id>` i nie podnosi kodu. Reszta wysokich i krytycznych podnosi.

Pliku na zapas w drzewie nie ma. Dopisuje się go w pull requeście, który tłumienie niesie.

### 5. Suchy przebieg

`tests/ci/dependencies.sh` woła skrypt na czterech katalogach tymczasowych i sprząta je.

| Katalog | Oczekiwany kod | Czego szuka w logu |
| --- | --- | --- |
| pusty | `0` | `no material` |
| sam `package.json` z `{}` | `1` | `pnpm-lock.yaml` |
| `.github/advisories-allow.txt` zawierający `*` | `1` | `gwiazdk` |
| wiersz `GHSA-xxxx-xxxx-xxxx 2026-11-20 powód tłumienia.` i nic więcej | `0` | `no material` |

Ostatni wiersz jest na drzewie bez projektu. Tłumienie jest poprawne, a skan i tak nie ma czego czytać, więc zostaje `no material`. Gwiazdka pada wcześniej, przy czytaniu pliku, także gdy drzewo jest puste.

```bash
bash tests/ci/dependencies.sh
```

Zielony przebieg milczy. Kod wyjścia to `0`.

### 6. Dependabot

Żywy `.github/dependabot.yml`:

```yaml
version: 2
updates:
  - package-ecosystem: nuget
    directory: /
    schedule:
      interval: weekly
      day: monday
    groups:
      nuget:
        patterns:
          - "*"
        update-types:
          - minor
          - patch
  - package-ecosystem: github-actions
    directory: /
    schedule:
      interval: weekly
      day: monday
    groups:
      github-actions:
        patterns:
          - "*"
        update-types:
          - minor
          - patch
```

Major nie jest na liście `update-types`, więc nie wchodzi do grupy z łatkami. Wpis npm w tym pliku nie żyje. Trzy komentarze mówią, który numer go dopisze, bo Dependabot pada na katalogu, którego nie ma:

```text
# npm /src/clients — dochodzi w 11-web/01-motyw
# npm /src/clients/web — dochodzi w 11-web/02-szkielet
# npm /src/clients/mobile — dochodzi w 12-mobile/01-szkielet-expo
```

To jest komentarz, który nazywa numer. Nie wyłącza checka. Check i tak skanuje `package.json`, gdy plik już jest, niezależnie od tego, czy wpis Dependabota zdążył dojść.

Sprawdzenie:

```bash
python3 -c 'import pathlib
text=pathlib.Path(".github/dependabot.yml").read_text()
assert "package-ecosystem: nuget" in text
assert "package-ecosystem: github-actions" in text
assert "package-ecosystem: npm" not in text
assert "update-types:" in text
assert "minor" in text and "patch" in text
assert "11-web/01-motyw" in text
assert "11-web/02-szkielet" in text
assert "12-mobile/01-szkielet-expo" in text
'
```

### 7. Ruleset

`dependency-vulnerabilities` wchodzi do trzech rulesetów. Nazwa jest ta sama co `name` joba.

```bash
python3 -c 'import json,pathlib
for path in pathlib.Path(".github/rulesets").glob("*.json"):
    data=json.load(path.open())
    checks={item["context"] for item in data["rules"][-1]["parameters"]["required_status_checks"]}
    assert "dependency-vulnerabilities" in checks, path
'
```

### Co jest do czego

| Krok | Po co |
| --- | --- |
| restore i `dotnet list --format json` | CVE paczek .NET, także przechodnich |
| `pnpm audit --audit-level high` | CVE frontu, z lockfile'a |
| `advisories-allow.txt` | jeden identyfikator z datą i zdaniem, bez gwiazdki |
| Dependabot | przynosi podbicie `minor` i `patch` w grupie; major osobno; nie zastępuje checka |
| `tests/ci/dependencies.sh` | werdykt na katalogu tymczasowym, bez sieci |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| katalog bez `csproj` i bez `package.json` | `vulnerable.sh <katalog>` | log `no material`, kod `0` |
| katalog z `package.json` i bez lockfile'a | `vulnerable.sh <katalog>` | kod `1`, w logu `pnpm-lock.yaml` |
| allowlista z `*` | `vulnerable.sh <katalog>` | kod `1`, w logu `gwiazdk` |
| allowlista z datą i zdaniem, drzewo puste | `vulnerable.sh <katalog>` | kod `0`, `no material` |

Żadnego z tych wierszy nie robi się w korzeniu solucji. Robi je `tests/ci/dependencies.sh`. Korzeń solucji ma projekty, więc żywy krok joba idzie w restore.

## Checkpoint

```bash
test -f .github/workflows/dependencies.yml
test -f .github/dependabot.yml
test -f .github/scripts/vulnerable.sh
bash tests/ci/dependencies.sh
```

Suchy przebieg zwraca kod zero i milczy. `actionlint .github/workflows/dependencies.yml` zwraca kod zero. Na korzeniu `vulnerable.sh` nie jest tu wołany: wymaga sieci do poradników, bo `*.csproj` już jest.

## Umiesz wyjaśnić

Dlaczego CVE bez łatki też pada?

Bo skrypt nie ma gałęzi „brak poprawki”. Wysoka i krytyczna waga kończy się kodem `1` tak samo, gdy łatka jest, i tak samo, gdy jej nie ma. Padnięcie zmusza do decyzji w pull requeście: podbicie, wymiana paczki albo wiersz w `advisories-allow.txt`.

Czym to różni się od CodeQL?

CodeQL czyta nasz kod. Ten check czyta wersje cudzych paczek z restore i z lockfile'a. Oba są wymagane. Żaden nie pokrywa drugiego.

Dlaczego lockfile jest obowiązkowy?

Bez `pnpm-lock.yaml` obok `package.json` skrypt kończy się kodem `1`, zanim zawoła `pnpm audit`. Bramka ma oceniać drzewo, które wejdzie do obrazu.

Co Dependabot wolno scalić?

Tylko pull request, który ma zielone wszystkie wymagane checki. Grupa zbiera `minor` i `patch`. Major nie jest w `update-types` tej grupy.

Dlaczego na tym numerze nie ma jeszcze wpisu npm?

Bo nie ma katalogu klienta. Wpis do nieistniejącego katalogu psuje harmonogram. Trzy komentarze w `dependabot.yml` wskazują [motyw](../11-web/01-motyw.md), [szkielet webu](../11-web/02-szkielet.md) i [Expo](../12-mobile/01-szkielet-expo.md). Check i tak zeskanuje `package.json` w dniu, w którym plik się pojawi, nawet dzień przed wpisem Dependabota.

Czym gwiazdka w allowliście różni się od jednego identyfikatora?

Gwiazdka kończy skrypt kodem `1` od razu, także na pustym drzewie. Jeden identyfikator z datą i zdaniem gasi tylko ten identyfikator przy wadze wysokiej albo krytycznej.

Dlaczego suchy przebieg nie woła `dotnet list`?

Bo katalogi tymczasowe nie mają projektu. Żywy krok joba woła `dotnet list` na `JJDevHub.slnx`, gdy projekt jest. Suchy przebieg pilnuje progu, lockfile'a i kształtu tłumienia.

## Zasady

- Próg to wysoki i krytyczny, z łatką i bez łatki.
- Tłumienie konkretnego identyfikatora jest plikiem `.github/advisories-allow.txt` z datą i zdaniem powodu. Nie ma gwiazdki.
- Job nie drukuje tokenu. `GITHUB_TOKEN` ma `contents: read`.
- Dependabot nie dostaje sekretu aplikacji.
- `package.json` klienta bez `pnpm-lock.yaml` nie przechodzi.
- Brak `global.json` przy obecnym `.csproj` nie przechodzi.

## Poza zakresem

- Skan obrazu, SBOM i atestacja są w [08-obrazy.md](08-obrazy.md).
- Skan kodu jest w [04-codeql.md](04-codeql.md).
- Pin akcji do SHA jest w [07-lancuch-dostaw.md](07-lancuch-dostaw.md). Dependabot dla `github-actions` przynosi podbicie, a tamten check pilnuje, że w `uses` zostaje SHA.
- Wpisy npm dochodzą w numerach nazwanych w komentarzach `dependabot.yml`.

## Źródła

- [dotnet list package](https://learn.microsoft.com/dotnet/core/tools/dotnet-list-package). Wiążące są `--vulnerable` i `--include-transitive`. Dotyczy progu NuGet. Ze źródła nie bierzemy trybu, który pomija przechodnie.
- [npm audit](https://docs.npmjs.com/cli/v10/commands/npm-audit). Wiążący jest poziom `high`. U nas komenda to `pnpm audit` z tym samym poziomem, bo menedżerem jest pnpm. Ze źródła nie bierzemy `--omit=dev` jako sposobu na ukrycie podatności narzędzia, które wchodzi do CI.
- [Dependabot](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/configuration-options-for-the-dependabot.yml-file). Wiążące są ekosystem, katalog, harmonogram i `groups`. Dotyczy poniedziałkowego podbicia `minor` i `patch`. Ze źródła nie bierzemy automatycznego scalania bez zielonych checków.

## Gotowe gdy

- Job `dependency-vulnerabilities` jest w trzech rulesetach.
- `tests/ci/dependencies.sh` zwraca kod zero: puste drzewo daje `no material`, katalog z `package.json` bez lockfile'a daje kod `1`, gwiazdka w allowliście daje kod `1`.
- `dependabot.yml` opisuje `nuget` i `github-actions`, grupuje `minor` i `patch`, i nie ma żywego wpisu `npm`.
- W workflow nie ma sekretu aplikacji.
