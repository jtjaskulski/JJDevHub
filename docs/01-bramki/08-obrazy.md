# 08. Obrazy, SBOM i atestacja

Poprzedni numer to [łańcuch dostaw](07-lancuch-dostaw.md). Następny to [wersje](09-wersje.md).

Ten numer dodaje checki `image-scan`, `sbom` i `attestations` w jednym workflow `.github/workflows/artifacts.yml`. Na drzewie bez Dockerfile kończą się jawnym `no material`. Gdy Dockerfile jest, skanują obraz Trivy, składają SBOM w SPDX narzędziem Syft i wystawiają atestację GitHuba. Nie wypychają obrazu do rejestru. Wypchnięcie i deploy są później.

## Po co

Podatność w paczce NuGet łapie [check zależności](05-zaleznosci.md). Podatność w obrazie bazowym, której nie ma w `csproj`, ten check omija. Bez SBOM nie wiadomo, co w obrazie było w dniu tagu. Bez atestacji rejestr przyjmie obraz zbudowany gdzie indziej i nazwany tak samo.

Checki są wymagane od teraz, na `develop`, `release` i `main`, żeby ruleset nie czekał na status, którego nie ma, w dniu, w którym dojdzie pierwszy Dockerfile.

## Dlaczego ta technologia

### Trivy

Trivy czyta obraz i zgłasza CVE warstw systemu oraz paczek. Próg jest ten sam co przy paczkach: wysoki i krytyczny, także bez łatki. Problem, który zamyka, to obraz `runtime`, który ciągnie bibliotekę systemową z CVE, podczas gdy `dotnet list` jest zielony. Odpada skan tylko paczek NuGet. Koszt: job buduje obraz. Dlatego uruchamia budowanie tylko wtedy, gdy w diffie jest Dockerfile albo plik, który do obrazu wchodzi. Sam job i tak startuje zawsze.

Zmiana w News nie buduje obrazu bramy. Lista obrazów bierze się ze skryptu materiału, a ten woła [affected-components.sh](03-build-testy-format.md) z `--from-git`. Na tym numerze w drzewie nie ma `deploy/docker/Dockerfile`, więc i tak jest `no material`.

### Syft i SPDX

Syft wypisuje składniki do SPDX. Problem, który zamyka, to „nie wiemy, co było w `news-v1.4.0`”. Odpada ręczna lista paczek. Koszt: artefakt jest duży. Zostaje przy przebiegu jako artefakt GitHuba i jedzie z tagiem. Nie commituje się go do git, bo starzeje się w tym samym commicie, który go wygenerował.

Binarka Syft jest tą samą, którą pinuje [numer łańcucha](07-lancuch-dostaw.md): `install-pinned-tool.sh syft`. Wersja w pliku sum to `1.57.2`.

### Atestacja bez klucza w repozytorium

Wbudowane atestacje artefaktów GitHuba wiążą digest z przebiegiem. Problem, który zamyka, to klucz podpisujący w sekrecie, który ktoś skopiuje. Odpada cosign z kluczem w `secrets`. Koszt: weryfikacja działa w GitHubie i na runnerze, który ufa temu repozytorium. Deploy w [07-deploy.md](../02-szkielet-i-deploy/07-deploy.md) weryfikuje atestację, zanim cokolwiek podmieni.

Obraz bez atestacji nie jest wydaniem. Tag `:latest` nie jest referencją. Na tym numerze obraz nie jedzie do rejestru: `push-to-registry` jest `false`, a podmiotem atestacji jest spakowany plik `docker save`.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| digest | skrót obrazu, nie tag |
| SBOM | lista składników, tu SPDX |
| atestacja | stwierdzenie, że ten plik obrazu powstał w tym przebiegu |
| materiał | linia `projekt projekt-msbuild zestaw.dll` albo `no material` |

## Klasy i ścieżki

| Ścieżka | Check |
| --- | --- |
| `.github/workflows/artifacts.yml` | `image-scan`, `sbom`, `attestations` |
| `.github/scripts/artifacts-material.sh` | czy w ogóle budować, i który wiersz |
| `tests/ci/artifacts.sh` | suchy przebieg materiału, progu Trivy i zakazu `docker push` |
| `.github/trivy-allow.txt` | powstaje, gdy jest konkretny CVE; na zapas go nie ma |

Trzy joby w jednym pliku. Nazwa joba jest nazwą checka. Uprawnienia: skan ma `contents: read`. SBOM dokłada `actions: write`, bo wgrywa artefakt. Atestacja ma `contents: read`, `id-token: write` i `attestations: write`. Reszta tych uprawnień do pozostałych jobów nie przechodzi.

## Plan

### 1. Kiedy jest materiał

`.github/scripts/artifacts-material.sh` ma jedną listę obrazów. W drzewie jest na niej jeden wiersz:

```text
gateway JJDevHub.Gateway JJDevHub.Gateway.dll src/gateway/JJDevHub.Gateway/JJDevHub.Gateway.csproj
```

Trzy pierwsze pola idą do builda: nazwa lokalna, argument `PROJECT`, argument `ASSEMBLY`. Czwarte mówi, czy projekt w ogóle jest.

Bez `deploy/docker/Dockerfile` skrypt wypisuje `no material` i kończy się kodem `0`, także wtedy, gdy projekt bramy już leży. Sam Dockerfile, bez tego `.csproj`, też jest `no material`. Dockerfile i projekt, ale bez `BASE_REF` i bez gita, kończą się kodem `1` i komunikatem `brak BASE_REF`: skrypt woła `affected-components.sh --from-git`.

Gdy git i `BASE_REF` są, diff `origin/$BASE_REF...HEAD` decyduje:

- zmiana `deploy/docker/Dockerfile` albo `.dockerignore` wybiera każdy wiersz, którego projekt istnieje
- zmiana w komponencie z listy wybiera ten wiersz, gdy `affected-components.sh` go zwróci
- zmiana w News, przy liście, która zna tylko bramę, zostaje przy `no material`

Na korzeniu tego repozytorium Dockerfile nie ma, więc:

```bash
bash .github/scripts/artifacts-material.sh
```

wypisuje `no material` i zwraca kod zero.

Tłumienie CVE jest osobnym trybem `--ignore`. Czyta `.github/trivy-allow.txt`. Brak pliku wypisuje pustkę i kod `0`. Wiersz to identyfikator, data `RRRR-MM-DD` i zdanie, tak jak allowlista paczek. Gwiazdka i brak zdania kończą się kodem `1`. Poprawny wiersz wypisuje sam identyfikator. Pliku na zapas nie ma.

### 2. Trzy joby, jeden początek

Każdy job w `.github/workflows/artifacts.yml` startuje na `pull_request` do `develop`, `release` i `main`. `timeout-minutes` to `20`. Checkout ma `fetch-depth: 0` i `persist-credentials: false`. Potem każdy job odpala `bash tests/ci/artifacts.sh`.

Krok materiału jest ten sam w trzech jobach. `BASE_REF` to `github.base_ref`. Fetch bazy jest wołany tylko wtedy, gdy `deploy/docker/Dockerfile` już jest:

```bash
output=$(bash .github/scripts/artifacts-material.sh)
```

Linia `no material` ustawia `any=false` i pomija build. Inna treść ląduje w `${RUNNER_TEMP}/images.txt` i ustawia `any=true`. Dalsze kroki mają `if: steps.material.outputs.any == 'true'`.

### 3. Skan

`image-scan` ma wyłącznie `contents: read`. Gdy jest materiał, stawia Trivy akcją `aquasecurity/setup-trivy` w wersji `v0.70.0`, przypiętą do SHA. Potem dla każdej linii:

```bash
tag="jjdevhub-${projekt}:${GITHUB_SHA}"
docker build \
  --file deploy/docker/Dockerfile \
  --build-arg "PROJECT=${project}" \
  --build-arg "ASSEMBLY=${assembly}" \
  --tag "$tag" \
  .
trivy image \
  --severity HIGH,CRITICAL \
  --exit-code 1 \
  --ignore-unfixed=false \
  "$tag"
```

`HIGH` i `CRITICAL` dają kod `1`. `--ignore-unfixed=false` zostawia wynik bez łatki. Gdy jest `.github/trivy-allow.txt` i `--ignore` zwraca identyfikatory, do Trivy dochodzi `--ignorefile`. Nie ma wpisu na zapas.

`docker push` w tym jobie nie występuje.

### 4. SBOM

`sbom` dokłada `actions: write`. Gdy jest materiał, woła:

```bash
bash .github/scripts/install-pinned-tool.sh syft "$tools"
syft "docker:${tag}" -o "spdx-json=sbom-${projekt}.spdx.json"
```

Pusty plik kończy się komunikatem `pusty sbom` i kodem `1`. Artefakt przebiegu:

```yaml
name: sbom
path: sbom-*.spdx.json
retention-days: 14
if-no-files-found: error
```

Plik SPDX nie jest commitowany. Zostaje przy przebiegu.

### 5. Atestacja

`attestations` ma `id-token: write` i `attestations: write`. Gdy jest materiał, ten sam `docker build` pakuje obraz:

```bash
docker save "$tag" -o "image-subjects/image-${projekt}.tar"
```

Potem `actions/attest-build-provenance` w wersji `v4.2.2`, przypięte do SHA:

```yaml
subject-path: image-subjects/image-*.tar
push-to-registry: false
```

Podmiotem jest plik tar z tego przebiegu, nie digest w GHCR. Przełączenie na rejestr jest zmianą w numerze deployu, nie drugim checkiem. W workflow nie ma `cosign` i nie ma klucza podpisującego.

### 6. Suchy przebieg

`tests/ci/artifacts.sh` nie buduje obrazu i nie ściąga Trivy. Sprawdza skrypt materiału i tekst workflow.

| Arrange | Act | Assert |
| --- | --- | --- |
| korzeń repozytorium, bez Dockerfile | `artifacts-material.sh` | `no material`, kod `0` |
| pusty katalog | ten sam skrypt | `no material` |
| sam Dockerfile, bez `.csproj` bramy | ten sam skrypt | `no material` |
| Dockerfile i projekt bramy, bez gita | ten sam skrypt | kod `1`, log `brak BASE_REF` |
| diff tylko w `src/services/News/` | skrypt z `BASE_REF=develop` | `no material` |
| diff w `.csproj` bramy, w Dockerfile albo w `.dockerignore` | ten sam układ | linia `gateway JJDevHub.Gateway JJDevHub.Gateway.dll` |
| `--ignore` bez pliku | `artifacts-material.sh --ignore` | pusty stdout, kod `0` |
| allowlista z gwiazdką | `--ignore` | kod `1` |
| wiersz `CVE-2024-0001` z datą i zdaniem | `--ignore` | stdout to sam identyfikator |

Na końcu skrypt czyta `artifacts.yml`: są `--severity HIGH,CRITICAL`, `--exit-code 1`, `--ignore-unfixed=false`, `push-to-registry: false` i `actions/attest-build-provenance@`. Nie ma `docker push` i nie ma `cosign`. Trzy nazwy jobów są w pliku i w trzech rulesetach.

```bash
bash tests/ci/artifacts.sh
```

Zielony przebieg milczy. Kod wyjścia to `0`.

### 7. Ruleset

Trzy nazwy są już w `.github/rulesets/develop.json`, `release.json` i `main.json`: `image-scan`, `sbom`, `attestations`. Suchy przebieg to sprawdza. Osobno:

```bash
python3 -c 'import json,pathlib
need={"image-scan","sbom","attestations"}
for path in pathlib.Path(".github/rulesets").glob("*.json"):
    data=json.load(path.open())
    checks={item["context"] for item in data["rules"][-1]["parameters"]["required_status_checks"]}
    assert need <= checks, path
'
```

### Co jest do czego

| Job | Co zostawia |
| --- | --- |
| `image-scan` | kod wyjścia; wysokie CVE blokuje scalenie |
| `sbom` | artefakt SPDX przy przebiegu, 14 dni |
| `attestations` | powiązanie pliku tar z przebiegiem, bez wypchnięcia do rejestru |
| `artifacts-material.sh` | wspólna decyzja `no material` albo jedna linia builda |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| brak `deploy/docker/Dockerfile` | `artifacts-material.sh` na korzeniu | `no material`, kod `0` |
| diff News przy liście, która zna bramę | suchy przebieg | `no material` |
| diff bramy przy Dockerfile | suchy przebieg | jedna linia `gateway …` |

Gdy Dockerfile już będzie, osobny suchy przebieg Trivy na obrazie z celowo starą paczką bazową nie wchodzi do repozytorium. Próg jest w workflow i suchy przebieg go czyta grepem.

## Checkpoint

```bash
test -f .github/workflows/artifacts.yml
test ! -f deploy/docker/Dockerfile
bash .github/scripts/artifacts-material.sh
bash tests/ci/artifacts.sh
grep -E 'image-scan|sbom|attestations' .github/rulesets/*.json
```

Na bieżącym drzewie skrypt materiału wypisuje `no material`. Suchy przebieg milczy i zwraca kod zero. `actionlint .github/workflows/artifacts.yml` zwraca kod zero.

## Umiesz wyjaśnić

Dlaczego trzy joby istnieją, zanim jest obraz?

Bo wymagany check, który się nie uruchamia, blokuje każdy pull request. Pusty sukces z logiem `no material` jest uczciwy. Brak joba nie jest. Ruleset już wymienia trzy nazwy.

Czym skan obrazu różni się od skanu paczek?

Paczki to restore. Obraz to warstwy systemu i wszystko, co Dockerfile skopiował. CVE w `libssl` obrazu bazowego nie widać w `dotnet list`. Próg jest ten sam: wysoki i krytyczny, także bez łatki, bo `--ignore-unfixed=false`.

Dlaczego nie ma klucza podpisującego w git?

Bo wyciek klucza podpisuje cokolwiek. Atestacja GitHuba używa tożsamości przebiegu. `push-to-registry: false` zostawia podmiotem plik tar. Nie ma czego wynosić z repozytorium. W workflow nie ma `cosign`.

Czemu `:latest` nie jest wydaniem?

Bo nie wskazuje digestu. Dwa pobrania w odstępie godziny mogą być dwoma obrazami. Lokalny tag tego numeru to `jjdevhub-<projekt>:<sha przebiegu>`. Na maszynę, później, wchodzi digest z tagu komponentu.

Dlaczego News nie skanuje obrazu bramy?

Bo lista w `artifacts-material.sh` zna dziś jeden wiersz, `gateway`, a i ten wiersz wchodzi tylko wtedy, gdy diff ruszył bramę, Dockerfile albo `.dockerignore`. Zmiana w `src/services/News/` zostaje przy `no material`. Wspólna zmiana Dockerfile rusza wiersze, których projekt istnieje.

Dlaczego bez Dockerfile jest `no material`, skoro projekt bramy już jest?

Bo nie ma czego budować. Sam `.csproj` nie jest obrazem. Suchy przebieg na korzeniu tego wymaga.

## Zasady

- Bez Dockerfile job jest zielony i mówi `no material`. Z Dockerfile próg obowiązuje już na `develop`.
- `docker push` nie występuje w tym workflow.
- Próg Trivy to `HIGH,CRITICAL`, z `--exit-code 1` i `--ignore-unfixed=false`.
- SBOM nie jest commitowany. Pusty plik SPDX kończy job kodem `1`.
- Atestacja nie używa klucza z sekretu repozytorium.
- Lista obrazów w skrypcie jest jawna. Dziś jest na niej brama.

## Poza zakresem

- Dockerfile jest w [03-dockerfile.md](../02-szkielet-i-deploy/03-dockerfile.md). Do tego czasu pliku nie ma i checki są puste w sposób jawny.
- Wypchnięcie do GHCR i weryfikacja przed uruchomieniem są w [07-deploy.md](../02-szkielet-i-deploy/07-deploy.md).
- Tag, pod który obraz podpinamy, jest w [09-wersje.md](09-wersje.md).
- Pin Syft i plik sum są w [07-lancuch-dostaw.md](07-lancuch-dostaw.md). Ten numer tylko woła `syft`.

## Źródła

- [Trivy](https://trivy.dev/docs/latest/guide/target/container_image/). Wiążące są wagi `HIGH` i `CRITICAL`. Dotyczy progu `image-scan`. Ze źródła nie bierzemy ignorowania całego obrazu bazowego.
- [Syft SPDX](https://github.com/anchore/syft). Wiążący jest format SPDX. Dotyczy artefaktu `sbom`. Ze źródła nie bierzemy commita SBOM do gałęzi.
- [Artifact attestations](https://docs.github.com/en/actions/security-guides/using-artifact-attestations-to-establish-provenance-for-builds). Wiążące jest powiązanie podmiotu z przebiegiem bez klucza w git. Dotyczy joba `attestations` i późniejszej weryfikacji przy deployu. Ze źródła nie bierzemy tagu `:latest` jako referencji na maszynie.

## Gotowe gdy

- Trzy nazwy jobów są w rulesetach i w `artifacts.yml`.
- Bez Dockerfile `artifacts-material.sh` zwraca `no material` i kod zero. `tests/ci/artifacts.sh` też zwraca kod zero.
- W workflow nie ma `docker push` i nie ma klucza podpisującego.
- `actionlint` zwraca kod zero.
- Próg Trivy jest zapisany w jobie: `HIGH,CRITICAL`, `--exit-code 1`, `--ignore-unfixed=false`.
- Atestacja ma `push-to-registry: false`.
