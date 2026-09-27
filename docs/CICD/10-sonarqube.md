# SonarQube — Quality Gate na PR

## Po co ten krok

CodeQL ([04-codeql.md](04-codeql.md)) łapie klasy błędów bezpieczeństwa. Dependencies i obrazy ([05-zaleznosci-i-obrazy.md](05-zaleznosci-i-obrazy.md)) pilnują CVE. SonarQube pilnuje **jakości i coverage** na PR: smród kodu, duplikaty, brak testów na nowym kodzie. Self-hosted na tej samej VM Proxmox co Compose ([01-proxmox.md](01-proxmox.md)). Jeden projekt Sonara na serwis (`jjdevhub-api`, `jjdevhub-web`). Wynik Quality Gate ma być **wymaganym status checkiem** na `main` ([03-github.md](03-github.md)).

## Co już jest w repo

- VM z Dockerem, `/opt/jjdevhub`, `/etc/jjdevhub/api.env` — [01-proxmox.md](01-proxmox.md)
- Tunel tylko na aplikację (`4200`) — [02-cloudflare-tunnel.md](02-cloudflare-tunnel.md). Sonara **nie** publikujesz tunelem
- Workflowy [`.github/workflows/api.yml`](../../.github/workflows/api.yml), [`web.yml`](../../.github/workflows/web.yml), deploy self-hosted — [03-github.md](03-github.md)
- Testy API: `dotnet test tests/JJDevHub.Api.Tests/JJDevHub.Api.Tests.csproj` (już w `api.yml`)
- Testy web: `pnpm test:ci` w `src/Clients/web` (już w `web.yml`)
- Observability 06–09 nie jest wymagana do Sonara; nie mieszaj portów Sonara z Grafana/Jaeger

SonarQube **nie** wchodzi do [infra/docker/docker-compose.yml](../../infra/docker/docker-compose.yml) aplikacji — osobny Compose na VM (cięższy JVM + własny Postgres), żeby `release-and-deploy.sh` nie przebudowywał Sonara przy każdym deployu hubu.

## Słownik pojęć z tego pliku

| Pojęcie | Znaczenie tu |
| --- | --- |
| SonarQube | Serwer analizy statycznej + historii quality |
| SonarScanner | Klient CLI / plugin, który wysyła raport analizy do serwera |
| Quality Gate | Zestaw progów (coverage na new code, bugs, …); Pass/Fail |
| Project key | Identyfikator projektu na serwerze (`jjdevhub-api`, `jjdevhub-web`) |
| token | Sekret do `sonar.token` w CI; nie commitować |
| coverage report | Plik (np. OpenCover/Cobertura) z `dotnet test --collect` |
| PR decoration / check | Status na PR w GitHubie (tu: job w Actions + required check) |

## Kroki

### 1. Osobny Compose Sonara na VM

Na VM (nie w drzewie aplikacji przy każdym release, chyba że wolisz trzymać plik w repo pod `infra/sonar/` — wtedy **nie** dodawaj go do `release-and-deploy.sh`):

```bash
sudo mkdir -p /opt/sonarqube
sudo chown "$USER:$USER" /opt/sonarqube
```

Plik `/opt/sonarqube/docker-compose.yml`:

```yaml
services:
  sonarqube-db:
    image: postgres:16-alpine
    container_name: sonarqube-db
    environment:
      POSTGRES_USER: sonar
      POSTGRES_PASSWORD: ${SONAR_DB_PASSWORD}
      POSTGRES_DB: sonarqube
    volumes:
      - sonarqube_db:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U sonar"]
      interval: 5s
      timeout: 5s
      retries: 10

  sonarqube:
    image: sonarqube:community
    container_name: sonarqube
    depends_on:
      sonarqube-db:
        condition: service_healthy
    environment:
      SONAR_JDBC_URL: jdbc:postgresql://sonarqube-db:5432/sonarqube
      SONAR_JDBC_USERNAME: sonar
      SONAR_JDBC_PASSWORD: ${SONAR_DB_PASSWORD}
    ports:
      - "127.0.0.1:9000:9000"
    volumes:
      - sonarqube_data:/opt/sonarqube/data
      - sonarqube_extensions:/opt/sonarqube/extensions
      - sonarqube_logs:/opt/sonarqube/logs
    restart: unless-stopped

volumes:
  sonarqube_db:
  sonarqube_data:
  sonarqube_extensions:
  sonarqube_logs:
```

Env:

```bash
openssl rand -base64 24
printf 'SONAR_DB_PASSWORD=%s\n' 'WARTOSC' > /opt/sonarqube/.env
chmod 600 /opt/sonarqube/.env
cd /opt/sonarqube
docker compose --env-file .env up -d
```

Kernel (raz na VM, jeśli Sonar nie wstaje — typowe na Linux):

```bash
sudo sysctl -w vm.max_map_count=524288
echo 'vm.max_map_count=524288' | sudo tee /etc/sysctl.d/99-sonarqube.conf
```

UI: `http://127.0.0.1:9000` (SSH tunnel jak przy Grafanie). Pierwsze hasło `admin` / `admin` — wymuś zmianę od razu.

### 2. Dwa projekty Sonara (jeden na serwis)

W UI: **Create project** → manual:

| Project key | Display name |
| --- | --- |
| `jjdevhub-api` | JJDevHub API |
| `jjdevhub-web` | JJDevHub Web |

Dla każdego wygeneruj **Global Analysis Token** albo token projektu (SonarQube 10+: User → My Account → Security). Dwa sekrety w GitHubie:

- `SONAR_TOKEN_API`
- `SONAR_TOKEN_WEB`

Oraz URL dostępny **dla runnerów**:

- Hosted `ubuntu-latest` nie dosięgnie `127.0.0.1` na Twojej VM. Opcje:
  1. Dodać job analizy na `runs-on: [self-hosted, jjdevhub]` (ten sam runner co deploy) z `SONAR_HOST_URL=http://127.0.0.1:9000`, albo
  2. Wystawić Sonara tylko w LAN / VPN i podać ten URL runnerowi self-hosted.

Ten tutorial wybiera **(1)**: analiza na self-hosted runnerze. Nie wystawiaj `:9000` w Cloudflare.

Sekret wspólny:

- `SONAR_HOST_URL` = `http://127.0.0.1:9000`

### 3. Coverage API z `dotnet test`

Centralne paczki w [Directory.Packages.props](../../Directory.Packages.props). Dopisz wersje:

```xml
<PackageVersion Include="coverlet.collector" Version="6.0.4" />
```

W [tests/JJDevHub.Api.Tests/JJDevHub.Api.Tests.csproj](../../tests/JJDevHub.Api.Tests/JJDevHub.Api.Tests.csproj):

```xml
<PackageReference Include="coverlet.collector">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

Lokalny przebieg raportu:

```bash
dotnet test tests/JJDevHub.Api.Tests/JJDevHub.Api.Tests.csproj -c Release \
  --collect:"XPlat Code Coverage" \
  --results-directory TestResults
```

Coverlet zwykle zapisze `TestResults/**/coverage.cobertura.xml`.

Plik `src/JJDevHub.Api/sonar-project.properties` (albo parametry tylko w CLI — poniżej w workflow):

```properties
sonar.projectKey=jjdevhub-api
sonar.projectName=JJDevHub API
sonar.sources=src/JJDevHub.Api
sonar.tests=tests/JJDevHub.Api.Tests
sonar.exclusions=**/bin/**,**/obj/**,**/Migrations/**
sonar.cs.opencover.reportsPaths=
sonar.cs.cobertura.reportsPaths=TestResults/**/coverage.cobertura.xml
```

Ścieżki raportu doprecyzuj po pierwszym `dotnet test` (`find TestResults -name coverage.cobertura.xml`).

Skaner .NET czyta `sonar-project.properties` z katalogu, w którym odpalasz `begin` (w jobie to korzeń repo po `checkout`). Plik położony tylko w `src/JJDevHub.Api/` sam się nie włączy. Wykluczenia migracji podaj w `begin` (`/d:sonar.exclusions=**/bin/**,**/obj/**,**/Migrations/**`) albo połóż properties w korzeniu i nie mieszaj go ze skanem web. `sonar.sources` przy `dotnet sonarscanner` pomiń — źródła biorą się z projektów zbudowanych między begin a end.

### 4. Quality Gate

W Sonar UI: **Quality Gates**. **Sonar way** ma próg coverage na new code (zwykle 80%). Dla `jjdevhub-api` jest sensowny, gdy raport Cobertura dochodzi. Dla `jjdevhub-web` ten sam próg obleje każdy PR: `pnpm test:ci` nie oddaje lcov, a krok 6 raportu coverage nie wysyła. Sklonuj gate, zdejmij warunek coverage i przypisz go tylko do web. API zostaw na Sonar way (albo na klonie z progiem, który akceptujesz). Jeden gate z coverage na obu projektach robi z `sonar-web` stały czerwony check.

Gate Fail = nieprzechodzący check w CI, o ile skaner ma `qualitygate.wait=true` (krok 8).

### 5. Workflow API — skan na self-hosted

Rozszerz [`.github/workflows/api.yml`](../../.github/workflows/api.yml): po istniejącym jobie `build` na `ubuntu-latest` dodaj job `sonar` (albo przenieś test+sonar na self-hosted — ważne, by token i host były osiągalne).

Przykład joba (obok istniejącego `build`; nie kasuj build/test na hosted, jeśli chcesz szybki feedback — wtedy sonar osobno):

Kolejność skanera .NET: **begin → build → test z coverage → end**.

```yaml
  sonar:
    name: sonar-api
    needs: build
    if: github.event_name == 'pull_request' || github.ref == 'refs/heads/main'
    runs-on: [self-hosted, jjdevhub]
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "11.0.x"
          dotnet-quality: preview

      - name: SonarScanner begin
        run: |
          dotnet tool update --global dotnet-sonarscanner
          export PATH="$PATH:$HOME/.dotnet/tools"
          dotnet sonarscanner begin \
            /k:"jjdevhub-api" \
            /d:sonar.token="${{ secrets.SONAR_TOKEN_API }}" \
            /d:sonar.host.url="${{ secrets.SONAR_HOST_URL }}" \
            /d:sonar.cs.cobertura.reportsPaths="TestResults/**/coverage.cobertura.xml" \
            /d:sonar.qualitygate.wait=true

      - name: Build for Sonar
        run: |
          export PATH="$PATH:$HOME/.dotnet/tools"
          dotnet build JJDevHub.sln -c Release

      - name: Test with coverage
        run: |
          dotnet test tests/JJDevHub.Api.Tests/JJDevHub.Api.Tests.csproj -c Release --no-build \
            --collect:"XPlat Code Coverage" \
            --results-directory TestResults

      - name: SonarScanner end
        run: |
          export PATH="$PATH:$HOME/.dotnet/tools"
          dotnet sonarscanner end /d:sonar.token="${{ secrets.SONAR_TOKEN_API }}"
```

Nazwa checka na GitHubie będzie `sonar-api` (pole `name` joba).

### 6. Workflow web — osobny projekt

W [`.github/workflows/web.yml`](../../.github/workflows/web.yml) dodaj job `sonar-web` na `[self-hosted, jjdevhub]`:

```yaml
  sonar:
    name: sonar-web
    needs: build
    if: github.event_name == 'pull_request' || github.ref == 'refs/heads/main'
    runs-on: [self-hosted, jjdevhub]
    defaults:
      run:
        working-directory: src/Clients/web
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - uses: pnpm/action-setup@v4
        with:
          package_json_file: src/Clients/web/package.json

      - uses: actions/setup-node@v4
        with:
          node-version: "22"
          cache: pnpm
          cache-dependency-path: src/Clients/web/pnpm-lock.yaml

      - name: Install and test
        run: |
          pnpm install --frozen-lockfile
          pnpm test:ci

      - name: Sonar scan
        working-directory: .
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN_WEB }}
          SONAR_HOST_URL: ${{ secrets.SONAR_HOST_URL }}
        run: |
          docker run --rm --network host \
            -e SONAR_HOST_URL \
            -e SONAR_TOKEN \
            -v "$PWD:/usr/src" \
            sonarsource/sonar-scanner-cli \
            -Dsonar.projectKey=jjdevhub-web \
            -Dsonar.sources=src/Clients/web/src \
            -Dsonar.exclusions='**/node_modules/**,**/*.spec.ts,**/jjdevhub-api.client.ts' \
            -Dsonar.qualitygate.wait=true
```

`--network host` na Linuxie self-hosted pozwala CLI dosięgnąć `127.0.0.1:9000`. Coverage frontu (lcov) dołóż później, jeśli `pnpm test:ci` zacznie emitować raport — na start Gate może opierać się o issues bez coverage web.

### 7. Branch protection

GitHub → **Settings** → **Branches** → reguła `main` ([03-github.md](03-github.md)):

- Require status checks: istniejące `api`, `web`, **oraz** `sonar-api` i `sonar-web` po pierwszym zielonym runie

Bez pierwszego runu check nie ma na liście — dodaj po PR testowym. Na liście bywa sama nazwa joba albo `api / sonar-api` — zaznacz tę, którą PR już pokazuje.

Joby Sonara siedzą w workflowach z filtrem `paths`. PR, który rusza tylko API, nie uruchamia `web.yml`, więc check `sonar-web` w ogóle nie powstaje. Required check, którego nie ma, potrafi zablokować merge. Po pierwszym teście obu stron albo zostaw required tylko check, który na danym PR realnie startuje, albo wynieś skan do workflow bez filtra ścieżek.

### 8. Quality Gate w CI (fail joba)

Domyślnie scanner kończy się 0 nawet przy failed gate, jeśli nie włączysz wait:

```bash
/d:sonar.qualitygate.wait=true
```

Dodaj tę właściwość do `dotnet sonarscanner begin` i do `sonar-scanner-cli` (`-Dsonar.qualitygate.wait=true`). Job czerwony = Gate Fail = merge zablokowany przy required check.

## Jak sprawdzić, że działa

Na VM:

```bash
cd /opt/sonarqube && docker compose --env-file .env ps
curl -fsS -o /dev/null -w '%{http_code}\n' http://127.0.0.1:9000/
```

W UI widać projekty `jjdevhub-api` i `jjdevhub-web`. Otwórz PR zmieniający `src/JJDevHub.Api/**`: check `sonar-api` się pojawia. Przy `qualitygate.wait=true` job jest czerwony, gdy gate nie przejdzie. Obraz `community` trzyma jedną gałąź — w UI widać ostatnią analizę, nie osobną zakładkę PR.

Negatyw: `https://hub.example.com` nie serwuje `:9000`. Compose aplikacji (`jjdevhub-*`) nadal bez kontenera Sonara.

## Czego w tym pliku nie ruszać

- Nie wpinaj Sonara w [infra/docker/docker-compose.yml](../../infra/docker/docker-compose.yml) aplikacji ani w `release-and-deploy.sh` bez osobnej decyzji — restart hubu nie powinien zrywać Sonara
- Nie publikuj `:9000` w Cloudflare Tunnel
- Nie używaj jednego `projectKey` na całe monorepo — osobno API i web
- Nie commituj `SONAR_TOKEN_*` ani hasła DB Sonara
- Nie wyłączaj `fetch-depth: 0` — analiza new code na PR tego potrzebuje
- Nie zastępuj tym krokiem CodeQL (04) ani Trivy (05)

## Następny numer

[przyszlosc.md](przyszlosc.md) — krótka notatka (Kafka, event sourcing, Jenkins). **To nie jest tutorial wdrożeniowy** i nie ma kroków build/deploy.

---

## Pełny tutorial: SonarQube (od zera)

Ta sekcja tłumaczy serwer, skaner i bramkę. Przepis Compose i jobów jest wyżej. Nie zastępuje CodeQL ([04-codeql.md](04-codeql.md)) ani skanu CVE ([05-zaleznosci-i-obrazy.md](05-zaleznosci-i-obrazy.md)).

### Co to jest

SonarQube trzyma historię jakości kodu i liczy, czy **nowy** kod mieści się w progach. CI buduje projekt, opcjonalnie dokleja raport pokrycia i wysyła paczkę na serwer. Serwer zwraca Pass albo Fail. W tym repozytorium serwer to obraz `sonarqube:community` na tej samej VM co hub, w **osobnym** Compose pod `/opt/sonarqube`. Deploy huba (`release-and-deploy.sh`) go nie restartuje.

Trzy warstwy obok siebie:

| Warstwa | Pytanie | Gdzie |
| --- | --- | --- |
| CodeQL | Czy jest ścieżka do niebezpiecznego API? | 04, GitHub code scanning |
| Dependabot, audyt, Trivy | Czy paczka albo obraz ma znane CVE? | 05 |
| SonarQube | Jaki jest dług, duplikacja i pokrycie **nowego** kodu? | ten plik |

Reguły security w Sonarze częściowo zahaczają o te same klasy co CodeQL. Bramka jakości i tak zostaje tutaj: pokrycie, bugi, zapachy. `security-and-quality` w CodeQL tego nie zastępuje.

```
PR (checkout na runnerze jjdevhub na VM)
    ├─ dotnet sonarscanner begin
    ├─ dotnet build
    ├─ dotnet test  →  TestResults/**/coverage.cobertura.xml
    └─ dotnet sonarscanner end
            │
            ▼  HTTP, token
    127.0.0.1:9000   sonarqube  →  sonarqube-db (osobny Postgres)
            │
            ▼
    Quality Gate Pass/Fail  →  kod wyjścia joba (gdy wait=true)
            │
            ▼
    check sonar-api / sonar-web na GitHubie
```

Front idzie inną drogą: obraz `sonarsource/sonar-scanner-cli` w Dockerze, bez raportu pokrycia, dopóki `pnpm test:ci` go nie zacznie pisać.

### Community i jedna gałąź

Obraz z kroku 1 to **Community**. Analiza gałęzi i osobny widok pull requestu są w edycji Developer i wyżej. Na Community jest jeden snapshot projektu, zwykle gałąź główna.

Z tego wynikają trzy rzeczy, których UI nie pokaże:

- Nie ma listy PR-ów w projekcie. Ostatni skan nadpisuje poprzedni. Skan z PR i skan z `main` wymieniają się na tym samym ekranie.
- Parametry `sonar.pullrequest.key` / `sonar.pullrequest.branch` serwer Community odrzuca. Nie doklejaj ich „żeby zobaczyć PR”.
- Check na GitHubie i tak działa. Runner robi checkout kodu z PR, skaner liczy gate **tej** migawki, `qualitygate.wait=true` kończy job niezerem, gdy próg nie przeszedł. Czerwień jest na checku `sonar-api`, nie jako komentarz Sonara pod diffem.

Wtyczka community-branch (osobne repo, bez wsparcia SonarSource) umie domalować gałęzie. Ten przepis jej nie używa. SonarCloud byłby tym samym modelem w SaaS, z PR-ami w cenie planu — tu zostaje serwer na VM.

### Serwer obok huba, nie w nim

`/opt/sonarqube/docker-compose.yml` ma własny Postgres (`sonarqube-db`, baza `sonarqube`, user `sonar`). To nie jest `jjdevhub-db`. JDBC idzie po nazwie usługi w **tej** sieci Compose: `jdbc:postgresql://sonarqube-db:5432/sonarqube`. Port 5432 nie jest publikowany na hosta. Aplikacyjny Postgres i tak wisi na `5433` na hoście — kolizji nie ma, dopóki ktoś nie wystawi Sonarowej bazy na `5432` albo `5433`.

Hasło w `/opt/sonarqube/.env` (`SONAR_DB_PASSWORD`) jest hasłem **bazy**, nie konta w UI. Plik `chmod 600`, nie w git.

UI: `http://127.0.0.1:9000`. Z laptopa tak samo jak Grafana:

```bash
ssh -L 9000:127.0.0.1:9000 deploy@IP_VM
```

Pierwsze logowanie to `admin` / `admin`. Zmiana hasła zapisuje się w volume `sonarqube_db`. Podmiana `.env` jej nie rusza. Tunel Cloudflare zostaje przy `4200`. Sonar bez hasła albo ze startowym `admin` na publicznym porcie to mapa kodu i tokenów analizy na zewnątrz.

Kontener po `up` jeszcze przez kilka minut stawia schemat. `curl` na `:9000` w tej pierwszej chwili pada, choć `ps` już pokazuje proces. Log: `docker logs sonarqube`.

Gdy log mówi, że `vm.max_map_count` jest za niskie, proces indeksu nie wstanie i kontener wchodzi w restart. Sysctl z kroku 1 (`524288`, plik w `/etc/sysctl.d/`) zostaje po rebootcie VM. Sam `-w` bez pliku znika po restarcie maszyny.

Obraz `sonarqube:community` bez tagu wersji przy następnym `pull` może skoczyć o major. Po pierwszym zdrowym starcie warto przypiąć konkretny tag. Skok wersji na istniejącym volume bywa migracją schematu, nie drobiazgiem.

RAM: to JVM obok API, Prometheusa, Jaegera i Grafany. Osobny Compose jest po to, żeby deploy huba jej nie zabijał, nie po to, żeby udawać, że pamięci starcza na wszystko. Gdy VM się dusi, Sonar jest pierwszym kandydatem do zejścia z tej maszyny, nie Jaeger.

### Projekt, token, gate

Dwa klucze, dwa sekrety:

| Project key | Co wchodzi do skanu | Pokrycie w tym kroku |
| --- | --- | --- |
| `jjdevhub-api` | Projekty zbudowane między `begin` a `end` (API i testy) | Cobertura z Coverlet |
| `jjdevhub-web` | `src/Clients/web/src`, bez `node_modules`, speców i wygenerowanego klienta | Brak raportu, dopóki testy nie zaczną pisać lcov |

Token tworzysz w UI (My Account → Security), typ analizy projektu albo globalny. Do CI idzie token, nie hasło admina. `SONAR_TOKEN_API` analizuje tylko API, `SONAR_TOKEN_WEB` tylko web. `SONAR_HOST_URL` w sekretach GitHuba to `http://127.0.0.1:9000`.

Ten adres jest prawdziwy wyłącznie dla procesu na VM. Job na `ubuntu-latest` łączy się ze **swoim** loopbackiem i dostaje connection refused. Dlatego oba joby Sonara mają `runs-on: [self-hosted, jjdevhub]`. Runner w kontenerze z własną siecią też nie widzi Sonara na `127.0.0.1` hosta — ma być procesem na tej maszynie, tak jak deploy z 03.

**Sonar way** wymaga pokrycia nowego kodu (próg w okolicy 80%) oraz ratingów bugów i podatności. Dla API, gdy Cobertura dochodzi, ten próg ma sens. Dla web ten sam gate jest czerwony na każdym PR: `pnpm test:ci` (`ng test --watch=false`) nie zapisuje lcov, a skaner web go nie dostaje. Linie bez raportu liczą się jako niepokryte. Dlatego web dostaje **klon** gate bez warunku coverage. Issues zostają. Pokrycie frontu dołożysz razem z raportem, nie przez zaostrzenie gate’u w ciemno.

New code na Community ustawiasz jako „previous version” albo liczbę dni. Nie ma „reference branch” z edycji płatnej. Pierwsza analiza często bierze cały kod za nowy. Przy 80% coverage i cienkich testach pierwszy `sonar-api` pada, choć skaner doszedł do końca. To próg, nie zepsuty raport. Albo obniżasz próg na start, albo oznaczasz wersję po pierwszym zielonym skanie i gate pilnuje tylko tego, co dojdzie później (Clean as You Code).

### Skaner .NET: begin, build, test, end

`dotnet sonarscanner` wstrzykuje się w MSBuild. Kolejność jest częścią narzędzia, nie ozdobą:

1. **begin** — zapisuje `.sonarqube` w katalogu roboczym i podpina targety pod następny build. Tu podajesz klucz, token, URL, ścieżkę Cobertury, wykluczenia i `sonar.qualitygate.wait=true`.
2. **build** — kompilacja, którą skaner nagrywa. Build sprzed `begin` do analizy nie wchodzi. Job hosted `api` buduje osobno i ten artefakt się nie liczy. Job `sonar-api` buduje jeszcze raz, już po `begin`.
3. **test** — `dotnet test … --no-build` na tym buildzie, z `--collect:"XPlat Code Coverage"` i `--results-directory TestResults`. Coverlet (paczka `coverlet.collector` w projekcie testów) kładzie `TestResults/<guid>/coverage.cobertura.xml`.
4. **end** — pakuje raport i wysyła. Potem, przy `wait=true`, odpytuje serwer o gate i kończy proces kodem niezerowym, gdy gate jest Fail.

Bez `end` serwer nie dostaje analizy. Sam `end` bez wcześniejszego `begin` w tym katalogu pada.

`sonar-project.properties` skaner czyta z **bieżącego katalogu** `begin`. W jobie po `checkout` to korzeń repo. Plik pod `src/JJDevHub.Api/sonar-project.properties` leży obok, niewidoczny. Wykluczenie `**/Migrations/**` albo jest w `/d:sonar.exclusions=…` przy `begin`, albo properties leży w korzeniu. Pusta linia `sonar.cs.opencover.reportsPaths=` w takim pliku nic nie wnosi — raport w przepisie jest Coberturą.

`sonar.sources` ustawiasz przy skanerze frontu. Przy `dotnet sonarscanner` źródła są projektami, które build objął. Ręczne `sonar.sources=src/JJDevHub.Api` potrafi zejść się z auto-detekcją i wyciąć testy albo zdublować moduły.

Projekt testowy SDK jest rozpoznawany jako testy. Migracje EF są zwykłym C# w projekcie API: bez wykluczenia wchodzą do new code i do mianownika coverage. Wygenerowany OpenAPI JSON skaner C# zwykle olewa; liczy się `Migrations`.

Ścieżka `TestResults/**/coverage.cobertura.xml` musi zgadzać się z tym, co Coverlet naprawdę zapisał. Po pierwszym teście: `find TestResults -name coverage.cobertura.xml`. Zły glob daje analizę **bez** błędu skanera i coverage 0%. Przy progu 80% gate pada. Testy mogą być zielone.

`fetch-depth: 0` na `checkout` tego joba ściąga historię. Płytki klon psuje blame i to, które linie Sonar uzna za nowe. Job `build` na `ubuntu-latest` może zostać płytki. Pełna historia jest potrzebna tylko skanerowi.

`dotnet tool update --global dotnet-sonarscanner` na self-hosted runnerze zostawia narzędzie w `$HOME/.dotnet/tools`. Każdy step Actions to nowa powłoka, więc `PATH` z tą ścieżką jest w `begin` i w `end`. Krok testu go nie potrzebuje.

Runner samohostowany **zostawia** katalog roboczy między runami. `actions/checkout` z domyślnym czyszczeniem untracked plików zwykle zabiera `.sonarqube` i `TestResults`. Gdy begin narzeka na poprzednią analizę w tym katalogu, w logu widać resztkę `.sonarqube` — następny checkout ma ją sprzątnąć, nie Ty ręcznie w git.

### Skaner frontu

Job `sonar-web` najpierw, w `src/Clients/web`, robi `pnpm install` i `pnpm test:ci`. Krok skanu ustawia `working-directory: .`. W Actions ścieżka stepu jest względem katalogu repozytorium, nie względem domyślnego katalogu joba. `.` to korzeń. `docker run -v "$PWD:/usr/src"` montuje całe repo. `-Dsonar.sources=src/Clients/web/src` jest względem tego montowania. Przeniesienie stepu „bliżej frontu” bez zmiany `sources` sprawi, że skaner szuka ścieżki, której w środku nie ma.

`--network host` daje kontenerowi skanera loopback VM, czyli Sonara na `:9000`. Bez tej flagi `127.0.0.1` to sam kontener skanera i połączenie pada, choć z hosta UI działa.

Wykluczenia: `node_modules` (po `pnpm install` katalog jest na dysku), `**/*.spec.ts` (testy nie są kodem produktu), `jjdevhub-api.client.ts` (klient NSwag, generowany). Zostawienie klienta w skanie sypie zapachami, których nie poprawisz ręcznie.

`SONAR_TOKEN` i `SONAR_HOST_URL` skaner CLI czyta ze środowiska. W przepisie wchodzą przez `-e` do kontenera. Nie wypisuj ich w `echo`.

### Co znaczy Pass i Fail

Gate to zestaw warunków na **new code**, nie na cały historia projektu (o ile new code nie jest „wszystko”).

- **Passed** — progi spełnione. Job z `wait=true` jest zielony.
- **Failed** — w UI: projekt → warunki gate (który próg) i lista issues na new code. Coverage 0% przy zielonych testach to prawie zawsze zły plik raportu, nie „Sonar nie lubi xUnit”.
- Job zielony i w UI gate czerwony — brak `qualitygate.wait`. Skaner wysłał raport i wyszedł z kodem 0. Required check tego nie złapie. Flaga jest w `begin` (API) i przy CLI (web). Domyślnie jej nie ma.
- „You're not authorized” / 401 — token nie ma prawa „Execute Analysis” na tym `projectKey`, albo sekret API wsadzony do joba web.
- Timeout przy wait — serwer jeszcze liczy (pierwszy skan, mało RAM) albo indeks nie wstał (`max_map_count`). Wydłużanie timeoutu nie naprawia padającego kontenera.

Issue w UI ma typ (bug, vulnerability, code smell) i ciężar. Na PR w Community i tak oglądasz ostatni snapshot, a werdykt merga jest w checku GitHuba.

### Check na `main` i filtr ścieżek

Nazwa joba (`name: sonar-api` / `sonar-web`) jest tym, czego szukasz w branch protection. GitHub czasem pokazuje `api / sonar-api`. Zaznaczasz wpis, który już widzisz na PR, po pierwszym runie. Przed nim listy nie ma.

`api.yml` i `web.yml` mają `paths`. PR tylko z API nie uruchamia `web.yml`, więc check `sonar-web` nie powstaje. Required check, którego workflow nie wystartował, wisi i blokuje merge. To nie jest czerwony Sonar. Albo nie wymagasz checka, który na tym PR nie startuje, albo skan leci z workflow bez filtra ścieżek.

Sam Sonar nie dekoruje diffu komentarzem. Blokada merga to required status check z 03, ten sam mechanizm co check `api` i `web`.

### Gdy wynik jest zły

| Objaw | Co sprawdzić |
| --- | --- |
| Kontener `sonarqube` w restart loop | Log o `vm.max_map_count`. Sysctl z kroku 1, potem `compose up -d` jeszcze raz. |
| UI nie wchodzi z VM tuż po `up` | Za wcześnie. Poczekaj, aż log przestanie pisać o migracji. |
| UI z laptopa nie wchodzi, z VM wchodzi | Bind `127.0.0.1`. SSH `-L 9000`. |
| Job na `ubuntu-latest` — connection refused | `SONAR_HOST_URL` wskazuje loopback VM. Job ma być na `[self-hosted, jjdevhub]`. |
| Ten sam błąd na self-hosted | Runner nie jest procesem na VM (własny network namespace) albo Sonar nie słucha. Z konta usługi runnera: `curl -I http://127.0.0.1:9000`. |
| `begin` pada 401 | Zły sekret, token web w jobie API, projekt o tym kluczu nie istnieje. |
| Analiza weszła, coverage 0%, testy zielone | Glob Cobertury. `find TestResults -name coverage.cobertura.xml` na runnerze (albo lokalnie tą samą komendą). |
| Gate coverage pada na web od pierwszego PR | Oba projekty na Sonar way. Web potrzebuje gate bez progu coverage, dopóki nie ma lcov. |
| W UI nie ma zakładki PR | Community. Werdykt jest w checku, snapshot w UI jest ostatnim skanem. |
| Skan PR nadpisał to, co wczoraj było na main | Ten sam skutek jednej gałęzi. Następny skan `main` nadpisze z powrotem. |
| Migracje psują coverage | Brak `sonar.exclusions` w tym `begin`, który naprawdę leci. Plik properties w podkatalogu się nie liczy. |
| `end` pada, bo nie było `begin` | Build poszedł wcześniej albo katalog roboczy się zmienił w połowie joba. |
| Check `sonar-web` wisi na PR tylko z API | Workflow web się nie uruchomił (`paths`). To nie jest Fail gate. |
| Hasło admina po zmianie `.env` stare | `.env` jest od Postgresa. Hasło UI siedzi w volume. |

### Typowe pomyłki

1. **Sonar w `infra/docker/docker-compose.yml` huba.** Każdy deploy przebudowuje albo rusza JVM. Zostaje `/opt/sonarqube`.
2. **`:9000` w tunelu albo `admin`/`admin` na stałe.** Kod i historia analiz na zewnątrz.
3. **Jeden `projectKey` na API i Angulara.** Jeden coverage, pomieszane reguły. Dwa klucze, dwa tokeny.
4. **JDBC do `jjdevhub-db`.** Osobna baza, osobny volume, bez publikacji portu.
5. **Properties tylko pod `src/JJDevHub.Api`, begin w korzeniu.** Wykluczenia migracji nie działają. Coverage wygląda gorzej, niż jest.
6. **`sonar.sources` przy skanerze .NET.** Źródła daje build między begin a end.
7. **Build przed begin** albo test na artefaktach z joba hosted. Skaner nie widział tej kompilacji.
8. **Sonar way na web bez lcov.** Czerwony check z definicji. Najpierw gate bez coverage.
9. **`sonar.pullrequest.*` na Community.** Serwer odrzuca analizę. Check i tak bierze się z `qualitygate.wait` i kodu wyjścia.
10. **Brak `qualitygate.wait`.** Job zielony, gate w UI czerwony, merge przechodzi.
11. **Płytki checkout w jobie Sonara.** New code i autor issues się rozjeżdżają. `fetch-depth: 0` tylko tu.
12. **Token w `echo` albo w logu skanera skopiowany do issue.** GitHub maskuje sekret w akcji; nie drukuj go sam.
13. **Required `sonar-web` przy filtrze `paths`.** PR bez zmian we froncie nie dostaje checka i stoi.
14. **Oczekiwanie, że Sonar zastąpi CodeQL.** Inny silnik. Dziury danych zostają w 04, CVE w 05.
15. **Pływający tag `sonarqube:community` po tym, jak volume już żyje.** Przypnij wersję, zanim `pull` zrobi migrację w piątek.

### Oficjalne źródła

- SonarQube Server: instalacja Docker, `vm.max_map_count`, Quality Gate, definicja New Code.
- Porównanie edycji: analiza gałęzi i pull requestu od Developer Edition. Community analizuje jedną gałąź.
- SonarScanner for .NET: kolejność begin / build / test / end, raport Cobertura (`sonar.cs.cobertura.reportsPaths`), `sonar.qualitygate.wait`.
- SonarScanner CLI: `sonar.sources`, `sonar.exclusions`, zmienne `SONAR_TOKEN` i `SONAR_HOST_URL`.
- Coverlet: `--collect:"XPlat Code Coverage"` i plik `coverage.cobertura.xml`.

### Co zapamiętać

Sonar w tym układzie to Community na `127.0.0.1:9000`, poza Compose huba, z własnym Postgresem. Dwa projekty: API z Coberturą i web bez progu coverage, dopóki nie ma lcov. Skaner .NET musi objąć build. Gate blokuje merge tylko wtedy, gdy `qualitygate.wait=true` zrzuci czerwony kod na check, który na tym PR w ogóle wystartował. Osobnej zakładki pull requestu na tym obrazie nie będzie.
