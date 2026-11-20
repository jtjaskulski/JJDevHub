# 03. Build, testy i format

Poprzedni numer to [gałęzie](02-galezie.md). Następny to [CodeQL](04-codeql.md).

Ten numer dodaje checki `build`, `unit-tests`, `format`, `integration-tests` i `performance` oraz skrypt komponentów dotkniętych zmianą. Na pustej solucji cztery z nich kończą się jawnym brakiem materiału. Od pierwszego projektu ten sam job kompiluje i testuje tylko to, co zmiana ruszyła, a format sprawdza całe drzewo.

## Po co

Bez joba `build` ruleset nie ma czego wymagać, a pierwszy projekt wchodzi czerwony albo wcale nie jest kompilowany. Bez osobnego `unit-tests` i `integration-tests` ktoś wyłącza kontenery filtrem i nazywa to pełnym testem. Bez `format` spór o spacje jest recenzją. Bez `performance` test obciążenia nie ma gdzie żyć, gdy dojdzie w fazie testów.

Filtr `paths` na `on` pomija job, a pominięty job nie daje statusu. Dlatego każdy z tych workflow startuje zawsze i sam decyduje, czy ma materiał.

## Dlaczego ta technologia

### Jeden job, jedna nazwa

Nazwa joba jest nazwą checka. Problem, który zamyka, to workflow `ci` z jobem `test`, którego ruleset szuka pod inną nazwą. Odpada jeden plik YAML ze wszystkimi checkami, bo padnięcie formatu zlewa się z padnięciem testu w jednym statusie. Koszt: pięć plików i pięć wpisów w rulesecie.

### `dotnet format` i `.editorconfig`

Format jest checkiem, nie sugestią IDE. Problem, który zamyka, to diff złożony ze zmiany stylu i zmiany reguły. Odpada samo ustawienie „formatuj przy zapisie” na laptopie, bo runner go nie ma. Koszt: `.editorconfig` jest kontraktem. Zmiana reguły stylu formatuje całe drzewo osobnym pull requestem o typie `chore`.

### Komponenty dotknięte zmianą

Skrypt `.github/scripts/affected-components.sh` mówi, które komponenty ruszył diff. Problem, który zamyka, to przebudowa Identity przy zmianie w News. Odpada zawsze-wszystko, gdy solucja dorośnie do pięciu usług. Koszt: zmiana w `JJDevHub.Shared.Kernel` oznacza wszystkie komponenty, które mają do niego `ProjectReference`. Skrypt to liczy z grafu projektów, nie z listy w komentarzu.

Na pustej solucji skrypt wypisuje pustą listę i job loguje `no material`.

### xUnit jako jedyny runner

[Technologie](../00-fundamenty/technologie.md) przypinają xunit.v3. Ten numer jeszcze nie dodaje projektu testowego. Ustala tylko, że job szuka testów komendą `dotnet test` i filtrem cechy. Drugi runner nie wchodzi.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| `no material` | jawny log i kod `0`, gdy nie ma czego sprawdzić |
| cecha | `Category=Integration` albo `Category=Performance` na teście |
| komponent dotknięty | katalog usługi, którego diff albo zależność od zmienionego projektu dotyczy |

## Klasy i ścieżki

| Ścieżka | Check |
| --- | --- |
| `.github/workflows/build.yml` | `build` |
| `.github/workflows/unit-tests.yml` | `unit-tests` |
| `.github/workflows/format.yml` | `format` |
| `.github/workflows/integration-tests.yml` | `integration-tests` |
| `.github/workflows/performance.yml` | `performance` |
| `.github/scripts/affected-components.sh` | lista komponentów albo pusto |
| `.editorconfig` | reguły `dotnet format` |

Wszystkie joby: `runs-on: ubuntu-latest`, `permissions: contents: read`, setup SDK z `global.json`.

## Plan

### 1. Build

Gdy w drzewie nie ma `*.csproj`, job wypisuje `no material` i kończy się kodem `0`. Gdy jest, uruchamia `dotnet build JJDevHub.slnx -c Release --nologo`. Ostrzeżenie jest błędem, bo tak mówi `Directory.Build.props` z [01-solucja.md](01-solucja.md).

### 2. Testy jednostkowe

Gdy nie ma projektu testowego, `no material`. Gdy jest, `dotnet test` z filtrem `Category!=Integration&Category!=Performance`. Brak testów w projekcie produkcyjnym, który nie jest testem, nie jest pustym sukcesem: projekt `*.Tests` musi istnieć dla każdego projektu produkcyjnego poza `AppHost`. Tę regułę włącza pierwszy numer, który doda projekt produkcyjny. Do tego czasu nie ma czego wymagać.

### 3. Integracja i obciążenie

`integration-tests` uruchamia filtr `Category=Integration|Category=Contract`. `performance` uruchamia `Category=Performance`. Brak testów danej cechy to `no material`, nie błąd. Obecność testu i jego padnięcie to kod `1`. Test integracyjny podnosi kontener sam. Job nie dostaje sekretu aplikacji i nie łączy się z maszyną.

### 4. Format

Gdy nie ma `*.csproj` ani `*.cs`, `no material`. Gdy są, `dotnet format JJDevHub.slnx --verify-no-changes --severity warn`. Padnięcie wypisuje diff i nie commituje poprawki samo. Poprawkę wnosi się do tej samej gałęzi.

### 5. Ruleset

Do trzech plików ruleset dopisać pięć nazw checków w tym samym scaleniu co workflow.

### Co jest do czego

| Job | Po co żyje |
| --- | --- |
| `build` | czy solucja w ogóle się kompiluje |
| `unit-tests` | reguły bez kontenera |
| `integration-tests` | baza, broker, kontrakt |
| `performance` | próg opóźnienia, gdy test już jest |
| `format` | styl poza recenzją |
| `affected-components.sh` | późniejsze skanowanie obrazu jednej usługi, nie wszystkich |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| drzewo bez `*.csproj` | job `build` lokalnie tym samym skryptem | log zawiera `no material`, kod `0` |
| projekt z jedną przestrzenią zbędną | `dotnet format --verify-no-changes` | kod `1` |

Na tym numerze drugi wiersz wykonuje się na projekcie tymczasowym w katalogu testu skryptu, nie w solucji. Solucja zostaje pusta do [Aspire](../02-szkielet-i-deploy/01-aspire.md).

## Checkpoint

```bash
test -f .github/workflows/build.yml
test -f .github/workflows/unit-tests.yml
test -f .github/workflows/format.yml
test -f .github/workflows/integration-tests.yml
test -f .github/workflows/performance.yml
bash .github/scripts/affected-components.sh
actionlint
```

Skrypt komponentów wypisuje pustą listę i kod `0`. `actionlint` milczy. W rulesetach widać pięć nowych nazw.

## Umiesz wyjaśnić

Czym różni się `no material` od joba pominiętego?

`no material` to zielony status z logiem. Job pominięty nie wysyła statusu, a ruleset wtedy czeka w nieskończoność albo blokuje. Dlatego nie ma `paths` na `on`.

Dlaczego integracja nie leci w filtrze jednostkowym?

Bo kontener jest wolny i sprawdza co innego niż regułę domeny. Oba joby są wymagane. Żaden nie zastępuje drugiego.

Co znaczy zmiana w jądrze dla skryptu komponentów?

Wszystkie komponenty z `ProjectReference` do zmienionego projektu. Inaczej News zostałby na starym `Result`, a tag by tego nie pokazał.

Dlaczego format nie poprawia plików na runnerze?

Runner nie commituje do gałęzi pull requestu. Pokazuje diff. Poprawka jest w gałęzi, którą widać.

Kiedy `performance` przestaje być pustym sukcesem?

Gdy w drzewie jest test z `Category=Performance`. Do tego czasu job istnieje i jest wymagany, żeby ruleset nie czekał na check, którego nie ma.

## Zasady

- `runs-on` tych jobów to `ubuntu-latest`. Etykieta `jjdevhub` tu nie występuje.
- `permissions` to `contents: read`, nic szerszego.
- Filtr `paths` na poziomie `on` jest zabroniony.
- Projekt produkcyjny, gdy już powstanie, ma projekt testowy. Wyjątkiem jest `JJDevHub.AppHost`.
- Test bez cechy nie otwiera połączenia sieciowego poza procesem.

## Poza zakresem

- Treść testów usług jest w numerach, które dokładają kod, i domyka się w [folderze testów](../10-testy/01-integracyjne.md).
- Skan kodu jest w [04-codeql.md](04-codeql.md).
- Skan paczek jest w [05-zaleznosci.md](05-zaleznosci.md).

## Źródła

- [dotnet test](https://learn.microsoft.com/dotnet/core/tools/dotnet-test). Wiążący jest `--filter`. Dotyczy rozdziału cech. Ze źródła nie bierzemy jednego joba bez filtra, który ciągnie kontenery przy każdej zmianie komentarza.
- [dotnet format](https://learn.microsoft.com/dotnet/core/tools/dotnet-format). Wiążące jest `--verify-no-changes`. Dotyczy checka `format`. Ze źródła nie bierzemy auto-commita z runnera.
- [EditorConfig](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/). Wiążące są reguły stylu w pliku, nie w ustawieniu IDE. Dotyczy `.editorconfig` w korzeniu. Ze źródła nie bierzemy drugiego pliku stylu na usługę.

## Gotowe gdy

- Pięć workflow istnieje i `actionlint` zwraca kod zero.
- Na solucji bez `.csproj` każdy z pięciu jobów loguje `no material` i zwraca kod zero.
- Rulesety `develop`, `release` i `main` wymieniają te pięć checków obok checków z numeru gałęzi.
- `affected-components.sh` zwraca pustą listę i kod zero.
- W workflow nie ma `pull_request_target` ani sekretu.
