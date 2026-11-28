# Cloudflare

Hostname aplikacji to `hub.jjdevhub.com`. Tunel oddaje go na `http://127.0.0.1:8080`. Ostatnia reguła ingressu, bez hostname, zwraca 404.

Strefa `jjdevhub.com` ma tryb SSL Full. Rekord `hub` jest CNAME na tunel i jest proxied. Rekordu A na adres domu nie ma.

## Czego w git nie ma

- Token tunelu. Leży w `/etc/jjdevhub/cloudflared.env` o trybie `600`. W git jest `deploy/cloudflare/cloudflared.env.example` z pustą wartością.
- Hostname Postgresa, Kafki, OTLP, OpenBao i Grafany.
