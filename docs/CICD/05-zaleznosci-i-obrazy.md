# 05 — Zależności i obrazy

## Po co ten krok

Zależności NuGet / npm oraz obrazy Dockera mają swoje CVE. Ten krok:

1. Każe Dependabotowi otwierać PR z bumpami.
2. Blokuje PR, które dokładają podatne paczki (dependency review).
3. Skanuje obrazy z obu Dockerfile’y Trivym (fail na CRITICAL/HIGH).
4. Odpalają audyt NuGet i `pnpm audit` w CI.
5. Doszczelnia [api.yml](../../.github/workflows/api.yml) i [web.yml](../../.github/workflows/web.yml): `permissions: contents: read` oraz `concurrency`.

Komendy skanów żyją w [infra/ci/](../../infra/ci/). Workflowy tylko je wołają. Deploy z [03-github.md](03-github.md) i CodeQL z [04-codeql.md](04-codeql.md) zostają osobno.

## Co już jest w repo

- Stack i Compose: [01-proxmox.md](01-proxmox.md), [infra/docker/docker-compose.yml](../../infra/docker/docker-compose.yml).
- Tunel (nie skanuje zależności): [02-cloudflare-tunnel.md](02-cloudflare-tunnel.md).
- CI: [03-github.md](03-github.md) — `api.yml` (restore, build, test, `docker build -f infra/docker/Dockerfile .`), `web.yml` (`pnpm` + `docker build -f src/Clients/web/Dockerfile src/Clients`), `deploy.yml`.
- CodeQL: [04-codeql.md](04-codeql.md).
- Centralne wersje NuGet: [Directory.Packages.props](../../Directory.Packages.props), frontend: [src/Clients/web/package.json](../../src/Clients/web/package.json) + `pnpm-lock.yaml`.
- Dockerfile API: [infra/docker/Dockerfile](../../infra/docker/Dockerfile). Dockerfile web: [src/Clients/web/Dockerfile](../../src/Clients/web/Dockerfile).
- Skrypty audytu i Trivy: [nuget-audit.sh](../../infra/ci/nuget-audit.sh), [pnpm-audit.sh](../../infra/ci/pnpm-audit.sh), [trivy-api.sh](../../infra/ci/trivy-api.sh), [trivy-web.sh](../../infra/ci/trivy-web.sh). Obok zostaje [release-and-deploy.sh](../../infra/ci/release-and-deploy.sh) i cron.
- Dependabot: [.github/dependabot.yml](../../.github/dependabot.yml). Dependency review: [.github/workflows/dependency-review.yml](../../.github/workflows/dependency-review.yml).

## Słownik pojęć z tego pliku

| Pojęcie | Znaczenie |
| --- | --- |
| Dependabot | Bot GitHuba: PR z aktualizacją wersji zależności według ekosystemu (`nuget`, `npm`, `github-actions`, `docker`). |
| Dependency review | Action na PR: porównuje lock/manifest z bazą i failuje przy nowych podatnościach według progu. |
| Trivy | Skaner Aqua: OS packages + zależności w obrazie / filesystemie. Tu: fail CRITICAL i HIGH. |
| NuGet audit | `dotnet restore` / `dotnet list package --vulnerable` z infrastrukturą audytu NuGet. |
| pnpm audit | Audyt drzewa npm przez pnpm; `--audit-level=high` failuje od High w górę. |
| `permissions` | Najmniejsze uprawnienia GITHUB_TOKEN w workflow (`contents: read`). |
| `concurrency` | Jedna aktywna runda danego workflow na ref; anuluje starsze runy przy nowym pushu. |

## Kroki

### 1. Dependabot

Utwórz [`.github/dependabot.yml`](../../.github/dependabot.yml):

```yaml
version: 2
updates:
  - package-ecosystem: nuget
    directory: "/"
    schedule:
      interval: weekly
      day: monday
    open-pull-requests-limit: 10

  - package-ecosystem: npm
    directory: "/src/Clients/web"
    schedule:
      interval: weekly
      day: monday
    open-pull-requests-limit: 10

  - package-ecosystem: github-actions
    directory: "/"
    schedule:
      interval: weekly
      day: monday

  - package-ecosystem: docker
    directory: "/infra/docker"
    schedule:
      interval: weekly
      day: monday

  - package-ecosystem: docker
    directory: "/src/Clients/web"
    schedule:
      interval: weekly
      day: monday
```

`nuget` z `directory: "/"` widzi [Directory.Packages.props](../../Directory.Packages.props) i projekty w solution. `npm` celuje wyłącznie w Angular (`src/Clients/web`), nie w inne foldery. Dwa wpisy `docker` odpowiadają dwóm Dockerfile’om (katalog, w którym leży plik `Dockerfile`).

Po merge: **Settings → Code security → Dependabot** — włącz **Dependabot version updates** (i alerts, jeśli chcesz osobne powiadomienia CVE). W ciągu harmonogramu pojawią się PR od `dependabot[bot]`.

### 2. Skrypty w `infra/ci/`

Wszystkie skrypty: `chmod +x`, shebang `bash`, `set -euo pipefail`. Root = katalog repo (skrypt sam `cd` do top-level gita).

#### `infra/ci/nuget-audit.sh`

```bash
#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

export DOTNET_NOLOGO=1
dotnet restore JJDevHub.sln
dotnet list JJDevHub.sln package --vulnerable --include-transitive --format json \
  --source https://api.nuget.org/v3/index.json >/tmp/nuget-audit.json

# Nagłówek „has the following vulnerable packages” jest w raporcie tekstowym przy każdym
# severity, także Low i Moderate. Tu liczy się tylko pole JSON "severity".
if grep -Eq '"severity": "(Critical|High)"' /tmp/nuget-audit.json; then
  echo "nuget-audit: failing on Critical/High" >&2
  exit 1
fi

echo "nuget-audit: ok"
```

Low i Moderate zostają w `/tmp/nuget-audit.json` i nie kończą joba. Fail jest tylko przy `"severity": "Critical"` albo `"severity": "High"`. Stderr z `dotnet list` nie mieszaj z tym plikiem — `grep` ma widzieć sam JSON.

#### `infra/ci/pnpm-audit.sh`

```bash
#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT/src/Clients/web"

corepack enable
pnpm install --frozen-lockfile
pnpm audit --audit-level=high
```

#### `infra/ci/trivy-api.sh`

```bash
#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

IMAGE="${TRIVY_API_IMAGE:-jjdevhub-api:ci}"

docker build -f infra/docker/Dockerfile -t "$IMAGE" .
trivy image --exit-code 1 --severity CRITICAL,HIGH --ignore-unfixed "$IMAGE"
```

#### `infra/ci/trivy-web.sh`

```bash
#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

IMAGE="${TRIVY_WEB_IMAGE:-jjdevhub-web:ci}"

docker build -f src/Clients/web/Dockerfile -t "$IMAGE" src/Clients
trivy image --exit-code 1 --severity CRITICAL,HIGH --ignore-unfixed "$IMAGE"
```

`--ignore-unfixed` pomija CVE bez dostępnej poprawki w upstreamie. Jeśli wolisz failować także na unfixed, usuń flagę — świadomie, bo job będzie częściej czerwony.

Lokalnie (opcjonalnie):

```bash
# Trivy: ta sama wersja co w workflow (v0.74.0), nie skrypt z gałęzi main.
# https://github.com/aquasecurity/trivy/releases/tag/v0.74.0
./infra/ci/nuget-audit.sh
./infra/ci/pnpm-audit.sh
./infra/ci/trivy-api.sh
./infra/ci/trivy-web.sh
```

### 3. Dependency review — osobny workflow na PR

Utwórz [`.github/workflows/dependency-review.yml`](../../.github/workflows/dependency-review.yml):

```yaml
name: dependency-review

on:
  pull_request:

permissions:
  contents: read
  pull-requests: write

jobs:
  review:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Dependency Review
        uses: actions/dependency-review-action@v4
        with:
          fail-on-severity: high
```

Wymaga włączonego dependency graph / Dependabot w Settings. Działa na PR; na samym pushu do `main` nie zastępuje Trivy ani audytów.

### 4. Dopiski do `api.yml` i `web.yml`

Nie powielaj logiki skanów w YAML — wołaj skrypty. Zachowaj istniejące path filters i kroki build/test.

Docelowy kształt [api.yml](../../.github/workflows/api.yml) (pełny plik po zmianach):

```yaml
name: api

on:
  pull_request:
    paths:
      - "src/JJDevHub.Api/**"
      - "tests/JJDevHub.Api.Tests/**"
      - "nuget.config"
      - "Directory.Build.props"
      - "Directory.Packages.props"
      - "global.json"
      - "JJDevHub.sln"
      - "infra/docker/Dockerfile"
      - "infra/docker/docker-compose.yml"
      - "infra/ci/nuget-audit.sh"
      - "infra/ci/trivy-api.sh"
      - ".github/workflows/api.yml"
  push:
    branches: [main]
    paths:
      - "src/JJDevHub.Api/**"
      - "tests/JJDevHub.Api.Tests/**"
      - "nuget.config"
      - "Directory.Build.props"
      - "Directory.Packages.props"
      - "global.json"
      - "JJDevHub.sln"
      - "infra/docker/Dockerfile"
      - "infra/docker/docker-compose.yml"
      - "infra/ci/nuget-audit.sh"
      - "infra/ci/trivy-api.sh"
      - ".github/workflows/api.yml"
  workflow_dispatch:

concurrency:
  group: api-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

permissions:
  contents: read

jobs:
  build:
    name: api
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "11.0.x"
          dotnet-quality: preview

      - name: Restore
        run: dotnet restore JJDevHub.sln

      - name: NuGet audit
        run: ./infra/ci/nuget-audit.sh

      - name: Build
        run: dotnet build JJDevHub.sln --no-restore -c Release

      - name: Test
        run: dotnet test tests/JJDevHub.Api.Tests/JJDevHub.Api.Tests.csproj --no-build -c Release

      - name: Install Trivy
        env:
          TRIVY_VERSION: "0.74.0"
          TRIVY_SHA256: "2ae6fe3ee734b7fdf11335663e18c75ea12dccc76062f09f164a3b0f8be4371a"
        run: |
          set -euo pipefail
          tarball="trivy_${TRIVY_VERSION}_Linux-64bit.tar.gz"
          curl -fsSL -o "/tmp/${tarball}" \
            "https://github.com/aquasecurity/trivy/releases/download/v${TRIVY_VERSION}/${tarball}"
          echo "${TRIVY_SHA256}  /tmp/${tarball}" | sha256sum -c -
          sudo tar -xzf "/tmp/${tarball}" -C /usr/local/bin trivy
          trivy --version

      - name: Docker image + Trivy
        run: ./infra/ci/trivy-api.sh
```

Docelowy kształt [web.yml](../../.github/workflows/web.yml):

```yaml
name: web

on:
  pull_request:
    paths:
      - "src/Clients/web/**"
      - "src/Clients/.dockerignore"
      - "src/Clients/shared/theme/**"
      - "infra/docker/docker-compose.yml"
      - "infra/ci/pnpm-audit.sh"
      - "infra/ci/trivy-web.sh"
      - ".github/workflows/web.yml"
  push:
    branches: [main]
    paths:
      - "src/Clients/web/**"
      - "src/Clients/.dockerignore"
      - "src/Clients/shared/theme/**"
      - "infra/docker/docker-compose.yml"
      - "infra/ci/pnpm-audit.sh"
      - "infra/ci/trivy-web.sh"
      - ".github/workflows/web.yml"
  workflow_dispatch:

concurrency:
  group: web-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

permissions:
  contents: read

jobs:
  build:
    name: web
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: src/Clients/web
    steps:
      - uses: actions/checkout@v4

      - uses: pnpm/action-setup@v4
        with:
          package_json_file: src/Clients/web/package.json

      - uses: actions/setup-node@v4
        with:
          node-version: "22"
          cache: pnpm
          cache-dependency-path: src/Clients/web/pnpm-lock.yaml

      - name: Install
        run: pnpm install --frozen-lockfile

      - name: pnpm audit
        working-directory: .
        run: ./infra/ci/pnpm-audit.sh

      - name: Test
        run: pnpm test:ci

      - name: Build
        run: pnpm build

      - name: Install Trivy
        working-directory: .
        env:
          TRIVY_VERSION: "0.74.0"
          TRIVY_SHA256: "2ae6fe3ee734b7fdf11335663e18c75ea12dccc76062f09f164a3b0f8be4371a"
        run: |
          set -euo pipefail
          tarball="trivy_${TRIVY_VERSION}_Linux-64bit.tar.gz"
          curl -fsSL -o "/tmp/${tarball}" \
            "https://github.com/aquasecurity/trivy/releases/download/v${TRIVY_VERSION}/${tarball}"
          echo "${TRIVY_SHA256}  /tmp/${tarball}" | sha256sum -c -
          sudo tar -xzf "/tmp/${tarball}" -C /usr/local/bin trivy
          trivy --version

      - name: Docker image + Trivy
        working-directory: .
        run: ./infra/ci/trivy-web.sh
```

Uwagi:

- Stary krok samego `docker build` w `api` / `web` zastępuje skrypt Trivy (on i tak buduje obraz).
- `pnpm-audit.sh` sam robi `pnpm install` w `src/Clients/web` — podwójny install jest OK; jeśli chcesz przyspieszyć, wydziel w skrypcie sam `pnpm audit` i polegaj na kroku Install (wtedy skrypt nie wolaj `install` drugi raz — wybierz jedną konwencję i trzymaj ją).
- `deploy.yml` **nie** dostaje Trivy ani `permissions` poza tym, co już ma; nie ruszaj self-hosted joba w tym numerze.
- Instalacja Trivy to wydanie `v0.74.0` i SHA256 wpisany w YAML. Nie wracaj do `curl | sh` ze skryptu na gałęzi `main`. Przy bumpie wersji podmień oba pola i hash z `trivy_<wersja>_checksums.txt` dla `Linux-64bit.tar.gz`.

### 5. Branch protection (po pierwszym zielonym runie)

W regule `main` możesz dodać check `dependency-review` obok `api` i `web`. Nie wymagaj checków, które jeszcze nigdy nie przeszły — dopisz je po pierwszym sukcesie (jak w [03-github.md](03-github.md)).

## Jak sprawdzić, że działa

1. PR zmieniający tylko markdown nie musi odpalać `api`/`web` (path filters). PR z bumpem w `Directory.Packages.props` → `api` z krokiem NuGet audit; PR w `package.json` → `web` z pnpm audit.
2. **Actions**: zielone `api` i `web` z logami `nuget-audit: ok` / `pnpm audit` exit 0 oraz Trivy bez CRITICAL/HIGH.
3. Otwórz testowy PR z celowo starą, znaną podatną wersją (na throwaway branch) → dependency review albo audit/Trivy czerwone → zamknij bez merge.
4. Po tygodniu (albo ręcznym **Dependabot → Check for updates**): PR od bota dla nuget/npm/actions/docker.
5. `permissions: contents: read` widać w YAML; przy `GITHUB_TOKEN` job nie powinien móc pushować do repo.

## Czego w tym pliku nie ruszać

- Logiki [release-and-deploy.sh](../../infra/ci/release-and-deploy.sh) i [deploy.yml](../../.github/workflows/deploy.yml) (poza tym, że path filters w `api`/`web` mogą zawierać nowe skrypty — deploy zostaje jak jest).
- Tunelu Cloudflare, bindów portów Compose (to 01 / 07).
- CodeQL ([04-codeql.md](04-codeql.md)) — osobny workflow / default setup.
- OpenTelemetry i Prometheus (06–07).
- Nie wrzucaj pełnych komend `trivy` / `pnpm audit` inline do YAML poza instalacją Trivy i wywołaniem `./infra/ci/…`.

## Następny numer

[06-opentelemetry.md](06-opentelemetry.md) — instrumentacja `JJDevHub.Api`, eksport OTLP gRPC przez zmienną środowiskową.

---

## Pełny tutorial: zależności, audyty i skan obrazów (od zera)

### Problem, który rozwiązujesz

Aplikacja to nie tylko Twój kod. NuGet i npm ciągną setki transitive. Obraz Docker = system + runtime + Twoje binaria. CVE pojawiają się w bibliotekach i w warstwach OS (`apk`/`apt` w Alpine/Debian). Bez automatu dowiadujesz się o nich z bloga albo po incydencie.

Trzy warstwy, które się uzupełniają:

1. **Aktualizacje** (Dependabot) — PR z nowszą wersją, zanim CVE stanie się krytyczne w produkcji.
2. **Brama na PR** (dependency review + audit w CI) — nie wpuszczaj *nowych* High/Critical.
3. **Obraz** (Trivy) — to, co naprawdę odpalasz w Compose, nie tylko `package.json`.

### Dependabot — jak działa

Plik `.github/dependabot.yml` mówi botowi: ekosystem, katalog z manifestem, harmonogram, limit otwartych PR.

Ekosystemy istotne w monorepo:

| Ecosystem | Manifest | Typowy `directory` |
| --- | --- | --- |
| `nuget` | `*.csproj`, `Directory.Packages.props`, `packages.lock.json` | `/` albo katalog projektu |
| `npm` | `package.json`, lockfile (npm/yarn/pnpm) | katalog frontendu |
| `github-actions` | `.github/workflows/*.yml` | `/` |
| `docker` | `Dockerfile` | katalog z Dockerfile |

Bot nie deployuje. Otwiera PR; Twoje CI (build, test, audit, Trivy) ma powiedzieć, czy bump jest bezpieczny. Grupowanie (`groups:`) zmniejsza liczbę PR — opcjonalne na później.

**Alerts** vs **version updates**: alerts to powiadomienia o CVE w zależnościach; version updates to automatyczne PR. Często włącza się oba.

### Dependency review action

Na `pull_request` action czyta diff manifestów/locków i bazę GitHub Advisory. Parametr `fail-on-severity: high` kończy job czerwono, gdy PR *wprowadza* podatność High lub Critical. Już istniejący dług na `main` nie zawsze blokuje — sensem jest nie pogarszać stanu.

Wymaga dependency graph (włączany razem z Dependabot / automatycznie dla publicznych repo).

### Audyt menedżera pakietów

**NuGet** (SDK 5+ / nowoczesne):

```bash
dotnet restore
dotnet list package --vulnerable --include-transitive
```

W CI ustawiasz fail, gdy output zawiera Critical/High. Central Package Management (`Directory.Packages.props`) oznacza, że bump wersji jest w jednym pliku — Dependabot i audyt patrzą tam.

**npm / pnpm**:

```bash
pnpm audit --audit-level=high
npm audit --audit-level=high
```

Exit code ≠ 0 przy znalezieniu poziomu ≥ progu. `audit` korzysta z rejestru advisories; bywa rozjazd względem GitHub Advisory — dlatego dependency review + audit razem mają sens.

Ograniczenia audytu: false positive, brak fix version, spór „czy to w ogóle osiągalne w naszym kodzie”. Polityka zespołu: High+ fail w CI, Medium w raporcie tygodniowym.

### Trivy — skan obrazu

Typowy przebieg:

```bash
docker build -t myapp:ci -f Dockerfile .
trivy image --severity CRITICAL,HIGH --exit-code 1 myapp:ci
```

Trivy raportuje CVE w:

- pakietach OS warstwy bazowej (`nginx:1.27-alpine`, `mcr.microsoft.com/dotnet/aspnet:…`),
- bibliotekach aplikacji wykrytych w warstwach.

`--exit-code 1` = fail CI. `--ignore-unfixed` = nie failuj, gdy vendor nie wydał poprawki (kompromis operacyjny).

Inne tryby (na naukę): `trivy fs .` (filesystem bez Dockera), `trivy config` (IaC). W JJDevHub wystarczy `trivy image` na obu Dockerfile’ach.

Czytanie wyniku: ID CVE, severity, pakiet, zainstalowana wersja, fixed version. Remediacja: nowszy base image, bump paczki, albo `.trivyignore` z uzasadnieniem (ostrożnie).

### `permissions` i `concurrency` w Actions

Domyślny `GITHUB_TOKEN` bywał szeroki. Jawne:

```yaml
permissions:
  contents: read
```

ogranicza token do odczytu treści repo — wystarczy do checkout + build. Job, który uploaduje SARIF, dokłada `security-events: write`. Deploy na self-hosted i tak nie potrzebuje write do contents, jeśli nie pushuje gałęzi (w JJDevHub push release jest wyłączony domyślnie).

```yaml
concurrency:
  group: api-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true
```

Przy serii pushy na ten sam branch anuluje poprzedni run — oszczędza minuty i unikasz race „starszy build nadpisuje status nowszego” w UI.

### Typowy przebieg w zespole

1. Dependabot otwiera PR „bump Npgsql…”.
2. Odpalają się `api`, dependency-review, ewentualnie CodeQL.
3. Zielone → review → merge.
4. `deploy.yml` (po sukcesie CI na `main`) przebudowuje Compose na VM.
5. Gdy Trivy złapie CVE w `aspnet` base — PR podbija tag obrazu bazowego w Dockerfile, nie „wyłącza skaner”.

### Typowe pomyłki

1. **Skan tylko `package.json`, bez obrazu** — produkcja i tak leci na nginx/dotnet base z dziurą OS.
2. **Trivy w YAML skopiowany 4 razy** — dryf flag; trzymaj skrypt w `infra/ci/`.
3. **Dependabot bez CI na PR** — merge bumpa „na oko”.
4. **`permissions: write-all` „na wszelki wypadek”** — zbędne ryzyko przy skradzionym workflow.
5. **Ignorowanie transitive** — `dotnet list … --include-transitive` i lockfile istnieją po to.
6. **Jeden ekosystem `npm` na root monorepo**, gdy frontend siedzi w podkatalogu — bot nic nie znajdzie; `directory` musi wskazać `src/Clients/web`.
7. **Fail na Low w pierwszym tygodniu** — zespół wyłącza cały skaner; zacznij od High/Critical.
8. **Mylenie dependency review z CodeQL** — review = zależności; CodeQL = wzorce w Twoim kodzie.

### Oficjalne źródła

- GitHub Docs: Dependabot, dependency review action, GITHUB_TOKEN permissions, concurrency.
- NuGet: auditing packages / `dotnet list package --vulnerable`.
- pnpm: `pnpm audit`.
- Aqua Trivy: dokumentacja `trivy image`, severity, ignore file.
