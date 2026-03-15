# Task 6.1: Podpiecie SonarQube z Quality Gate

| Pole | Wartosc |
|------|---------|
| Sprint | 6 - Observability & DevOps |
| Status | IN PROGRESS |
| Priorytet | High |
| Estymacja | 5 story points |
| Powiazane pliki | `infra/docker/docker-compose.yml`, `Jenkinsfile` |

## Opis

SonarQube to platforma do automatycznej analizy jakosci kodu. Wykrywa bugi, code smells, security vulnerabilities i mierzy pokrycie testami. Quality Gate to zestaw warunkow ktore musza byc spelnione aby build przeszedl - np. minimum 80% pokrycia testami dla nowego kodu. SonarQube jest juz w docker-compose i Jenkinsfile, ale brakuje konfiguracji Quality Gate i szczegolowej konfiguracji projektu.

### Co juz jest zrobione

**Docker-compose:**
- SonarQube: `sonarqube:lts-community` na porcie `9000`
- SonarQube DB: `postgres:16-alpine` (dedykowana baza)
- Wolumeny: `sonarqube_data`, `sonarqube_logs`, `sonarqube_extensions`

**Jenkinsfile (Stage 6 - SonarQube Analysis):**
- `dotnet-sonarscanner begin` z kluczem projektu `JJDevHub`
- Raporty pokrycia: OpenCover format + TRX test results
- Exclusions: `**/Migrations/**`, `**/Program.cs`, `**/DependencyInjection.cs`
- Stage 7 - Quality Gate: `waitForQualityGate()` z 5 min timeout

### Co pozostalo

- Konfiguracja Quality Gate w SonarQube UI (customowy profile)
- Ustawienie minimalnego pokrycia testami: 80% dla nowego kodu
- Konfiguracja regu SonarQube dla C# / .NET 10
- Token SonarQube w Vault (zamiast hardcoded w Jenkins)
- Webhook SonarQube -> Jenkins (dla Quality Gate callback)

## Kryteria akceptacji

- [x] SonarQube uruchomiony w Docker z dedykowana baza PostgreSQL
- [x] Jenkinsfile z dotnet-sonarscanner (analiza + Quality Gate)
- [x] Coverage exclusions dla Migrations, Program.cs, DI
- [ ] Custom Quality Gate: minimum 80% pokrycia dla nowego kodu
- [ ] Custom Quality Gate: 0 nowych bugow, 0 nowych vulnerabilities
- [ ] Custom Quality Gate: Code Smells < 10 dla nowego kodu
- [ ] Duplications < 3% dla nowego kodu
- [ ] Webhook SonarQube -> Jenkins skonfigurowany
- [ ] Token SonarQube przechowywany w Vault
- [ ] Quality Profile C# z regulami dostosowanymi do .NET 10

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `coverlet.collector` | 8.0.0 | JJDevHub.Content.UnitTests | Juz zainstalowany - zbieranie pokrycia kodu w formacie OpenCover |
| `coverlet.collector` | 8.0.0 | JJDevHub.Content.IntegrationTests | Juz zainstalowany |

## Kroki implementacji

1. **Skonfiguruj Quality Gate w SonarQube:**
   - Zaloguj sie na `http://localhost:9000` (admin/admin)
   - Quality Gates -> Create -> "JJDevHub Gate"
   - Warunki:
     - Coverage on New Code >= 80%
     - New Bugs = 0
     - New Vulnerabilities = 0
     - New Code Smells <= 10
     - Duplicated Lines on New Code <= 3%
   - Ustaw jako domyslny gate

2. **Stworz token API w SonarQube:**
   - My Account -> Security -> Generate Token: `jenkins-scanner`
   - Zapisz token w Vault: `secret/sonarqube/token`

3. **Skonfiguruj webhook SonarQube -> Jenkins:**
   - Administration -> Configuration -> Webhooks
   - URL: `http://jjdevhub-jenkins:8080/sonarqube-webhook/`
   - Webhook pozwala na asynchroniczne sprawdzenie Quality Gate (zamiast polling)

4. **Zaktualizuj Jenkinsfile:**
   - Pobierz token SonarQube z Vault zamiast z Jenkins credentials
   - Upewnij sie ze `waitForQualityGate()` uzywa webhook (nie polling)

5. **Skonfiguruj Quality Profile dla C#:**
   - Quality Profiles -> C# -> Create -> "JJDevHub C#"
   - Aktywuj dodatkowe reguly dla .NET 10 (async/await, nullable, etc.)
   - Deaktywuj reguly nieistotne dla projektu

6. **Dodaj `sonar-project.properties` do roota:**
   ```properties
   sonar.projectKey=JJDevHub
   sonar.projectName=JJDevHub
   sonar.sources=src/
   sonar.tests=tests/
   sonar.exclusions=**/Migrations/**,**/bin/**,**/obj/**
   sonar.coverage.exclusions=**/Program.cs,**/DependencyInjection.cs
   ```

## Zaleznosci

- **Wymaga:** Task 6.2 (Jenkinsfile - juz DONE)
- **Blokuje:** Nic (ale blokuje merge do main jesli Quality Gate nie przejdzie)

## Notatki techniczne

- SonarQube LTS Community jest darmowy. Ograniczenia: brak branch analysis (tylko main), brak pull request decoration. Dla branch analysis potrzeba Developer Edition (platna).
- `coverlet.collector` generuje raporty pokrycia w formacie OpenCover ktory jest natywnie obslugiwany przez SonarQube scanner.
- Quality Gate jest sprawdzany asynchronicznie - Jenkins czeka na webhook od SonarQube (max 5 minut). Jesli SonarQube jest przeciazony, analiza moze trwac dluzej.
- Exclusions w `sonar-project.properties` i w Jenkinsfile musza byc spojne. Properties file jest uzywany przez dotnet-sonarscanner.
- Dokumentacja: https://docs.sonarsource.com/sonarqube/latest/
