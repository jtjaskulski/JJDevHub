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

Grupowanie: jedna zmiana na katalog ekosystemu tygodniowo, nie osobny pull request na każdą paczkę łatkową. Major jest osobno, bez grupy, bo major czyta się inaczej niż łatka.

### Brak drugiego skanera paczek w tym numerze

GitHub dependency review na pull requeście jest włączony jako widok, nie jako drugi wymagany check. Werdykt jest jeden, z tego workflow. Dwa checki na ten sam CVE dają dwa miejsca, w których próg się rozjeżdża.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| advisory | opis podatności z identyfikatorem i wagą |
| lockfile | `pnpm-lock.yaml`, drzewo, które naprawdę się zainstaluje |
| major | podbicie, które może złamać API paczki |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `.github/workflows/dependencies.yml` | job `dependency-vulnerabilities` |
| `.github/scripts/vulnerable.sh` | próg i kod wyjścia |
| `.github/dependabot.yml` | harmonogram i ekosystemy |

## Plan

### 1. Skan NuGet

Gdy nie ma `*.csproj`, krok loguje `no material` dla NuGet. Gdy jest, `dotnet restore JJDevHub.slnx` i `dotnet list JJDevHub.slnx package --vulnerable --include-transitive`. Skrypt zwraca `1`, gdy w wyniku jest `High` albo `Critical`.

Brak `global.json` przy obecnym `.csproj` też zwraca `1`. Inaczej restore weźmie SDK runnera spoza linii produktu.

### 2. Skan npm

Gdy nie ma `package.json`, `no material` dla npm. Gdy jest `package.json` bez `pnpm-lock.yaml` w tym samym katalogu, kod `1`. Gdy lockfile jest, `pnpm audit --audit-level high` w katalogu web i w katalogu telefonu. `high` u npm obejmuje wysoki i krytyczny.

Katalog bez `package.json` nie jest błędem. Katalog klienta z `package.json` i bez lockfile'a jest błędem.

### 3. Dependabot

```yaml
version: 2
updates:
  - package-ecosystem: nuget
    directory: /
    schedule:
      interval: weekly
      day: monday
  - package-ecosystem: npm
    directory: /src/clients/web
    schedule:
      interval: weekly
      day: monday
  - package-ecosystem: npm
    directory: /src/clients/mobile
    schedule:
      interval: weekly
      day: monday
  - package-ecosystem: github-actions
    directory: /
    schedule:
      interval: weekly
      day: monday
```

Katalogi npm dopisuje się w numerze, który tworzy `package.json`. Na tym numerze w pliku są `nuget` i `github-actions`. Wpisy npm dochodzą w [szkielecie webu](../11-web/02-szkielet.md) i [Expo](../12-mobile/01-szkielet-expo.md), bo Dependabot pada na katalogu, którego nie ma. Ten numer zostawia w pliku komentarz z docelowymi ścieżkami, a żywe wpisy to NuGet i akcje.

To jest jedyny przypadek, w którym komentarz w YAML jest kontraktem: mówi, którego numeru brakuje. Nie wyłącza checka.

### 4. Ruleset

`dependency-vulnerabilities` wchodzi do trzech rulesetów.

### Co jest do czego

| Krok | Po co |
| --- | --- |
| restore i `dotnet list` | CVE paczek .NET, także przechodnich |
| `pnpm audit` | CVE frontu, z lockfile'a |
| Dependabot | przynosi podbicie, nie zastępuje checka |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| puste drzewo bez `csproj` i bez `package.json` | `vulnerable.sh` | log `no material`, kod `0` |
| katalog z `package.json` i bez lockfile'a | `vulnerable.sh` | kod `1` |

Drugiego wiersza nie robi się w korzeniu solucji. Robi się go w katalogu tymczasowym testu skryptu.

## Checkpoint

```bash
test -f .github/workflows/dependencies.yml
test -f .github/dependabot.yml
actionlint .github/workflows/dependencies.yml
bash .github/scripts/vulnerable.sh
```

Na pustej solucji skrypt zwraca kod zero i w logu jest `no material`.

## Umiesz wyjaśnić

Dlaczego CVE bez łatki też pada?

Bo inaczej `main` trzyma znaną podatność do czasu, aż autor paczki coś wyda. Padnięcie zmusza do decyzji w pull requeście: podbicie, wymiana paczki albo uzasadnione tłumienie w pliku, nie cisza.

Czym to różni się od CodeQL?

CodeQL czyta nasz kod. Ten check czyta wersje cudzych paczek. Oba są wymagane. Żaden nie pokrywa drugiego.

Dlaczego lockfile jest obowiązkowy?

Bez niego `npm audit` i instalacja mogą widzieć inne drzewo. Bramka ma oceniać to, co wejdzie do obrazu.

Co Dependabot wolno scalić?

Tylko pull request, który ma zielone wszystkie wymagane checki. Major nie jest w grupie z łatkami.

Dlaczego na tym numerze nie ma jeszcze wpisu npm?

Bo nie ma katalogu klienta. Wpis do nieistniejącego katalogu psuje harmonogram. Dochodzi w numerze klienta.

## Zasady

- Próg to wysoki i krytyczny, z łatką i bez łatki.
- Tłumienie konkretnego identyfikatora, jeśli w ogóle, jest plikiem `.github/advisories-allow.txt` z datą i zdaniem powodu. Nie ma gwiazdki „wszystko”.
- Job nie drukuje tokenu. `GITHUB_TOKEN` ma `contents: read`.
- Dependabot nie dostaje sekretu aplikacji.
- `package.json` klienta bez `pnpm-lock.yaml` nie przechodzi.

## Poza zakresem

- Skan obrazu, SBOM i atestacja są w [08-obrazy.md](08-obrazy.md).
- Skan kodu jest w [04-codeql.md](04-codeql.md).
- Pin akcji do SHA jest w [07-lancuch-dostaw.md](07-lancuch-dostaw.md). Dependabot dla `github-actions` przynosi podbicie, a tamten check pilnuje, że w `uses` zostaje SHA.

## Źródła

- [dotnet list package](https://learn.microsoft.com/dotnet/core/tools/dotnet-list-package). Wiążące są `--vulnerable` i `--include-transitive`. Dotyczy progu NuGet. Ze źródła nie bierzemy trybu, który pomija przechodnie.
- [npm audit](https://docs.npmjs.com/cli/v10/commands/npm-audit). Wiążący jest poziom `high`. U nas komenda to `pnpm audit` z tym samym poziomem, bo menedżerem jest pnpm. Ze źródła nie bierzemy `--omit=dev` jako sposobu na ukrycie podatności narzędzia, które wchodzi do CI.
- [Dependabot](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/configuration-options-for-the-dependabot.yml-file). Wiążące są ekosystem, katalog i harmonogram. Dotyczy poniedziałkowego podbicia. Ze źródła nie bierzemy automatycznego scalania bez zielonych checków.

## Gotowe gdy

- Job `dependency-vulnerabilities` jest w trzech rulesetach.
- Pusta solucja daje `no material` i kod zero.
- Katalog testowy z `package.json` bez lockfile'a daje kod `1`.
- `dependabot.yml` opisuje `nuget` i `github-actions`.
- W workflow nie ma sekretu aplikacji.
