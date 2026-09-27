# CI/CD — spis dokumentów

Numerowane pliki poniżej to kolejność wdrożenia. Agent wykonuje **jeden numer naraz** — nie łączy kilku kroków w jednej sesji.

| Plik | Temat |
| --- | --- |
| [01-proxmox.md](01-proxmox.md) | VM na Proxmoxie, Docker i pierwszy `compose up`. |
| [02-cloudflare-tunnel.md](02-cloudflare-tunnel.md) | Tunel Cloudflare do nginx na VM (bez publicznego IP). |
| [03-github.md](03-github.md) | Self-hosted runner, branch protection i deploy na `main`. |
| [04-codeql.md](04-codeql.md) | CodeQL w CI (analiza bezpieczeństwa kodu). |
| [05-zaleznosci-i-obrazy.md](05-zaleznosci-i-obrazy.md) | Skan zależności i obrazów kontenerowych. |
| [06-opentelemetry.md](06-opentelemetry.md) | Instrumentacja OpenTelemetry w aplikacji. |
| [07-prometheus.md](07-prometheus.md) | Zbieranie metryk Prometheus. |
| [08-jaeger.md](08-jaeger.md) | Tracing rozproszony w Jaegerze. |
| [09-grafana.md](09-grafana.md) | Dashboardy i wizualizacja w Grafanie. |
| [10-sonarqube.md](10-sonarqube.md) | Jakość kodu i SonarQube. |
| [przyszlosc.md](przyszlosc.md) | Kafka, event sourcing i Jenkins — tylko kierunek, bez kroków wdrożenia. |
