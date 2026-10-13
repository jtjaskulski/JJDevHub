# CI usług

Jeden workflow `ci.yml` wykrywa, które usługi zmienił commit, i dla każdej woła reużywalny `_dotnet-service.yml`: build, testy, obraz z `dotnet publish -t:PublishContainer`, skan Trivy i push do rejestru w sieci lokalnej. Deploy bierze obrazy po digeście, uruchamia migracje osobnym krokiem i podnosi Compose wygenerowany przez Aspire z [14-aspire.md](14-aspire.md). Ten numer przenosi też VM na nowy układ: nginx kieruje ruch na gateway, Prometheus zbiera metryki z pięciu usług, a ślady idą do Jaegera pod nowymi nazwami. Obecny układ (runner `jjdevhub`, `deploy.yml`, cron jako zapas, Sonar) jest opisany w [docs/CICD/03-github.md](../CICD/03-github.md) i [10-sonarqube.md](../CICD/10-sonarqube.md). Ten numer go rozszerza i na końcu wymienia, co w tamtych plikach trzeba zaktualizować.

## Po co

Dziś jest jeden `api.yml` dla jednego API i jeden kontener `api` na VM. Przy pięciu usługach jeden workflow budowałby wszystko przy każdej zmianie, a jeden czerwony test Notifications blokowałby deploy poprawki w News. Pięć osobnych workflowów z filtrami `paths` ma za to inny problem: wymagany check z workflowu, który się nie uruchomił, zostawia PR w stanie „Expected — waiting” na zawsze ([docs/CICD/03-github.md](../CICD/03-github.md) opisuje to przy `api` i `web`). Dlatego workflow jest jeden, uruchamia się zawsze, a w środku decyduje, które usługi budować. Wymagany check jest jeden: `ci`.

Obraz buduje się raz, w CI. Deploy nie przebudowuje go na serwerze, jak dziś robi `docker compose up --build` w [infra/ci/release-and-deploy.sh](../../infra/ci/release-and-deploy.sh). Na produkcję trafia dokładnie ten obraz, który przeszedł testy i skan, wskazany digestem `sha256:…`.

Do tego dochodzi przejście VM. [docs/CICD](../CICD/README.md) zna kontener `api` na `127.0.0.1:5080`, job Prometheusa `jjdevhub-api`, `service.name` `JJDevHub.Api` i jeden plik `/etc/jjdevhub/api.env`. Po tym numerze żadna z tych rzeczy nie jest prawdą, więc przejście musi być opisane krok po kroku, razem z przeniesieniem danych z obecnej bazy.

## Dlaczego ta technologia

### GitHub Actions i dwa rodzaje runnerów

Kod jest na GitHubie, workflow już istnieją, a Actions jest darmowe dla repozytoriów publicznych. Jenkins (trzymany poza drzewem) wymagałby osobnego serwera z wtyczkami. GitLab CI wymagałby przeniesienia repozytorium.

PR wykonuje cudzy albo niesprawdzony kod: `dotnet test` może zrobić wszystko, na co pozwala proces, a z gniazdem Dockera (Testcontainers) to w praktyce root na maszynie. GitHub odradza self-hosted runnery dla PR w repozytoriach publicznych, a [docs/CICD/03-github.md](../CICD/03-github.md) i [10-sonarqube.md](../CICD/10-sonarqube.md) trzymają PR na `ubuntu-latest`. Ten numer to zachowuje. `ubuntu-latest` ma Dockera, więc Testcontainers działa tam bez zmian.

Self-hosted runner jest potrzebny tam, gdzie hostowany nie dosięgnie: push obrazu do rejestru w domowej sieci i deploy na VM. To robi tylko kod z `main`, który przeszedł review. Są dwa runnery, bo mają inne uprawnienia:

- `jjdevhub-build` na osobnej VM: buduje obrazy z `main`, ma Dockera i rejestr, nie ma sekretów produkcji,
- `jjdevhub` na VM produkcji, jak dziś: deploy i Sonar, ma `/etc/jjdevhub/*.env`.

Gdyby obraz budował runner produkcji, kompilacja i testy `main` konkurowałyby o pamięć z działającymi usługami na VM, która i tak jest ciasna (sekcja „Zasoby VM”).

### Runner efemeryczny

Zwykły runner zostawia stan między jobami: pliki w katalogu roboczym, kontenery, zmienne. Flaga `--ephemeral` sprawia, że runner bierze jeden job i się wyrejestrowuje, a systemd stawia nowy kontener z czystego obrazu. Kod z `main` jest zaufany, ale czysty start usuwa całą klasę błędów „u mnie działa, bo został plik z poprzedniego joba”. Actions Runner Controller robi to samo na Kubernetes. Na jednej VM pętla systemd wystarcza.

### Jeden `ci.yml` z wykrywaniem zmian zamiast filtrów `paths`

Filtr `paths` decyduje, czy workflow w ogóle startuje. Pominięty workflow nie raportuje statusu, więc nie może być wymaganym checkiem. Wykrywanie zmian wewnątrz workflowu odwraca to: `ci.yml` startuje zawsze, job `changes` liczy listę usług z `git diff`, macierz buduje tylko je, a job `ci` na końcu raportuje jeden status. Pominięta usługa to pominięty job w macierzy, nie brak checka.

Do wykrywania jest gotowa akcja `dorny/paths-filter` (MIT). Własny skrypt `infra/ci/changed-services.mjs` wygrywa, bo repo ma już ten wzór (`deploy-gate.mjs` z testem `node --test`), mapa usług jest w jednym pliku JSON, którego używa też deploy, a reguła „zmiana w `src/building-blocks/**` buduje wszystko” jest testowana.

### Reużywalny workflow

Pięć usług z tym samym przepisem (restore, audit, build, test, obraz, skan) to jedna definicja z parametrami. `workflow_call` z macierzą daje osobny job na usługę w widoku GitHuba. Composite action działa na poziomie kroków, więc nie da osobnych jobów `test` i `image` ani różnych `runs-on` w jednym przepisie.

### Obraz z `dotnet publish` zamiast Dockerfile

SDK .NET od wersji 8 buduje obraz OCI sam (`-t:PublishContainer`): bierze obraz bazowy `aspnet` w wersji SDK, ustawia użytkownika bez roota, port i etykiety. Dockerfile na usługę to pięć plików, w których łatwo zapomnieć o `USER` albo podbiciu bazy. Dockerfile zostaje tam, gdzie SDK .NET nie ma zastosowania: klient web ma swój `web/Dockerfile`.

### Lokalny rejestr i digest

GHCR byłby wygodny, ale VM produkcji potrzebowałaby tokenu do pobierania, a obrazy wychodziłyby z domowej sieci i wracały. `registry:2` (Distribution, Apache 2.0) w LAN-ie nie wymaga sekretu GitHuba. Tag można nadpisać, digest `sha256:…` nie: to skrót zawartości. Wdrożenie po digeście gwarantuje, że produkcja uruchamia ten obraz, który przeszedł testy i Trivy.

### Migracje jako osobny krok

`Database.Migrate()` przy starcie jest wygodne w dev (`DevelopmentMigrator` z [16-usluga-news.md](16-usluga-news.md)). Na produkcji dwie repliki migrują naraz, usługa potrzebuje uprawnień DDL, a nieudana migracja to pętla restartów. `efbundle` to samodzielny plik wykonywalny z migracjami, uruchamiany raz przed `compose up`. Błąd zatrzymuje deploy, zanim stara wersja zniknie.

### Trivy

Trivy (Apache 2.0) skanuje obraz pod kątem znanych podatności. Już jest w [infra/ci/trivy-api.sh](../../infra/ci/trivy-api.sh). Grype (Apache 2.0) jest równoważny. Zostajemy przy tym, co działa.

### Telemetria zostaje, zmienia się adresacja

Prometheus, Jaeger i Grafana z [docs/CICD/07](../CICD/07-prometheus.md)–[09](../CICD/09-grafana.md) zostają. Aspire na serwerze ich nie zastępuje: dashboard Aspire nie ma retencji ani alertów. Zmienia się to, co oni widzą: pięć celów zamiast jednego, nazwy usług z Aspire i endpoint śladów w `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` zamiast wspólnego. Kod po stronie usług (endpointy na sygnał, filtr śladów, mierniki) jest w [14-aspire.md](14-aspire.md).

## Klasy i ścieżki

| Plik | Odpowiedzialność |
| --- | --- |
| `.github/workflows/ci.yml` | `changes` → macierz `service` → `contracts`, `building-blocks`, `lint` → `ci` (jedyny wymagany check .NET) |
| `.github/workflows/_dotnet-service.yml` | reużywalny: test, obraz, Trivy, push, artefakt z digestem |
| `.github/workflows/apphost.yml` | `JJDevHub.AppHost.Tests` co noc i ręcznie |
| `.github/workflows/deploy.yml` | gate, publikacja Compose, migracje, `up`, smoke, zapis stanu |
| `.github/workflows/api.yml` | usunięty; kroki przejmuje `ci.yml` |
| `.github/workflows/web.yml` | bez zmian |
| `infra/ci/services.json` | usługa → projekt, testy, obraz, migracje, ścieżki, klucz Sonara |
| `infra/ci/changed-services.mjs`, `changed-services.test.mjs` | lista usług z `git diff` |
| `infra/ci/deploy-gate.mjs`, `deploy-gate.test.mjs` | wymagane workflowy: `ci` i `web` |
| `infra/ci/release-and-deploy.sh` | obrazy po digeście, migracje, smoke, rollback |
| `infra/ci/trivy-image.sh` | skan dowolnego obrazu (z `trivy-api.sh`) |
| `infra/ci/migrate-from-single-api.sh` | jednorazowe przejście VM: dump `jjdevhub`, przeniesienie env |
| `infra/runner/Dockerfile`, `infra/runner/jjdevhub-build-runner.service` | efemeryczny runner build |
| `infra/docker/docker-compose.yml` | zostają `web`, `prometheus`, `jaeger`, `grafana`; znikają `db` i `api`; sieć `jjdevhub` jako zewnętrzna |
| `src/Clients/web/nginx.conf` | `/api/`, `/.well-known/`, `/health` → `gateway:8080` |
| `infra/prometheus/prometheus.yml` | job na usługę |
| `infra/grafana/dashboards/jjdevhub-services.json` | zastępuje `jjdevhub-api-overview.json` |
| `src/aspire/JJDevHub.AppHost/AppHost.cs` | parametry obrazów i endpointu śladów w trybie publikacji, porty, limity pamięci |
| `.cursor/rules/github-workflows.mdc` | reguła przepisana pod `ci.yml` |

## Plan

### 1. Zasoby VM

[docs/CICD/01-proxmox.md](../CICD/01-proxmox.md) daje VM 4096 MB. Po tym numerze na VM produkcji działają:

| Kontener | Limit pamięci |
| --- | --- |
| `postgres` | 512 MB |
| `kafka` (`KAFKA_HEAP_OPTS=-Xms256m -Xmx512m`) | 768 MB |
| `identity-api`, `news-api`, `users-api`, `gateway`, `notifications-worker` | po 256 MB |
| `web` (nginx) | 64 MB |
| `prometheus` | 384 MB |
| `jaeger` (`max_traces: 10000`) | 256 MB |
| `grafana` | 256 MB |
| `sonarqube` + `sonarqube-db` ([10-sonarqube.md](../CICD/10-sonarqube.md)) | około 2,5 GB |

Razem około 6,5 GB plus system i Docker. Minimum to 8 GB, zalecane 12 GB. VM `jjdevhub-build`: 4 GB i 2 vCPU, bo testy `main` uruchamiają Postgresa i Kafkę w Testcontainers. Limity ustawia AppHost w trybie publikacji (callback `PublishAsDockerComposeService`, nazwy właściwości według wersji Aspire z `Directory.Packages.props`). .NET w kontenerze z limitem sam dopasowuje stertę GC.

Kafka UI z `WithKafkaUI()` działa tylko w dev i nie trafia do publikacji.

### 2. Runnery

| Runner | Etykiety | Gdzie | Joby |
| --- | --- | --- | --- |
| hostowany | `ubuntu-latest` | GitHub | wszystkie joby PR: `changes`, testy, `contracts`, `building-blocks`, `lint`, `ci` |
| build | `[self-hosted, jjdevhub-build]` | VM Proxmox `jjdevhub-build` | na `push` do `main`: testy i obraz |
| produkcja | `[self-hosted, jjdevhub]` | VM produkcji, jak dziś | na `push` do `main`: Sonar; `deploy.yml` |

`runs-on` w `_dotnet-service.yml` to wyrażenie: `github.event_name == 'pull_request' && 'ubuntu-latest' || 'jjdevhub-build'` z etykietą `self-hosted`. Żaden job wywołany przez `pull_request` nie trafia na self-hosted. To jest warunek, nie konwencja: test `actionlint` z regułą w `lint` sprawdza, że każde `runs-on` z `self-hosted` ma obok `if` z `github.event_name != 'pull_request'` albo wyrażenie wyżej.

Runner build: `infra/runner/Dockerfile` na bazie `ghcr.io/actions/actions-runner` z SDK .NET z [global.json](../../global.json), Node 22 i Trivy. Systemd uruchamia kontener w pętli z `--ephemeral`. Token rejestracji (GitHub App albo PAT z uprawnieniem do runnerów) leży w `/etc/jjdevhub/runner.env` na VM build. Kontener dostaje gniazdo Dockera VM build. VM build widzi VM produkcji tylko na porcie rejestru.

### 3. Rejestr

`registry:2` na VM build, port `5000`, tylko z LAN-u Proxmoxa. Adres to zmienna repozytorium `vars.REGISTRY` (np. `10.0.0.20:5000`), nie sekret. Obie VM mają go w `insecure-registries` Dockera albo dostają certyfikat z lokalnego CA.

Obrazy: `jjdevhub/identity-api`, `jjdevhub/news-api`, `jjdevhub/users-api`, `jjdevhub/notifications-worker`, `jjdevhub/gateway` oraz `jjdevhub/<usługa>-migrations`.

### 4. Mapa usług i wykrywanie zmian

`infra/ci/services.json`:

```json
{
  "shared": ["src/building-blocks/**", "src/aspire/JJDevHub.ServiceDefaults/**", "Directory.Packages.props", "Directory.Build.props", "global.json", ".github/workflows/ci.yml", ".github/workflows/_dotnet-service.yml"],
  "services": {
    "news": {
      "paths": ["src/services/News/**", "tests/News.*/**"],
      "project": "src/services/News/News.API/News.API.csproj",
      "tests": ["tests/News.Domain.Tests", "tests/News.Application.Tests", "tests/News.Infrastructure.Tests", "tests/News.API.Tests"],
      "image": "jjdevhub/news-api",
      "migrations": [
        { "context": "NewsWriteContext", "project": "src/services/News/News.Infrastructure" },
        { "context": "NewsReadContext", "project": "src/services/News/News.Infrastructure" }
      ],
      "sonarKey": "jjdevhub-news"
    }
  }
}
```

Pozostałe usługi analogicznie. Identity zachowuje `sonarKey` `jjdevhub-api`, żeby historia analizy z [10-sonarqube.md](../CICD/10-sonarqube.md) nie przepadła. Users ma w `paths` także `src/services/Identity/Identity.Contracts/**`, Notifications oba `*.Contracts`.

`changed-services.mjs` dostaje bazę porównania (`github.event.pull_request.base.sha` albo `github.event.before`) i wypisuje `services=["news"]` do `GITHUB_OUTPUT`. Zmiana pliku z `shared` zwraca wszystkie usługi. `workflow_dispatch` i pierwszy push gałęzi (baza `0000…`) też.

### 5. `ci.yml`

`on: pull_request`, `push` na `main`, `workflow_dispatch`. Bez `paths`.

- `changes` — checkout z `fetch-depth: 0`, `node --test infra/ci/changed-services.test.mjs`, potem `changed-services.mjs`,
- `service` — `needs: changes`, `if: needs.changes.outputs.services != '[]'`, `strategy.matrix.service: ${{ fromJson(needs.changes.outputs.services) }}`, `uses: ./.github/workflows/_dotnet-service.yml` z `service: ${{ matrix.service }}`,
- `building-blocks` — testy `tests/JJDevHub.*.Tests` bez obrazu, gdy zmienił się `shared`,
- `contracts` — `tests/JJDevHub.Contracts.Tests`, gdy zmieniły się `*.Contracts`,
- `lint` — `actionlint`, `shellcheck infra/ci/*.sh`, `infra/ci/observability-smoke.sh` (zostaje z `api.yml`),
- `ci` — `needs` wszystkich powyżej, `if: always()`, kończy się błędem, gdy którykolwiek wynik to `failure` albo `cancelled`. `skipped` jest w porządku.

Branch protection: wymagane checki `ci` i `web` zamiast `api` i `web`.

### 6. Reużywalny workflow

`_dotnet-service.yml`, `on: workflow_call`, wejście `service`. Pierwszy krok czyta parametry usługi z `services.json` (`jq`), więc wywołanie ma jedno wejście.

- `test` — macierz po `tests`: `setup-dotnet` z [global.json](../../global.json), `restore`, `infra/ci/nuget-audit.sh`, `build -c Release`, `dotnet test` z coverage, artefakt wyników,
- `image` — po `test`, tylko na `push` do `main` i `workflow_dispatch`, na `jjdevhub-build`:
  - `dotnet publish {project} -c Release -t:PublishContainer -p:ContainerRepository={image} -p:ContainerImageTag=${{ github.sha }}`,
  - `infra/ci/trivy-image.sh {image}:{sha}` z progiem `HIGH,CRITICAL`,
  - `docker push`, digest z `docker inspect --format '{{index .RepoDigests 0}}'`,
  - dla każdej migracji `dotnet ef migrations bundle --self-contained -r linux-x64 --context {context} --project {project}`, obraz `{image}-migrations`, push, digest,
  - artefakt `digest-{service}` z dwiema liniami `image=` i `migrations=`,
- `sonar` — po `test`, tylko na `push` do `main`, na `[self-hosted, jjdevhub]`, bo SonarQube słucha na `127.0.0.1:9000` VM produkcji. Klucz projektu z `sonarKey`. Token: jeden globalny token analizy `SONAR_TOKEN_SERVICES` dla usług .NET zamiast tokenu na projekt. `SONAR_TOKEN_WEB` zostaje dla `web.yml`.

Uprawnienia workflowu: `contents: read`, `actions: read`. Push do lokalnego rejestru nie wymaga sekretu GitHuba.

### 7. Telemetria na VM

Zmiany w plikach z [docs/CICD](../CICD/README.md), wdrażane razem z pierwszym deployem nowego układu:

- `infra/prometheus/prometheus.yml` — zamiast `jjdevhub-api` jeden job na usługę: `job_name` równe nazwie zasobu (`identity-api`, `news-api`, `users-api`, `notifications-worker`, `gateway`), cel `<nazwa>:8080`, `metrics_path: /metrics`, `scrape_interval: 15s`. Prometheus jest w sieci `jjdevhub` razem z usługami z Compose Aspire.
- `infra/grafana/dashboards/jjdevhub-services.json` — zmienna `service` z `label_values(up, job)`, panele: `up{job=~"$service"}`, tempo i p95 z `http_server_request_duration_seconds_*` po `http_route`, `jjdevhub_projection_lag`, `jjdevhub_outbox_pending`, `jjdevhub_kafka_consumer_lag`, `rate(jjdevhub_kafka_dlq_total[5m])`. Stary `jjdevhub-api-overview.json` znika, jego `uid` przestaje istnieć.
- Ślady: AppHost w trybie publikacji ustawia każdej usłudze `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` z parametru `otlp-traces-endpoint` (`http://jaeger:4317` w `compose.env`). `OTEL_EXPORTER_OTLP_ENDPOINT` zostaje puste, więc metryki i logi nie idą do Jaegera ([14-aspire.md](14-aspire.md), krok 3a). `OTEL_SERVICE_NAME` to nazwa zasobu. W Jaegerze i w Explore Grafany usługi to `identity-api`, `news-api`… zamiast `JJDevHub.Api`.
- `max_traces: 10000` w [infra/jaeger/config.yaml](../../infra/jaeger/config.yaml) zostaje. Filtr `/metrics` i `/health` oraz sampler z 14 utrzymują ruch tła blisko zera.

### 8. Porty i nginx

W wygenerowanym Compose port na hoście ma tylko gateway: `127.0.0.1:5080:8080`, ten sam co dziś API. Skrypt deployu i ręczny `curl 127.0.0.1:5080/health` działają dalej, a `/metrics` usług nie jest widoczne z LAN-u ([docs/CICD/07-prometheus.md](../CICD/07-prometheus.md)). Postgres: `127.0.0.1:5433:5432`, jak dziś `db`. Kafka bez portu na hoście. AppHost ustawia to w callbacku publikacji. `aspire publish` w CI kończy się `grep` na wygenerowanym pliku: każdy wpis `ports:` zaczyna się od `127.0.0.1:`, inaczej job pada.

[src/Clients/web/nginx.conf](../../src/Clients/web/nginx.conf): `location /api/`, `location /.well-known/` i `location = /health` kierują na `http://gateway:8080`. `/openapi/` i `/scalar` znikają, bo w produkcji i tak nie działały ([docs/CICD/02-cloudflare-tunnel.md](../CICD/02-cloudflare-tunnel.md)). Tunel Cloudflare się nie zmienia: origin `127.0.0.1:4200`, hostname'y Grafany, Jaegera i Prometheusa bez zmian.

### 9. Sekrety i env

| Plik na VM | Zawartość | Skąd |
| --- | --- | --- |
| `/etc/jjdevhub/compose.env` | hasło Postgresa, `GRAFANA_ADMIN_*`, `GF_SERVER_ROOT_URL`, `--web.external-url` Prometheusa, `otlp-traces-endpoint` | `POSTGRES_*`, `GRAFANA_*`, adresy z `api.env` |
| `/etc/jjdevhub/identity.env` | `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKeyPath=/etc/jjdevhub/identity-signing.pem` | `JWT_ISSUER`, `JWT_AUDIENCE` z `api.env`; `JWT_KEY` znika |
| `/etc/jjdevhub/news.env`, `users.env`, `notifications.env` | `Identity__Url`, ustawienia usług | nowe |
| `/etc/jjdevhub/identity-signing.pem` | klucz RSA, `chmod 600` | [15-identity.md](15-identity.md) |

`OTEL_*` znikają z env: ustawia je AppHost. `/etc/jjdevhub/api.env` zostaje do udanego pierwszego deployu jako punkt powrotu, potem jest usuwany. `release-and-deploy.sh` sprawdza, że wszystkie nowe pliki istnieją, zanim ruszy cokolwiek. Workflow nie zawiera sekretów produkcji.

### 10. Przejście z jednego API

Jednorazowo, ręcznie, przez `infra/ci/migrate-from-single-api.sh` na VM produkcji:

- zatrzymanie crona i `deploy.yml` (wyłączenie workflowu w GitHubie),
- `pg_dump -Fc jjdevhub` z kontenera `jjdevhub-db` do `/var/lib/jjdevhub/backup/`,
- rozbicie `/etc/jjdevhub/api.env` na pliki z kroku 9 (skrypt wypisuje, co przeniósł, i nie kasuje źródła),
- pierwszy deploy nowego układu bez usług (`postgres`, `kafka`), `pg_restore` do bazy `jjdevhub` w nowym Postgresie,
- pełny deploy, smoke, włączenie crona i `deploy.yml`.

Stary kontener `jjdevhub-db` i jego volume zostają zatrzymane, nie usunięte, do czasu potwierdzenia, że logowanie działa na starych kontach.

### 11. Deploy

`deploy.yml` startuje na `workflow_run` workflowów `ci` i `web` na `main`. Gate z [infra/ci/deploy-gate.mjs](../../infra/ci/deploy-gate.mjs) wymaga zielonego `ci` dla SHA i zielonego `web`, gdy zmiany dotykają klienta web. Lista jest w skrypcie i jego teście.

Job `deploy` na `[self-hosted, jjdevhub]`, `environment: production` (tylko `main`):

- digesty z artefaktów `digest-*` przebiegu `ci`; dla usługi, której SHA nie zmienił, digest z `/var/lib/jjdevhub/images.env`,
- `aspire publish -o /var/lib/jjdevhub/compose` z parametrami obrazów (`news-api-image=…@sha256:…`),
- migracje: `docker run --rm --network jjdevhub --env-file /etc/jjdevhub/news.env {news-api-migrations}@{digest} --connection "$NEWS_WRITE"` dla każdego kontekstu; błąd przerywa deploy,
- `docker compose -f /var/lib/jjdevhub/compose/docker-compose.yaml --env-file /etc/jjdevhub/compose.env up -d --wait --wait-timeout 180`,
- `docker compose -f infra/docker/docker-compose.yml --env-file /etc/jjdevhub/compose.env up -d` dla `web` i telemetrii,
- smoke jak `wait_http` w obecnym skrypcie: `127.0.0.1:5080/health` (gateway), `127.0.0.1:4200/health` (nginx → gateway), `127.0.0.1:16686/`, `127.0.0.1:9090/api/v1/targets` z pięcioma celami `up`,
- sukces → zapis `images.env` i SHA do `last-release-sha`,
- porażka → `compose up` z poprzednim `images.env` i kod wyjścia 1.

Migracje są wstecznie zgodne (dodanie kolumny teraz, usunięcie w kolejnym wydaniu), więc rollback obrazów nie wymaga rollbacku schematu. Cron z [docs/CICD/03-github.md](../CICD/03-github.md) zostaje zapasem i woła ten sam skrypt.

### 12. Aktualizacja docs/CICD i reguły

Pliki w [docs/CICD](../CICD/README.md) opisują stan przed tym numerem i zostają prawdziwe do jego wdrożenia. Przy wdrożeniu tego numeru dostają aktualizację:

| Plik | Zmiana |
| --- | --- |
| [01-proxmox.md](../CICD/01-proxmox.md) | RAM VM produkcji 8–12 GB; druga VM `jjdevhub-build` z rejestrem |
| [03-github.md](../CICD/03-github.md) | wymagane checki `ci` i `web`; `deploy.yml` na `ci` |
| [06-opentelemetry.md](../CICD/06-opentelemetry.md) | kod OTel w ServiceDefaults; ślady przez `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` |
| [07-prometheus.md](../CICD/07-prometheus.md) | job na usługę, cele `<zasób>:8080` |
| [08-jaeger.md](../CICD/08-jaeger.md) | nazwy usług z Aspire |
| [09-grafana.md](../CICD/09-grafana.md) | dashboard `jjdevhub-services` |
| [10-sonarqube.md](../CICD/10-sonarqube.md) | klucz projektu na usługę, `SONAR_TOKEN_SERVICES` |

[.cursor/rules/github-workflows.mdc](../../.cursor/rules/github-workflows.mdc) dostaje nową treść:

- jeden `ci.yml` bez `paths`, wymagane checki `ci` i `web`,
- joby `pull_request` tylko na `ubuntu-latest`,
- obraz na `jjdevhub-build`, deploy i Sonar na `jjdevhub`, tylko z `main`,
- obraz na produkcję tylko po digeście,
- env serwera to `/etc/jjdevhub/<usługa>.env` i `compose.env`.

### Co jest do czego

- `ci.yml` — zawsze startuje, więc zawsze raportuje `ci`. Wymagany check nie wisi.
- `changed-services.mjs` i `services.json` — jedno miejsce z wiedzą, która ścieżka należy do której usługi. Używa go CI i deploy.
- `_dotnet-service.yml` — jedna definicja „jak budujemy usługę .NET”. Poprawka trafia do wszystkich usług naraz.
- `ubuntu-latest` dla PR — niezaufany kod nie dotyka domowej sieci.
- runner `jjdevhub-build` — buduje obrazy `main` w kontenerze, który znika po jobie, z dala od pamięci produkcji.
- runner `jjdevhub` — ma sekrety, robi deploy i Sonar, tylko z `main`.
- rejestr lokalny i digest — produkcja uruchamia przetestowany obraz, a obrazy nie wychodzą z sieci.
- obraz migracji (`efbundle`) — schemat zmieniany przed startem nowej wersji, raz.
- `images.env` — stan „co jest wdrożone”. Podstawa rollbacku.
- job Prometheusa na usługę i dashboard `jjdevhub-services` — widać, która usługa leży i gdzie rośnie lag.
- `migrate-from-single-api.sh` — jedyne miejsce, które zna stary układ. Po przejściu można go usunąć.

## Testy

Projektu .NET w tym numerze nie ma. Sprawdzenia:

| Sprawdzenie | Gdzie | Wynik |
| --- | --- | --- |
| `node --test infra/ci/changed-services.test.mjs` | job `changes` | zmiana tylko w `src/services/News/**` daje `["news"]`; zmiana w `src/building-blocks/**` daje wszystkie; zmiana w `Identity.Contracts` daje `identity`, `users`, `notifications`; brak zmian .NET daje `[]` |
| `node --test infra/ci/deploy-gate.test.mjs` | job `gate` w `deploy.yml` | wymaga `ci`; `web` tylko przy zmianach klienta |
| `actionlint` | job `lint` | kod 0 |
| `shellcheck infra/ci/*.sh` | job `lint` | kod 0 |
| porty w wygenerowanym Compose | po `aspire publish` | każdy `ports:` zaczyna się od `127.0.0.1:` |
| PR zmieniający tylko `src/services/News/**` | GitHub | `service (news)` na `ubuntu-latest`, check `ci` zielony, brak jobów self-hosted |
| PR zmieniający tylko `README.md` | GitHub | `service` pominięty, check `ci` zielony |
| push na `main` | GitHub + VM | obraz z `jjdevhub-build`, `deploy` zielony, `images.env` z digestami |
| Prometheus po deployu | `127.0.0.1:9090/api/v1/targets` | pięć celów `up` |
| Jaeger po `POST /api/articles` | `127.0.0.1:16686` | ślad `gateway` → `news-api` → `publish news.integration` → `notifications-worker` |
| smoke z celowo zepsutym `/health` | ręczny `workflow_dispatch` z gałęzi testowej | rollback do poprzednich digestów |

## Zasady

- Joby `pull_request` nie działają na self-hosted runnerach.
- Build i testy `main` nie działają na runnerze produkcji. Deploy nie działa na runnerze build.
- Wymagany check .NET jest jeden: `ci`. Workflow z `paths` nie może być wymagany.
- Obraz na produkcję jest wskazany digestem. Serwer nie buduje obrazów.
- Migracje to osobny krok przed `up` i są wstecznie zgodne.
- Na hoście porty są tylko na `127.0.0.1`. Z usług port ma tylko gateway.
- Sekrety są w `/etc/jjdevhub/*.env`. Workflow nie ma sekretów produkcji.
- Zmiana w `src/building-blocks/**` buduje wszystkie usługi.

## Poza zakresem

- Kubernetes, Argo CD, GitOps.
- Środowisko staging. Smoke i rollback na produkcji wystarczają przy jednym użytkowniku.
- Backend logów (Loki, OpenSearch) i Alertmanager.
- Wydawanie paczek NuGet bibliotek budulcowych.
- Przeniesienie SonarQube na osobną VM. Przy 8 GB na VM produkcji to pierwsza rzecz do rozważenia.

## Źródła

- [Reusing workflows](https://docs.github.com/en/actions/sharing-automations/reusing-workflows) — `workflow_call`, macierz z reużywalnym workflow.
- [Troubleshooting required status checks](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/collaborating-on-repositories-with-code-quality-features/troubleshooting-required-status-checks) — dlaczego pominięty workflow blokuje PR.
- [Self-hosted runners: security](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners/about-self-hosted-runners#self-hosted-runner-security) — dlaczego PR nie trafia na runner domowy.
- [Ephemeral runners](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners/autoscaling-with-self-hosted-runners#using-ephemeral-runners-for-autoscaling) — `--ephemeral`, jeden job na runner.
- [actions/runner — obraz kontenera](https://github.com/actions/runner/pkgs/container/actions-runner) — oficjalna baza runnera.
- [Containerize a .NET app with dotnet publish](https://learn.microsoft.com/en-us/dotnet/core/containers/sdk-publish) — `PublishContainer` bez Dockerfile.
- [EF Core migration bundles](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying#bundles) — `efbundle` jako osobny krok wdrożenia.
- [Using environments for deployment](https://docs.github.com/en/actions/managing-workflow-runs-and-deployments/managing-deployments/managing-environments-for-deployment) — `production` i reguły ochrony.
- [Distribution registry](https://distribution.github.io/distribution/) — `registry:2` w sieci lokalnej.
- [Prometheus configuration](https://prometheus.io/docs/prometheus/latest/configuration/configuration/) — `scrape_configs` na usługę.
- [Kafka: memory](https://kafka.apache.org/documentation/#java) — `KAFKA_HEAP_OPTS` i rozmiar sterty brokera.
- [actionlint](https://github.com/rhysd/actionlint) — statyczna kontrola workflowów.

## Gotowe gdy

- PR dotykający tylko News uruchamia `service (news)` na `ubuntu-latest`, a check `ci` jest zielony bez jobów self-hosted.
- Zmiana w `src/building-blocks/**` buduje wszystkie usługi.
- Push na `main` buduje obrazy na `jjdevhub-build`, a deploy podnosi je po digeście po udanych migracjach i zapisuje `images.env`.
- Na VM nginx kieruje `/api/` na gateway, a Prometheus pokazuje pięć celów `up`.
- W Jaegerze są ślady pod nazwami zasobów, bez śladów `/metrics` i `/health`.
- Nieudany smoke przywraca poprzednie digesty.
- `node --test infra/ci/changed-services.test.mjs`, `node --test infra/ci/deploy-gate.test.mjs`, `actionlint` i `shellcheck` kończą się kodem 0.
