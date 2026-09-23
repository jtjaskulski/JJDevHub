# Cloudflare Tunnel

VM z [proxmox.md](proxmox.md) nie potrzebuje publicznego IP. `cloudflared` wychodzi z VM do Cloudflare, a Cloudflare kończy TLS i wysyła ruch HTTP na nginx.

Jeden origin: `http://127.0.0.1:4200`. Kontener `web` proxy'uje `/api/`, `/health`, `/openapi/` i `/scalar` do API. Postgresa (`5433`) tunelem nie publikujesz.

## 1. Domena

Darmowy plan Cloudflare wystarcza. Domena musi być strefą na tym koncie (nameservery u rejestratora wskazują na Cloudflare). Dalej w tekście host to `hub.example.com` — podmień na swój.

## 2. Tunel zarządzany z panelu

Konfiguracja zostaje w dashboardzie, nie w tym repo.

1. [Cloudflare Zero Trust](https://one.dash.cloudflare.com/) → **Networks** → **Tunnels** → **Create a tunnel**.
2. Typ: **Cloudflared**. Nazwa np. `jjdevhub`.
3. **Remotely managed** (kreator domyślnie tak działa: trasa jest w panelu, na VM jest tylko token).
4. Na stronie instalacji wybierz Debian/Ubuntu i **skopiuj polecenie**. Zawiera jednorazowy token. Nie wklejaj tokenu do gita, issue ani tego dokumentu.

Na VM (po Dockrze i działającym Compose):

```bash
curl -L --output /tmp/cloudflared.deb \
  https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
sudo dpkg -i /tmp/cloudflared.deb
sudo cloudflared service install 'TOKEN_Z_PANELU'
```

`service install` zakłada jednostkę systemd i startuje ją od razu. Sprawdzenie:

```bash
systemctl status cloudflared --no-pager
```

W panelu tunel przechodzi w **Healthy**. Na architekturze innej niż amd64 weź pakiet z kreatora, nie link powyżej.

## 3. Public Hostname

W tym samym tunelu: **Public Hostname** → **Add a public hostname**.

| Pole | Wartość |
| --- | --- |
| Subdomain | `hub` (albo inny) |
| Domain | Twoja strefa |
| Path | puste |
| Type | HTTP |
| URL | `127.0.0.1:4200` |

Zapisz. Rekord DNS (proxied CNAME na tunel) panel dopisuje sam. Nie dodawaj drugiego rekordu A na adres domowy.

Innych hostname'ów nie twórz dla API ani Postgresa.

## 4. TLS i router

W strefie: **SSL/TLS** → tryb **Full**. Ruch przeglądarka → Cloudflare jest HTTPS. Odcinek Cloudflare → `cloudflared` też. Na nginx zostaje HTTP na porcie 80 wewnątrz kontenera — to zamierzone, originem jest `http://127.0.0.1:4200`.

Na routerze **nie** przekierowuj 80 ani 443 na VM. Firewall VM może zostać zamknięty na te porty z internetu. `cloudflared` sam nawiązuje połączenie wychodzące (443).

## 5. Sprawdzenie

Z innej sieci niż LAN VM:

```bash
curl -fsS "https://hub.example.com/health"
```

Potem w przeglądarce `https://hub.example.com` i logowanie. API:

```bash
curl -s -X POST "https://hub.example.com/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d '{"email":"owner@test.com","password":"Password1"}'
```

Przy `ASPNETCORE_ENVIRONMENT=Production` ścieżki `/openapi` i `/scalar` nie działają. To ustawienie z `/etc/jjdevhub/api.env`, nie z tunelu.

## 6. Cloudflare Access

Nie włączaj Access przed tą aplikacją. JJDevHub ma własne logowanie JWT. Access (osobne logowanie Cloudflare) da się dołożyć później, jeśli strona ma być niewidoczna bez konta w Zero Trust.

Deploy po commitach: [github.md](github.md).
