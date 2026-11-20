# 07. Łańcuch dostaw

Poprzedni numer to [SonarQube](06-sonarqube.md). Następny to [obrazy](08-obrazy.md).

Ten numer dodaje cztery checki: `gitleaks`, `zizmor`, `action-pins` i `scorecard`. Zatrzymują sekret w diffie, niebezpieczny workflow, akcję przypiętą tagiem i trzy sondy OpenSSF, które da się policzyć z plików. Nie sięgają do sieci po ocenę, która wymaga tokenu i która i tak nie blokuje.

## Po co

Sekret wcommittowany zostaje w historii, nawet gdy następny commit go usuwa. Akcja `@v4` wskazuje ruchomy tag: wczorajszy przebieg i dzisiejszy nie budują tego samego kodu. `pull_request_target` z checkoutem głowy pull requestu uruchamia cudzy kod z uprawnieniami tokenu bazowego. Scorecard w interfejsie pokazuje średnią, w której sonda „czy jest licencja” przykrywa sondę „czy workflow jest niebezpieczny”.

Cztery checki są wymagane już na `develop`, zanim powstanie pierwsza usługa. Workflow z numerów poprzednich też przez nie przechodzą.

## Dlaczego ta technologia

### gitleaks

gitleaks czyta diff i historię pod kątem wzorców sekretu. Problem, który zamyka, to connection string z hasłem w `appsettings.json`. Odpada sama lista `.gitignore`, bo nie łapie pliku, który ktoś dodał świadomie. Koszt: fałszywy trop na przykładzie w teście. Przykład w `tests/ci/` jest w `.gitleaksignore` z uzasadnieniem, a nie w kodzie usługi.

Plik SOPS, `*.sops.yaml` i plik kończący się na `.enc.yaml` padają osobną regułą, nawet gdy gitleaks nie widzi entropii. Sekret zaszyfrowany w git nadal jest sekretem w git. Docelowe miejsce sekretu to OpenBao, a do tego czasu plik env poza repozytorium.

### zizmor

zizmor czyta workflow pod kątem wzorców, które oddają uprawnienia. Problem, który zamyka, to `pull_request_target` i `permissions: write-all`. Odpada ręczna lektura YAML przy każdym numerze. Koszt: pin wersji zizmora w jobie, tym samym SHA co inne akcje.

### Pin do SHA

`action-pins` przechodzi po `uses:` i wymaga pełnego SHA commita oraz komentarza z tagiem, na przykład `uses: actions/checkout@<40-hex> # v4.2.2`. Problem, który zamyka, to tag, który ktoś przesunął. Odpada sam Dependabot bez checka: Dependabot przynosi podbicie, a check nie pozwala scalić tagu bez SHA. Koszt: podbicie akcji jest edycją SHA i komentarza.

### Scorecard, trzy sondy

Lokalny przebieg OpenSSF Scorecard, bez wysyłania repozytorium. Blokują sondy plikowe, każda z progiem `10`: `Pinned-Dependencies`, `Token-Permissions`, `Dangerous-Workflow`. Średnia z sond, które potrzebują tokenu i historii wydań, nie blokuje. Problem, który zamyka, to zielona średnia przy jednym niebezpiecznym workflow. Odpada bramka „Scorecard co najmniej 7”, bo tę siódemkę da się kupić licencją w pliku i kodem, którego nikt nie czyta.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| pin | `uses` wskazuje commit, nie tag |
| sonda | jedna kontrola Scorecard, nie średnia |
| `pull_request_target` | przebieg w kontekście bazy, z jej tokenem; u nas zabroniony |

## Klasy i ścieżki

| Ścieżka | Check |
| --- | --- |
| `.github/workflows/gitleaks.yml` | `gitleaks` |
| `.github/workflows/zizmor.yml` | `zizmor` |
| `.github/workflows/action-pins.yml` | `action-pins` |
| `.github/workflows/scorecard.yml` | `scorecard` |
| `.gitleaks.toml` | reguły dodatkowe, w tym zakaz plików SOPS |
| `.github/scripts/action-pins.sh` | parser `uses` |

## Plan

### 1. gitleaks

Job na `pull_request` skanuje diff. Dodatkowo w poniedziałek skanuje całą historię na `ubuntu-latest`. Harmonogram nie zastępuje checka pull requestu i nie dostaje sekretu. Znalezisko to kod `1`. Plik dopasowany do `*.sops.yaml` albo `*.enc.yaml` to kod `1`, nawet bez dopasowania entropii.

### 2. zizmor

Job uruchamia zizmor na `.github/workflows`. Znalezisko o wadze, którą zizmor oznacza jako blokującą w konfiguracji `deny`, kończy się kodem `1`. W konfiguracji zabronione jest co najmniej: `pull_request_target`, uprawnienie szersze niż wypisane w jobie, checkout głowy pull requestu z tokenem zapisu.

### 3. action-pins

Skrypt pada, gdy linia `uses:` poza lokalnym `./` nie ma SHA o długości 40 hex albo nie ma komentarza `# v`. Akcje z tego numeru i z numerów poprzednich są poprawione w tym samym scaleniu. Inaczej check sam siebie oblewa i ruleset staje.

### 4. Scorecard

Job uruchamia `scorecard` z flagą lokalną. Czyta trzy sondy. Każda poniżej `10` to kod `1`. Reszty sond nie czyta jako werdyktu. Log może je wypisać.

### 5. Ruleset

Cztery nazwy wchodzą do trzech rulesetów.

### Co jest do czego

| Check | Co łapie |
| --- | --- |
| `gitleaks` | wartość sekretu i plik SOPS |
| `zizmor` | kształt workflow |
| `action-pins` | ruchomy tag akcji |
| `scorecard` | trzy sondy, które te pliki już powinny spełniać |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| plik testowy z napisem `Password=super-secret-value` poza allowlistą | `gitleaks detect` | kod `1` |
| workflow z `uses: actions/checkout@v4` | `action-pins.sh` | kod `1` |
| workflow z SHA i komentarzem `# v4.2.2` | `action-pins.sh` | kod `0` |

Plik z hasłem żyje tylko w teście skryptu, w katalogu tymczasowym, i nie jest commitowany. Test kopiuje go do katalogu roboczego i sprząta.

## Checkpoint

```bash
actionlint .github/workflows/gitleaks.yml .github/workflows/zizmor.yml .github/workflows/action-pins.yml .github/workflows/scorecard.yml
bash .github/scripts/action-pins.sh
test ! -f .sops.yaml
```

`action-pins.sh` na bieżącym drzewie zwraca kod zero. `grep -R pull_request_target .github/workflows` nic nie znajduje.

## Umiesz wyjaśnić

Dlaczego tag `@v4` nie wystarcza?

Bo wskazuje etykietę, którą autor akcji może przesunąć. SHA wskazuje commit, który przebieg już widział. Komentarz z wersją jest dla człowieka. Werdykt czyta SHA.

Czemu SOPS nie wchodzi, skoro treść jest zaszyfrowana?

Bo klucz i plik żyją obok siebie w procesie, a repozytorium zostaje kopią sekretu. Docelowy magazyn to OpenBao. Do tego czasu env jest poza gitem.

Dlaczego Scorecard nie blokuje na średniej?

Średnia miesza sondy plikowe z sondami, które bez tokenu i wydań są puste. Trzy sondy, które ten numer naprawdę egzekwuje, mają próg `10` każda.

Czym harmonogram poniedziałkowy różni się od checka?

Check blokuje pull request. Harmonogram powtarza skan historii i nie otwiera drogi na `main`. Nie dostaje sekretu.

Dlaczego `pull_request_target` jest zabroniony w całości, a nie „używany ostrożnie”?

Bo ostrożny wariant i tak czyta kod z pull requestu w kontekście tokenu bazy. Jeden błąd checkoutu oddaje zapis. Nie mamy przypadku, który tego wymaga.

## Zasady

- W workflow nie ma wartości sekretu i nie ma `secrets.` wskazującego sekret aplikacji.
- `permissions` jest wypisane w jobie. Nie ma `write-all`.
- `uses` spoza `./` ma SHA i komentarz wersji.
- Plik SOPS nie wchodzi do drzewa.
- Runner tych jobów to `ubuntu-latest`.

## Poza zakresem

- OpenBao jest w [05-openbao.md](../13-kubernetes/05-openbao.md). Do tego czasu sekret jest w user secrets i w `/etc/jjdevhub/compose.env`.
- Atestacja obrazu jest w [08-obrazy.md](08-obrazy.md).
- Podatności paczek są w [05-zaleznosci.md](05-zaleznosci.md).

## Źródła

- [gitleaks](https://github.com/gitleaks/gitleaks). Wiążące jest skanowanie diffu i kod wyjścia. Dotyczy checka `gitleaks`. Ze źródła nie bierzemy ignorowania całego katalogu `src/`.
- [zizmor](https://docs.zizmor.sh/). Wiążące są reguły niebezpiecznego workflow. Dotyczy zakazu `pull_request_target`. Ze źródła nie bierzemy trybu, który tylko ostrzega i zwraca kod zero.
- [OpenSSF Scorecard](https://securityscorecards.dev/docs/checks). Wiążące są trzy sondy plikowe i wynik `10`. Dotyczy checka `scorecard`. Ze źródła nie bierzemy średniej jako progu scalenia.
- [Security hardening for GitHub Actions](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions). Wiążące jest przypięcie akcji do SHA i najwęższe `permissions`. Dotyczy `action-pins` i każdego workflow od tego numeru wstecz. Ze źródła nie bierzemy `pull_request_target` jako zalecanego skrótu.

## Gotowe gdy

- Cztery checki są w trzech rulesetach i `actionlint` milczy.
- `uses` z tagiem `@v4` pada lokalnie w `action-pins.sh`.
- Bieżące workflow mają SHA i komentarz wersji.
- W drzewie nie ma pliku SOPS i nie ma `pull_request_target`.
- Scorecard w jobie czyta trzy sondy, nie średnią.
