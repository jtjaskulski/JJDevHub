# Task 4.3: Aplikacja Angular 21 - Widok Publiczny

| Pole | Wartosc |
|------|---------|
| Sprint | 4 - Public Face & Mobile |
| Status | DONE |
| Priorytet | High |
| Estymacja | 13 story points |
| Powiazane pliki | `src/Clients/web/` |

## Opis

Angular 21 SPA (Single Page Application) sluzy jako glowny interfejs webowy JJDevHub. Publicznie prezentuje blog programistyczny (portfolio, doswiadczenie zawodowe, snippety kodu). Dla roli `Owner` odkrywa ukryte moduly administracyjne (Sprint 5). Uzywa Angular Material jako biblioteki komponentow UI.

### Co juz jest zrobione

**Konfiguracja:**
- Angular 21 z Angular CLI ~21.2.1
- Angular Material + CDK
- Providers: Router, Animations, HttpClient, BrowserGlobalErrorListeners, `provideHighlightOptions` (ngx-highlightjs)
- Keycloak (opcjonalnie): `provideKeycloak` + `includeBearerTokenInterceptor` gdy `environment.keycloak.url` jest ustawione
- Lazy loading routes
- Environment: `contentApiUrl` (dev `http://localhost:8081/api/v1/content`, prod `/api/v1/content`), opcjonalny blok `keycloak`, flaga `allowAdminWithoutAuth` (dev bez IdP)

**Routes:**
- `''` -> `HomePage` (lazy)
- `'work-experience'` -> `WorkExperiencePage` (lazy)
- `'about'` -> `AboutPage` (lazy)
- `'blog'` -> `BlogListPage` (lazy)
- `'blog/:slug'` -> `BlogPostPage` (lazy)
- `'admin/work-experience'` -> `AdminWorkExperiencePage` (lazy, `adminAuthGuard`)

**Komponenty:**
- `App` — toolbar, sidenav, nawigacja + Blog + Admin / Log in / Log out (zależnie od Keycloak)
- `HomePage` — hero, karty (Work Experience, Blog, About)
- `WorkExperiencePage` — timeline z API, loading/error
- `AboutPage` — architektura / stack
- `BlogListPage` / `BlogPostPage` — lista i szczegół wpisu; **dane mock** do czasu API bloga w Content
- `AdminWorkExperiencePage` — tabela + formularz CRUD (Material), snackbar przy błędach

**Serwisy:**
- `ApiService` — CRUD work-experiences względem `environment.contentApiUrl`
- `BlogService` — mock `Observable` (zamiana na HTTP po endpointach Content)
- `AuthService` — opcjonalny `Keycloak` z DI (`inject(Keycloak, { optional: true })`)

**Modele:**
- `WorkExperience` — m.in. id, version, companyName, position, daty, isPublic, isCurrent, durationInMonths
- `BlogPost` — slug, title, excerpt, intro, codeSample, codeLanguage

### Co pozostalo

- **Blog:** podłączenie listy i treści do Content API (read), zamiast `BlogService` mock
- **RBAC / Owner:** odkrywanie modułów admin i guardy oparte o role — Sprint 5 ([task-5.1-angular-rbac.md](../sprint-5/task-5.1-angular-rbac.md))
- **Keycloak w Docker / JWT na Content.Api:** Task 1.1 — do pełnego E2E zabezpieczeń zapisów
- **PWA** (`@angular/pwa`) — poza checklistą akceptacji Task 4.3
- **SEO / SSR lub prerendering** — osobny zakres

## Kryteria akceptacji

- [x] Angular 21 z Angular Material
- [x] Lazy loading routes (Home, WorkExperience, About, Blog, Admin WE)
- [x] Material toolbar z nawigacja i sidenav
- [x] ApiService z pelnym CRUD do Content API (`/api/v1/content/...`)
- [x] WorkExperiencePage z timeline UI, loading i error handling
- [x] Environment config (dev/prod + `contentApiUrl`, opcjonalny Keycloak)
- [x] Widok listy blog postow (mock)
- [x] Widok pojedynczego posta z syntax highlighting (ngx-highlightjs + motyw github-dark)
- [x] Formularz CRUD dla WorkExperience (Admin)
- [x] Integracja z Keycloak (OIDC login/logout) — gdy skonfigurowany w `environment`
- [x] HTTP Interceptor dodajacy JWT token do zapytan (`includeBearerTokenInterceptor` + `INCLUDE_BEARER_TOKEN_INTERCEPTOR_CONFIG` dla `/api/v1/content/`)
- [x] Responsive design (clamp / minmax / overflow na mobile, istniejący breakpoint toolbar 768px)

## Wymagane pakiety npm

| Pakiet | Wersja | Uzasadnienie |
|--------|--------|--------------|
| `@angular/core` | 21.x | Framework |
| `@angular/material` | 21.x | Material Design |
| `keycloak-angular` | ^21.0.0 | OIDC — `provideKeycloak`, interceptor |
| `keycloak-js` | ^26.x | Klient Keycloak (peer) |
| `ngx-highlightjs` | ^14.x | Podświetlanie kodu na stronie wpisu |
| `highlight.js` | ^11.x | Silnik highlight (full loader w `provideHighlightOptions`) |
| `@angular/pwa` | — | Opcjonalnie; nie wdrożone w tym tasku |

## Architektura komponentow

```
App (root)
├── Toolbar (Material)
├── Sidenav (Material, mobile)
├── Router Outlet
│   ├── HomePage (lazy)
│   ├── WorkExperiencePage (lazy)
│   ├── AboutPage (lazy)
│   ├── BlogListPage (lazy)
│   ├── BlogPostPage (lazy)
│   └── AdminWorkExperiencePage (lazy, adminAuthGuard)
└── (Footer — opcjonalnie)
```

Sprint 5: dalsze moduly admin (CV Manager, Application Tracker) — [task-5.1](../sprint-5/task-5.1-angular-rbac.md).

## Zaleznosci

- **Wymaga:** Task 4.1 (Content API endpointy), Task 4.2 (Nginx routing do Angular)
- **Blokuje:** Task 5.1 (Angular RBAC rozbudowuje te aplikacje)

## Notatki techniczne

- Lazy loading routes zmniejszaja poczatkowy bundle size - kazda strona jest ladowana na zadanie.
- Angular Material uzywa systemu tematow (theming) - mozna latwo zmienic kolorystyke calej aplikacji.
- Bearer token jest dolaczany **tylko** do URLi pasujacych do `INCLUDE_BEARER_TOKEN_INTERCEPTOR_CONFIG` (bezpieczenstwo — token nie idzie na zewnetrzne hosty).
- Gdy `environment.keycloak.url` jest puste, aplikacja dziala w trybie publicznym; `allowAdminWithoutAuth: true` tylko w dev (`environment.ts`), w prod `false`.
- `contentApiUrl` wskazuje kanoniczna sciezke wersjonowanego API (Nginx: `/api/v1/content/`).
- Motyw highlight.js: `github-dark.min.css` w `angular.json` styles.
