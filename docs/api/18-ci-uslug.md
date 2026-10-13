# CI usług

Każda usługa ma własny workflow, który wywołuje jeden reużywalny `_dotnet-service.yml`: build, testy, obraz z `dotnet publish /t:PublishContainer`, skan Trivy i push do rejestru na lokalnej sieci. Deploy bierze obrazy po digeście, uruchamia migracje jako osobny krok i podnosi Compose wygenerowany przez Aspire z [14-aspire.md](14-aspire.md). Wszystkie joby działają na self-hosted runnerach. Obecny układ (runner `jjdevhub`, `deploy.yml`, cron jako zapas) jest opisany w [docs/CICD/03-github.md](../CICD/03-github.md). Ten numer go rozszerza, nie zastępuje.

## Po co

Dziś jest jeden `api.yml` dla jednego API. Przy pięciu usługach jeden workflow budowałby i testował wszystko przy każdej zmianie, a jeden czerwony test Notifications blokowałby deploy poprawki w News. eShop ma osobny pipeline na usługę. U nas to jeden reużywalny workflow i cienkie wywołania z filtrami `paths`. Zmiana w `src/services/News/**` uruchamia tylko News. Zmiana w `src/building-blocks/**` uruchamia wszystkie usługi, które z niego korzystają.

Obraz buduje się raz, w CI. Deploy nie przebudowuje go na serwerze, jak dziś robi `docker compose up --build` w [infra/ci/release-and-deploy.sh](../../infra/ci/release-and-deploy.sh). Na produkcję trafia dokładnie ten obraz, który przeszedł testy i skan, wskazany digestem `sha256:…`, a nie tagiem, który można nadpisać.

Runnery są dwa i mają różne poziomy zaufania. Build wykonuje kod z PR i potrzebuje Dockera dla Testcontainers. Dostęp do gniazda Dockera to w praktyce root na maszynie. Deploy ma sekrety produkcji. Dlatego nie mogą być tym samym procesem na tej samej maszynie.

## Klasy i ścieżki

| Plik | Odpowiedzialność |
| --- | --- |
| `.github/workflows/_dotnet-service.yml` | reużywalny: build, test, obraz, Trivy, push, digest |
| `.github/workflows/identity.yml`, `news.yml`, `users.yml`, `notifications.yml`, `gateway.yml` | wywołania z filtrami `paths` |
| `.github/workflows/building-blocks.yml` | testy `JJDevHub.*.Tests`, `actionlint`, `shellcheck` |
| `.github/workflows/contracts.yml` | `JJDevHub.Contracts.Tests` przy zmianie `*.Contracts` |
| `.github/workflows/apphost.yml` | `JJDevHub.AppHost.Tests`, na `main` i co noc |
| `.github/workflows/deploy.yml` | gate, publikacja Compose, migracje, `up`, smoke, zapis stanu |
| `.github/workflows/api.yml` | usunięty; jego kroki przejmuje `identity.yml` |
| `infra/ci/deploy-gate.mjs` | lista workflowów usług zamiast `api` |
| `infra/ci/release-and-deploy.sh` | obrazy po digeście, migracje, rollback |
| `infra/ci/trivy-image.sh` | skan dowolnego obrazu (z `trivy-api.sh`) |
| `infra/runner/Dockerfile` | obraz efemerycznego runnera build |
| `infra/runner/jjdevhub-build-runner.service` | systemd: pętla runnera `--ephemeral` |
| `.cursor/rules/github-workflows.mdc` | reguła przepisana pod workflowy usług |

## Tutorial

### 1. Runnery

| Runner | Etykiety | Gdzie | Co może |
| --- | --- | --- | --- |
| build | `[self-hosted, jjdevhub-build]` | osobna VM Proxmox `jjdevhub-build` | Docker, Testcontainers, push do rejestru; bez `/etc/jjdevhub` |
| deploy | `[self-hosted, jjdevhub]` | VM produkcji, jak dziś | `/etc/jjdevhub/*.env`, `docker compose`; tylko `main` |

Runner build jest efemeryczny. `infra/runner/Dockerfile` bazuje na oficjalnym obrazie `ghcr.io/actions/actions-runner` i dokłada SDK .NET z [global.json](../../global.json), Node 22 i Trivy. Usługa systemd uruchamia kontener w pętli: rejestracja z `--ephemeral`, jeden job, usunięcie, nowy kontener. Brudny stan po jobie z PR nie przechodzi do następnego. Token rejestracji (GitHub App albo PAT z uprawnieniem do runnerów) leży w `/etc/jjdevhub/runner.env` na VM build, poza repozytorium.

Kontener runnera dostaje gniazdo Dockera hosta VM build (`/var/run/docker.sock`). VM build nie ma dostępu do VM produkcji poza portem rejestru.

Repozytorium publiczne wymaga dwóch zabezpieczeń:

- Settings → Actions → General: „Require approval for all external contributors”,
- każdy job na self-hosted ma warunek `github.event.pull_request.head.repo.full_name == github.repository || github.event_name != 'pull_request'`. PR z forka nie dostaje runnera domowego.

### 2. Rejestr

`registry:2` na VM build, port `5000`, dostępny tylko z sieci LAN Proxmoxa. Adres to zmienna repozytorium `vars.REGISTRY` (np. `10.0.0.20:5000`), nie sekret. Obie VM mają go w `insecure-registries` Dockera albo dostają certyfikat z lokalnego CA. Wybór zapisuje [docs/CICD/01-proxmox.md](../CICD/01-proxmox.md) przy wdrożeniu.

Obrazy: `jjdevhub/identity-api`, `jjdevhub/news-api`, `jjdevhub/users-api`, `jjdevhub/notifications-worker`, `jjdevhub/gateway`, a do tego obrazy migracji `jjdevhub/<usługa>-migrations`.

### 3. Reużywalny workflow

`_dotnet-service.yml`, `on: workflow_call`. Wejścia:

| Wejście | Przykład dla News |
| --- | --- |
| `service` | `news` |
| `project` | `src/services/News/News.API/News.API.csproj` |
| `tests` | JSON: `["tests/News.Domain.Tests", "tests/News.Application.Tests", "tests/News.Infrastructure.Tests", "tests/News.API.Tests"]` |
| `image` | `jjdevhub/news-api` |
| `migrations` | JSON: `[{"context":"NewsWriteContext","project":"src/services/News/News.Infrastructure"}, {"context":"NewsReadContext","project":"src/services/News/News.Infrastructure"}]` |
| `sonar-key` | `jjdevhub-news` (opcjonalne) |

Wyjście: `digest` (obraz usługi) i `migrations-digest`.

Joby:

- `test` na `jjdevhub-build`, macierz po `tests`: `setup-dotnet` z [global.json](../../global.json), `restore`, `infra/ci/nuget-audit.sh`, `build -c Release`, `dotnet test` z coverage, artefakt wyników.
- `image` po `test`, tylko dla `push` na `main` i `workflow_dispatch`:
  - `dotnet publish {project} -c Release -t:PublishContainer -p:ContainerRepository={image} -p:ContainerImageTag=${{ github.sha }}` — obraz bez Dockerfile, z obrazu bazowego `mcr.microsoft.com/dotnet/aspnet` w wersji SDK,
  - `infra/ci/trivy-image.sh {image}:{sha}` z progiem `HIGH,CRITICAL`,
  - `docker push`, digest z `docker inspect --format '{{index .RepoDigests 0}}'`,
  - dla każdej migracji `dotnet ef migrations bundle --self-contained -r linux-x64 --context {context} --project {project}`, obraz `{image}-migrations` z bundlem, push, digest.
- `sonar` jak dziś w `api.yml`, gdy `sonar-key` jest podane, tylko na `main`.

Uprawnienia workflowu: `contents: read`. Do push w lokalnym rejestrze żaden sekret GitHuba nie jest potrzebny.

### 4. Workflow usługi

`news.yml`:

```yaml
name: news

on:
  pull_request:
    paths: &news-paths
      - "src/services/News/**"
      - "src/building-blocks/**"
      - "src/aspire/JJDevHub.ServiceDefaults/**"
      - "tests/News.*/**"
      - "Directory.Packages.props"
      - "Directory.Build.props"
      - "global.json"
      - ".github/workflows/news.yml"
      - ".github/workflows/_dotnet-service.yml"
  push:
    branches: [main]
    paths: *news-paths
  workflow_dispatch:

jobs:
  service:
    uses: ./.github/workflows/_dotnet-service.yml
    with:
      service: news
      project: src/services/News/News.API/News.API.csproj
      tests: '["tests/News.Domain.Tests","tests/News.Application.Tests","tests/News.Infrastructure.Tests","tests/News.API.Tests"]'
      image: jjdevhub/news-api
      migrations: '[{"context":"NewsWriteContext","project":"src/services/News/News.Infrastructure"},{"context":"NewsReadContext","project":"src/services/News/News.Infrastructure"}]'
```

Kotwice YAML (`&`, `*`) w `on.paths` działają w GitHub Actions. Jeśli `actionlint` w wersji z CI ich nie przyjmie, lista ścieżek jest powtórzona. Testy bibliotek budulcowych (`JJDevHub.*.Tests`) uruchamia osobny `building-blocks.yml` z tym samym reużywalnym workflow bez obrazu (`image` puste). Pozostałe usługi dostają analogiczne pliki. Consumer Users ma w `paths` także `src/services/Identity/Identity.Contracts/**`.

### 5. Deploy

`deploy.yml` startuje na `workflow_run` workflowów usług (`identity`, `news`, `users`, `notifications`, `gateway`, `web`) na `main`, jak dziś. Gate z [infra/ci/deploy-gate.mjs](../../infra/ci/deploy-gate.mjs) sprawdza, że każdy workflow, którego filtr `paths` obejmuje zmiany w SHA, jest zielony. Lista workflowów jest w skrypcie i w jego teście.

Job `deploy` na `[self-hosted, jjdevhub]`, `environment: production` (ochrona środowiska: tylko `main`, opcjonalnie wymagane zatwierdzenie):

- digesty obrazów z artefaktów wywołań `_dotnet-service.yml`; dla usługi, która w tym SHA się nie zmieniła, digest z `/var/lib/jjdevhub/images.env` (ostatni wdrożony),
- `aspire publish -o /var/lib/jjdevhub/compose` z parametrami obrazów (`news-api-image=…@sha256:…`); AppHost czyta je przez `builder.AddParameter` i `PublishAsDockerComposeService`, a w `aspire run` ich nie potrzebuje,
- migracje: `docker run --rm --network jjdevhub --env-file /etc/jjdevhub/news.env {news-api-migrations}@{digest} --connection "$NEWS_WRITE"` dla każdego kontekstu; błąd przerywa deploy przed podmianą usług,
- `docker compose -f /var/lib/jjdevhub/compose/docker-compose.yaml --env-file /etc/jjdevhub/compose.env up -d --wait --wait-timeout 180`,
- smoke: `curl` na `/health` gatewaya i każdej usługi HTTP w sieci `jjdevhub`, jak `wait_http` w obecnym skrypcie,
- sukces → zapis `/var/lib/jjdevhub/images.env` i SHA do `last-release-sha`,
- porażka smoke → `compose up` z poprzednim `images.env` i kod wyjścia 1.

Migracje są wstecznie zgodne (najpierw dodanie kolumny, usunięcie w kolejnym wydaniu), więc rollback obrazów nie wymaga rollbacku schematu.

Sekrety: `/etc/jjdevhub/<usługa>.env` (`identity.env`, `news.env`, `users.env`, `notifications.env`) i `/etc/jjdevhub/compose.env` (hasło Postgresa, ścieżka klucza podpisu). `release-and-deploy.sh` sprawdza, że wszystkie istnieją, zanim ruszy cokolwiek. Cron z [docs/CICD/03-github.md](../CICD/03-github.md) zostaje zapasem i woła ten sam skrypt bez argumentu.

### 6. Reguła workflowów

[.cursor/rules/github-workflows.mdc](../../.cursor/rules/github-workflows.mdc) w tym numerze zmienia treść:

- workflow na usługę wywołuje `_dotnet-service.yml`, z filtrem `paths`,
- build i testy na `jjdevhub-build`, deploy na `jjdevhub`, nigdy odwrotnie,
- `pull_request` z forka nie trafia na self-hosted,
- obraz na produkcję tylko po digeście,
- env serwera to `/etc/jjdevhub/<usługa>.env`.

### Co jest do czego

- `_dotnet-service.yml` — jedna definicja „jak budujemy usługę .NET”. Poprawka (np. nowa wersja Trivy) trafia do wszystkich usług naraz.
- workflow usługi — filtr `paths` i parametry. Decyduje, kiedy usługa się buduje.
- runner `jjdevhub-build` — wykonuje niezaufany kod z PR w kontenerze, który znika po jobie.
- runner `jjdevhub` — zaufany, ma sekrety, robi tylko deploy z `main`.
- rejestr lokalny — obrazy nie wychodzą z domowej sieci, push nie wymaga sekretu GitHuba.
- digest — gwarancja, że produkcja uruchamia przetestowany obraz.
- obraz migracji (`efbundle`) — schemat zmieniany przed startem nowej wersji, osobnym procesem, raz.
- `images.env` — stan „co jest wdrożone”. Podstawa rollbacku i deployu usług, które się nie zmieniły.
- gate — deploy rusza, gdy wszystko, czego dotyczy SHA, jest zielone.

## Testy

Projektu .NET w tym numerze nie ma. Sprawdzenia:

| Sprawdzenie | Gdzie | Wynik |
| --- | --- | --- |
| `actionlint` na `.github/workflows/*.yml` | job `lint` w `building-blocks.yml` | kod 0 |
| `node --test infra/ci/deploy-gate.test.mjs` | job `gate` w `deploy.yml` | nowe przypadki: zmiana tylko w News wymaga `news`, zmiana w `src/building-blocks` wymaga wszystkich usług |
| `shellcheck infra/ci/*.sh` | job `lint` | kod 0 |
| PR zmieniający tylko `src/services/News/**` | GitHub | uruchomiony tylko `news` (i `building-blocks`, jeśli dotyczy) |
| push na `main` | GitHub + VM | `deploy` zielony, `images.env` z digestami |
| smoke z celowo zepsutym `/health` na gałęzi testowej | VM staging albo ręczny `workflow_dispatch` | rollback do poprzednich digestów |

## Zasady

- Build i testy nie działają na runnerze produkcji. Deploy nie działa na runnerze build.
- PR z forka nie dostaje self-hosted runnera.
- Obraz na produkcję jest wskazany digestem. Serwer nie buduje obrazów.
- Migracje to osobny krok przed `up` i są wstecznie zgodne.
- Sekrety są w plikach `/etc/jjdevhub/*.env`. Workflow nie ma sekretów produkcji.
- Zmiana w `src/building-blocks/**` buduje wszystkie usługi.

## Poza zakresem

- Kubernetes, Argo CD, GitOps.
- Środowisko staging. Smoke i rollback na produkcji wystarczają przy jednym użytkowniku.
- Wydawanie paczek NuGet bibliotek budulcowych.
- Zmiana numerów `01`–`10` w [docs/CICD](../CICD/README.md). Ten plik dopisuje się do nich odnośnikiem.

## Źródła

- [Reusing workflows](https://docs.github.com/en/actions/sharing-automations/reusing-workflows) — `workflow_call`, wejścia, wyjścia.
- [Self-hosted runners: security](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners/about-self-hosted-runners#self-hosted-runner-security) — dlaczego PR z forka nie może trafić na runner domowy.
- [Ephemeral runners](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners/autoscaling-with-self-hosted-runners#using-ephemeral-runners-for-autoscaling) — `--ephemeral`, jeden job na runner.
- [actions/runner — obraz kontenera](https://github.com/actions/runner/pkgs/container/actions-runner) — oficjalna baza runnera.
- [Containerize a .NET app with dotnet publish](https://learn.microsoft.com/en-us/dotnet/core/containers/sdk-publish) — `PublishContainer` bez Dockerfile.
- [EF Core migration bundles](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying#bundles) — `efbundle` jako osobny krok wdrożenia.
- [Using environments for deployment](https://docs.github.com/en/actions/managing-workflow-runs-and-deployments/managing-deployments/managing-environments-for-deployment) — `production` i reguły ochrony.
- [Distribution registry](https://distribution.github.io/distribution/) — `registry:2` w sieci lokalnej.
- [dotnet/eShop — .github/workflows](https://github.com/dotnet/eShop/tree/main/.github/workflows) — pipeline per usługa.
- [actionlint](https://github.com/rhysd/actionlint) — statyczna kontrola workflowów.

## Gotowe gdy

- Zmiana tylko w News uruchamia `news.yml`, a zmiana w `src/building-blocks` uruchamia wszystkie usługi.
- Joby build działają na `jjdevhub-build` w kontenerze efemerycznym, a deploy na `jjdevhub`.
- Deploy podnosi obrazy po digeście z lokalnego rejestru, po udanych migracjach, i zapisuje `images.env`.
- Nieudany smoke przywraca poprzednie digesty.
- `actionlint`, `shellcheck` i `node --test infra/ci/deploy-gate.test.mjs` kończą się kodem 0.
