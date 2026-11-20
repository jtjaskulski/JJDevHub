# 08. Obrazy, SBOM i atestacja

Poprzedni numer to [łańcuch dostaw](07-lancuch-dostaw.md). Następny to [wersje](09-wersje.md).

Ten numer dodaje checki `image-scan`, `sbom` i `attestations` w jednym workflow `.github/workflows/artifacts.yml`. Na drzewie bez Dockerfile kończą się jawnym `no material`. Gdy Dockerfile jest, skanują obraz Trivy, składają SBOM w SPDX narzędziem Syft i wystawiają atestację GitHuba. Nie wypychają obrazu do rejestru. Wypchnięcie i deploy są później.

## Po co

Podatność w paczce NuGet łapie [check zależności](05-zaleznosci.md). Podatność w obrazie bazowym, której nie ma w `csproj`, ten check omija. Bez SBOM nie wiadomo, co w obrazie było w dniu tagu. Bez atestacji rejestr przyjmie obraz zbudowany gdzie indziej i nazwany tak samo.

Checki są wymagane od teraz, na `develop`, `release` i `main`, żeby ruleset nie czekał na status, którego nie ma, w dniu, w którym dojdzie pierwszy Dockerfile.

## Dlaczego ta technologia

### Trivy

Trivy czyta obraz i zgłasza CVE warstw systemu oraz paczek. Próg jest ten sam co przy paczkach: wysoki i krytyczny, także bez łatki. Problem, który zamyka, to obraz `runtime`, który ciągnie bibliotekę systemową z CVE, podczas gdy `dotnet list` jest zielony. Odpada skan tylko paczek NuGet. Koszt: job buduje obraz. Dlatego uruchamia budowanie tylko wtedy, gdy w diffie jest Dockerfile albo plik, który do obrazu wchodzi. Sam job i tak startuje zawsze.

Zmiana w News nie buduje obrazu Identity. Lista obrazów bierze się z [affected-components.sh](03-build-testy-format.md). Na tym numerze skrypt zwraca pustą listę, więc i tak jest `no material`, dopóki nie ma Dockerfile.

### Syft i SPDX

Syft wypisuje składniki do SPDX. Problem, który zamyka, to „nie wiemy, co było w `news-v1.4.0`”. Odpada ręczna lista paczek. Koszt: artefakt jest duży. Zostaje przy przebiegu jako artefakt GitHuba i jedzie z tagiem. Nie commituje się go do git, bo starzeje się w tym samym commicie, który go wygenerował.

### Atestacja bez klucza w repozytorium

Wbudowane atestacje artefaktów GitHuba wiążą digest z przebiegiem. Problem, który zamyka, to klucz podpisujący w sekrecie, który ktoś skopiuje. Odpada cosign z kluczem w `secrets`. Koszt: weryfikacja działa w GitHubie i na runnerze, który ufa temu repozytorium. Deploy w [07-deploy.md](../02-szkielet-i-deploy/07-deploy.md) weryfikuje atestację, zanim cokolwiek podmieni.

Obraz bez atestacji nie jest wydaniem. Tag `:latest` nie jest referencją.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| digest | skrót obrazu, nie tag |
| SBOM | lista składników, tu SPDX |
| atestacja | stwierdzenie, że ten digest powstał w tym przebiegu |

## Klasy i ścieżki

| Ścieżka | Check |
| --- | --- |
| `.github/workflows/artifacts.yml` | `image-scan`, `sbom`, `attestations` |

Trzy joby w jednym pliku. Nazwa joba jest nazwą checka. Uprawnienia: `contents: read`, a `attestations` dodatkowo `id-token: write` i `attestations: write`. Reszta jobów tych uprawnień nie dostaje.

## Plan

### 1. Kiedy jest materiał

Wspólny krok: jeśli w drzewie nie ma `deploy/docker/Dockerfile`, każdy z trzech jobów loguje `no material` i zwraca kod `0`. Jeśli plik jest, buduje obraz lokalnie, bez `docker push`.

Nazwa lokalna to `jjdevhub-<projekt>:<sha>`. Jedyny Dockerfile, z argumentem `PROJECT`, opisuje [03-dockerfile.md](../02-szkielet-i-deploy/03-dockerfile.md). Do czasu tamtego numeru pliku nie ma i checki są puste w sposób jawny.

### 2. Skan

`image-scan` uruchamia Trivy na lokalnym obrazie. `HIGH` i `CRITICAL` dają kod `1`. Wynik bez łatki nie jest wyjątkiem. Ignorowanie identyfikatora, jeśli w ogóle, jest w `.github/trivy-allow.txt` z datą i zdaniem. Nie ma wpisu na zapas.

### 3. SBOM

`sbom` uruchamia Syft i wgrywa artefakt `sbom-<projekt>.spdx.json` do przebiegu. Pusty artefakt to kod `1`, gdy obraz był budowany.

### 4. Atestacja

`attestations` woła `actions/attest` na digest lokalnego obrazu. Na tym numerze obraz nie jedzie do GHCR, więc atestacja dotyczy artefaktu przebiegu (spakowany obraz jako artifact albo sam SBOM, jeśli digest obrazu nie jest jeszcze w rejestrze). Docelowy kształt, od numeru deployu: atestacja digestu w GHCR. Ten numer ustawia workflow tak, żeby job istniał i żeby od dnia Dockerfile atestował digest zbudowany w przebiegu. Przełączenie na GHCR jest zmianą w numerze deployu, nie drugim checkiem.

### 5. Ruleset

Trzy nazwy wchodzą do trzech rulesetów.

### Co jest do czego

| Job | Co zostawia |
| --- | --- |
| `image-scan` | kod wyjścia; wysokie CVE blokuje scalenie |
| `sbom` | artefakt SPDX przy przebiegu |
| `attestations` | powiązanie digestu z przebiegiem |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| brak `deploy/docker/Dockerfile` | warunek wspólnego kroku | `no material`, kod `0` |

Gdy Dockerfile już będzie, osobny suchy przebieg Trivy na obrazie z celowo starą paczką bazową nie wchodzi do repozytorium. Wystarczy, że próg jest w workflow i da się go przeczytać.

## Checkpoint

```bash
test -f .github/workflows/artifacts.yml
actionlint .github/workflows/artifacts.yml
grep -E 'image-scan|sbom|attestations' .github/rulesets/*.json
```

Na bieżącym drzewie, bez Dockerfile, lokalny warunek wypisuje `no material`.

## Umiesz wyjaśnić

Dlaczego trzy joby istnieją, zanim jest obraz?

Bo wymagany check, który się nie uruchamia, blokuje każdy pull request. Pusty sukces z logiem jest uczciwy. Brak joba nie jest.

Czym skan obrazu różni się od skanu paczek?

Paczki to restore. Obraz to warstwy systemu i wszystko, co Dockerfile skopiował. CVE w `libssl` obrazu bazowego nie widać w `dotnet list`.

Dlaczego nie ma klucza podpisującego w git?

Bo wyciek klucza podpisuje cokolwiek. Atestacja GitHuba używa tożsamości przebiegu. Nie ma czego wynosić z repozytorium.

Czemu `:latest` nie jest wydaniem?

Bo nie wskazuje digestu. Dwa pobrania w odstępie godziny mogą być dwoma obrazami. Na maszynę wchodzi digest z tagu komponentu.

Dlaczego News nie skanuje obrazu Identity?

Bo skrypt komponentów ogranicza budowanie do tego, co diff ruszył. Wspólna zmiana w jądrze rusza wszystkie komponenty, które jądro referencjonują.

## Zasady

- Bez Dockerfile job jest zielony i mówi `no material`. Z Dockerfile próg obowiązuje już na `develop`.
- `docker push` nie występuje w tym workflow.
- Próg Trivy to wysoki i krytyczny.
- SBOM nie jest commitowany.
- Atestacja nie używa klucza z sekretu repozytorium.

## Poza zakresem

- Dockerfile jest w [03-dockerfile.md](../02-szkielet-i-deploy/03-dockerfile.md).
- Wypchnięcie do GHCR i weryfikacja przed uruchomieniem są w [07-deploy.md](../02-szkielet-i-deploy/07-deploy.md).
- Tag, pod który obraz podpinamy, jest w [09-wersje.md](09-wersje.md).

## Źródła

- [Trivy](https://trivy.dev/docs/latest/guide/target/container_image/). Wiążące są wagi `HIGH` i `CRITICAL`. Dotyczy progu `image-scan`. Ze źródła nie bierzemy ignorowania całego obrazu bazowego.
- [Syft SPDX](https://github.com/anchore/syft). Wiążący jest format SPDX. Dotyczy artefaktu `sbom`. Ze źródła nie bierzemy commita SBOM do gałęzi.
- [Artifact attestations](https://docs.github.com/en/actions/security-guides/using-artifact-attestations-to-establish-provenance-for-builds). Wiążące jest powiązanie digestu z przebiegiem bez klucza w git. Dotyczy joba `attestations` i późniejszej weryfikacji przy deployu. Ze źródła nie bierzemy tagu `:latest` jako referencji na maszynie.

## Gotowe gdy

- Trzy nazwy jobów są w rulesetach i w `artifacts.yml`.
- Bez Dockerfile lokalny warunek zwraca `no material` i kod zero.
- W workflow nie ma `docker push` i nie ma klucza podpisującego.
- `actionlint` zwraca kod zero.
- Próg Trivy jest zapisany w jobie, nie tylko w tym pliku.
