# Task 6.3: Wdrozenie OpenTelemetry w Aplikacjach .NET

| Pole | Wartosc |
|------|---------|
| Sprint | 6 - Observability & DevOps |
| Status | IN PROGRESS |
| Priorytet | Medium |
| Estymacja | 8 story points |
| Powiazane pliki | `JJDevHub.Content.Api/Program.cs`, `infra/docker/monitoring/prometheus/prometheus.yml`, `infra/docker/docker-compose.yml` (serwis `jaeger`, zmienna `OTEL_EXPORTER_OTLP_ENDPOINT`) |

## Opis

OpenTelemetry to standard observability obejmujacy metryki, tracing i logowanie. W JJDevHub metryki sa eksportowane w formacie Prometheus i scrapowane przez Prometheus server, ktory z kolei jest zrodlem danych dla Grafana dashboardow. Tracing pozwala na sledzenie requestow przez caly stack (Nginx -> API -> PostgreSQL -> Kafka -> MongoDB).

### Co juz jest zrobione

**Content.Api (Program.cs):**
- OpenTelemetry Metrics:
  - `AddAspNetCoreInstrumentation()` - metryki HTTP (request rate, latency, status codes)
  - `AddRuntimeInstrumentation()` - metryki .NET runtime (GC, thread pool, memory)
  - `AddPrometheusExporter()` - eksport na `/metrics`
- Endpoint: `app.MapPrometheusScrapingEndpoint()` -> `/metrics`
- Distributed tracing (OTLP gRPC): `AddOtlpExporter` z `OtlpExportProtocol.Grpc`, instrumentacja ASP.NET Core; w Dockerze eksport do `jaeger:4317` (Jaeger all-in-one, UI `:16686`)

**Prometheus (prometheus.yml):**
- Job `jjdevhub-content-api`:
  - Target: `jjdevhub-content-api:8080`
  - Path: `/metrics`
  - Interval: 10s
  - Labels: `service: content-api`, `environment: docker`
- Job `jjdevhub-jenkins`:
  - Target: `jjdevhub-jenkins:8080`
  - Path: `/prometheus`

**Zainstalowane pakiety:**
- `OpenTelemetry.Exporter.Prometheus.AspNetCore` 1.15.0-beta.1
- `OpenTelemetry.Extensions.Hosting` 1.15.0
- `OpenTelemetry.Instrumentation.AspNetCore` 1.12.0
- `OpenTelemetry.Instrumentation.Runtime` 1.12.0
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` (OTLP do Jaeger)

### Co pozostalo

- Rozszerzenie trace przez Nginx i pozostale serwisy (wspolny trace context)
- Structured Logging z Serilog + OpenTelemetry
- Metryki customowe (biznesowe: ile CV wygenerowano, ile aplikacji dodano)
- Instrumentacja HTTP client (outgoing calls do innych serwisow)
- Instrumentacja EF Core (query duration, connection pool)
- Instrumentacja Kafka (produce/consume latency)
- Dodanie OpenTelemetry do pozostalych serwisow (Analytics, Identity, etc.)

## Kryteria akceptacji

- [x] OpenTelemetry Metrics w Content.Api (AspNetCore + Runtime)
- [x] Prometheus exporter na `/metrics`
- [x] Prometheus scrapuje metryki z Content.Api
- [ ] Distributed Tracing: trace ID propagowany przez HTTP headers
- [ ] HTTP Client instrumentation (outgoing calls)
- [ ] EF Core instrumentation (query metrics)
- [ ] Serilog z OpenTelemetry enricher (trace ID w logach)
- [ ] Custom business metrics (cv_generated_total, applications_created_total)
- [ ] OpenTelemetry w pozostalych serwisach (minimum Analytics, Identity)
- [ ] OTLP Exporter (opcjonalnie, do Jaeger lub Tempo)

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `OpenTelemetry.Exporter.Prometheus.AspNetCore` | 1.15.0-beta.1 | Content.Api | Juz zainstalowany - Prometheus exporter |
| `OpenTelemetry.Extensions.Hosting` | 1.15.0 | Content.Api | Juz zainstalowany - hosting integration |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.12.0 | Content.Api | Juz zainstalowany - HTTP metrics |
| `OpenTelemetry.Instrumentation.Runtime` | 1.12.0 | Content.Api | Juz zainstalowany - .NET runtime metrics |
| `OpenTelemetry.Instrumentation.Http` | latest | Content.Api | NOWY - instrumentacja HttpClient (outgoing calls) |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | latest | Content.Api | NOWY - instrumentacja EF Core queries |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | latest | Content.Api | NOWY - OTLP exporter (do Jaeger/Tempo) |
| `Serilog.AspNetCore` | latest | Content.Api | NOWY - strukturalne logowanie |
| `Serilog.Sinks.Console` | latest | Content.Api | NOWY - output do konsoli (Docker logs) |
| `Serilog.Enrichers.OpenTelemetry` | latest | Content.Api | NOWY - trace ID w logach Serilog |

## Kroki implementacji

1. **Dodaj Distributed Tracing w Program.cs:**
   ```csharp
   builder.Services.AddOpenTelemetry()
       .WithTracing(tracing => tracing
           .AddAspNetCoreInstrumentation()
           .AddHttpClientInstrumentation()
           .AddEntityFrameworkCoreInstrumentation()
           .AddOtlpExporter())
       .WithMetrics(metrics => metrics
           .AddAspNetCoreInstrumentation()
           .AddRuntimeInstrumentation()
           .AddHttpClientInstrumentation()
           .AddPrometheusExporter());
   ```

2. **Zainstaluj nowe pakiety:**
   ```
   dotnet add src/Services/JJDevHub.Content/JJDevHub.Content.Api package OpenTelemetry.Instrumentation.Http
   dotnet add src/Services/JJDevHub.Content/JJDevHub.Content.Api package OpenTelemetry.Instrumentation.EntityFrameworkCore
   dotnet add src/Services/JJDevHub.Content/JJDevHub.Content.Api package Serilog.AspNetCore
   dotnet add src/Services/JJDevHub.Content/JJDevHub.Content.Api package Serilog.Sinks.Console
   ```

3. **Skonfiguruj Serilog:**
   ```csharp
   builder.Host.UseSerilog((context, config) => config
       .ReadFrom.Configuration(context.Configuration)
       .Enrich.FromLogContext()
       .Enrich.WithOpenTelemetryTraceId()
       .WriteTo.Console(outputTemplate:
           "[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {Message:lj}{NewLine}{Exception}"));
   ```

4. **Dodaj custom business metrics:**
   ```csharp
   var meter = new Meter("JJDevHub.Content");
   var cvGeneratedCounter = meter.CreateCounter<int>("cv_generated_total");
   var applicationsCounter = meter.CreateCounter<int>("applications_created_total");
   ```

5. **Rozszerz Prometheus config o nowe serwisy:**
   ```yaml
   - job_name: 'jjdevhub-analytics-api'
     metrics_path: '/metrics'
     static_configs:
       - targets: ['jjdevhub-analytics-api:8080']
   ```

6. **Opcjonalnie: dodaj Jaeger/Tempo dla tracing UI:**
   - Dodaj Jaeger do docker-compose: `jaegertracing/all-in-one:latest`
   - OTLP exporter wysyla traces do Jaeger
   - Grafana laczy sie z Jaeger jako data source

## Architektura observability

```
[Content.Api] --metrics--> [Prometheus :9090] --query--> [Grafana :3000]
     |
     |--traces--> [OTLP] --> [Jaeger/Tempo] --query--> [Grafana :3000]
     |
     |--logs--> [Serilog] --> [Console/Docker logs]
                                  |
                              [Loki] --> [Grafana :3000] (opcjonalnie)
```

## Zaleznosci

- **Wymaga:** Task 1.4 (infrastruktura Docker), Task 4.1 (Content.Api)
- **Blokuje:** Task 6.4 (Grafana dashboardy korzystaja z metryk Prometheus)

## Notatki techniczne

- OpenTelemetry Prometheus exporter jest w wersji beta (1.15.0-beta.1) ale jest stabilny i szeroko uzywany.
- Trace ID propagation przez HTTP headers (`traceparent`) pozwala na korelacje requestow miedzy serwisami. Serilog enricher dolacza trace ID do kazdego loga.
- EF Core instrumentation mierzy czas trwania zapytan SQL - przydatne do wykrywania slow queries.
- Custom metryki biznesowe (countery, histogramy) sa rejestrowane przez `System.Diagnostics.Metrics` API i automatycznie eksportowane przez Prometheus exporter.
- Na przyszlosc: Grafana Loki dla centralizacji logow (zamiast czytania Docker logs bezposrednio).
- Dokumentacja: https://opentelemetry.io/docs/languages/dotnet/
