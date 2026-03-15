# Task 6.4: Budowa Dashboardow w Grafanie

| Pole | Wartosc |
|------|---------|
| Sprint | 6 - Observability & DevOps |
| Status | IN PROGRESS |
| Priorytet | Medium |
| Estymacja | 5 story points |
| Powiazane pliki | `infra/docker/monitoring/grafana/dashboards/`, `infra/docker/monitoring/grafana/provisioning/` |

## Opis

Grafana wizualizuje metryki zbierane przez Prometheus w formie interaktywnych dashboardow. Dashboard pokazuje kluczowe wskazniki wydajnosci (KPI): rate requestow, latencje, uzycie pamieci, GC collections, bledy 5xx. Dashboardy sa provisionowane automatycznie z plikow JSON w Docker - nie trzeba ich tworzyc recznie po kazdym `docker-compose up`.

### Co juz jest zrobione

**Grafana w docker-compose:**
- Obraz: `grafana/grafana:latest`
- Port: `3000`
- Credentials: admin/admin
- Wolumen: `grafana_data`

**Provisioning:**
- Datasource: Prometheus (`http://jjdevhub-prometheus:9090`) - automatycznie skonfigurowany
- Dashboard provider: file-based, folder `/var/lib/grafana/dashboards`

**Dashboard `content-api.json`:**
- HTTP request rate (req/s) - `rate(http_server_request_duration_seconds_count[5m])`
- HTTP p95 latency - `histogram_quantile(0.95, ...)`
- Active HTTP connections
- 5xx error rate
- Process memory (MB) - `process_working_set_bytes`
- GC collections (Gen 0/1/2) - `dotnet_gc_collections_total`

### Co pozostalo

- Dashboard per serwis (Analytics, Identity, etc.)
- Business metrics dashboard (CV generated, applications tracked)
- Infrastructure dashboard (PostgreSQL, MongoDB, Kafka metryki)
- Alerting rules (Grafana Alerting)
- Dashboard variables (service selector, time range)

## Kryteria akceptacji

- [x] Grafana uruchomiona w Docker z auto-provisioned datasource
- [x] Content API dashboard z 6 panelami (rate, latency, connections, errors, memory, GC)
- [x] Dashboardy provisionowane z plikow JSON (nie recznie)
- [ ] Dashboard: Infrastructure Overview (PostgreSQL connections, MongoDB ops, Kafka lag)
- [ ] Dashboard: Business Metrics (CV generated, applications created, match scores)
- [ ] Dashboard variables: service selector dropdown, environment filter
- [ ] Alerting: notyfikacja gdy error rate > 5%, latency p95 > 2s, memory > 80%
- [ ] Dashboard per dodatkowy serwis (Analytics, Identity)
- [ ] Annotations: deploy events z Jenkins (marker na timeline)

## Wymagane pakiety NuGet

Brak - to jest zadanie infrastrukturalne (Grafana + Prometheus).

## Kroki implementacji

1. **Stworz dashboard Infrastructure Overview:**
   - Panel: PostgreSQL active connections (`pg_stat_activity_count` jesli postgres_exporter)
   - Panel: MongoDB operations/s (`mongodb_*` jesli mongodb_exporter)
   - Panel: Kafka consumer lag (`kafka_consumer_group_lag` jesli kafka_exporter)
   - Zapisz jako `infra/docker/monitoring/grafana/dashboards/infrastructure.json`

2. **Stworz dashboard Business Metrics:**
   - Panel: CV generated total (counter `cv_generated_total`)
   - Panel: Applications created per week
   - Panel: Average match score (histogram)
   - Panel: Applications by status (pie chart)
   - Zapisz jako `infra/docker/monitoring/grafana/dashboards/business-metrics.json`

3. **Dodaj zmienne dashboardowe:**
   - Variable `service`: dropdown z lista serwisow (content-api, analytics-api, etc.)
   - Variable `environment`: docker, staging, production
   - Uzyj w query: `{service="$service", environment="$environment"}`

4. **Skonfiguruj Grafana Alerting:**
   - Alert: Error Rate > 5% przez 5 minut
   - Alert: P95 Latency > 2s przez 5 minut
   - Alert: Memory Usage > 80% przez 10 minut
   - Contact point: Email lub Slack webhook (konfiguracja w provisioning)

5. **Dodaj Jenkins deploy annotations:**
   - Jenkins post-deploy: curl do Grafana API z annotation
   ```bash
   curl -X POST http://grafana:3000/api/annotations \
     -H "Authorization: Bearer $GRAFANA_TOKEN" \
     -d '{"text":"Deploy v${BUILD_NUMBER}","tags":["deploy"]}'
   ```

6. **Opcjonalnie: dodaj exportery dla infrastruktury:**
   - `postgres_exporter` -> metryki PostgreSQL
   - `mongodb_exporter` -> metryki MongoDB
   - `kafka_exporter` -> metryki Kafka (consumer lag, partitions)
   - Dodaj do docker-compose i Prometheus config

## Istniejacy dashboard Content API

```
┌─────────────────────────────────────────────────────────┐
│                  Content API Dashboard                   │
├──────────────────────┬──────────────────────────────────┤
│  HTTP Request Rate   │  HTTP P95 Latency               │
│  (req/s)             │  (seconds)                      │
├──────────────────────┼──────────────────────────────────┤
│  Active Connections  │  5xx Error Rate                  │
│                      │  (errors/s)                      │
├──────────────────────┼──────────────────────────────────┤
│  Process Memory (MB) │  GC Collections                  │
│                      │  (Gen 0 / Gen 1 / Gen 2)        │
└──────────────────────┴──────────────────────────────────┘
```

## Zaleznosci

- **Wymaga:** Task 6.3 (OpenTelemetry eksportuje metryki na Prometheus)
- **Blokuje:** Nic (ale krytyczne dla monitoringu produkcji)

## Notatki techniczne

- Dashboardy jako JSON w repozytorium (Infrastructure as Code) - provisionowane automatycznie przy starcie Grafana. Zmiany w UI nie sa persystowane jesli dashboard jest provisionowany (read-only w UI).
- Grafana Alerting zastapila Grafana Legacy Alerting w nowszych wersjach. Alerty sa definiowane w provisioning YAML lub przez API.
- Annotations z Jenkins pozwalaja korelowac deploy events z metrykami - widac na timeline kiedy byl deploy i jak wplynql na metryki.
- Eksportery infrastruktury (postgres_exporter, mongodb_exporter) sa oddzielnymi kontenerami w docker-compose ktore lacza sie z bazami i eksponuja metryki na `/metrics`.
- Dokumentacja: https://grafana.com/docs/grafana/latest/
