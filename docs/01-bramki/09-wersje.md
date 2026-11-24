# 09. Wersje komponentów

Poprzedni numer to [obrazy](08-obrazy.md). Następny folder zaczyna się od [Aspire](../02-szkielet-i-deploy/01-aspire.md).

Ten numer dodaje osobny numer wersji każdego komponentu i check `release-tags`. Tag powstaje na pushu do `main`, tylko dla komponentu, którego plik wersji się zmienił. Obrazu ten numer nie buduje. Zostawia pliki, z których późniejszy deploy wyczyta `learning-v0.3.0`.

## Po co

Jeden tag `v1.2.3` na całe repozytorium przebudowuje Identity, gdy zmieniła się notatka. Nie da się cofnąć samego News do poprzedniego wydania. Osobny tag na komponent to umożliwia. Check na pull requeście pilnuje, że plik wersji w ogóle jest spójny, zanim `main` dostanie push, z którego tag się wycina.

Bez tego numeru [deploy](../02-szkielet-i-deploy/07-deploy.md) nie ma czego wgrać.

## Dlaczego ta technologia

### release-please

release-please czyta commity Conventional Commits na `main` i otwiera pull request z podbiciem `versions/<komponent>.json`. Problem, który zamyka, to ręczne edytowanie trzech plików wersji i zapominanie o changelogu. Odpada jeden `version.json` w korzeniu. Koszt: manifest w `.github/release-please-config.json` musi znać listę komponentów. Nowy komponent dopisuje się tu i w słowniku w tym samym scaleniu.

Wtyczka `linked-versions` nie wchodzi. Powiązanie „wszystko na ten sam numer” znosi sens osobnych tagów. Check pada komunikatem `linked-versions nie wchodzi`, gdy klucz pojawi się w dowolnym miejscu konfiguracji.

### SemVer i seria `0.y.z`

Przed `1.0.0` zmiana łamiąca, oznaczona `!` w tytule, podbija minor. Od `1.0.0` podbija major. `feat` podbija minor. `fix` podbija patch. Problem, który zamyka, to `1.0.0` wystawione na pierwszej usłudze, która jeszcze nie ma klientów poza tym repozytorium. Odpada brak reguły i wersje `1.0.0` oraz `1.0.1` tego samego dnia bez różnicy znaczenia. Koszt: trzeba pamiętać, że `0.2.0` może złamać kontrakt. Klienci web i telefon są w tym samym repozytorium i czytają `GET /versions`, więc widzą numer, który właśnie wszedł.

Wersja `0.0.0` nie dostaje tagu. Plik startowy komponentu ma `0.0.0`. W manifeście `initial-version` to `0.1.0`: pierwsze realne podbicie, zwykle `0.1.0`, jest pierwszym tagiem.

### Jedna wersja, kilka obrazów

Komponent `learning` wydaje obrazy `jjdevhub-learning-api`, `jjdevhub-learning-relay` i `jjdevhub-learning-projector`. Wszystkie trzy noszą ten sam tag `learning-vX.Y.Z`. Problem, który zamyka, to relay w wersji `0.4.0` czytający outbox, którego api `0.3.0` jeszcze nie pisze. Odpada osobny numer wersji na każdy proces. Koszt: poprawka samego relay podbija też api. To jest akceptowane, bo procesy jednej usługi wdraża się razem.

`web` i `mobile` mają po jednym artefakcie. `gateway` ma jeden obraz.

### Jądro podbija usługi

Zmiana w `JJDevHub.Shared.Kernel` nie ma własnego tagu. Ma zakres `shared`. Skrypt wersji podbija każdy komponent, który ma `ProjectReference` do jądra. Problem, który zamyka, to ciche zostawienie News na starym `Result`. Odpada ręczna lista „pamiętaj podbić wszystkie”. Koszt: mała poprawka w jądrze jest pięcioma tagami, gdy pięć usług już istnieje. Zanim którykolwiek `.csproj` referencjonuje jądro, podbicie dotyczy pustej listy i check wypisuje `nie ma kogo podbijać`.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| komponent | jednostka tagu ze słownika |
| manifest release-please | który katalog należy do którego komponentu |
| `!` w tytule | zmiana łamiąca |
| kandydat | komponent, którego ścieżka jest w diffie; jeszcze nie tag |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `.github/workflows/versions.yml` | job `release-tags` na pull requeście i job `release-please` na pushu do `main` |
| `.github/scripts/release-tags.sh` | `--check` i `--candidates`; na pull requeście nie tworzy tagu |
| `.github/scripts/bump-shared.sh` | komponenty z `ProjectReference` do jądra |
| `.github/release-please-config.json` | osiem pakietów, `initial-version` `0.1.0`, bez `linked-versions` |
| `.release-please-manifest.json` | te same ścieżki i ta sama wersja co pliki `versions/` |
| `versions/identity.json` i pozostałe siedem plików | `{"version":"0.0.0"}` |
| `tests/ci/versions.sh` | suchy przebieg plików, tytułu, kandydatów i pary jobów |
| `.github/scripts/branch-name.sh` | czwarta para: gałąź wydania na `main`, opisana niżej |

Osiem plików: `identity`, `news`, `learning`, `cv`, `contact`, `gateway`, `web`, `mobile`.

## Plan

### 1. Pliki wersji

Każdy plik w `versions/` to JSON z polem `version`. Start to `0.0.0`:

```json
{"version":"0.0.0"}
```

`.github/scripts/release-tags.sh` w trybie `--check` pada, gdy plik komponentu ze słownika nie istnieje, gdy JSON jest zły, albo gdy wersja nie pasuje do `^[0-9]+\.[0-9]+\.[0-9]+$`. Napis `nope` pada komunikatem `wersja nie pasuje`. Ta sama wersja musi stać w `.release-please-manifest.json` przy ścieżce pakietu. Rozjazd pada komunikatem `manifest nie zgadza się z plikiem`.

Manifest w drzewie ma osiem ścieżek, każdą na `0.0.0`:

```text
src/services/Identity
src/services/News
src/services/Learning
src/services/Cv
src/services/Contact
src/gateway
src/clients/web
src/clients/mobile
```

Sprawdzenie:

```bash
for c in identity news learning cv contact gateway web mobile; do
  python3 -c 'import json,sys; data=json.load(open(sys.argv[1])); assert data["version"]=="0.0.0"' "versions/$c.json"
done
```

Osiem poleceń kończy się kodem zero. Samo `--check` na tym drzewie nic nie wypisuje i nie tworzy tagu. To sprawdza suchy przebieg.

### 2. Manifest

`.github/release-please-config.json` mapuje ścieżki. Fragment, który check wymaga w całości:

```json
{
  "separate-pull-requests": true,
  "bump-minor-pre-major": true,
  "include-component-in-tag": true,
  "include-v-in-tag": true,
  "tag-separator": "-",
  "component-no-space": true,
  "initial-version": "0.1.0",
  "pull-request-title-pattern": "chore(${component}): release ${version}"
}
```

`bump-minor-pre-major` jest regułą serii `0.y.z`: złamanie przed `1.0.0` podbija minor. Tag składa się z komponentu, myślnika i `v`, stąd `news-v0.1.0`. Tytuł pull requestu wydania to `chore(news): release 0.1.0`. Check pada, gdy wzorzec albo `initial-version` są inne.

Każdy pakiet ma `release-type` równe `simple`, pole `component` i `extra-files` wskazujące `versions/<komponent>.json` po ścieżce `$.version`. `src/shared/JJDevHub.Shared.Kernel/` w manifeście nie jest. Obsługuje go `bump-shared.sh`.

`linked-versions` w pliku nie występuje. Dopisanie go, także jako wtyczki, kończy `--check` kodem `1`.

### 3. Job na pull requeście

W `.github/workflows/versions.yml` job `release-tags` ma warunek `github.event_name == 'pull_request'`. Uprawnienie to `contents: read`. Nie ma `contents: write` i nie woła akcji release-please. Kroki: checkout z `fetch-depth: 0`, suchy przebieg `bash tests/ci/versions.sh`, potem:

```bash
bash .github/scripts/release-tags.sh --check --title "$TITLE" --base-ref "origin/${BASE_REF}" --head-ref HEAD
```

`TITLE` to tytuł pull requestu. Tryb `--check` nie tworzy tagu. Sprawdza pliki i, gdy tytuł to `feat` albo `fix` z zakresem komponentu, sprawdza, że ścieżki diffu wpadają w katalog tego komponentu. Inaczej komunikat `release-please nie podbiłby: <zakres>`.

Samo podbicie dzieje się na `main`. Na pull requeście do `develop` check jest zielony, gdy pliki są poprawne. Nie wymaga podbicia wersji w tym samym pull requeście co kod: podbiciem jest osobny pull request release-please po wejściu na `main`.

Zakres spoza listy komponentów, `infra` i `shared` pada. `feat(cicd)` pada, bo `cicd` nie jest zakresem. `feat` bez zakresu pada. `feat(infra)` przy ścieżce workflow przechodzi: `infra` nie wydaje obrazu i nie musi trafić w katalog komponentu. `feat(shared)` bez ścieżki jądra pada komunikatem `shared bez jądra`.

Gdy w tytule jest zakres komponentu, którego pliku wersji nie ma, check pada komunikatem `brak versions/<komponent>.json`.

### 4. Jądro

Gdy diff rusza `src/shared/JJDevHub.Shared.Kernel`, `--check` woła `.github/scripts/bump-shared.sh`. Skrypt wypisuje komponent, którego `.csproj` ma `ProjectReference` do `JJDevHub.Shared.Kernel`. Gdy nikogo nie ma, wypisuje:

```text
nie ma kogo podbijać
```

i kończy się kodem `0`. Na dzisiejszym drzewie żaden projekt jądra nie referencjonuje, więc ta linia jest wynikiem ścieżki jądra. Ścieżka samego News nic nie wypisuje: jądro nie zostało ruszone i skrypt kończy się od razu.

Suchy przebieg składa tymczasowe drzewo, w którym News ma `ProjectReference` do jądra, a Identity nie. Ta sama ścieżka jądra wypisuje wtedy samo `news`.

### 5. Tag na `main`

Job `release-please` ma warunek `github.event_name == 'push' && github.ref == 'refs/heads/main'`. Uprawnienia: `contents: write` i `pull-requests: write`. Jedyny krok poza checkoutem:

```yaml
uses: googleapis/release-please-action@45996ed1f6d02564a971a2fa1b5860e934307cf7 # v5.0.0
with:
  config-file: .github/release-please-config.json
  manifest-file: .release-please-manifest.json
  token: ${{ secrets.GITHUB_TOKEN }}
```

`GITHUB_TOKEN` jest tokenem przebiegu, nie sekretem aplikacji. Akcja czyta commity na `main` i dla komponentu, który ma co podbić, otwiera pull request. Tag `<komponent>-vX.Y.Z` wycina ta akcja, nie `git tag` w workflow. W pliku workflow nie ma `git tag` ani `push-tags`. Wersja, która zostaje `0.0.0`, nie dostaje tagu, bo `initial-version` to `0.1.0`. Tag nie uruchamia deployu sam. Deploy słucha tagu w swoim workflow, od numeru deployu.

W rulesecie jest tylko `release-tags`. Job `release-please` nie jest checkiem pull requestu: na pull requeście się nie uruchamia.

Zmiana tylko w News nie tworzy tagu `identity-v…`. Kandydatów pokazuje `--candidates`:

```bash
bash .github/scripts/release-tags.sh --candidates --paths src/services/News/Foo.cs
```

Wypisuje `news` i kod zero. Dwie ścieżki, News i Identity, wypisują `identity` i `news` w kolejności słownika. Ścieżka jądra i `README.md` wypisują pustkę: jądro nie jest komponentem manifestu.

### 6. Gałąź wydania

Pull request, który release-please otwiera, ma głowę:

```text
release-please--branches--main--components--<komponent>
```

i bazę `main`. [Numer gałęzi](02-galezie.md) zostawił ten wzorzec w `branch-name.sh`, żeby check nazwy nie zjadł wydania. Gałąź robocza to nie jest. Na `develop` ta głowa pada.

```bash
bash .github/scripts/branch-name.sh release-please--branches--main--components--news main; test $? -eq 0
bash .github/scripts/branch-name.sh release-please--branches--main--components--news develop; test $? -eq 1
bash .github/scripts/pr-title.sh "chore(news): release 0.1.0"; test $? -eq 0
```

Tytuł `chore(news): release 0.1.0` przechodzi `pr-title.sh`. Tytuł `chore(main): release news 0.1.0` pada: `main` nie jest zakresem.

### 7. Suchy przebieg i ruleset

`tests/ci/versions.sh` woła te same skrypty. Na końcu wypisuje `versions: ok`. Między innymi:

| Wejście | Wynik |
| --- | --- |
| `--check` bez tytułu na tym drzewie | kod `0`, stdout pusty, lista tagów gita bez zmiany |
| `--check --title 'feat(cicd): x'` | kod `1` |
| `--candidates --paths src/services/News/Foo.cs` | `news` |
| `--check --title 'feat(news): add lesson' --paths docs/README.md` | kod `1` |
| `--check --title 'feat(shared): bump kernel'` i ścieżka jądra | `nie ma kogo podbijać`, kod `0` |
| wersja `nope` w kopii `versions/news.json` | kod `1` |
| `linked-versions` dopisane do kopii konfiguracji | kod `1` |
| diff tylko w `src/services/News/` i tytuł `feat(identity)` | kod `1` |

Skrypt czyta też workflow: akcja release-please nie stoi w jobie `release-tags`, a job wydania jest ograniczony do pusha na `main` i ma pin SHA z kroku 5.

```bash
bash tests/ci/versions.sh
```

Ostatnia linia to `versions: ok`.

`release-tags` jest w trzech rulesetach. Suchy przebieg to sprawdza. Osobno:

```bash
python3 -c 'import json,pathlib
for path in pathlib.Path(".github/rulesets").glob("*.json"):
    data=json.load(path.open())
    checks={item["context"] for item in data["rules"][-1]["parameters"]["required_status_checks"]}
    assert "release-tags" in checks, path
    assert "release-please" not in checks
config=json.load(open(".github/release-please-config.json"))
assert "linked-versions" not in json.dumps(config)
assert config["initial-version"]=="0.1.0"
'
```

### Co jest do czego

| Plik | Kto go czyta |
| --- | --- |
| `versions/*.json` | `release-tags.sh`, release-please, stopka klienta przez numer wbity przy buildzie, `GET /versions` |
| `.release-please-manifest.json` | akcja na `main` i check, który wymaga zgodności z plikami |
| tag | workflow deployu, nie ten numer |
| `bump-shared.sh` | `release-tags.sh`, gdy diff ruszył jądro |
| job `release-please` | push na `main`; ruleset go nie wymaga |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| osiem plików `0.0.0` | `release-tags.sh --check` | kod `0`, brak nowego tagu, pusty stdout |
| tytuł `feat(cicd): x` | `--check --title` | kod `1` |
| diff tylko w `src/services/News/` | `--candidates --paths` | kandydatem jest wyłącznie `news` |
| tytuł `feat(news)` i ścieżka `docs/README.md` | `--check` | kod `1`, `release-please nie podbiłby` |
| ścieżka jądra i brak `ProjectReference` | `bump-shared.sh` | `nie ma kogo podbijać` |

Wszystkie wiersze wykonuje `tests/ci/versions.sh`. Ostatni wiersz na sztucznym drzewie z referencją News wypisuje samo `news`.

## Checkpoint

```bash
test -f .github/workflows/versions.yml
test -f .github/release-please-config.json
test -f .release-please-manifest.json
for c in identity news learning cv contact gateway web mobile; do test -f versions/$c.json; done
python3 -c "import json,pathlib; [json.load(open(p)) for p in pathlib.Path('versions').glob('*.json')]"
bash tests/ci/versions.sh
```

Każdy plik ma `version` równe `0.0.0`. Ostatnia linia suchego przebiegu to `versions: ok`. `actionlint .github/workflows/versions.yml` zwraca kod zero.

## Umiesz wyjaśnić

Dlaczego `shared` nie ma tagu?

Bo jądro nie jest procesem. Wydaniem jest usługa, która jądro wciąga. `bump-shared.sh` wypisuje te usługi. Gdy żadna jeszcze nie ma `ProjectReference`, linia brzmi `nie ma kogo podbijać` i check jest zielony.

Czemu przed `1.0.0` złamanie podbija minor?

Bo SemVer tak traktuje serię zerową: major zero znaczy, że API jeszcze nie jest stabilne. W manifeście stoi `bump-minor-pre-major`. `!` i tak jest widoczne w tytule. `initial-version` `0.1.0` sprawia, że plik `0.0.0` nie jest jeszcze tagiem.

Dlaczego relay i api mają ten sam numer?

Bo czytają ten sam outbox. Rozjazd wersji między nimi jest gorszy niż wspólne wydanie przy małej poprawce. Manifest ma jeden pakiet `learning`, nie trzy.

Kto tworzy tag?

Job `release-please` na pushu do `main`, nie człowiek z laptopa i nie job `release-tags`. Tag z laptopa nie ma przebiegu, do którego da się przywiązać atestację. W workflow nie ma `git tag`.

Czy pull request funkcji musi ruszyć `versions/*.json`?

Nie. Rusza go release-please po scaleniu na `main`. Pull request funkcji ma zielony `release-tags`, gdy pliki istnieją, wersja jest SemVer i zakres tytułu zgadza się ze ścieżkami. `feat(news)` przy samym markdownie pada.

Dlaczego gałąź `release-please--branches--main--components--news` wchodzi na `main`?

Bo to głowa pull requestu wydania, nie gałąź robocza z `develop`. Wzorzec jest w `branch-name.sh` od [numeru gałęzi](02-galezie.md). Na `develop` ta sama głowa pada.

Czym job `release-please` różni się od checka `release-tags`?

Check jest na pull requeście, ma `contents: read` i niczego nie taguje. Job wydania jest na pushu do `main`, ma zapis i woła akcję. W rulesecie jest tylko `release-tags`, bo drugi job na pull requeście się nie uruchamia.

## Zasady

- Osiem komponentów, osiem plików, żadnego tagu `vX.Y.Z` bez prefiksu komponentu.
- `linked-versions` nie wchodzi. Check tego pilnuje.
- `0.0.0` nie jest tagiem. `initial-version` to `0.1.0`.
- Zmiana News nie taguje Identity. `--candidates` pokazuje jeden komponent.
- Tag nie jest tworzony z gałęzi roboczej ani z joba pull requestu.
- Wersja w `versions/*.json` i w `.release-please-manifest.json` jest tą samą liczbą.

## Poza zakresem

- Budowanie obrazu i wysłanie na maszynę są w [07-deploy.md](../02-szkielet-i-deploy/07-deploy.md).
- Stopka `web-vX.Y.Z` jest w [02-szkielet.md](../11-web/02-szkielet.md). Ekran telefonu jest w [03-ekrany.md](../12-mobile/03-ekrany.md).
- `GET /versions` jest w [02-gateway.md](../02-szkielet-i-deploy/02-gateway.md).
- Trzy pary gałęzi roboczych są w [02-galezie.md](02-galezie.md). Ten numer dokłada czwartą, wydaniową.

## Źródła

- [SemVer](https://semver.org/lang/pl/). Wiążące są major, minor, patch i seria `0.y.z`. Dotyczy `bump-minor-pre-major` i zakazu tagu `0.0.0`. Ze źródła nie bierzemy jednego tagu na całe repozytorium.
- [release-please](https://github.com/googleapis/release-please). Wiążący jest manifest komponentów, `initial-version` i pull request z podbiciem. Dotyczy tagu na `main`. Ze źródła nie bierzemy `linked-versions`.
- [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/). Wiążące są `feat`, `fix` i `!` jako wejście do podbicia. Dotyczy tego, skąd release-please bierze numer. Ze źródła nie bierzemy zakresu spoza słownika.

## Gotowe gdy

- Istnieje osiem plików `versions/<komponent>.json` z `0.0.0`, a `.release-please-manifest.json` ma te same wersje.
- Check `release-tags` jest w trzech rulesetach. Kontekstu `release-please` w rulesecie nie ma.
- Manifest nie zawiera `linked-versions`, a `initial-version` to `0.1.0`.
- `tests/ci/versions.sh` wypisuje `versions: ok`. Tytuł `feat(cicd)` pada. Kandydat ścieżki News to samo `news`.
- Job na pull requeście nie tworzy tagu. Akcja release-please w workflow stoi tylko w jobie pusha na `main`, z pinem SHA.
