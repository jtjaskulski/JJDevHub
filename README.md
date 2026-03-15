# JJDevHub: Enterprise-Grade Blog & Applicant Tracking System

> JJDevHub is a dual-purpose platform. Publicly, it serves as an educational coding blog for students. Privately, behind a strict IAM gate, it acts as a personalized CV manager and company-specific resume generator.

## The Dual Architecture

The system is built using an Event-Driven Microservices architecture, demonstrating complex domain separation and RBAC (Role-Based Access Control).

- **Public Face (Student Blog):** A high-performance reading platform serving code snippets, tutorials, and work experience timeline.
- **Hidden Core (CV Manager):** A secured administration panel for managing professional experience, skills, and generating tailored CVs for specific company applications.

## Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| Backend | .NET 10, ASP.NET Core Minimal APIs | API, business logic, CQRS |
| Write DB | PostgreSQL 16 | Command side (EF Core) |
| Read DB | MongoDB | Query side (denormalized views) |
| Message Broker | Apache Kafka | Event-driven sync between write/read stores |
| Frontend Web | Angular 21 + Angular Material | SPA client |
| Frontend Mobile | React Native 0.84 + React Native Paper | iOS/Android client |
| CI/CD | Jenkins (Multibranch Pipeline) | 9-stage automated pipeline |
| Code Quality | SonarQube | Static analysis + coverage gates |
| Monitoring | Prometheus + Grafana (OpenTelemetry) | Metrics, dashboards |
| Secrets | HashiCorp Vault | Dynamic secret injection |
| Reverse Proxy | Nginx | Routing, SSL termination |
| Edge / Network | Cloudflare (DNS/WAF) | DDoS protection, CDN (production) |
| Containers | Docker + Docker Compose | Service orchestration |
| IAM | Keycloak (OIDC) | Identity, roles, JWT |

## Core Data Flow (CQRS)

State changes (e.g. adding work experience) are recorded in **PostgreSQL** via EF Core. Domain Events are raised by aggregates and dispatched through **MediatR**. Domain Event Handlers publish Integration Events to **Apache Kafka**. A background Sync Worker consumes these events and upserts denormalized read models into **MongoDB**, which serves frontend queries with low latency.

```
[Angular/Mobile] --> [Nginx] --> [Content API]
                                      |
                         Command --> [PostgreSQL]
                                      |
                         Domain Event --> [MediatR]
                                           |
                         Integration Event --> [Kafka]
                                                 |
                                          [Sync Worker]
                                                 |
                                          [MongoDB] <-- Query <-- [Content API] <-- [Frontend]
```

## Security Posture

Zero Trust approach. The frontend application validates JWT roles issued by **Keycloak**. If the `Owner` claim is present, the hidden CV management modules are unlocked. All microservice secrets and database connection strings are injected dynamically via **HashiCorp Vault**.

## Project Structure

```
JJDevHub/
├── src/
│   ├── Services/
│   │   ├── JJDevHub.Content/           # Main bounded context (DDD/CQRS)
│   │   │   ├── JJDevHub.Content.Api/           # Endpoints, middleware, health checks
│   │   │   ├── JJDevHub.Content.Application/   # Commands, queries, MediatR handlers
│   │   │   ├── JJDevHub.Content.Core/          # Domain entities, value objects, events
│   │   │   ├── JJDevHub.Content.Infrastructure/ # Kafka, MongoDB read store
│   │   │   └── JJDevHub.Content.Persistence/   # EF Core, PostgreSQL
│   │   ├── JJDevHub.Shared.Kernel/     # DDD building blocks, CQRS interfaces
│   │   ├── JJDevHub.Analytics/         # Analytics bounded context
│   │   ├── JJDevHub.Identity/          # Identity service (Keycloak integration)
│   │   ├── JJDevHub.AI.Gateway/        # AI Gateway service
│   │   ├── JJDevHub.Notification/      # Notification service
│   │   ├── JJDevHub.Education/         # Education service
│   │   └── JJDevHub.Sync/             # Kafka consumer / sync worker
│   └── Clients/
│       ├── web/                        # Angular 21 SPA
│       └── mobile/JJDevHubMobile/      # React Native app
├── tests/
│   ├── JJDevHub.Content.UnitTests/     # xUnit + NSubstitute + FluentAssertions
│   └── JJDevHub.Content.IntegrationTests/ # Testcontainers (PostgreSQL, MongoDB)
├── infra/
│   └── docker/
│       ├── docker-compose.yml          # Full stack orchestration
│       ├── nginx/                      # Reverse proxy config
│       └── monitoring/                 # Prometheus + Grafana
├── docs/
│   ├── architecture-tutorial.md        # Full architecture guide
│   ├── jenkins_tutorial.md             # Jenkins setup guide
│   └── backlog/                        # Sprint task descriptions
├── Jenkinsfile                         # CI/CD pipeline definition
└── JJDevHub.sln                        # .NET solution
```

## Quick Start

```bash
cd infra/docker
docker-compose up -d
```

Services will be available at:
- **Web App:** http://localhost:8081
- **Content API:** http://localhost:8081/api/content/
- **Grafana:** http://localhost:3000 (admin/admin)
- **Prometheus:** http://localhost:9090
- **SonarQube:** http://localhost:9000
- **Jenkins:** http://localhost:8082
- **Vault:** http://localhost:8201

## Documentation

- [Architecture Tutorial](docs/architecture-tutorial.md) - Full DDD/CQRS architecture guide
- [Jenkins Tutorial](docs/jenkins_tutorial.md) - CI/CD pipeline setup
- [Backlog & Roadmap](docs/backlog/README.md) - Sprint tasks and implementation plan
- [NuGet Reference](docs/backlog/nuget-reference.md) - Package dependencies
- [Hosting & Cloudflare](docs/backlog/hosting-cloudflare.md) - Production deployment guide
