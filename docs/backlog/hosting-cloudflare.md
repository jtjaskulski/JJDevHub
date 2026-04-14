# JJDevHub - Hosting & Cloudflare Production Deployment

> Architektura wdrozenia produkcyjnego: VPS lub Proxmox (home lab) + Cloudflare jako warstwa DNS/WAF/Tunnel.

## Kluczowa zasada

**Cloudflare NIE jest hostem aplikacji.** Cloudflare to siec CDN, ochrona DDoS (WAF) i platforma DNS. Nie uruchamia kontenerow Docker, baz danych ani message brokerow. JJDevHub wymaga ciaglego dzialania uslug takich jak Kafka, PostgreSQL, MongoDB, Keycloak - to wymaga dedykowanego serwera (VPS) lub klastra w chmurze.

Cloudflare sluzy **wylacznie jako warstwa sieciowa i ochronna** przed serwerem - co jest standardowa i pozadana praktyka w branzy.

---

## Architektura sieci

```
[Uzytkownik / Przegladarka]
        |
        | DNS query: jjdevhub.com
        v
[Cloudflare DNS] ── A record → VPS IP (proxied)
        |
        | Cloudflare Proxy (WAF, DDoS, CDN, SSL)
        v
[Cloudflare Edge Server]
        |
        | Encrypted connection (Full Strict SSL)
        | Cloudflare Origin CA certificate
        v
[VPS - Hetzner/OVH]
    |
    | Port 443 (HTTPS only)
    v
[Nginx Reverse Proxy]
    |
    ├── /                    → Angular SPA (:80)
    ├── /api/v1/content/     → Content API (:8080)
    ├── /api/analytics/      → Analytics API (:8080)
    ├── /api/identity/       → Identity API (:8080)
    ├── /api/ai/             → AI Gateway (:8080)
    ├── /api/notification/   → Notification API (:8080)
    ├── /api/education/      → Education API (:8080)
    └── /jenkins/            → Jenkins (:8080) [opcjonalnie, lub osobna subdomena]
        |
        ├── [frontend-net] ── Angular, Nginx
        └── [backend-net]  ── APIs, PostgreSQL, MongoDB, Kafka, Vault, Keycloak
```

---

## 1. Wybor VPS

### Rekomendowani dostawcy

| Dostawca | Plan | RAM | CPU | Dysk | Cena/mies. | Uwagi |
|----------|------|-----|-----|------|-----------|-------|
| **Hetzner** | CX32 | 8 GB | 4 vCPU | 80 GB NVMe | ~7 EUR | Najlepszy stosunek cena/jakosc, datacenter w EU |
| **OVH** | VPS Essential | 8 GB | 4 vCPU | 80 GB SSD | ~12 EUR | Datacenter w Polsce (Warszawa) |
| **Contabo** | VPS M | 16 GB | 6 vCPU | 200 GB NVMe | ~12 EUR | Duzo zasobow za niska cene |

### Minimalne wymagania

- **RAM:** 8 GB (minimum dla calego stacku Docker)
  - PostgreSQL: ~256 MB
  - MongoDB: ~256 MB
  - Kafka + Zookeeper: ~1 GB
  - Keycloak: ~512 MB
  - SonarQube: ~2 GB (ciezki)
  - .NET APIs (7x): ~1.5 GB
  - Nginx + Angular: ~128 MB
  - Prometheus + Grafana: ~512 MB
  - System + Docker: ~1.5 GB
- **CPU:** 4 vCPU (minimum)
- **Dysk:** 40 GB NVMe (minimum, 80 GB rekomendowane)
- **OS:** Ubuntu 24.04 LTS lub Debian 12
- **Siec:** 1 Gbps, unlimited traffic

### Rekomendacja

**Hetzner CX32 (8 GB RAM, 4 vCPU, 80 GB NVMe, ~7 EUR/mies.)** - najlepsza opcja na start. Jesli SonarQube i Jenkins beda zbyt ciezkie, mozna je wylaczyc na produkcji i uruchamiac tylko na CI/CD serverze.

---

## 2. Konfiguracja Cloudflare

### 2.1 DNS Records

Zakladajac domene `jjdevhub.com` i VPS IP `203.0.113.42`:

| Type | Name | Content | Proxy | TTL |
|------|------|---------|-------|-----|
| A | `jjdevhub.com` | `203.0.113.42` | Proxied (pomaranczowa chmurka) | Auto |
| A | `www` | `203.0.113.42` | Proxied | Auto |
| A | `api` | `203.0.113.42` | Proxied | Auto |
| A | `jenkins` | `203.0.113.42` | DNS only (szara chmurka) | Auto |
| A | `grafana` | `203.0.113.42` | DNS only | Auto |

- **Proxied** = ruch przechodzi przez Cloudflare (CDN, WAF, DDoS protection). IP serwera jest ukryty.
- **DNS only** = ruch idzie bezposrednio do serwera. Uzywaj dla serwisow wewnetrznych (Jenkins, Grafana) + dodatkowe zabezpieczenia (VPN, IP whitelist).

### 2.2 SSL/TLS Mode

| Ustawienie | Wartosc | Uzasadnienie |
|------------|---------|--------------|
| SSL Mode | **Full (Strict)** | Cloudflare weryfikuje certyfikat Origin CA na serwerze |
| Minimum TLS Version | 1.2 | Bezpieczenstwo |
| Always Use HTTPS | On | Redirect HTTP -> HTTPS |
| HSTS | On (max-age: 31536000) | HTTP Strict Transport Security |
| Automatic HTTPS Rewrites | On | Naprawia mixed content |

### 2.3 Cloudflare Origin CA Certificate

Cloudflare Origin CA to darmowy certyfikat SSL wazny do 15 lat, uzywany **wylacznie** na polaczeniu Cloudflare <-> serwer VPS.

**Generowanie:**
1. Cloudflare Dashboard -> SSL/TLS -> Origin Server -> Create Certificate
2. Hostnames: `*.jjdevhub.com`, `jjdevhub.com`
3. Validity: 15 years
4. Pobierz: `origin.pem` (certyfikat) i `origin-key.pem` (klucz prywatny)
5. Skopiuj na VPS: `/etc/ssl/cloudflare/origin.pem` i `/etc/ssl/cloudflare/origin-key.pem`

### 2.4 WAF Rules (Web Application Firewall)

| Regula | Akcja | Uzasadnienie |
|--------|-------|--------------|
| Block countries (opcjonalnie) | Block | Blokuj kraje z ktorych nie oczekujesz ruchu |
| Rate limiting: /api/* | Challenge (100 req/10s) | Ochrona API przed brute force |
| Rate limiting: /auth/* | Challenge (20 req/10s) | Ochrona logowania |
| Bot Fight Mode | On | Blokuj znane boty |
| Challenge Passage | 30 min | Jak dlugo challenge jest wazny |

### 2.5 Page Rules / Cache Rules

| Rule | Ustawienie | Uzasadnienie |
|------|------------|--------------|
| `jjdevhub.com/api/*` | Cache Level: Bypass | API nie powinno byc cachowane przez Cloudflare |
| `jjdevhub.com/assets/*` | Cache Level: Cache Everything, Edge TTL: 1 month | Statyczne assety Angulara |
| `jjdevhub.com/*.js` | Cache Level: Cache Everything, Edge TTL: 1 year | JS bundles (z hash w nazwie) |
| `jjdevhub.com/*.css` | Cache Level: Cache Everything, Edge TTL: 1 year | CSS bundles |

---

## 3. Konfiguracja VPS

### 3.1 Firewall (UFW)

```bash
# Reset
sudo ufw default deny incoming
sudo ufw default allow outgoing

# SSH (zmien port z 22 na niestandardowy!)
sudo ufw allow 2222/tcp comment 'SSH'

# HTTP/HTTPS (tylko Cloudflare IPs)
# Lista IP Cloudflare: https://www.cloudflare.com/ips/
sudo ufw allow from 173.245.48.0/20 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 103.21.244.0/22 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 103.22.200.0/22 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 103.31.4.0/22 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 141.101.64.0/18 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 108.162.192.0/18 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 190.93.240.0/20 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 188.114.96.0/20 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 197.234.240.0/22 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 198.41.128.0/17 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 162.158.0.0/15 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 104.16.0.0/13 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 104.24.0.0/14 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 172.64.0.0/13 to any port 443 proto tcp comment 'Cloudflare'
sudo ufw allow from 131.0.72.0/22 to any port 443 proto tcp comment 'Cloudflare'

# Wlacz firewall
sudo ufw enable
```

**Kluczowe:** Port 443 otwarty TYLKO dla IP Cloudflare. Nikt nie moze polaczyc sie bezposrednio z serwerem omijajac Cloudflare.

### 3.2 SSH Hardening

```bash
# /etc/ssh/sshd_config
Port 2222                    # Niestandardowy port
PermitRootLogin no           # Brak logowania jako root
PasswordAuthentication no    # Tylko klucze SSH
MaxAuthTries 3
AllowUsers jjdevhub          # Tylko jeden uzytkownik
```

### 3.3 Docker i Docker Compose

```bash
# Instalacja Docker (Ubuntu 24.04)
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker jjdevhub

# Instalacja Docker Compose plugin
sudo apt install docker-compose-plugin

# Weryfikacja
docker --version
docker compose version
```

---

## 4. Docker Networks - Separacja Sieci

Zamiast jednej sieci `jjdevhub-net`, na produkcji nalezy uzyc **oddzielnych sieci** dla warstwy publicznej i warstwy danych:

```yaml
networks:
  frontend-net:
    driver: bridge
  backend-net:
    driver: bridge
  data-net:
    driver: bridge
    internal: true    # brak dostepu do internetu
```

### Przypisanie serwisow do sieci

| Serwis | frontend-net | backend-net | data-net |
|--------|:----------:|:---------:|:-------:|
| Nginx | x | x | |
| Angular Web | x | | |
| Content API | | x | x |
| Analytics API | | x | x |
| Identity API | | x | x |
| AI Gateway | | x | |
| Notification API | | x | |
| Education API | | x | |
| Sync Worker | | x | x |
| PostgreSQL | | | x |
| MongoDB | | | x |
| Kafka | | | x |
| Zookeeper | | | x |
| Keycloak | | x | x |
| Vault | | x | |
| Prometheus | | x | |
| Grafana | | x | |

**Kluczowe:**
- `data-net` jest `internal: true` - bazy danych nie maja dostepu do internetu
- Nginx jest mostem miedzy `frontend-net` i `backend-net`
- PostgreSQL, MongoDB, Kafka sa dostepne TYLKO z `data-net`

---

## 5. Nginx - Konfiguracja produkcyjna

```nginx
server {
    listen 443 ssl http2;
    server_name jjdevhub.com www.jjdevhub.com;

    # Cloudflare Origin CA
    ssl_certificate     /etc/ssl/cloudflare/origin.pem;
    ssl_certificate_key /etc/ssl/cloudflare/origin-key.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;

    # Security headers
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Gzip compression
    gzip on;
    gzip_types text/plain text/css application/json application/javascript text/xml;
    gzip_min_length 1000;

    # Verify Cloudflare IP (double protection)
    # Tylko requesty z IP Cloudflare sa akceptowane
    set_real_ip_from 173.245.48.0/20;
    set_real_ip_from 103.21.244.0/22;
    # ... (wszystkie zakresy IP Cloudflare)
    real_ip_header CF-Connecting-IP;

    # Angular SPA
    location / {
        proxy_pass http://angular-web:80;
        proxy_set_header Host $host;
        try_files $uri $uri/ /index.html;
    }

    # API routes
    location /api/v1/content/ {
        proxy_pass http://content-api:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # ... inne API routes
}

# Redirect HTTP -> HTTPS
server {
    listen 80;
    server_name jjdevhub.com www.jjdevhub.com;
    return 301 https://$host$request_uri;
}
```

---

## 6. Produkcyjny docker-compose (roznice vs dev)

Na produkcji docker-compose powinien roznic sie od dev:

| Aspekt | Dev | Produkcja |
|--------|-----|-----------|
| Porty | Wystawione na hosta (5433, 27018, 9092/29092 Kafka, etc.) | Brak wystawionych portow (tylko Nginx 443) |
| Sieci | Jedna `jjdevhub-net` | Trzy: `frontend-net`, `backend-net`, `data-net` |
| Wolumeny | Named volumes | Bind mounts do /data na hoście |
| SonarQube | Wlaczony | Wylaczony (uruchamiany tylko na CI/CD) |
| Jenkins | Wlaczony (8082) | Osobny serwer lub wylaczony |
| Vault | Dev mode (in-memory) | Production mode (file backend) |
| Nginx | HTTP (8081) | HTTPS (443) z Origin CA |
| Restart policy | Brak | `restart: unless-stopped` |
| Logging | Default | JSON file driver z rotacja |
| Healthchecks | Proste | Z retry i timeout |

### Przyklad produkcyjnego PostgreSQL:

```yaml
db:
  image: postgres:16-alpine
  container_name: jjdevhub-db
  # BRAK portow! Dostep tylko z data-net
  environment:
    POSTGRES_DB: jjdevhub_content
    POSTGRES_USER: ${DB_USER}          # z .env lub Vault
    POSTGRES_PASSWORD: ${DB_PASSWORD}  # z .env lub Vault
  volumes:
    - /data/postgres:/var/lib/postgresql/data
  networks:
    - data-net
  restart: unless-stopped
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U ${DB_USER}"]
    interval: 10s
    timeout: 5s
    retries: 5
  deploy:
    resources:
      limits:
        memory: 512M
```

---

## 7. Deployment Workflow

```
[Developer] → git push → [Jenkins CI/CD]
                              |
                    Build + Test + SonarQube
                              |
                    Docker Build (images)
                              |
                    Docker Push → [Registry]
                              |
                    SSH → VPS → docker compose pull
                              |
                    docker compose up -d --remove-orphans
                              |
                    Health check → [Prometheus/Grafana]
```

### Opcje Docker Registry:

| Registry | Cena | Uwagi |
|----------|------|-------|
| Docker Hub | Darmowy (1 prywatne repo) | Limit 200 pulls/6h |
| GitHub Container Registry (ghcr.io) | Darmowy (publiczne) | Zintegrowany z GitHub |
| Self-hosted Registry | Darmowy | Dodatkowy kontener na VPS |

---

## 8. Backup Strategy

| Co | Jak | Czestotliwosc |
|----|-----|---------------|
| PostgreSQL | `pg_dump` + compress + upload do S3/Backblaze | Codziennie |
| MongoDB | `mongodump` + compress + upload | Codziennie |
| Docker volumes | Snapshot VPS (Hetzner snapshots) | Tygodniowo |
| Konfiguracja | Git (docker-compose, nginx.conf, etc.) | Kazdy commit |
| Vault secrets | `vault operator raft snapshot` | Po kazdej zmianie |

---

## Checklist przed wdrozeniem

- [ ] VPS zamowiony i skonfigurowany (Ubuntu 24.04, Docker, UFW)
- [ ] Domena skonfigurowana w Cloudflare (DNS records, SSL Full Strict)
- [ ] Cloudflare Origin CA certificate wygenerowany i zainstalowany na Nginx
- [ ] Firewall VPS: port 443 tylko dla IP Cloudflare
- [ ] SSH na niestandardowym porcie z kluczami
- [ ] Docker networks: frontend-net, backend-net, data-net (internal)
- [ ] docker-compose produkcyjny: brak wystawionych portow, restart policy, resource limits
- [ ] Vault w trybie produkcyjnym (file backend zamiast dev mode)
- [ ] Backup automatyczny (pg_dump, mongodump, cron)
- [ ] Monitoring: Prometheus scrapuje metryki, Grafana dashboardy dzialaja
- [ ] Jenkins: pipeline wdraza na VPS przez SSH
- [ ] Test: HTTPS dziala, WAF blokuje podejrzany ruch, API odpowiada

---

## Alternatywa: Proxmox + Starlink + Cloudflare Tunnel (home lab)

Jesli zamiast VPS wolisz hostowac na wlasnym sprzecie (np. PC pod Proxmoxem na Starlink Residential), schemat z rekordami A **nie zadziala** — Starlink uzywa CGNAT i nie przydziela publicznego IP.

### Cloudflare Tunnel — jak to dziala

`cloudflared` to lekki daemon, ktory nawiazuje **polaczenie wychodzace** do Cloudflare Edge. Ruch od uzytkownikow przechodzi przez Cloudflare i dociera do tunelu — nie potrzebujesz publicznego IP, otwartych portow na firewallu ani przekierowania portow na routerze.

```
[Uzytkownik] → HTTPS → [Cloudflare Edge] → tunel (wychodzacy) → [cloudflared → Nginx → Docker]
```

### Roznice vs VPS

| Aspekt | VPS (sekcje 1-8 powyzej) | Proxmox + Starlink |
|--------|-------------------------|--------------------|
| Siec | Publiczny IP, rekordy A | CGNAT, Cloudflare Tunnel |
| Firewall | UFW: port 443 tylko Cloudflare IPs | Nie trzeba — brak otwartych portow |
| DNS | Rekordy A wskazujace na IP VPS | Rekordy CNAME zarzadzane przez tunel |
| Koszt | ~7-12 EUR/mies. VPS | 0 EUR (wlasny hardware) |
| Uptime | 99.9%+ (datacenter) | Zalezny od Starlink + zasilania |
| Latency | ~10-30 ms (EU datacenter) | ~40-80 ms (Starlink satelitarny) |

### Szybki start (Cloudflare Tunnel)

1. **Cloudflare Dashboard** → Zero Trust → Networks → Tunnels → Create a tunnel
2. Nazwa: `jjdevhub-proxmox`, connector: Cloudflared
3. Skopiuj token instalacyjny, nastepnie na VM:
   ```bash
   curl -L https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb -o cloudflared.deb
   sudo dpkg -i cloudflared.deb
   sudo cloudflared service install <TOKEN>
   ```
4. W sekcji **Public Hostnames** dodaj routingi (np. `@ → https://localhost:443`, `grafana → http://localhost:3000`)
5. Wlacz **Cloudflare Access** dla paneli administracyjnych (Grafana, Jenkins, Keycloak)

Pelna instrukcja krok po kroku: [deployment-proxmox-starlink.md](../deployment-proxmox-starlink.md)

### Pliki produkcyjne

Gotowe pliki infrastrukturalne dla obu wariantow (VPS i Proxmox):

| Plik | Opis |
|------|------|
| [`docker-compose.prod.yml`](../../infra/docker/docker-compose.prod.yml) | Override produkcyjny (sieci, porty, restart, env) |
| [`nginx-prod.conf`](../../infra/docker/nginx/nginx-prod.conf) | Nginx z SSL Origin CA, gzip, security headers |
| [`.env.prod.example`](../../infra/docker/.env.prod.example) | Template zmiennych produkcyjnych |
| [`backup.sh`](../../infra/docker/backup.sh) | Skrypt pg_dump + mongodump + upload (rclone) |

---

## Implementacja w repozytorium (Task „Production Readiness”)

Powiazane pliki: [docker-compose.yml](../../infra/docker/docker-compose.yml), [nginx.conf](../../infra/docker/nginx/nginx.conf), [Jenkinsfile](../../Jenkinsfile).

- [x] `docker-compose.prod.yml`: sieci `frontend-net` / `backend-net` / `data-net`, brak wystawionych portow na hosta, resource limits
- [x] `nginx-prod.conf`: SSL Origin CA, gzip, security headers, rate limiting, Cloudflare real IP
- [x] `.env.prod.example`: template zmiennych produkcyjnych (DB, Keycloak, Grafana, SSL)
- [x] `backup.sh`: pg_dump + mongodump + kompresja + opcjonalny upload (rclone)
- [x] Nginx: `location /api/v1/content/` i rewrite legacy `/api/content/` (zaimplementowane w dev compose)
- [x] Keycloak: realm `jjdevhub`, klienci `jjdevhub-web` / `jjdevhub-api` (import z [keycloak/jjdevhub-realm.json](../../infra/docker/keycloak/jjdevhub-realm.json))
- [x] Vault: `bootstrap-vault.sh` po starcie; `Vault__Enabled=true` w API gdy sekrety maja zastapic env
- [ ] Registry obrazow + Jenkins deploy SSH na VPS
