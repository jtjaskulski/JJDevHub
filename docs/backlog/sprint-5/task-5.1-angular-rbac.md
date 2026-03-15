# Task 5.1: Rozbudowa Angulara o Mechanizm RBAC

| Pole | Wartosc |
|------|---------|
| Sprint | 5 - The Secret Feature |
| Status | TODO |
| Priorytet | High |
| Estymacja | 8 story points |
| Powiazane pliki | `src/Clients/web/src/app/` |

## Opis

Implementacja Role-Based Access Control (RBAC) w Angular SPA. Aplikacja integruje sie z Keycloak przez OIDC (Authorization Code + PKCE). Po zalogowaniu JWT token zawiera role uzytkownika. Jesli uzytkownik ma role `Owner`, Angular odkrywa ukryte menu administracyjne z modulami: CV Manager, Application Tracker, Work Experience CRUD form. Dla roli `Student` lub niezalogowanego uzytkownika widoczne sa tylko publiczne strony.

### Co trzeba zrobic

1. Integracja z Keycloak (login/logout flow)
2. HTTP Interceptor dodajacy JWT do kazdego requestu API
3. AuthGuard chroniacy routy administracyjne
4. Serwis `AuthService` z informacja o roli uzytkownika
5. Dynamiczne menu - ukryte elementy widoczne tylko dla `Owner`
6. Lazy-loaded `AdminModule` z chronionymi stronami

## Kryteria akceptacji

- [ ] Przycisk Login/Logout w toolbarze
- [ ] Keycloak login flow (redirect do Keycloak, powrot z tokenem)
- [ ] JWT token automatycznie dolaczany do HTTP requestow (Interceptor)
- [ ] Token refresh automatyczny (silent refresh)
- [ ] `AuthService` eksponujacy: `isLoggedIn$`, `userRoles$`, `isOwner$`
- [ ] `AuthGuard` blokujacy dostep do `/admin/*` dla nie-Owner
- [ ] Menu nawigacyjne: publiczne linki zawsze widoczne, "Admin Panel" widoczny tylko dla Owner
- [ ] Lazy-loaded `AdminModule` z routami: `/admin/work-experience`, `/admin/cv`, `/admin/tracker`
- [ ] Graceful handling: jesli Keycloak niedostepny, aplikacja dziala w trybie publicznym (bez logowania)

## Wymagane pakiety npm

| Pakiet | Wersja | Uzasadnienie |
|--------|--------|--------------|
| `keycloak-angular` | latest | Angular adapter - `KeycloakService`, `KeycloakAuthGuard`, inicjalizacja w `APP_INITIALIZER` |
| `keycloak-js` | latest | Keycloak JavaScript adapter (zaleznosc keycloak-angular) |

## Kroki implementacji

1. **Zainstaluj pakiety:**
   ```bash
   cd src/Clients/web
   npm install keycloak-angular keycloak-js
   ```

2. **Stworz `AuthService`:**
   ```typescript
   @Injectable({ providedIn: 'root' })
   export class AuthService {
     isLoggedIn$ = signal(false);
     isOwner$ = signal(false);
     userName$ = signal<string | null>(null);

     constructor(private keycloak: KeycloakService) {}

     async init(): Promise<void> {
       const loggedIn = await this.keycloak.isLoggedIn();
       this.isLoggedIn$.set(loggedIn);
       if (loggedIn) {
         const roles = this.keycloak.getUserRoles();
         this.isOwner$.set(roles.includes('Owner'));
         const profile = await this.keycloak.loadUserProfile();
         this.userName$.set(profile.firstName);
       }
     }

     login(): void { this.keycloak.login(); }
     logout(): void { this.keycloak.logout(); }
   }
   ```

3. **Skonfiguruj Keycloak w `app.config.ts`:**
   ```typescript
   export function initializeKeycloak(keycloak: KeycloakService) {
     return () => keycloak.init({
       config: { url: environment.keycloakUrl, realm: 'jjdevhub', clientId: 'jjdevhub-web' },
       initOptions: { onLoad: 'check-sso', silentCheckSsoRedirectUri: '...' },
       bearerExcludedUrls: ['/assets']
     });
   }
   ```

4. **Dodaj HTTP Interceptor:**
   - `keycloak-angular` automatycznie dodaje `KeycloakBearerInterceptor` ktory dolacza `Authorization: Bearer <token>` do zapytan

5. **Stworz `AuthGuard`:**
   ```typescript
   export class OwnerGuard extends KeycloakAuthGuard {
     isAccessAllowed(route: ActivatedRouteSnapshot): Promise<boolean> {
       return Promise.resolve(this.roles.includes('Owner'));
     }
   }
   ```

6. **Dodaj routy administracyjne:**
   ```typescript
   { path: 'admin', canActivate: [OwnerGuard], loadChildren: () => import('./admin/admin.module') }
   ```

7. **Zaktualizuj toolbar w AppComponent:**
   - `@if (authService.isOwner$())` -> pokaz link "Admin Panel"
   - Przycisk Login/Logout w zaleznosci od stanu autentykacji

8. **Stworz `AdminModule` (lazy loaded):**
   - Route: `/admin/work-experience` -> formularz CRUD
   - Route: `/admin/cv` -> CV Manager (Task 5.3)
   - Route: `/admin/tracker` -> Application Tracker (Task 5.2)

## Zaleznosci

- **Wymaga:** Task 1.1 (Keycloak serwer z realm i rolami), Task 4.3 (Angular bazowa aplikacja)
- **Blokuje:** Task 5.2 (Application Tracker UI), Task 5.3 (CV Generation UI)

## Notatki techniczne

- `keycloak-angular` uzywa `APP_INITIALIZER` do inicjalizacji Keycloak PRZED renderowaniem aplikacji. Jesli Keycloak jest niedostepny, `onLoad: 'check-sso'` pozwala na kontynuacje bez logowania (w przeciwienstwie do `'login-required'` ktore wymusza login).
- Authorization Code + PKCE jest rekomendowanym flow dla SPA (nie Implicit Flow). PKCE chroni przed atakami code interception.
- Silent SSO check (`silentCheckSsoRedirectUri`) pozwala na automatyczne sprawdzenie sesji Keycloak bez widocznego redirectu - uzytkownik nie widzi migniecia strony przy refresh.
- Token refresh jest obslugiwany automatycznie przez `keycloak-js` - token jest odswiezany w tle przed wygasnieciem.
- Angular Signals (`signal()`) zamiast klasycznych `BehaviorSubject` - nowoczesny reactive pattern w Angular 21.
- Menu "Admin Panel" jest renderowane warunkowo - nie istnieje w DOM jesli uzytkownik nie jest Owner. To nie jest security (bo kod Angular jest publicznie dostepny), ale UX. Prawdziwa ochrona jest na backendzie (JWT validation w API).
