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

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `.github/workflows/codeql.yml` | job `codeql` |
| `.github/codeql/codeql-config.yml` | zestaw zapytań `security-extended`, próg nie jest tu chowany |

Próg jest w kroku, który czyta SARIF i zwraca kod. Plik konfiguracji nie jest jedynym miejscem, w którym da się przeoczyć siódemkę.

## Plan

### 1. Workflow

Wyzwalacz: `pull_request` do `develop`, `release` i `main`, oraz `push` do tych gałęzi. Bez filtra `paths`. Uprawnienia: `contents: read`, `security-events: write`. `security-events: write` jest potrzebne, żeby wgrać SARIF. Szerszych uprawnień nie ma.

Macierz języków: `csharp`, `javascript-typescript`. Krok języków, dla których nie ma plików źródłowych, jest pomijany wewnątrz joba z logiem `no material`, ale sam job się uruchamia i na końcu zwraca jeden status `codeql`.

### 2. Werdykt

Po analizie krok czyta SARIF. Kończy się kodem `1`, gdy istnieje wynik o `security-severity` większej albo równej `7.0`, albo wynik bezpieczeństwa bez tego pola. W logu jest reguła, plik i linia. Nie ma tłumienia w komentarzu kodu. Tłumienie, gdy jest naprawdę fałszywe, jest plikiem `.github/codeql/baseline.yml` z identyfikatorem reguły i uzasadnieniem w tym samym pull requeście. Pusty baseline nie wchodzi „na zapas”.

### 3. Ruleset

Nazwa `codeql` wchodzi do trzech rulesetów w tym samym scaleniu.

### Co jest do czego

| Krok | Po co |
| --- | --- |
| inicjalizacja i analiza | produkuje SARIF |
| odczyt progu | zamienia SARIF w kod wyjścia, którego pilnuje ruleset |
| wgranie SARIF | zostawia ślad w zakładce; nie zastępuje kodu wyjścia |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| drzewo bez `*.cs` i bez `*.ts` | lokalny warunek ze skryptu joba | log `no material`, kod `0` |
| SARIF z jednym wynikiem `security-severity: 7.0` | krok werdyktu na tym pliku | kod `1` |
| SARIF z wynikiem `3.0` | ten sam krok | kod `0` |

Plik SARIF do suchych przebiegów leży w `tests/ci/codeql/` i nie jest kodem aplikacji. Nie zawiera celowo dziurawej usługi tylko po to, żeby skan miał co znaleźć.

## Checkpoint

```bash
test -f .github/workflows/codeql.yml
actionlint .github/workflows/codeql.yml
grep -q 'security-extended' .github/workflows/codeql.yml
```

`actionlint` zwraca kod zero. W rulesetach jest `codeql`.

## Umiesz wyjaśnić

Dlaczego werdykt nie może być tylko zakładką Security?

Bo scalenie pilnuje statusu checka. Zakładka jest archiwum. Bez kodu wyjścia wysoki wynik nie zatrzymuje pull requestu.

Co blokuje siódemka, a czego nie blokuje?

Wynik o wadze co najmniej `7.0` i wynik bezpieczeństwa bez wagi. Wynik poniżej siódemki zostaje w logu i nie przewraca joba.

Dlaczego osobno od `dependency-vulnerabilities`?

Tamten check czyta paczki i obrazy. Ten czyta kod, który paczka woła. Podatność w cudzej bibliotece i sklejenie SQL w naszym kodzie to dwa źródła.

Co z pustym repozytorium?

Job się uruchamia, loguje `no material` i jest zielony. Inaczej pierwszy pull request nie miałby jak przejść, a ruleset czekałby na status.

Dlaczego nie ma tłumienia w komentarzu?

Komentarz znika w refaktorze albo zostaje na zawsze bez uzasadnienia. Baseline jest plikiem z powodem i przechodzi recenzję diffu.

## Zasady

- Zestaw zapytań to `security-extended`. Nie zwęża się go do `security-and-quality` po to, żeby było zielono.
- Próg `7.0` jest w workflow, nie tylko w dokumentacji.
- Job nie dostaje sekretu aplikacji.
- `pull_request_target` nie występuje.
- Język spoza `csharp` i `javascript-typescript` nie jest dopisywany, dopóki w drzewie nie ma takich plików.

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
- Na drzewie bez źródeł job loguje `no material` i zwraca kod zero.
- Suchy SARIF z wagą `7.0` kończy krok werdyktu kodem `1`.
- Trzy rulesety wymieniają `codeql`.
- W pliku workflow nie ma tagu akcji w postaci `@v3` bez SHA.
