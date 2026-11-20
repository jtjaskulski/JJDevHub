# 05. Cloudflare: DNS i tunel

Poprzedni numer to [maszyna](04-maszyna.md). Następny to [compose](06-compose.md).

Ten numer stawia nazwę publiczną i tunel Cloudflare. Tunel wychodzi z maszyny. Jedyny origin aplikacji to brama na porcie `8080`. Rekord jest proxied, tryb SSL to Full. Rekord A na adres domu nie powstaje.

## Po co

Bez nazwy nie da się sprawdzić wydania z innej sieci. Z otwartym portem 443 na routerze domowym brama dostaje ruch, którego nie chcieliśmy publikować, i drugi adres, który omija tunel. Tunel zamyka oba problemy: gość nie słucha publicznie, a nazwa i tak otwiera bramę.

Panele telemetrii, jeśli w ogóle dostaną nazwę, nie idą przez YARP. YARP jest bramą aplikacji, nie bramą Grafany.

## Dlaczego ta technologia

### Tunel wychodzący

`cloudflared` utrzymuje połączenie wychodzące na port 7844. Problem, który zamyka, to przekierowanie portu na routerze. Odpada rekord A na adres domu i odpada wystawienie 443 na gościu. Koszt: dostępność nazwy zależy od procesu `cloudflared` i od Cloudflare. Jedna maszyna i tak ma jedną awarię. Tunel jej nie mnoży.

Na tym etapie `cloudflared` jest kontenerem w compose albo usługą na hoście. Docelowo, w fazie klastra, jest podem. Ten numer wybiera usługę na hoście, żeby tunel żył także wtedy, gdy compose bramy jest akurat restartowane przy deployu. Inaczej deploy gubi sobie drogę, którą właśnie sprawdza.

### Jeden origin

Konfiguracja tunelu ma jeden hostname aplikacji i jeden service: `http://127.0.0.1:8080`. Problem, który zamyka, to osobna nazwa na każdą usługę. Pięć nazw to pięć wejść obok bramy. Odpada publikacja portu Learning. Koszt: każda ścieżka produktu musi być trasą YARP. To już jest zasada bramy.

### SSL Full

Tryb Full, nie Flexible. Problem, który zamyka Flexible, to ruch między brzegiem Cloudflare a originem bez TLS, oraz pętle przekierowań. Na tym numerze origin jest HTTP na localhost, bo tunel kończy się na tym samym hoście. Full jest ustawione na strefie tak, żeby nie przejść na Flexible „dla wygody”. Gdy originem zostanie HTTPS w klastrze, Full strict dojdzie razem z certyfikatem originu. Nie wchodzi w tym numerze, bo nie ma jeszcze certyfikatu na gościu i nie jest potrzebny dla localhost.

### Panele obok

Grafana, jeśli dostaje hostname, ma osobny wpis tunelu i Cloudflare Access. Nie ma trasy w YARP. Postgres, Kafka, OTLP i później OpenBao nie dostają nazwy publicznej. Problem, który zamyka, to panel na tym samym hoście co CV, bez logowania. Koszt: drugi hostname do utrzymania. Wolno go nie zakładać. Wolno nie publikować Grafany wcale i wchodzić na nią wyłącznie z sieci maszyny.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| tunnel | połączenie wychodzące `cloudflared` |
| origin | adres, na który tunel oddaje żądanie; tu brama |
| proxied | rekord idzie przez brzeg Cloudflare, nie wprost na IP domu |
| Full | TLS od klienta do brzegu; do originu według trybu, u nas origin jest lokalny |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `deploy/cloudflare/config.yml` | ingress: hostname aplikacji na `http://127.0.0.1:8080`, reszta na `http_status:404` |
| `deploy/cloudflare/README.md` | nazwa hosta i to, czego w git nie ma |

Token tunelu nie jest w `config.yml`. Jest w `/etc/jjdevhub/cloudflared.env` o uprawnieniu `600`, obok compose.env. W git jest przykład z pustą wartością.

## Plan

### 1. Tunel i rekord

W panelu strefy powstaje tunel i rekord CNAME na tunel, proxied. Nie powstaje rekord A na adres WAN. Tego nie da się w pełni sprawdzić z repozytorium. README mówi, że sprawdzeniem jest brak rekordu A, oglądany przy każdej zmianie strefy. Od [Terraforma](../13-kubernetes/03-terraform-maszyna.md) to samo jest w kodzie i panel przestaje być źródłem.

### 2. Konfiguracja ingress

Jedyna reguła z hostname produktu wskazuje na `http://127.0.0.1:8080`. Ostatnia reguła, bez hostname, zwraca 404. Nie ma reguły łapiącej wszystko na bramę pod obcą nazwą.

### 3. Usługa

`cloudflared` jako systemd, użytkownik bez logowania interaktywnego, token z pliku env. Restart po deployu bramy nie restartuje tunelu.

### 4. Sprawdzenie, że usługi nie mają nazwy

W `config.yml` nie występuje `postgres`, `kafka`, `5432`, `9092` ani `4317` jako cel publiczny.

### Co jest do czego

| Element | Kto go używa |
| --- | --- |
| hostname | przeglądarka i telefon |
| tunel | jedyna droga z internetu do bramy |
| reguła 404 | każda inna nazwa w tej strefie, która nie ma własnego wpisu |

## Testy

Brak testu xUnit. Checkpoint jest sieciowy i lokalny.

| Arrange | Act | Assert |
| --- | --- | --- |
| tunel działa, brama jeszcze może nie nasłuchiwać | żądanie na hostname | połączenie dochodzi do `127.0.0.1:8080` (w logu cloudflared widać origin) |
| config | przegląd pliku | jeden origin aplikacji |

Gdy brama nie nasłuchuje, oczekiwany wynik to błąd originu, nie otwarta strona innej usługi.

## Checkpoint

```bash
test -f deploy/cloudflare/config.yml
grep -n '127.0.0.1:8080' deploy/cloudflare/config.yml
grep -E '5432|9092|4317' deploy/cloudflare/config.yml && exit 1 || true
systemctl is-active cloudflared
```

Z sieci innej niż dom:

```bash
curl -sS -o /dev/null -w '%{http_code}\n' "https://<hostname>/versions"
```

Gdy brama już stoi, kod to `200`. Gdy jeszcze nie stoi, kod jest błędem originu, a nie odpowiedzią z IP domu. W logu nie ma podstawienia prawdziwego hasła. Hostname jest w README, nie jest sekretem.

## Umiesz wyjaśnić

Dlaczego nie ma rekordu A na dom?

Bo rekord A publikuje adres i zachęca do otwarcia portu. Tunel nie potrzebuje adresu przychodzącego. Adres domu zostaje poza DNS produktu.

Dlaczego tunel jest usługą hosta, a nie kontenerem obok bramy?

Żeby restart compose przy deployu nie ucinał drogi, którą deploy sprawdza. Tunel przeżywa podmianę bramy.

Czemu Grafana nie idzie przez YARP?

YARP przepuszcza API produktu i sprawdza JWT aplikacji. Panel ma inny model dostępu. Mieszanie ich daje albo panel bez logowania, albo bramę, która zna ciasteczka Grafany.

Co dostaje nazwę publiczną?

Hostname aplikacji na bramę. Ewentualnie hostname panelu za Access. Nic więcej.

Dlaczego Full, skoro origin jest HTTP na localhost?

Żeby tryb strefy nie był Flexible. Flexible rozjeżdża przekierowania i uczy złego schematu na dzień, w którym origin wyjdzie poza localhost. Ruch tunelu i tak nie idzie otwartym portem po LAN.

## Zasady

- Jeden origin aplikacji: brama na `127.0.0.1:8080`.
- Brak rekordu A na adres domu.
- Token tunelu nie wchodzi do git.
- Postgres, Kafka, OTLP i OpenBao nie mają hostname.
- YARP nie dostaje trasy do panelu.

## Poza zakresem

- Treść compose bramy jest w [06-compose.md](06-compose.md).
- Rollout, który sprawi, że `/versions` zwróci `200` z internetu, jest w [07-deploy.md](07-deploy.md).
- Opis tunelu Terraformem jest w [03-terraform-maszyna.md](../13-kubernetes/03-terraform-maszyna.md).
- Access do panelu, jeśli panel w ogóle jest publikowany, dopisuje się przy [telemetrii](08-telemetria.md) jako decyzja „nie publikować” albo osobny hostname. Domyślnie panel nie jest publikowany.

## Źródła

- [Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/). Wiążące jest połączenie wychodzące i origin wskazany w ingress. Dotyczy jednego hostname aplikacji. Ze źródła nie bierzemy opublikowanego portu 443 na gościu.
- [SSL/TLS encryption modes](https://developers.cloudflare.com/ssl/origin-configuration/ssl-modes/). Wiążące jest odrzucenie Flexible. Dotyczy trybu Full na strefie. Ze źródła nie bierzemy Full strict w tym numerze, bo nie ma certyfikatu originu.
- [Cloudflare Access](https://developers.cloudflare.com/cloudflare-one/policies/access/). Wiążące jest to, że panel nie jest anonimowy, jeśli w ogóle ma nazwę. Dotyczy decyzji, żeby panelu nie wpuszczać w YARP. Ze źródła nie bierzemy Access jako logowania do CV. Logowanie produktu jest w Identity.

## Gotowe gdy

- `config.yml` wskazuje bramę i kończy się regułą 404.
- W pliku nie ma portów bazy, brokera i OTLP.
- `cloudflared` jest aktywny i token nie leży w git.
- Strefa nie ma rekordu A na adres domu.
- `curl` na hostname z zewnątrz nie otwiera usługi innej niż brama.
