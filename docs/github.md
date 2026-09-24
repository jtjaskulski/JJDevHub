# GitHub — deploy na VM

Push na `main` ma przebudować Compose na VM z [proxmox.md](proxmox.md). Aplikacja jest wtedy dostępna tunelem z [cloudflare-tunnel.md](cloudflare-tunnel.md).

Zrób to **self-hosted runnerem** na tej samej VM. Runner sam wychodzi do GitHuba, więc do domu nie musi wchodzić webhook.

Godzinny cron zostaje jako zapas, gdy runner jest offline. Nie stawiaj nasłuchu webhooka na VM i nie uruchamiaj deployu na `ubuntu-latest`: hosted runner nie ma Twojego `/etc/jjdevhub/api.env` i nie buduje obrazów na Proxmoxie.

## Co już jest w repo

Dwa workflowy tylko budują i testują, bez sekretów produkcyjnych:

- [`.github/workflows/api.yml`](../.github/workflows/api.yml) — restore, build, test, `docker build` API
- [`.github/workflows/web.yml`](../.github/workflows/web.yml) — `pnpm` test/build i obraz Angulara

Oba startują na PR i na pushu do `main`, ale z filtrem ścieżek. Push, który nie tyka API, nie uruchomi `api`. To samo dotyczy `web`.

Cron co godzinę, jeśli `origin/main` zmienił SHA:

- [infra/ci/jjdevhub-release.cron](../infra/ci/jjdevhub-release.cron)
- [infra/ci/release-and-deploy.sh](../infra/ci/release-and-deploy.sh)

Skrypt robi `git fetch origin main`, przy nowym SHA tworzy lokalną gałąź `release/YYYY-MM-DD.N` i `docker compose up -d --build` z `--env-file /etc/jjdevhub/api.env`. Stan zapisuje w `/var/lib/jjdevhub/last-release-sha`.

Ograniczenia crona: deploy czeka do 59 minut i poleci nawet, gdy `api` albo `web` na GitHubie jest czerwony.

## 1. Branch protection

GitHub → **Settings** → **Branches** → reguła dla `main`:

- **Require a pull request before merging**
- **Require status checks to pass**: `api` i `web`

Check pojawia się na liście dopiero po pierwszym uruchomieniu danego workflowu. Dopóki push nie dotknie ścieżek z filtra, checka nie będzie — wtedy wymagaj tylko tych, które już raz przeszły, i dopisz drugi po pierwszym zielonym runie.

## 2. Runner na VM

GitHub → **Settings** → **Actions** → **Runners** → **New self-hosted runner**. System: Linux, architektura x64. Skopiuj stamtąd `mkdir`, `curl` i `tar` — wersja archiwum zmienia się w panelu, nie trzymaj jej w tym pliku.

Na VM jako ten sam użytkownik, który jest w grupie `docker` i właścicielem `/opt/jjdevhub`:

```bash
mkdir -p ~/actions-runner && cd ~/actions-runner
# wklej polecenia Download i tar z panelu GitHuba
./config.sh --url https://github.com/jtjaskulski/JJDevHub --token TOKEN_Z_PANELU --labels jjdevhub --name jjdevhub
sudo ./svc.sh install "$USER"
sudo ./svc.sh start
```

Token z kreatora jest jednorazowy. Etykieta `jjdevhub` jest tą, której użyje przyszły workflow. Usługa ma chodzić na użytkowniku deployu, nie na root, żeby `docker` i `git` w `/opt/jjdevhub` były te same co przy ręcznym `compose`.

Sprawdzenie: runner w panelu ma status **Idle**.

## 3. Docelowy workflow (jeszcze go nie ma)

Plik `.github/workflows/deploy.yml` powstanie w następnym kroku. Kształt:

- trigger `workflow_run` po **ukończonym sukcesem** workflow `api` i `web`, tylko dla `main`
- `runs-on: [self-hosted, jjdevhub]`
- job na VM woła deploy tego SHA, które przeszło CI — nie czeka na cron

`workflow_run` czyta definicję z domyślnej gałęzi (`main`). Runner musi być online w momencie sukcesu CI.

Przy implementacji rozdziel [release-and-deploy.sh](../infra/ci/release-and-deploy.sh):

- cron dalej sam sprawdza, czy `origin/main` się ruszył
- osobna ścieżka robi `compose up` dla SHA podanego przez runner

Oba mechanizmy czytają `/var/lib/jjdevhub/last-release-sha`. Drugi nie przebudowuje tego samego SHA (`already deployed` w obecnym skrypcie).

Szkic, **nie dodawaj go teraz**:

```yaml
name: deploy

on:
  workflow_run:
    workflows: [api, web]
    types: [completed]
    branches: [main]

jobs:
  deploy:
    if: ${{ github.event.workflow_run.conclusion == 'success' }}
    runs-on: [self-hosted, jjdevhub]
    steps:
      - name: Deploy SHA that passed CI
        run: /opt/jjdevhub/infra/ci/release-and-deploy.sh
```

Ścieżka „dla zadanego SHA” jeszcze nie istnieje — dzisiejszy skrypt zawsze bierze aktualny `origin/main`. Dopóki jej nie ma, nie polegaj na tym szkicu: zielone CI API bez zmian na `main` i tak zdeployowałoby cały czubek `main`.

## 4. Cron jako zapas

Gdy runner leży, godzinny wpis i tak dociągnie `main`. `/etc/jjdevhub/api.env` i `/var/lib/jjdevhub` są już z [proxmox.md](proxmox.md). Wpis crona dodaj jako użytkownik z grupy `docker`:

```bash
crontab -e
```

Wklej linię z [infra/ci/jjdevhub-release.cron](../infra/ci/jjdevhub-release.cron):

```cron
0 * * * * cd /opt/jjdevhub && JJDEVHUB_ENV_FILE=/etc/jjdevhub/api.env /opt/jjdevhub/infra/ci/release-and-deploy.sh >> /var/log/jjdevhub-release.log 2>&1
```

Log:

```bash
sudo touch /var/log/jjdevhub-release.log
sudo chown "$USER:$USER" /var/log/jjdevhub-release.log
```

`JJDEVHUB_PUSH_RELEASE` zostaw `0`. Gałąź `release/*` jest lokalna na VM. Ustaw `1` tylko wtedy, gdy remote key ma prawo push.

Ręczny przebieg (nic nie zrobi, jeśli SHA się nie zmienił):

```bash
cd /opt/jjdevhub
JJDEVHUB_ENV_FILE=/etc/jjdevhub/api.env ./infra/ci/release-and-deploy.sh
```

## Czego nie robić

- Webhook HTTP na VM (potrzebny otwarty port albo osobna trasa tunelu i sekret).
- Job deploy na `ubuntu-latest`.
- Sekretów (`JWT_KEY`, hasło Postgresa, token tunelu, token runnera) w repo albo w GitHub Actions secrets po to, żeby hosted runner stawiał produkcję.

## Historia zmian

Dopisuj tu kolejny stan. Sekcji wyżej nie przerabiaj.

### Zanim był `deploy.yml`

Skrypt [infra/ci/release-and-deploy.sh](../infra/ci/release-and-deploy.sh) po `git fetch origin main` zawsze brał aktualny `origin/main`. Przy nowym SHA tworzył `release/YYYY-MM-DD.N` i robił `docker compose up -d --build`. Stan lądował w `/var/lib/jjdevhub/last-release-sha`. Nie było argumentu SHA: zielone CI API bez zmian na `main` i tak zdeployowałoby czubek `main`.

Pliku `.github/workflows/deploy.yml` nie było. Docelowy kształt, zapisany wtedy jako szkic do następnego kroku:

- trigger `workflow_run` po sukcesie `api` i `web`, tylko dla `main`
- `runs-on: [self-hosted, jjdevhub]`
- job woła ten sam skrypt, bez SHA

```yaml
name: deploy

on:
  workflow_run:
    workflows: [api, web]
    types: [completed]
    branches: [main]

jobs:
  deploy:
    if: ${{ github.event.workflow_run.conclusion == 'success' }}
    runs-on: [self-hosted, jjdevhub]
    steps:
      - name: Deploy SHA that passed CI
        run: /opt/jjdevhub/infra/ci/release-and-deploy.sh
```

Cron zostawał zapasem na tym samym pliku stanu.

### Po dodaniu ścieżki SHA

Doszedł [.github/workflows/deploy.yml](../.github/workflows/deploy.yml). Job startuje tylko przy `conclusion == success`, `head_branch == main` i evencie `push` albo `workflow_dispatch`. Woła skrypt z `workflow_run.head_sha`.

Skrypt dostał opcjonalny argument. Bez niego cron dalej bierze `origin/main`. Z argumentem deployuje ten commit, po `git fetch` i `git cat-file -e`. Jeśli zapisany SHA jest tym samym commitem albo już go zawiera (`git merge-base --is-ancestor`), skrypt kończy się `already deployed` i nie robi checkoutu. Drugi zielony workflow tego samego pusha nie przebudowuje obrazów, a wolniejsze CI starszego commita nie cofa nowszego deployu.

Job na runnerze, z `/opt/jjdevhub`:

```bash
./infra/ci/release-and-deploy.sh "$DEPLOY_SHA"
```

`DEPLOY_SHA` to `workflow_run.head_sha`. Push, który rusza i `api`, i `web`, odpala deploy dwa razy; drugi przebieg wychodzi przez plik stanu. Push tylko w API deployuje po samym `api`, bo `web` ma filtr ścieżek. Katalog `/opt/jjdevhub` ma być czysty. Po zmergowaniu na `main` raz `git pull` na VM, zanim pierwszy `workflow_run` wywoła skrypt.
