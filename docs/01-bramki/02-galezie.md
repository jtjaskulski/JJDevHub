# 02. Gałęzie, tytuł i rozmiar zmiany

Poprzedni numer to [solucja](01-solucja.md). Następny to [build, testy i format](03-build-testy-format.md).

Ten numer stawia trzy checki: `branch-name`, `pr-title` i `pr-size`, oraz trzy rulesety. Bez nich zła nazwa gałęzi wchodzi na `develop`, tytuł bez zakresu staje się commitem squasha, a jeden pull request mieści dwa numery.

## Po co

Tag komponentu wycina się z tytułu. Tytuł `feat:` bez zakresu nie mówi, czy podbić `news`, czy `learning`. Gałąź `feature/lesson_markdown` przechodzi przez git i psuje skrypt, który zakłada myślnik. Pull request na dwa tysiące linii miesza jądro z bramą i nie da się go przeczytać w jeden wieczór.

Osobna gałąź wycięta z `main` ominęłaby checki, które stoją na drodze `develop` → `release` → `main`. Dlatego para gałęzi jest częścią tego samego checka co nazwa.

## Dlaczego ta technologia

### Ruleset w pliku

Ruleset zapisany w `.github/rulesets` jest źródłem prawdy. Problem, który zamyka, to ochrona wyklikana w interfejsie i nieobecna u osoby, która sklonowała repozytorium. Odpada sama ochrona gałęzi z interfejsu, bez pliku. Koszt: zmiana reguły jest pull requestem i sama przechodzi checki.

Nie wymagamy drugiej recenzji. Bramką jest check. Jedna osoba utrzymuje produkt, a czekanie na drugie zatwierdzenie, którego nie ma od kogo wziąć, zatrzymałoby kolejkę.

### Conventional Commits

Tytuł ma typ, opcjonalny zakres i opis. Problem, który zamyka, to commit `update` i `fixes`. Odpada dowolny tytuł. Koszt: trzeba znać listę zakresów ze [słownika](../00-fundamenty/slownik.md). Zakres spoza listy pada, także `docs(cicd):`, bo `cicd` nie jest zakresem.

`feat` i `fix` mają zakres obowiązkowo. `docs`, `chore`, `ci`, `refactor`, `test`, `perf` i `build` mogą iść bez zakresu, gdy nie podbijają komponentu.

### Limit 800 linii

`pr-size` liczy dodane i usunięte linie diffu. Problem, który zamyka, to numer, który po cichu wciąga następny. Odpada brak limitu. Koszt: migracja EF i wygenerowany klient OpenAPI muszą być wyłączone z liczenia, inaczej sam generator wywala check. Wyłączenie jest listą ścieżek w skrypcie, nie komentarzem w pull requeście.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| ruleset | reguły GitHuba zapisane jako JSON w git |
| squash | jedno scalenie, jeden commit o treści tytułu |
| merge commit | scalenie, które zachowuje historię gałęzi źródłowej |
| kebab-case | małe litery i myślniki, bez podkreślnika |
| suchy przebieg | `tests/ci/branch-rules.sh`, te same skrypty na przykładach, bez żywego pull requestu |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `.github/workflows/branch-name.yml` | job `branch-name`: suchy przebieg, potem para głowa i baza |
| `.github/workflows/pr-title.yml` | job `pr-title`: tytuł zdarzenia pull requestu |
| `.github/workflows/pr-size.yml` | job `pr-size`: suma linii między SHA bazy a SHA głowy |
| `.github/scripts/branch-name.sh` | para gałęzi i wzorzec nazwy |
| `.github/scripts/pr-title.sh` | typ, zakres, opis |
| `.github/scripts/pr-size.sh` | suma linii poza listą wyłączeń, próg `800` |
| `tests/ci/branch-rules.sh` | przykłady, które te trzy skrypty mają oblać albo przepuścić |
| `.github/rulesets/develop.json` | squash, zakaz kasowania, zakaz force-push, `strict_required_status_checks_policy` równe `true` |
| `.github/rulesets/release.json` | merge commit, głowa musi być `develop`, polityka strict wyłączona |
| `.github/rulesets/main.json` | merge commit, głowa musi być `release`, polityka strict wyłączona |

## Plan

### 1. Nazwa i para gałęzi

Job `branch-name` powstaje w `.github/workflows/branch-name.yml`. Wyzwalacz to `pull_request` typów `opened`, `synchronize`, `reopened` i `edited`, na gałęzie `develop`, `release` i `main`. Filtra `paths` nie ma. Uprawnienie workflow to puste `permissions`, a job dostaje `contents: read`. Checkout ma `persist-credentials: false`.

Job ma dwa kroki. Pierwszy odpala suchy przebieg:

```bash
bash tests/ci/branch-rules.sh
```

Drugi woła skrypt na żywym pull requeście. Nazwy biorą się ze zdarzenia, nie z argumentów wpisanych w YAML:

```bash
bash .github/scripts/branch-name.sh "$HEAD_REF" "$BASE_REF"
```

`HEAD_REF` to `github.head_ref`, `BASE_REF` to `github.base_ref`.

W `.github/scripts/branch-name.sh` wzorzec głowy roboczej to:

```text
^(feature|fix|docs|chore|ci|refactor|test)/[a-z0-9]+(-[a-z0-9]+)*$
```

Dozwolone pary tego numeru:

| Głowa | Baza | Wynik |
| --- | --- | --- |
| prefiks roboczy i kebab-case | `develop` | kod `0` |
| `develop` | `release` | kod `0` |
| `release` | `main` | kod `0` |
| cokolwiek innego | cokolwiek innego | kod `1` i zdanie na stderr |

Gałąź `develop`, `release` i `main` jako głowa promocji nie przechodzi przez wzorzec kebab-case. Skrypt sprawdza ją wcześniej, osobnym porównaniem.

W tym samym skrypcie jest czwarta para: głowa `release-please--branches--main--components--<komponent>` i baza `main`. To nie jest gałąź robocza. Sens pary jest w [numerze wersji](09-wersje.md). Ten numer tylko zostawia ją w skrypcie, bo inaczej pull request wydania padałby na checku, który już stoi.

Sprawdzenie:

```bash
bash .github/scripts/branch-name.sh feature/lesson_markdown develop; test $? -eq 1
bash .github/scripts/branch-name.sh feature/lesson-markdown develop; test $? -eq 0
bash .github/scripts/branch-name.sh develop release; test $? -eq 0
bash .github/scripts/branch-name.sh release main; test $? -eq 0
bash .github/scripts/branch-name.sh feature/lesson-markdown release; test $? -eq 1
```

Podkreślnik pada. Myślnik na `develop` przechodzi. Promocja `develop` → `release` i `release` → `main` przechodzi. Gałąź robocza wycelowana w `release` pada.

### 2. Tytuł

Job `pr-title` jest w `.github/workflows/pr-title.yml`. Ten sam wyzwalacz co przy nazwie gałęzi. Suchego przebiegu w tym pliku nie ma: przykłady tytułu siedzą w `tests/ci/branch-rules.sh`, który odpala job `branch-name`. Krok żywego pull requestu bierze tytuł ze zdarzenia:

```bash
bash .github/scripts/pr-title.sh "$TITLE"
```

`TITLE` to `github.event.pull_request.title`.

Wzorzec w `.github/scripts/pr-title.sh`:

```text
^(feat|fix|docs|chore|ci|refactor|test|perf|build)(\(([^)]+)\))?(!)?: (.+)$
```

Zakres, gdy jest, należy do listy w tym samym skrypcie: `identity`, `news`, `learning`, `cv`, `contact`, `gateway`, `web`, `mobile`, `infra`, `shared`. `feat` i `fix` bez zakresu kończą się komunikatem `feat i fix wymagają zakresu` i kodem `1`. Opis ma co najmniej trzy znaki. Znak `!` przed dwukropkiem jest dozwolony. Taki tytuł podbija minor przed `1.0.0` i major od `1.0.0`. Liczy to numer wersji, nie ten check.

Sprawdzenie:

```bash
bash .github/scripts/pr-title.sh "feat: add lesson"; test $? -eq 1
bash .github/scripts/pr-title.sh "feat(learning): add lesson markdown"; test $? -eq 0
bash .github/scripts/pr-title.sh "feat(news): publish article revision"; test $? -eq 0
bash .github/scripts/pr-title.sh "docs(cicd): update the pipeline"; test $? -eq 1
bash .github/scripts/pr-title.sh "docs: abc"; test $? -eq 0
```

`feat` bez zakresu pada. Tytuł z zakresem komponentu przechodzi. `cicd` pada, bo nie ma go na liście. `docs` bez zakresu przechodzi.

### 3. Rozmiar

Job `pr-size` jest w `.github/workflows/pr-size.yml`. Checkout ma `fetch-depth: 0`, bo liczenie potrzebuje obu końców diffu. Krok woła:

```bash
bash .github/scripts/pr-size.sh "$BASE_SHA" "$HEAD_SHA"
```

`BASE_SHA` to `github.event.pull_request.base.sha`, `HEAD_SHA` to `github.event.pull_request.head.sha`. Skrypt liczy `git diff --numstat` z trzema kropkami, `baza...głowa`. Sumuje dodane i usunięte linie. Próg w skrypcie to `limit=800`. Powyżej skrypt wypisuje `suma <liczba>` i kończy się kodem `1`. Etykiety, która próg wyłącza, nie ma.

Wyłączenia są funkcją `excluded` w `.github/scripts/pr-size.sh`:

```text
Migrations/* i */Migrations/*
src/clients/shared/api-client/*
pnpm-lock.yaml
aspire-output/*
*.Designer.cs
```

Plik binarny (`-` w numstat) nie wchodzi do sumy. Ścieżka `src/services` nie jest na liście. Sam generator migracji nie podbija sumy. Kod usługi podbija.

Suchego przebiegu w `pr-size.yml` nie ma. Przypadki 800 i 801 linii oraz wyłączenia odpala `tests/ci/branch-rules.sh` na tymczasowym repozytorium, nie na drzewie produktu.

Sprawdzenie tego kroku jest w suchym przebiegu z kroku 4. Sam skrypt na żywym drzewie potrzebuje dwóch SHA i nie jest tu wołany na ślepo.

### 4. Suchy przebieg trzech skryptów

`tests/ci/branch-rules.sh` woła te same pliki co joby. Układa tymczasowe repozytorium gita, gdy liczy rozmiar, i sprząta je przy wyjściu. Na końcu wypisuje `branch-rules: ok`.

Przypadki, które skrypt utrwala:

| Wejście | Oczekiwany kod |
| --- | --- |
| `feature/lesson_markdown` na `develop` | `1` |
| `feature/lesson-markdown` na `develop` | `0` |
| `develop` na `release`, `release` na `main` | `0` |
| `feat: add lesson` | `1` |
| `feat(learning): add lesson markdown` | `0` |
| `docs(cicd): update the pipeline` | `1` |
| 801 linii w `src/file.cs` | `1` |
| 801 linii w `**/Migrations/**`, w `pnpm-lock.yaml`, w `*.Designer.cs`, w `src/clients/shared/api-client/` albo w `aspire-output/` | `0` |
| `release-please--branches--main--components--news` na `main` | `0` |

Ostatni wiersz jest parą z [numeru wersji](09-wersje.md). Suchy przebieg trzyma ją tutaj, bo skrypt nazwy jest jeden.

Job na prawdziwym pull requeście woła te same skrypty na żywych nazwach. Suchy przebieg nie zastępuje joba. Stoi przed nim, żeby zepsuty przykład oblał check, zanim skrypt spojrzy na tytuł człowieka.

Sprawdzenie:

```bash
bash tests/ci/branch-rules.sh
```

Ostatnia linia to `branch-rules: ok`. Kod wyjścia to `0`.

### 5. Rulesety

Trzy pliki leżą w `.github/rulesets/`. Każdy ma `enforcement` równe `active`, zakaz kasowania (`deletion`) i zakaz pushu, który nie jest przewinięciem do przodu (`non_fast_forward`). Recenzja drugiej osoby ma `required_approving_review_count` równe `0`.

Metoda scalenia:

| Plik | `allowed_merge_methods` | Gałąź |
| --- | --- | --- |
| `develop.json` | `squash` | `refs/heads/develop` |
| `release.json` | `merge` | `refs/heads/release` |
| `main.json` | `merge` | `refs/heads/main` |

Fragment, który ten numer dokłada do `required_status_checks`, to trzy konteksty. Nazwa w JSON jest tą samą, co `name` joba:

```json
{ "context": "branch-name" },
{ "context": "pr-title" },
{ "context": "pr-size" }
```

W drzewie, które czytasz, tablica jest dłuższa. Dalsze konteksty dopisały numery [builda](03-build-testy-format.md), [CodeQL](04-codeql.md), [zależności](05-zaleznosci.md), [SonarQube](06-sonarqube.md), [obrazów](08-obrazy.md) i [wersji](09-wersje.md). Ten numer ich nie uzasadnia. Przy czytaniu pliku widać je obok trzech nazw stąd, w tej samej tablicy.

`strict_required_status_checks_policy` w `develop.json` jest `true`: scalenie na `develop` czeka, aż głowa zawiera bazę. W `release.json` i `main.json` ta flaga jest `false`. `do_not_enforce_on_create` we wszystkich trzech jest `false`.

Pull request do `develop` ma głowę o prefiksie roboczym. Tego nie egzekwuje sam ruleset. Egzekwuje to job `branch-name`.

Sprawdzenie:

```bash
python3 -c 'import json,pathlib
roots=list(pathlib.Path(".github/rulesets").glob("*.json"))
assert len(roots)==3
for path in roots:
    data=json.load(path.open())
    checks=[item["context"] for item in data["rules"][-1]["parameters"]["required_status_checks"]]
    assert {"branch-name","pr-title","pr-size"} <= set(checks)
    methods=data["rules"][2]["parameters"]["allowed_merge_methods"]
    if path.name=="develop.json":
        assert methods==["squash"]
        assert data["rules"][-1]["parameters"]["strict_required_status_checks_policy"] is True
    else:
        assert methods==["merge"]
        assert data["rules"][-1]["parameters"]["strict_required_status_checks_policy"] is False
'
```

Trzy pliki są poprawnym JSON. Każdy wymaga trzech checków z tego numeru. `develop` scala się squashem i wymaga aktualnej bazy. `release` i `main` scalają się merge commitem.

`actionlint` na trzech workflow kończy się kodem zero, gdy narzędzie jest na maszynie:

```bash
actionlint .github/workflows/branch-name.yml .github/workflows/pr-title.yml .github/workflows/pr-size.yml
```

### Co jest do czego

| Job | Kto na niego czeka |
| --- | --- |
| `branch-name` | ruleset, zanim w ogóle spojrzy na kompilację; w tym samym jobie leci suchy przebieg trzech skryptów |
| `pr-title` | squash, który zapisze tytuł jako commit na `develop` |
| `pr-size` | osoba, która miałaby czytać dwa numery naraz |
| `tests/ci/branch-rules.sh` | job `branch-name`, przy każdym pull requeście, przed żywą parą gałęzi |

## Testy

Suchy przebieg jest testem tego numeru. Jeden przypadek, jeden kod. Skrypt zbiera je w jednym pliku, żeby job miał jeden krok.

| Arrange | Act | Assert |
| --- | --- | --- |
| nazwa `feature/lesson_markdown`, baza `develop` | `branch-name.sh` | kod `1` |
| nazwa `feature/lesson-markdown`, baza `develop` | `branch-name.sh` | kod `0` |
| tytuł `feat: add lesson` | `pr-title.sh` | kod `1`, bo `feat` bez zakresu |
| tytuł `feat(learning): add lesson markdown` | `pr-title.sh` | kod `0` |
| diff 801 linii w `src/file.cs` | `pr-size.sh` na tymczasowym repozytorium | kod `1` |
| diff 801 linii w `Migrations/` | `pr-size.sh` na tymczasowym repozytorium | kod `0` |

Job na prawdziwym pull requeście woła te same skrypty. Suchy przebieg nie zastępuje joba.

## Checkpoint

```bash
bash .github/scripts/branch-name.sh feature/lesson_markdown develop; test $? -eq 1
bash .github/scripts/branch-name.sh feature/lesson-markdown develop; test $? -eq 0
bash .github/scripts/pr-title.sh "feat(learning): add lesson markdown"; test $? -eq 0
bash tests/ci/branch-rules.sh
python3 -c "import json,pathlib; [json.load(open(p)) for p in pathlib.Path('.github/rulesets').glob('*.json')]"
```

Ostatnia linia suchego przebiegu to `branch-rules: ok`. Ostatnie polecenie kończy się kodem zero, czyli trzy pliki JSON są poprawne. `actionlint` na trzech workflow kończy się kodem zero.

## Umiesz wyjaśnić

Dlaczego nie ma gałęzi `hotfix` z `main`?

Bo ominęłaby checki stojące na `develop` i `release`. Poprawka idzie tą samą drogą co funkcja. Rollback wydania jest poprzednim tagiem, nie osobną gałęzią, i jest opisany przy deployu.

Czemu `feat` bez zakresu pada, a `docs` bez zakresu przechodzi?

`feat` i `fix` podbijają numer komponentu. Bez zakresu nie wiadomo którego. `docs` nie wydaje obrazu.

Co się dzieje, gdy job wymagany się nie uruchomi?

Ruleset traktuje brak statusu jak brak zgody. Dlatego te workflow nie mają filtra `paths` na `on`.

Dlaczego migracje nie liczą się do limitu?

Bo generator EF dopisuje tysiące linii, których nie czyta się jak kodu numeru. Ścieżka wyłączenia jest zamknięta i siedzi w `excluded` w `pr-size.sh`. Nie wyłącza się `src/services`.

Jaki commit ląduje na `develop`?

Squash o treści tytułu pull requestu. Dlatego tytuł jest kontraktem, nie ozdobą. `release` i `main` biorą merge commit, bo tam ma zostać historia promocji.

Dlaczego suchy przebieg jest w jobie `branch-name`, a nie w trzech?

Bo przykłady tytułu i rozmiaru mają paść tym samym skryptem co nazwa gałęzi, zanim człowiek czeka na trzy osobne logi. Joby `pr-title` i `pr-size` i tak oceniają żywy pull request własnym krokiem.

Skąd w `branch-name.sh` gałąź `release-please--…`?

Z pull requestu, który [release-please](09-wersje.md) otwiera na `main`. Wzorzec jest w skrypcie nazwy, żeby wydanie miało drogę. Opis tagu jest w tamtym numerze.

Czym `strict_required_status_checks_policy` na `develop` różni się od `release` i `main`?

Na `develop` flaga jest `true`: głowa ma zawierać aktualną bazę, zanim squash wejdzie. Na `release` i `main` flaga jest `false`. Metoda scalenia i tak jest inna: squash wobec merge commita.

## Zasady

- Gałąź robocza wychodzi tylko z `develop`.
- Recenzja drugiej osoby nie jest wymagana.
- Nazwa checka w workflow i w rulesecie jest tym samym napisem.
- `pull_request_target` nie występuje.
- Skrypt nie woła sieci. Decyzja jest lokalna na diffie i na nazwach.
- Próg `800` nie ma etykiety, która go wyłącza.

## Poza zakresem

- Kompilacja i testy są w [03-build-testy-format.md](03-build-testy-format.md). Tam dopisywane są konteksty `build`, `unit-tests`, `format`, `integration-tests` i `performance`.
- To, który zakres podbija który plik wersji, jest w [09-wersje.md](09-wersje.md). Tam jest też para gałęzi `release-please`.
- Deploy z tagu jest w [07-deploy.md](../02-szkielet-i-deploy/07-deploy.md).
- Pozostałe konteksty w trzech rulesetach dochodzą w numerach 04, 05, 06 i 08. Każdy numer pokazuje tylko swoje nazwy.

## Źródła

- [About rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets). Wiążący jest ruleset jako plik i nazwa checka w JSON. Dotyczy trzech długich gałęzi. Ze źródła nie bierzemy wymaganej drugiej recenzji.
- [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/). Wiążące są typ, zakres i `!`. Dotyczy tytułu, który squash zapisuje na `develop`. Ze źródła nie bierzemy zakresu spoza słownika.
- [Required status checks](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches#require-status-checks-before-merging). Wiążące jest to, że check bez statusu blokuje scalenie. Dotyczy zakazu `paths` na tych workflow. Ze źródła nie bierzemy pominiętego joba jako zielonego.

## Gotowe gdy

- `feature/lesson-markdown` przechodzi, `feature/lesson_markdown` pada kodem `1`.
- `feat(news): publish article revision` przechodzi, `feat:` bez zakresu pada.
- `tests/ci/branch-rules.sh` wypisuje `branch-rules: ok` i zwraca kod zero.
- Trzy rulesety wymagają `branch-name`, `pr-title` i `pr-size`. `develop` ma squash i strict włączone. `release` i `main` mają merge commit.
- Diff 801 linii w `src/services` pada, a same pliki w `**/Migrations/**` nie podbijają sumy. To sprawdza suchy przebieg.
- `actionlint` na trzech workflow zwraca kod zero.
