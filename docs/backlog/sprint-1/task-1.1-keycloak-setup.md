 # Task 1.1: Setup Serwera Tozsamosci (Keycloak)

| Pole | Wartosc |
|------|---------|
| Sprint | 1 - Identity & Foundation |
| Status | IN PROGRESS |
| Priorytet | High |
| Estymacja | 8 story points |
| Powiazane pliki | `infra/docker/docker-compose.yml`, `src/Services/JJDevHub.Identity/`, `src/Services/JJDevHub.Content/JJDevHub.Content.Api/Program.cs` |

## Opis

Keycloak to serwer tozsamosci (IAM) oparty na OpenID Connect (OIDC). Sluzy jako centralny punkt autentykacji i autoryzacji dla calego ekosystemu JJDevHub. Zadanie obejmuje pelna konfiguracje Keycloak w Dockerze, definicje realm, klientow OIDC oraz rol (`Student`, `Owner`), a nastepnie integracje z backendem .NET poprzez middleware JWT Bearer.

### Co juz jest zrobione

- Keycloak **nie jest jeszcze** w docker-compose (tylko stub Identity API z health endpoint)
- Identity API (`src/Services/JJDevHub.Identity/`) to minimalny serwis z `GET /health`
- Brak konfiguracji OIDC/JWT w zadnym serwisie .NET

### Co pozostalo

- Dodanie kontenera Keycloak do docker-compose
- Konfiguracja realm `jjdevhub` z klientami i rolami
- Integracja JWT Bearer w Content.Api i pozostalych serwisach
- Rozbudowa Identity API o proxy/integracje z Keycloak

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
