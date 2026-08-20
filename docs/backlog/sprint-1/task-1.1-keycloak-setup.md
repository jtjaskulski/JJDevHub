 # Task 1.1: Setup Serwera Tozsamosci (Keycloak)

| Pole | Wartosc |
|------|---------|
| Sprint | 1 - Identity & Foundation |
| Status | DONE |
| Priorytet | High |
| Estymacja | 8 story points |
| Powiazane pliki | `infra/docker/docker-compose.yml`, `src/Services/JJDevHub.Identity/`, `src/Services/JJDevHub.Content/JJDevHub.Content.Api/Program.cs` |

## Opis

Keycloak to serwer tozsamosci (IAM) oparty na OpenID Connect (OIDC). Sluzy jako centralny punkt autentykacji i autoryzacji dla calego ekosystemu JJDevHub. Zadanie obejmuje pelna konfiguracje Keycloak w Dockerze, definicje realm, klientow OIDC oraz rol (`Student`, `Owner`), a nastepnie integracje z backendem .NET poprzez middleware JWT Bearer.

### Co juz jest zrobione

- Keycloak w docker-compose (`quay.io/keycloak/keycloak:26.0`, port `8083`, `start-dev --import-realm`)
- Realm `jjdevhub` z rolami `Student` / `Owner`, klientami `jjdevhub-web` (PKCE) i `jjdevhub-api`
- Seed users: `owner@test.com` / `Owner123!` (Owner), `student@test.com` / `Student123!` (Student)
- JWT Bearer + polityka `OwnerOnly` w Content.Api; GET publiczne, zapis wymaga Owner
- Health check Keycloak w `/health` API
- Angular: `environment.keycloak.url = http://localhost:8083`

Uwaga: `--import-realm` dziala tylko gdy realm jeszcze nie istnieje. Aby wgrac seed users na istniejacym stacku, usun kontener/wolumen Keycloak albo utworz userow w Admin Console.

### Co pozostalo (poza MVP)

- Identity API nadal stub `/health` (Keycloak jest IdP — osobny serwis nie jest wymagany)
- Dev Keycloak uzywa H2 (`start-dev`); produkcja: Postgres w `docker-compose.prod.yml`
- `verify-token-audience` ustawione na `false` w compose (dev)

## Kryteria akceptacji

- [ ] Kontener Keycloak uruchomiony w docker-compose z persystentna baza danych
- [ ] Realm `jjdevhub` z dwoma rolami: `Student` (publiczny dostep) i `Owner` (pelny dostep)
- [ ] Klient OIDC `jjdevhub-web` skonfigurowany dla Angular SPA (Authorization Code + PKCE)
- [ ] Klient OIDC `jjdevhub-api` skonfigurowany dla backendu (Client Credentials)
- [ ] Content.Api waliduje JWT tokeny z Keycloak
- [ ] Endpointy zapisu (`POST`, `PUT`, `DELETE`) wymagaja roli `Owner`
- [ ] Endpointy odczytu (`GET` z `publicOnly=true`) sa publiczne
- [ ] Health check Keycloak dostepny w docker-compose

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `Keycloak.AuthServices.Authentication` | latest | JJDevHub.Content.Api | Integracja OIDC z Keycloak - automatyczna konfiguracja JWT Bearer na bazie Keycloak discovery endpoint |
| `Keycloak.AuthServices.Authorization` | latest | JJDevHub.Content.Api | Polityki autoryzacji mapowane na role Keycloak (realm roles + resource roles) |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.x | JJDevHub.Content.Api | Bazowy middleware JWT Bearer (zaleznosc Keycloak.AuthServices) |

## Kroki implementacji

1. **Dodaj Keycloak do docker-compose.yml:**
   - Obraz: `quay.io/keycloak/keycloak:latest`
   - Porty: `8083:8080`
   - Zmienne: `KEYCLOAK_ADMIN`, `KEYCLOAK_ADMIN_PASSWORD`
   - Komenda: `start-dev` (development) lub `start` (production z DB)
   - Wolumen na persystentna baze (lub podlaczenie do istniejacego PostgreSQL)
   - Siec: `jjdevhub-net`

2. **Skonfiguruj realm `jjdevhub`:**
   - Eksportuj konfiguracje realm do JSON (`infra/docker/keycloak/realm-export.json`)
   - Zdefiniuj role: `Student`, `Owner`
   - Zdefiniuj klientow: `jjdevhub-web` (public, Authorization Code + PKCE), `jjdevhub-api` (confidential)
   - Zdefiniuj uzytkownikow testowych: `student@test.com` (Student), `owner@test.com` (Owner)

3. **Zainstaluj pakiety NuGet w Content.Api:**
   ```
   dotnet add src/Services/JJDevHub.Content/JJDevHub.Content.Api package Keycloak.AuthServices.Authentication
   dotnet add src/Services/JJDevHub.Content/JJDevHub.Content.Api package Keycloak.AuthServices.Authorization
   ```

4. **Skonfiguruj JWT Bearer w Program.cs:**
   - Dodaj `builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration)`
   - Dodaj `builder.Services.AddAuthorization()` z polityka `OwnerOnly`
   - Dodaj `app.UseAuthentication()` i `app.UseAuthorization()`

5. **Zabezpiecz endpointy w WorkExperienceEndpoints.cs:**
   - `GET /` i `GET /{id}` - `AllowAnonymous` (filtr `publicOnly`)
   - `POST /`, `PUT /{id}`, `DELETE /{id}` - `RequireAuthorization("OwnerOnly")`

6. **Dodaj sekcje konfiguracji w appsettings.json:**
   ```json
   {
     "Keycloak": {
       "realm": "jjdevhub",
       "auth-server-url": "http://jjdevhub-keycloak:8080/",
       "ssl-required": "none",
       "resource": "jjdevhub-api",
       "verify-token-audience": true
     }
   }
   ```

7. **Zaktualizuj Nginx o routing do Keycloak:**
   - Dodaj `location /auth/` proxy do Keycloak

## Zaleznosci

- **Wymaga:** Task 1.4 (bazy danych musza byc uruchomione)
- **Blokuje:** Task 5.1 (Angular RBAC), Task 5.2 (Application Tracker)

## Notatki techniczne

- W srodowisku dev Keycloak uzywa wbudowanej bazy H2. Na produkcji nalezy podlaczyc go do dedykowanej instancji PostgreSQL.
- Keycloak realm export/import pozwala na wersjonowanie konfiguracji IAM w repozytorium.
- Pakiet `Keycloak.AuthServices` automatycznie konfiguruje JWT Bearer na bazie Keycloak discovery endpoint (`/.well-known/openid-configuration`), co eliminuje reczna konfiguracje `Authority`, `Audience`, `TokenValidationParameters`.
- Angular SPA powinien uzywac flow `Authorization Code + PKCE` (nie Implicit Flow, ktory jest deprecated).
- Dokumentacja: https://www.keycloak.org/documentation, https://github.com/NikiforovAll/keycloak-authorization-services-dotnet
