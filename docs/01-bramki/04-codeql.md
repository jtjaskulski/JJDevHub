# 04. CodeQL

Poprzedni numer to [build i testy](03-build-testy-format.md). Następny to [zależności](05-zaleznosci.md).

Ten numer dodaje check `codeql`. Skan czyta kod C#, JavaScript i TypeScript. Werdykt bierze się z kodu wyjścia na raporcie SARIF, nie z zakładki, w którą nikt nie wchodzi. Próg to `security-severity` co najmniej `7.0`. Alert bezpieczeństwa bez pola wagi też blokuje.

## Po co

Kompilacja nie widzi zapytania SQL złożonego ze sklejenia napisów ani odpowiedzi, która oddaje wyjątek do klienta. To nie jest test jednostkowy: test sprawdza zachowanie, które ktoś napisał. Skan szuka wzorców, o których test nie pomyślał.

Bez checka w rulesecie wynik skanu jest informacją. Informacja nie zatrzymuje scalenia. Ten numer robi z niej bramkę na `develop`, `release` i `main`.

## Dlaczego ta technologia

### CodeQL, zestaw `security-extended`

CodeQL buduje bazę faktów o kodzie i uruchamia zapytania. Zestaw `security-extended` jest szerszy niż domyślny `security-and-quality` w części bezpieczeństwa i nie zamienia stylu w błąd bramki. Styl jest w [formacie](03-build-testy-format.md), jakość w [SonarQube](06-sonarqube.md).

Odpada samo „włączę skan w ustawieniach repozytorium”. Ustawienie nie jedzie w klonie i nie ma pinu akcji. Workflow w git ma pin commita akcji, zgodnie z [łańcuchem dostaw](07-lancuch-dostaw.md). Do czasu tamtego numeru akcja i tak jest zapisana z pełnym SHA, nie z tagiem `@v3`, żeby nie było dwóch migracji.

### Próg `7.0`

`security-severity` w skali CVSS. Siódemka to wysoki próg. Problem, który zamyka, to czerwona bramka od uwagi niskiej wagi, którą i tak się akceptuje, aż ktoś wyłączy check. Odpada próg zero, który po miesiącu zostaje zignorowany. Koszt: znalezisko średnie nie blokuje. Widać je w logu. Nie znika.

Alert z kategorią bezpieczeństwa i bez liczby wagi blokuje. Brak liczby nie jest niską wagą.

### Języki

`csharp` oraz `javascript-typescript`. Innych języków w repozytorium nie ma. Python w workflow nie występuje, żeby skan nie udawał pokrycia, którego nie ma.

Gdy w drzewie nie ma `*.cs`, `*.js`, `*.ts` ani `*.tsx`, job loguje `no material` i zwraca kod `0`. Nie uruchamia analizy pustej bazy.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| SARIF | plik wynikowy skanu, który da się przeczytać bez interfejsu |
| `security-severity` | liczba wagi w wyniku zapytania |
| baza CodeQL | fakty wyciągnięte z kodu, na których lecą zapytania |
| baseline | plik z identyfikatorem reguły i zdaniem powodu; gasi werdykt tej reguły |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `.github/workflows/codeql.yml` | job `codeql`: języki, init, analiza, werdykt, wgranie SARIF |
| `.github/scripts/codeql-languages.sh` | `csharp`, `javascript-typescript` albo `no material` z `git ls-files` |
| `.github/scripts/codeql-gate.sh` | próg `7.0` i baseline; wymaga `jq` |
| `.github/codeql/codeql-config.yml` | zestaw `security-extended` i `paths-ignore`; progu tu nie ma |
| `tests/ci/codeql.sh` | suchy przebieg języków, trzech SARIF-ów i baseline |
| `tests/ci/codeql/high.sarif` | waga `7.0`, kod `1` |
| `tests/ci/codeql/low.sarif` | waga `3.0`, kod `0`, linia zostaje w logu |
| `tests/ci/codeql/unscored.sarif` | tag `security` bez liczby, kod `1` |

Próg jest w `codeql-gate.sh`. Plik konfiguracji nie jest miejscem, w którym da się przeoczyć siódemkę.

## Plan

### 1. Kiedy jest materiał

`.github/scripts/codeql-languages.sh` nie bierze argumentów. Czyta `git ls-files` i pomija segment `bin`, `obj`, `node_modules` i `dist`. `*.cs` włącza C#. `*.js`, `*.ts` i `*.tsx` włączają JavaScript z TypeScriptem.

Gdy nie ma żadnego z tych plików, skrypt wypisuje jedną linię `no material` i kończy się kodem `0`. Gdy jest tylko jeden język, druga linia mówi, którego brakuje:

```text
csharp
no material: javascript-typescript
```

albo odwrotnie. Job i tak zwraca jeden status `codeql`.

W `.github/workflows/codeql.yml` krok „Detect languages” zapisuje wyjścia. Linia `no material` bez dwukropka ustawia `skip=true` i `build_mode=skip`. Inaczej `skip=false`, a `build_mode` zależy od liczby języków: sam C# to `manual`, sam front to `none`, oba języki to `both`.

Sprawdzenie na kopii, nie na solucji, robi suchy przebieg. Ręcznie, na pustym repozytorium gita w katalogu tymczasowym, ta sama komenda wypisuje `no material`.

### 2. Konfiguracja zapytań

`.github/codeql/codeql-config.yml` wskazuje zestaw i ścieżki, których baza nie buduje:

```yaml
queries:
  - uses: security-extended
paths-ignore:
  - "docs/**"
  - "versions/**"
  - "**/*.Generated.cs"
  - "**/bin/**"
  - "**/obj/**"
```

Prozy, plików wersji i generatora skan nie traktuje jak źródła. Init w workflow podaje ten plik jako `config-file` i `queries: security-extended` jeszcze raz, w argumencie akcji.

Akcja init jest przypięta do SHA `9b3254085494b55a0f7c4c2b3d2d52cc712ce370` z komentarzem `v4.39.1`. Ta sama akcja ma dwa kroki. Przy `build_mode` `manual` albo `none` krok ustawia `build-mode` na tę wartość. Przy `both` drugi krok podaje języki, zestaw i plik konfiguracji, bez `build-mode`.

Gdy na liście jest C#, job stawia SDK z `global.json` i buduje każdy `*.csproj` z `git ls-files`, konfiguracją `Release`. Brak `global.json` przy projekcie albo pusta lista projektów kończy się kodem `1`. To jest ten sam wymóg, co przy [zależnościach](05-zaleznosci.md): restore poza linią SDK nie liczy się jako skan.

### 3. Workflow i uprawnienia

Wyzwalacz: `pull_request` oraz `push` do `develop`, `release` i `main`. Bez filtra `paths`. `timeout-minutes` to `30`. Uprawnienia joba: `contents: read` i `security-events: write`. Szerszych nie ma. `security-events: write` jest na wgranie SARIF. Checkout ma `fetch-depth: 2` i `persist-credentials: false`.

Kolejność kroków, które naprawdę stoją w pliku:

1. checkout
2. `bash tests/ci/codeql.sh`
3. wykrycie języków
4. init, jeden z dwóch kroków, gdy `skip` nie jest `true`
5. setup SDK i build C#, gdy jest `csharp`
6. analiza z `upload: never` i katalogiem `codeql-results`
7. artefakt `codeql-sarif` na 14 dni, `if-no-files-found: warn`
8. `.github/scripts/codeql-gate.sh --sarif` na każdym `*.sarif` w tym katalogu
9. wgranie do zakładki, `continue-on-error: true`, tylko gdy analiza się udała

Krok 6 nie wgrywa wyniku. Krok 8 jest werdyktem. Krok 9 jest archiwum i nie podnosi joba, gdy zakładka odrzuci plik. Brak pliku SARIF po analizie kończy krok 8 kodem `1` i komunikatem `brak pliku SARIF`.

### 4. Werdykt

`.github/scripts/codeql-gate.sh` wymaga `--sarif` i programu `jq`. Czyta wyniki, które mają tag `security` na wyniku albo na regule. Dla każdego wypisuje regułę, plik, linię i wagę.

Blokuje, gdy waga jest `missing` albo liczba jest większa albo równa `7.0`. Waga poniżej siódemki zostaje w logu, a pole blokady w wierszu jest `0`, więc kod wyjścia zostaje `0`.

Trzy pliki próbne w `tests/ci/codeql/` są układem, nie kodem aplikacji:

| Plik | Waga | Kod | Linia w logu |
| --- | --- | --- | --- |
| `high.sarif` | `7.0` na regule `fixture/high` | `1` | `fixture/high fixture.txt 1` |
| `low.sarif` | `3.0` na wyniku `fixture/low` | `0` | `fixture/low fixture.txt 1` |
| `unscored.sarif` | tag `security`, brak liczby, linia `4` | `1` | `fixture/unscored fixture.txt 4` |

Baseline, gdy jest, gasi regułę. Skrypt czyta `--baseline`, a bez tej flagi plik `.github/codeql/baseline.yml`, jeśli leży w drzewie. Tego pliku ten numer nie zakłada. Pusty baseline na zapas nie wchodzi. Żywy kształt jednej pozycji:

```yaml
- rule: fixture/high
  reason: false positive reviewed in this pull request
```

Reguła z powodem wypisuje `baseline <reguła> <plik> <linia>` i nie podnosi kodu. Powód pusty kończy się komunikatem `baseline bez powodu` i kodem `1`. Tłumienia w komentarzu kodu nie ma.

Sprawdzenie jednego pliku, bez całego joba:

```bash
bash .github/scripts/codeql-gate.sh --sarif tests/ci/codeql/high.sarif; test $? -eq 1
bash .github/scripts/codeql-gate.sh --sarif tests/ci/codeql/low.sarif; test $? -eq 0
bash .github/scripts/codeql-gate.sh --sarif tests/ci/codeql/unscored.sarif; test $? -eq 1
```

### 5. Suchy przebieg

`tests/ci/codeql.sh` składa dwa tymczasowe repozytoria. Puste wypisuje `no material`. Repozytorium z jednym `A.cs` wypisuje `csharp` i `no material: javascript-typescript`. Potem puszcza trzy SARIF-y przez bramkę z oczekiwanymi kodami z tabeli powyżej i dwa baseline: z powodem oraz z pustym `reason`.

Na końcu sprząta katalogi. Zielony przebieg milczy.

```bash
bash tests/ci/codeql.sh
```

Kod wyjścia `0`. `jq` musi być na `PATH`. Bez niego bramka kończy się komunikatem `brak jq` i kodem `2`.

### 6. Ruleset

Nazwa `codeql` wchodzi do trzech plików w `.github/rulesets/`, obok checków z numerów [gałęzi](02-galezie.md) i [builda](03-build-testy-format.md).

```bash
python3 -c 'import json,pathlib
for path in pathlib.Path(".github/rulesets").glob("*.json"):
    data=json.load(path.open())
    checks={item["context"] for item in data["rules"][-1]["parameters"]["required_status_checks"]}
    assert "codeql" in checks, path
'
```

Kod zero znaczy, że trzy rulesety wymieniają `codeql`.

### Co jest do czego

| Krok | Po co |
| --- | --- |
| `codeql-languages.sh` | jeden status i decyzja, czy w ogóle stawiać bazę |
| init i analiza | produkują SARIF; analiza ma `upload: never` |
| `codeql-gate.sh` | zamienia SARIF w kod wyjścia, którego pilnuje ruleset |
| wgranie SARIF | zostawia ślad w zakładce; `continue-on-error` sprawia, że nie zastępuje kodu wyjścia |
| `tests/ci/codeql.sh` | próg i baseline bez odpalania CodeQL |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| puste repozytorium gita | `codeql-languages.sh` | log `no material`, kod `0` |
| jeden plik `A.cs` w indeksie | `codeql-languages.sh` | `csharp` i `no material: javascript-typescript` |
| `high.sarif` | `codeql-gate.sh --sarif` | kod `1` |
| `low.sarif` | ten sam krok | kod `0`, linia w logu |
| `unscored.sarif` | ten sam krok | kod `1` |
| `high.sarif` i baseline z powodem | `--baseline` | kod `0`, log zaczyna się od `baseline` |
| baseline z pustym `reason` | `--baseline` | kod `1`, log `baseline bez powodu` |

Pliki SARIF leżą w `tests/ci/codeql/` i nie są kodem aplikacji. Nie zawierają celowo dziurawej usługi tylko po to, żeby skan miał co znaleźć. Wszystkie wiersze wykonuje `tests/ci/codeql.sh`.

## Checkpoint

```bash
test -f .github/workflows/codeql.yml
test -f .github/scripts/codeql-gate.sh
grep -q 'security-extended' .github/workflows/codeql.yml
grep -q 'security-extended' .github/codeql/codeql-config.yml
bash tests/ci/codeql.sh
```

`tests/ci/codeql.sh` zwraca kod zero. W rulesetach jest `codeql`. `actionlint .github/workflows/codeql.yml` zwraca kod zero.

## Umiesz wyjaśnić

Dlaczego werdykt nie może być tylko zakładką Security?

Bo scalenie pilnuje statusu checka. Zakładka jest archiwum. Analiza ma `upload: never`, a wgranie do zakładki ma `continue-on-error`. Bez kodu z `codeql-gate.sh` wysoki wynik nie zatrzymuje pull requestu.

Co blokuje siódemka, a czego nie blokuje?

Wynik o wadze co najmniej `7.0` i wynik bezpieczeństwa bez wagi. Wynik `3.0` zostaje w logu i nie przewraca joba. Trzy pliki w `tests/ci/codeql/` są tym podziałem.

Dlaczego osobno od `dependency-vulnerabilities`?

Tamten check czyta paczki i lockfile. Ten czyta kod, który paczka woła. Podatność w cudzej bibliotece i sklejenie SQL w naszym kodzie to dwa źródła.

Co z pustym repozytorium?

`codeql-languages.sh` wypisuje `no material`, job ustawia `skip=true` i jest zielony. Inaczej pierwszy pull request nie miałby jak przejść, a ruleset czekałby na status. Analiza pustej bazy się nie uruchamia.

Dlaczego nie ma tłumienia w komentarzu?

Komentarz znika w refaktorze albo zostaje na zawsze bez uzasadnienia. Baseline jest plikiem z `rule` i `reason`. Pusty powód pada kodem `1`. Pliku `.github/codeql/baseline.yml` nie ma, dopóki konkretna reguła go nie wymusi.

Czym `build_mode: manual` różni się od `none`?

Sam C# buduje się naszym `dotnet build` każdego `*.csproj`. Sam front nie ma kompilacji C#, więc init dostaje `none`. Oba języki idą osobnym krokiem init, bez `build-mode` w argumencie.

## Zasady

- Zestaw zapytań to `security-extended`. Nie zwęża się go do `security-and-quality` po to, żeby było zielono.
- Próg `7.0` jest w `codeql-gate.sh`, nie tylko w dokumentacji i nie w `codeql-config.yml`.
- Job nie dostaje sekretu aplikacji.
- `pull_request_target` nie występuje.
- Język spoza `csharp` i `javascript-typescript` nie jest dopisywany, dopóki w drzewie nie ma takich plików.
- Baseline bez zdania powodu nie wchodzi.

## Poza zakresem

- Podatności paczek i obrazów są w [05-zaleznosci.md](05-zaleznosci.md) i [08-obrazy.md](08-obrazy.md).
- Pin akcji do SHA sprawdza [07-lancuch-dostaw.md](07-lancuch-dostaw.md). Ten numer już pisze `uses` z SHA, żeby nie zostawiać tagu.
- Jakość, duplikacja i pokrycie są w [06-sonarqube.md](06-sonarqube.md).

## Źródła

- [CodeQL code scanning](https://docs.github.com/en/code-security/code-scanning/introduction-to-code-scanning/about-code-scanning-with-codeql). Wiążący jest workflow w git i zestaw zapytań. Dotyczy checka `codeql` na trzech długich gałęziach. Ze źródła nie bierzemy skanu włączonego tylko w interfejsie, bez pliku.
- [SARIF](https://docs.github.com/en/code-security/code-scanning/integrating-with-code-scanning/sarif-support-for-code-scanning). Wiążące jest pole wagi i to, że plik da się ocenić krokiem. Dotyczy kodu wyjścia. Ze źródła nie bierzemy werdyktu opartego wyłącznie na liczbie alertów w interfejsie.
- [Default and extended query suites](https://docs.github.com/en/code-security/code-scanning/managing-your-code-scanning-configuration/codeql-query-suites). Wiążący jest `security-extended` dla języków, które mamy. Dotyczy wyboru zestawu. Ze źródła nie bierzemy zestawu jakości jako bramki: jakość jest w SonarQube.

## Gotowe gdy

- Workflow `codeql.yml` przechodzi `actionlint`.
- `tests/ci/codeql.sh` zwraca kod zero: puste repozytorium daje `no material`, `high.sarif` i `unscored.sarif` dają kod `1`, `low.sarif` daje kod `0`.
- Trzy rulesety wymieniają `codeql`.
- W pliku workflow nie ma tagu akcji w postaci `@v3` bez SHA.
- `.github/codeql/codeql-config.yml` używa `security-extended` i nie trzyma progu `7.0`.
