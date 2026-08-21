# Task 6.2: Napisanie Pelnego Jenkinsfile

| Pole | Wartosc |
|------|---------|
| Sprint | 6 - Observability & DevOps |
| Status | DONE |
| Priorytet | High |
| Estymacja | 8 story points |
| Powiazane pliki | `Jenkinsfile`, `infra/docker/docker-compose.yml`, `infra/docker/jenkins-compose.yml` |

## Opis

Jenkinsfile definiuje pelny CI/CD pipeline budujacy wszystkie mikroserwisy, uruchamiajacy testy, analizujacy jakosc kodu i wdrazajacy na Docker. Pipeline ma 9 etapow, od restore zaleznosci po deploy. Jenkinsfile jest w rocie repozytorium (standard Multibranch Pipeline).

### Co jest zaimplementowane

Pelny 9-stage pipeline:

| Stage | Opis | Komendy |
|-------|------|---------|
| 1. Restore | Przywrocenie zaleznosci NuGet | `dotnet restore JJDevHub.sln` |
| 2. Build | Kompilacja w trybie Release | `dotnet build JJDevHub.sln --configuration Release --no-restore` |
| 3. Unit Tests | Testy jednostkowe z pokryciem | `dotnet test tests/JJDevHub.Content.UnitTests` (TRX + OpenCover) |
| 4. Integration Tests | Testy integracyjne z Testcontainers | `dotnet test tests/JJDevHub.Content.IntegrationTests` (TRX + OpenCover) |
| 5. Angular Build | Budowanie frontendu Angular | `npm ci` + `npx ng build --configuration production` |
| 6. SonarQube Analysis | Analiza statyczna kodu | `dotnet-sonarscanner begin/end` z raportami pokrycia |
| 7. Quality Gate | Sprawdzenie Quality Gate | `waitForQualityGate()` (5 min timeout) |
| 8. Docker Build | Budowanie obrazow Docker (parallel) | 8 obrazow: Content, Analytics, Identity, AI Gateway, Notification, Education, Sync, Angular |
| 9. Deploy | Wdrozenie na Docker Compose | `docker-compose up -d --remove-orphans` |

**Konfiguracja:**
- Environment: `DOTNET_CLI_TELEMETRY_OPTOUT=1`, `SONAR_HOST_URL`, `SONAR_PROJECT_KEY=JJDevHub`
- Post: `cleanWs()` na success i failure
- Test results: archiwizowane w `test-results/unit/**` i `test-results/integration/**`
- Docker Build: parallel execution (Stage 8)

**Jenkins Docker setup:**
- `jenkins-compose.yml` z `jenkins/jenkins:lts-jdk17`
- Porty: `8082:8080` (UI), `50001:50000` (agent)
- Docker socket mounted (`/var/run/docker.sock`)
- Wolumen: `jenkins_home`

## Kryteria akceptacji

- [x] 9-stage pipeline od Restore do Deploy
- [x] Unit testy z pokryciem kodu (OpenCover format)
- [x] Integration testy z Testcontainers
- [x] Angular build (npm ci + ng build production)
- [x] SonarQube analysis z raportami
- [x] Quality Gate check z timeout
- [x] Parallel Docker build (8 obrazow)
- [x] Deploy na docker-compose
- [x] Post-build cleanup (cleanWs)
- [x] Test results archiving

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `Microsoft.NET.Test.Sdk` | 18.0.1 | UnitTests, IntegrationTests | Test SDK - wymagany przez xunit runner |
| `xunit` | 2.9.3 | UnitTests, IntegrationTests | Test framework |
| `xunit.runner.visualstudio` | 3.1.0 | UnitTests, IntegrationTests | Test runner adapter |
| `coverlet.collector` | 8.0.0 | UnitTests, IntegrationTests | Zbieranie pokrycia kodu |
| `FluentAssertions` | 8.3.0 | UnitTests, IntegrationTests | Czytelne asercje |
| `NSubstitute` | 5.3.0 | UnitTests, IntegrationTests | Mocking framework |
| `Testcontainers.PostgreSql` | 4.10.0 | IntegrationTests | PostgreSQL w kontenerze testowym |
| `Testcontainers.MongoDb` | 4.10.0 | IntegrationTests | MongoDB w kontenerze testowym |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.3 | IntegrationTests | WebApplicationFactory dla API tests |

## Przeplyw pipeline

```
[Git Push] → Jenkins Multibranch Pipeline
    │
    ├── Stage 1: Restore (dotnet restore)
    ├── Stage 2: Build (dotnet build Release)
    ├── Stage 3: Unit Tests (xunit + coverlet)
    ├── Stage 4: Integration Tests (Testcontainers)
    ├── Stage 5: Angular Build (npm ci + ng build)
    ├── Stage 6: SonarQube Analysis (dotnet-sonarscanner)
    ├── Stage 7: Quality Gate (waitForQualityGate)
    ├── Stage 8: Docker Build (parallel x8)
    │   ├── Content API
    │   ├── Analytics API
    │   ├── Identity API
    │   ├── AI Gateway
    │   ├── Notification API
    │   ├── Education API
    │   ├── Sync Worker
    │   └── Angular Web
    └── Stage 9: Deploy (docker-compose up -d)
```

## Zaleznosci

- **Wymaga:** Wszystkie serwisy musza miec Dockerfile
- **Blokuje:** Task 6.1 (SonarQube Quality Gate wymaga Jenkinsfile)

## Notatki techniczne

- Jenkinsfile jest w rocie repozytorium co pozwala na automatyczny discovery przez Multibranch Pipeline.
- Stage 4 (Integration Tests) uzywa Testcontainers ktore uruchamiaja PostgreSQL i MongoDB w kontenerach Docker wewnatrz Jenkinsa. Wymaga Docker-in-Docker (Docker socket mounted).
- Stage 8 (Docker Build) uzywa `parallel` block - wszystkie 8 obrazow jest budowanych rownolegle, co znaczaco skraca czas pipeline.
- `cleanWs()` czysci workspace po zakonczeniu (success lub failure) - zapobiega akumulacji plikow na Jenkins master.
- Na przyszlosc: dodanie stage'u dla React Native build (Gradle/Xcode) i deployment na staging environment przed produkcja.
