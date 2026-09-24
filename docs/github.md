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

Skrypt robi `git fetch origin main`. Bez argumentu celem jest `origin/main` (cron). Z argumentem SHA deployuje ten commit. Przy nowym SHA tworzy lokalną gałąź `release/YYYY-MM-DD.N` i `docker compose up -d --build` z `--env-file /etc/jjdevhub/api.env`. Stan zapisuje w `/var/lib/jjdevhub/last-release-sha`. Jeśli zapisany commit jest tym samym SHA albo już go zawiera, wypisuje `already deployed` i nie robi checkoutu.

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

## 3. Workflow deploy

[`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml) startuje po **ukończonym sukcesem** `api` albo `web`, gdy `head_branch` to `main` i event to `push` albo `workflow_dispatch`. PR nie deployuje. Job idzie na `[self-hosted, jjdevhub]` i z `/opt/jjdevhub` woła:

```bash
./infra/ci/release-and-deploy.sh "$DEPLOY_SHA"
```

`DEPLOY_SHA` to commit, który przeszedł CI (`workflow_run.head_sha`), nie aktualny czubek `main`.

`workflow_run` czyta definicję z domyślnej gałęzi (`main`). Runner musi być online i mieć etykietę `jjdevhub`. Katalog `/opt/jjdevhub` ma być czysty, bo skrypt robi `git checkout`.

Push, który rusza i `api`, i `web`, odpali deploy dwa razy. Drugi przebieg zobaczy ten sam SHA w pliku stanu i wyjdzie. Push tylko w API deployuje po samym `api` — `web` się wtedy nie uruchamia przez filtr ścieżek.

Po zmergowaniu tego pliku na `main` zrób na VM raz `git pull` w `/opt/jjdevhub`, zanim pierwszy `workflow_run` wywoła skrypt. Na dysku musi być wersja, która przyjmuje SHA.

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
