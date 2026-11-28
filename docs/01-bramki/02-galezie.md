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

### Limit 1500 linii

`pr-size` liczy dodane i usunięte linie.diffu. Problem, który zamyka, to numer, który po cichu wciąga następny. Odpada brak limitu. Koszt: migracja EF i wygenerowany klient OpenAPI muszą być wyłączone z liczenia, inaczej sam generator wywala check. Wyłączenie jest listą ścieżek w skrypcie, nie komentarzem w pull requeście.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| ruleset | reguły GitHuba zapisane jako JSON w git |
| squash | jedno scalenie, jeden commit o treści tytułu |
| merge commit | scalenie, które zachowuje historię gałęzi źródłowej |
| kebab-case | małe litery i myślniki, bez podkreślnika |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `.github/workflows/branch-name.yml` | job `branch-name` |
| `.github/workflows/pr-title.yml` | job `pr-title` |
| `.github/workflows/pr-size.yml` | job `pr-size` |
| `.github/scripts/branch-name.sh` | para gałęzi i wzorzec nazwy |
| `.github/scripts/pr-title.sh` | typ, zakres, opis |
| `.github/scripts/pr-size.sh` | suma linii poza listą wyłączeń |
| `.github/rulesets/develop.json` | squash, zakaz kasowania, zakaz force-push |
| `.github/rulesets/release.json` | merge commit, głowa musi być `develop` |
| `.github/rulesets/main.json` | merge commit, głowa musi być `release` |

## Plan

### 1. Nazwa i para gałęzi

Job `branch-name` na `pull_request` czyta głowę i bazę.

Dozwolone pary: głowa o prefiksie roboczym i baza `develop`; głowa `develop` i baza `release`; głowa `release` i baza `main`. Inna para kończy się kodem `1`.

Nazwa głowy roboczej pasuje do `^(feature|fix|docs|chore|ci|refactor|test)/[a-z0-9]+(-[a-z0-9]+)*$`. Gałąź `develop`, `release` i `main` jako głowa promocji nie jest sprawdzana tym wzorcem, bo nie jest gałęzią roboczą.

### 2. Tytuł

Job `pr-title` bierze tytuł zdarzenia pull requestu. Wzorzec typu: `feat`, `fix`, `docs`, `chore`, `ci`, `refactor`, `test`, `perf`, `build`. Dla `feat` i `fix` zakres jest w nawiasie i należy do listy ze słownika. Opis ma co najmniej trzy znaki. Znak `!` przed dwukropkiem oznacza zmianę łamiącą i jest dozwolony. Taki tytuł podbija minor przed `1.0.0` i major od `1.0.0`, co liczy numer wersji, nie ten check.

### 3. Rozmiar

`pr-size` uruchamia `git diff --numstat` między bazą a głową. Sumuje dodane i usunięte linie. Nie liczy:

- `**/Migrations/**`
- `src/clients/shared/api-client/**`
- `pnpm-lock.yaml`
- `*.Designer.cs`
- `aspire-output/**`

Próg to 1500. Powyżej kod wyjścia `1` i w logu suma. Nie ma etykiety, która próg wyłącza.

### 4. Rulesety

Każdy z trzech plików wymaga statusów `branch-name`, `pr-title` i `pr-size`. Kolejne numery dopisują swoje checki do tych samych plików w tym samym scaleniu, w którym workflow zaczyna je raportować.

`develop` pozwala na squash i blokuje merge commit oraz rebase jako metodę scalenia. `release` i `main` pozwalają tylko na merge commit. Wszystkie trzy blokują kasowanie gałęzi i push, który nie jest przewinięciem do przodu.

Pull request do `develop` ma głowę o prefiksie roboczym. Tego nie egzekwuje sam ruleset GitHuba, tylko job `branch-name`.

### Co jest do czego

| Job | Kto na niego czeka |
| --- | --- |
| `branch-name` | ruleset, zanim w ogóle spojrzy na kompilację |
| `pr-title` | squash, który zapisze tytuł jako commit na `develop` |
| `pr-size` | osoba, która miałaby czytać dwa numery naraz |

## Testy

Skrypty mają test w `tests/ci/branch-rules.sh`, odpalany lokalnie i w jobie `branch-name` jako suchy przebieg na przykładach zaszytych w teście, nie na żywym pull requeście.

| Arrange | Act | Assert |
| --- | --- | --- |
| nazwa `feature/lesson_markdown` | `branch-name.sh` | kod `1` |
| nazwa `feature/lesson-markdown`, baza `develop` | `branch-name.sh` | kod `0` |
| tytuł `feat: add lesson` | `pr-title.sh` | kod `1`, bo `feat` bez zakresu |
| tytuł `feat(learning): add lesson markdown` | `pr-title.sh` | kod `0` |
| diff 1501 linii w `src/` | `pr-size.sh` | kod `1` |

Job na prawdziwym pull requeście woła te same skrypty. Suchy przebieg nie zastępuje joba.

## Checkpoint

```bash
bash .github/scripts/branch-name.sh feature/lesson_markdown develop; test $? -eq 1
bash .github/scripts/branch-name.sh feature/lesson-markdown develop; test $? -eq 0
bash .github/scripts/pr-title.sh "feat(learning): add lesson markdown"; test $? -eq 0
python3 -c "import json,pathlib; [json.load(open(p)) for p in pathlib.Path('.github/rulesets').glob('*.json')]"
```

Ostatnie polecenie kończy się kodem zero, czyli trzy pliki JSON są poprawne. `actionlint` na trzech workflow kończy się kodem zero.

## Umiesz wyjaśnić

Dlaczego nie ma gałęzi `hotfix` z `main`?

Bo ominęłaby checki stojące na `develop` i `release`. Poprawka idzie tą samą drogą co funkcja. Rollback wydania jest poprzednim tagiem, nie osobną gałęzią, i jest opisany przy deployu.

Czemu `feat` bez zakresu pada, a `docs` bez zakresu przechodzi?

`feat` i `fix` podbijają numer komponentu. Bez zakresu nie wiadomo którego. `docs` nie wydaje obrazu.

Co się dzieje, gdy job wymagany się nie uruchomi?

Ruleset traktuje brak statusu jak brak zgody. Dlatego te workflow nie mają filtra `paths` na `on`.

Dlaczego migracje nie liczą się do limitu?

Bo generator EF dopisuje tysiące linii, których nie czyta się jak kodu numeru. Ścieżka wyłączenia jest zamknięta. Nie wyłącza się `src/services`.

Jaki commit ląduje na `develop`?

Squash o treści tytułu pull requestu. Dlatego tytuł jest kontraktem, nie ozdobą.

## Zasady

- Gałąź robocza wychodzi tylko z `develop`.
- Recenzja drugiej osoby nie jest wymagana.
- Nazwa checka w workflow i w rulesecie jest tym samym napisem.
- `pull_request_target` nie występuje.
- Skrypt nie woła sieci. Decyzja jest lokalna na diffie i na nazwach.

## Poza zakresem

- Kompilacja i testy są w [03-build-testy-format.md](03-build-testy-format.md).
- To, który zakres podbija który plik wersji, jest w [09-wersje.md](09-wersje.md).
- Deploy z tagu jest w [07-deploy.md](../02-szkielet-i-deploy/07-deploy.md).

## Źródła

- [About rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets). Wiążący jest ruleset jako plik i nazwa checka w JSON. Dotyczy trzech długich gałęzi. Ze źródła nie bierzemy wymaganej drugiej recenzji.
- [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/). Wiążące są typ, zakres i `!`. Dotyczy tytułu, który squash zapisuje na `develop`. Ze źródła nie bierzemy zakresu spoza słownika.
- [Required status checks](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches#require-status-checks-before-merging). Wiążące jest to, że check bez statusu blokuje scalenie. Dotyczy zakazu `paths` na tych workflow. Ze źródła nie bierzemy pominiętego joba jako zielonego.

## Gotowe gdy

- `feature/lesson-markdown` przechodzi, `feature/lesson_markdown` pada kodem `1`.
- `feat(news): publish article revision` przechodzi, `feat:` bez zakresu pada.
- Trzy rulesety wymagają `branch-name`, `pr-title` i `pr-size`.
- Diff 1501 linii w `src/services` pada, a same pliki w `**/Migrations/**` nie podbijają sumy.
- `actionlint` na trzech workflow zwraca kod zero.
