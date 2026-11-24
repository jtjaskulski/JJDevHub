# 07. Łańcuch dostaw

Poprzedni numer to [SonarQube](06-sonarqube.md). Następny to [obrazy](08-obrazy.md).

Ten numer dodaje cztery checki: `gitleaks`, `zizmor`, `action-pins` i `scorecard`. Zatrzymują sekret w diffie, niebezpieczny workflow, akcję przypiętą tagiem i trzy sondy OpenSSF, które da się policzyć z plików. Nie sięgają do sieci po ocenę, która wymaga tokenu i która i tak nie blokuje.

W drzewie, które czytasz, są konfiguracja, skrypty i sumy binarek. Czterech plików workflow ze [słownika](../00-fundamenty/slownik.md) nie ma. Rulesety tych czterech kontekstów nie wymieniają. Ten numer opisuje pliki, które leżą, i nazywa brak wprost. Nie dopisuje YAML z pamięci.

## Po co

Sekret wcommittowany zostaje w historii, nawet gdy następny commit go usuwa. Akcja `@v4` wskazuje ruchomy tag: wczorajszy przebieg i dzisiejszy nie budują tego samego kodu. `pull_request_target` z checkoutem głowy pull requestu uruchamia cudzy kod z uprawnieniami tokenu bazowego. Scorecard w interfejsie pokazuje średnią, w której sonda „czy jest licencja” przykrywa sondę „czy workflow jest niebezpieczny”.

Cztery checki są wymagane już na `develop`, zanim powstanie pierwsza usługa. Workflow z numerów poprzednich też przez nie przechodzą, gdy joby istnieją. Dziś pin akcji da się sprawdzić lokalnie. Jobów, które wołałyby gitleaks, zizmor i scorecard, w `.github/workflows/` nie ma.

## Dlaczego ta technologia

### gitleaks

gitleaks czyta diff i historię pod kątem wzorców sekretu. Problem, który zamyka, to connection string z hasłem w `appsettings.json`. Odpada sama lista `.gitignore`, bo nie łapie pliku, który ktoś dodał świadomie. Koszt: fałszywy trop na przykładzie. W tym drzewie nie ma committowanego hasła i nie ma pliku `.gitleaksignore`. Przykład `@v4` z kroku pinu żyje w katalogu tymczasowym i jest sprzątany.

Plik SOPS, `*.sops.yaml` i plik kończący się na `.enc.yaml` padają osobną regułą, nawet gdy gitleaks nie widzi entropii. Sekret zaszyfrowany w git nadal jest sekretem w git. Docelowe miejsce sekretu to OpenBao, a do tego czasu plik env poza repozytorium.

### zizmor

zizmor czyta workflow pod kątem wzorców, które oddają uprawnienia. Problem, który zamyka, to `pull_request_target` i `permissions: write-all`. Odpada ręczna lektura YAML przy każdym numerze. Koszt: pin wersji zizmora w jobie, tym samym plikiem sum co inne binarki.

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
| suma | wiersz w `.github/supply-chain/checksums.txt`: nazwa, wersja, SHA-256, nazwa archiwum |

## Klasy i ścieżki

| Ścieżka | Co robi w drzewie |
| --- | --- |
| `.gitleaks.toml` | reguła `sops-file` na ścieżkę `*.sops.yaml` i `*.enc.yaml` |
| `.github/zizmor.yml` | konfiguracja: `rules` puste, bez wyjątku na zapas |
| `.github/scripts/action-pins.sh` | parser `uses` w workflow |
| `.github/scripts/scorecard-gate.sh` | trzy sondy z JSON na stdin, próg `10` |
| `.github/scripts/install-pinned-tool.sh` | ściąga binarkę i sprawdza sumę |
| `.github/supply-chain/checksums.txt` | `gitleaks` `8.30.1`, `zizmor` `1.31.1`, `scorecard` `5.6.0`, `syft` `1.57.2` |
| `.github/workflows/gitleaks.yml` | nazwa ze słownika; pliku w drzewie nie ma |
| `.github/workflows/zizmor.yml` | nazwa ze słownika; pliku w drzewie nie ma |
| `.github/workflows/action-pins.yml` | nazwa ze słownika; pliku w drzewie nie ma |
| `.github/workflows/scorecard.yml` | nazwa ze słownika; pliku w drzewie nie ma |

`syft` w pliku sum woła [numer obrazów](08-obrazy.md). Trzy pozostałe nazwy mają wiersz i nie mają kroku, który by je wołał.

## Plan

### 1. Reguła ścieżki sekretu

`.gitleaks.toml` rozszerza reguły domyślne i dokłada jedną własną. Ścieżka sama jest trafieniem. Treść pliku nie musi mieć wysokiej entropii:

```toml
[extend]
useDefault = true

[[rules]]
id = "sops-file"
description = "plik SOPS albo enc yaml"
regex = '''(?s).+'''
path = '''(?:^|/)[^/]*(?:\.sops\.yaml|\.enc\.yaml)$'''
```

Plik dopasowany do `*.sops.yaml` albo `*.enc.yaml` jest trafieniem tej reguły. W drzewie takiego pliku nie ma. `.sops.yaml` w korzeniu też nie.

Sprawdzenie:

```bash
test -f .gitleaks.toml
grep -q 'id = "sops-file"' .gitleaks.toml
test ! -f .sops.yaml
test -z "$(git ls-files '*.sops.yaml' '*.enc.yaml')"
```

Każde polecenie kończy się kodem zero.

### 2. Konfiguracja zizmora

`.github/zizmor.yml` ma pusty zestaw wyjątków. Komentarz w pliku jest kontraktem kształtu przyszłego wyjątku: jedna reguła, jedna ścieżka, jedna linijka powodu.

```yaml
rules: {}
```

Wyjątku „na zapas” nie ma. W workflow, które są w drzewie, nie ma `pull_request_target`. To da się przeczytać bez zizmora:

```bash
grep -q '^rules: {}$' .github/zizmor.yml
if grep -R -n pull_request_target .github/workflows; then exit 1; fi
```

Pierwsze polecenie kończy się kodem zero. Drugie nic nie wypisuje i też kończy się kodem zero: `grep` bez trafienia ma kod `1`, a `if` zamienia brak trafienia na sukces.

### 3. Pin akcji

`.github/scripts/action-pins.sh` czyta plik albo katalog. Bez argumentu bierze `.github/workflows` i pliki `*.yml` oraz `*.yaml` z pierwszego poziomu. Linia `uses:` jest dzielona na kod i komentarz po `#`.

Lokalne `./` skrypt pomija. Każde inne `uses` ma mieć SHA o długości 40 znaków szesnastkowych po `@` i komentarz zaczynający się od `v`. Inaczej kod wyjścia to `1`, a na stderr jest `brak pełnego SHA` albo `brak komentarza wersji`, ze ścieżką i numerem linii.

Żywe workflow w drzewie przechodzą. Tag bez SHA pada. Sprawdzenie na kopii, nie na produkcie:

```bash
dir=$(mktemp -d)
printf '%s\n' '        uses: actions/checkout@v4' >"$dir/bad.yml"
bash .github/scripts/action-pins.sh "$dir/bad.yml"; test $? -eq 1
printf '%s\n' '        uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1' >"$dir/good.yml"
bash .github/scripts/action-pins.sh "$dir/good.yml"; test $? -eq 0
printf '%s\n' '        uses: ./local/action' >"$dir/local.yml"
bash .github/scripts/action-pins.sh "$dir/local.yml"; test $? -eq 0
rm -rf "$dir"
bash .github/scripts/action-pins.sh
```

Ostatnie polecenie, na bieżących workflow, zwraca kod zero. W logu złej linii widać `brak pełnego SHA`.

### 4. Trzy sondy

`.github/scripts/scorecard-gate.sh` nie bierze argumentów. Czyta JSON ze stdin i wymaga `jq`. Brak JSON albo terminal na stdin kończy się kodem `2`.

Sondy, które blokują:

```text
Dangerous-Workflow
Token-Permissions
Pinned-Dependencies
```

Brak sondy, wynik `-1` albo liczba mniejsza niż `10` ustawiają kod `1` i wypisują powód na stderr. Pozostałe sondy idą na stdout (albo do `GITHUB_STEP_SUMMARY`, gdy zmienna jest ustawiona) i kodu nie zmieniają. Średnia nie jest czytana.

```bash
printf '%s\n' '{"checks":[{"name":"Dangerous-Workflow","score":10},{"name":"Token-Permissions","score":10},{"name":"Pinned-Dependencies","score":10},{"name":"License","score":0}]}' | bash .github/scripts/scorecard-gate.sh; test $? -eq 0
printf '%s\n' '{"checks":[{"name":"Dangerous-Workflow","score":9},{"name":"Token-Permissions","score":10},{"name":"Pinned-Dependencies","score":10}]}' | bash .github/scripts/scorecard-gate.sh; test $? -eq 1
```

Pierwsze wywołanie wypisuje `- License: 0` i kończy się kodem zero. Drugie wypisuje `Dangerous-Workflow wynik 9` i kończy się kodem `1`.

### 5. Binarka z sumą

`.github/scripts/install-pinned-tool.sh` przyjmuje nazwę i katalog. Zna cztery nazwy: `gitleaks`, `zizmor`, `scorecard`, `syft`. Inna nazwa kończy się kodem `2`. Wiersz bierze z `.github/supply-chain/checksums.txt`. Dwa wiersze tej samej nazwy, zła suma albo brak wiersza też kończą się kodem `2`.

Żywe wiersze:

```text
gitleaks 8.30.1 551f6fc83ea457d62a0d98237cbad105af8d557003051f41f3e7ca7b3f2470eb gitleaks_8.30.1_linux_x64.tar.gz
zizmor 1.31.1 d185f726bdaf92df78273217dfdd24ebcd6b0ef48cf3ce776ff5762b6e138c99 zizmor-x86_64-unknown-linux-gnu.tar.gz
scorecard 5.6.0 19bde07129450cb65a95c6e3b9f34e034341579e6f0e241546ec3ae6efb2dd9a scorecard_5.6.0_linux_amd64.tar.gz
syft 1.57.2 ad92bd3ec1b94a3df01d4d0aec199afa9c42b72e86da436ff5f837914d32706b syft_1.57.2_linux_amd64.tar.gz
```

Skrypt ściąga `https://github.com/<repo>/releases/download/v<wersja>/<archiwum>`, sprawdza SHA-256 i wstawia binarkę do podanego katalogu z trybem `0755`. Wywołanie idzie w sieć. Ten numer nie każe go odpalać na sucho. Job obrazu woła je dla `syft`. Jobów gitleaks, zizmor i scorecard, które wołałyby trzy pierwsze nazwy, w drzewie nie ma.

Sprawdzenie samego pliku sum, bez sieci:

```bash
python3 -c 'import pathlib
rows=[line.split() for line in pathlib.Path(".github/supply-chain/checksums.txt").read_text().splitlines() if line and not line.startswith("#")]
names=[row[0] for row in rows]
assert names==["gitleaks","zizmor","scorecard","syft"]
assert all(len(row[2])==64 for row in rows)
'
```

### 6. Czego w drzewie nie ma

Słownik wymienia cztery workflow: `.github/workflows/gitleaks.yml`, `zizmor.yml`, `action-pins.yml` i `scorecard.yml`. Żadnego z nich nie ma. Trzy pliki w `.github/rulesets/` nie mają kontekstów `gitleaks`, `zizmor`, `action-pins` ani `scorecard`.

Ten krok nie wkleja YAML tych jobów. Nie ma w drzewie pliku, z którego dałoby się wziąć fragment. Warunek numeru zostaje: cztery nazwy w rulesetach, `actionlint` na czterech workflow, poniedziałkowy skan historii gitleaks i lokalny scorecard. Spełni go osobna zmiana kodu. Do tego czasu tutorial nie każe otwierać pliku, którego nie ma.

Sprawdzenie braku:

```bash
test ! -f .github/workflows/gitleaks.yml
test ! -f .github/workflows/zizmor.yml
test ! -f .github/workflows/action-pins.yml
test ! -f .github/workflows/scorecard.yml
python3 -c 'import json,pathlib
missing={"gitleaks","zizmor","action-pins","scorecard"}
for path in pathlib.Path(".github/rulesets").glob("*.json"):
    data=json.load(path.open())
    checks={item["context"] for item in data["rules"][-1]["parameters"]["required_status_checks"]}
    assert missing.isdisjoint(checks), path
'
```

Kod zero znaczy, że plików nie ma i rulesety ich nie wymieniają.

### Co jest do czego

| Plik | Co łapie dziś |
| --- | --- |
| `.gitleaks.toml` | ścieżka SOPS i `*.enc.yaml`, gdy ktoś uruchomi gitleaks z tym plikiem |
| `.github/zizmor.yml` | zakaz wyjątku bez jednego powodu |
| `action-pins.sh` | ruchomy tag w workflow, które już są |
| `scorecard-gate.sh` | trzy sondy w JSON, bez średniej |
| `install-pinned-tool.sh` | suma binarki; żywe wołanie to `syft` w numerze obrazów |
| cztery workflow ze słownika | jeszcze nie stoją w `.github/workflows/` |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| linia `uses: actions/checkout@v4` w pliku tymczasowym | `action-pins.sh` | kod `1`, log `brak pełnego SHA` |
| linia z SHA o długości 40 i komentarzem `# v7.0.1` | `action-pins.sh` | kod `0` |
| linia `uses: ./local/action` | `action-pins.sh` | kod `0` |
| JSON z trzema sondami po `10` i `License` równe `0` | `scorecard-gate.sh` | kod `0`, na stdout jest `License` |
| JSON z `Dangerous-Workflow` równym `9` | `scorecard-gate.sh` | kod `1` |

Plik z hasłem nie jest commitowany. Przykład `@v4` żyje w katalogu tymczasowym z kroku 3 i jest sprzątany. Suchego przebiegu `tests/ci/` dla tego numeru nie ma.

## Checkpoint

```bash
bash .github/scripts/action-pins.sh
test ! -f .sops.yaml
test -z "$(git ls-files '*.sops.yaml' '*.enc.yaml')"
if grep -R -n pull_request_target .github/workflows; then exit 1; fi
python3 -c 'import pathlib; text=pathlib.Path(".github/zizmor.yml").read_text(); assert "rules: {}" in text'
```

`action-pins.sh` na bieżącym drzewie zwraca kod zero. `grep` nic nie znajduje.

## Umiesz wyjaśnić

Dlaczego tag `@v4` nie wystarcza?

Bo wskazuje etykietę, którą autor akcji może przesunąć. SHA wskazuje commit, który przebieg już widział. Komentarz z wersją jest dla człowieka. Werdykt czyta SHA. Skrypt pada komunikatem `brak pełnego SHA`, zanim sprawdzi komentarz.

Czemu SOPS nie wchodzi, skoro treść jest zaszyfrowana?

Bo klucz i plik żyją obok siebie w procesie, a repozytorium zostaje kopią sekretu. Reguła `sops-file` trafia w ścieżkę, nie w entropię. Docelowy magazyn to OpenBao. Do tego czasu env jest poza gitem.

Dlaczego Scorecard nie blokuje na średniej?

Średnia miesza sondy plikowe z sondami, które bez tokenu i wydań są puste. Trzy sondy mają próg `10` każda. `License` równe `0` jest wypisywane i nie przewraca kodu. Wynik `9` na `Dangerous-Workflow` przewraca.

Czym harmonogram poniedziałkowy różni się od checka?

Check blokuje pull request. Harmonogram powtarza skan historii i nie otwiera drogi na `main`. Nie dostaje sekretu. Joba, który ten harmonogram by odpalał, w drzewie nie ma. Zostaje warunek numeru.

Dlaczego `pull_request_target` jest zabroniony w całości, a nie „używany ostrożnie”?

Bo ostrożny wariant i tak czyta kod z pull requestu w kontekście tokenu bazy. Jeden błąd checkoutu oddaje zapis. Nie mamy przypadku, który tego wymaga. W `.github/workflows/` tego wyzwalacza nie ma.

Dlaczego pin da się sprawdzić, a gitleaks w CI jeszcze nie?

Bo `action-pins.sh` jest w drzewie i czyta workflow, które już leżą. Workflow `gitleaks.yml` ze słownika nie leży, więc żaden pull request tego skanu nie uruchamia. Konfiguracja `.gitleaks.toml` czeka na ten job.

## Zasady

- W workflow nie ma wartości sekretu i nie ma `secrets.` wskazującego sekret aplikacji.
- `permissions` jest wypisane w jobie. Nie ma `write-all`.
- `uses` spoza `./` ma SHA i komentarz wersji. Sprawdza to `action-pins.sh`.
- Plik SOPS nie wchodzi do drzewa.
- Runner jobów, gdy powstaną, to `ubuntu-latest`.
- Wyjątek w `.github/zizmor.yml` ma jedną regułę, jedną ścieżkę i jedno zdanie. Pusty `rules` tego nie łamie.

## Poza zakresem

- OpenBao jest w [05-openbao.md](../13-kubernetes/05-openbao.md). Do tego czasu sekret jest w user secrets i w `/etc/jjdevhub/compose.env`.
- Atestacja obrazu jest w [08-obrazy.md](08-obrazy.md). Tam `install-pinned-tool.sh` jest wołany dla `syft`.
- Podatności paczek są w [05-zaleznosci.md](05-zaleznosci.md).
- Wstawienie czterech brakujących workflow i czterech kontekstów do rulesetów jest osobną zmianą kodu. Ten numer jej nie robi.

## Źródła

- [gitleaks](https://github.com/gitleaks/gitleaks). Wiążące jest skanowanie diffu i kod wyjścia. Dotyczy checka `gitleaks` i reguły `sops-file`. Ze źródła nie bierzemy ignorowania całego katalogu `src/`.
- [zizmor](https://docs.zizmor.sh/). Wiążące są reguły niebezpiecznego workflow. Dotyczy zakazu `pull_request_target` i pustego `rules` w `.github/zizmor.yml`. Ze źródła nie bierzemy trybu, który tylko ostrzega i zwraca kod zero.
- [OpenSSF Scorecard](https://securityscorecards.dev/docs/checks). Wiążące są trzy sondy plikowe i wynik `10`. Dotyczy `scorecard-gate.sh`. Ze źródła nie bierzemy średniej jako progu scalenia.
- [Security hardening for GitHub Actions](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions). Wiążące jest przypięcie akcji do SHA i najwęższe `permissions`. Dotyczy `action-pins.sh` i każdego workflow od tego numeru wstecz. Ze źródła nie bierzemy `pull_request_target` jako zalecanego skrótu.

## Gotowe gdy

- Cztery checki są w trzech rulesetach i `actionlint` milczy na czterech workflow. To jest warunek numeru.
- W drzewie, które czytasz, tego warunku nie da się zaliczyć: czterech plików workflow nie ma, a rulesety nie wymieniają `gitleaks`, `zizmor`, `action-pins` ani `scorecard`. Checkpoint poniżej sprawdza pliki, które są.
- `uses` z tagiem `@v4` pada lokalnie w `action-pins.sh`. Bieżące workflow mają SHA i komentarz wersji.
- W drzewie nie ma pliku SOPS i nie ma `pull_request_target`.
- `scorecard-gate.sh` czyta trzy sondy. Wynik poniżej `10` pada. Inna sonda jest wypisywana i nie jest werdyktem.
