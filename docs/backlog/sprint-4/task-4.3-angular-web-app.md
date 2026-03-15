# Task 4.3: Aplikacja Angular 21 - Widok Publiczny

| Pole | Wartosc |
|------|---------|
| Sprint | 4 - Public Face & Mobile |
| Status | IN PROGRESS |
| Priorytet | High |
| Estymacja | 13 story points |
| Powiazane pliki | `src/Clients/web/` |

## Opis

Angular 21 SPA (Single Page Application) sluzy jako glowny interfejs webowy JJDevHub. Publicznie prezentuje blog programistyczny (portfolio, doswiadczenie zawodowe, snippety kodu). Dla roli `Owner` odkrywa ukryte moduly administracyjne (Sprint 5). Uzywa Angular Material jako biblioteki komponentow UI.

### Co juz jest zrobione

**Konfiguracja:**
- Angular 21 z Angular CLI ~21.2.1
- Angular Material + CDK
- Providers: Router, Animations, HttpClient, BrowserGlobalErrorListeners
- Lazy loading routes
- Environment config: dev `http://localhost:8081`, prod relative URL

**Routes:**
- `''` -> `HomePage` (lazy loaded)
- `'work-experience'` -> `WorkExperiencePage` (lazy loaded)
- `'about'` -> `AboutPage` (lazy loaded)

**Komponenty:**
- `AppComponent` - root layout z Material toolbar, sidenav, nawigacja (Home, Work Experience, About)
- `HomePage` - hero section, karty linki do Work Experience i About
- `WorkExperiencePage` - laduje dane z API, wyswietla timeline, stany loading/error
- `AboutPage` - opis architektury, tech stack, przeglad serwisow

**Serwisy:**
- `ApiService` - metody: `getWorkExperiences()`, `getWorkExperience(id)`, `createWorkExperience()`, `updateWorkExperience()`, `deleteWorkExperience()`
- Base URL z environment config

**Modele:**
- `WorkExperience` - id, companyName, position, period, isPublic, isActive, createdDate, modifiedDate

### Co pozostalo

- Widok listy blog postow i snippetow kodu
- Widok pojedynczego posta z syntax highlighting
- Formularz dodawania/edycji doswiadczenia (Admin)
- Integracja z Keycloak (login/logout, JWT w headerach)
- RBAC - odkrywanie ukrytych modulow (Sprint 5)
- PWA support (Service Worker)
- SEO - Server-Side Rendering lub prerendering
- Responsive design na mobile

## Kryteria akceptacji

- [x] Angular 21 z Angular Material
- [x] Lazy loading routes (Home, WorkExperience, About)
- [x] Material toolbar z nawigacja i sidenav
- [x] ApiService z pelnym CRUD do Content API
- [x] WorkExperiencePage z timeline UI, loading i error handling
- [x] Environment config (dev/prod base URL)
- [ ] Widok listy blog postow
- [ ] Widok pojedynczego posta z syntax highlighting
- [ ] Formularz CRUD dla WorkExperience (Admin)
- [ ] Integracja z Keycloak (OIDC login/logout)
- [ ] HTTP Interceptor dodajacy JWT token do zapytan
- [ ] Responsive design (mobile breakpoints)

## Wymagane pakiety npm

| Pakiet | Wersja | Uzasadnienie |
|--------|--------|--------------|
| `@angular/core` | 21.x | Juz zainstalowany - framework |
| `@angular/material` | 21.x | Juz zainstalowany - Material Design components |
| `keycloak-angular` | latest | NOWY - Angular adapter dla Keycloak OIDC |
| `keycloak-js` | latest | NOWY - Keycloak JavaScript client |
| `ngx-highlightjs` | latest | NOWY - syntax highlighting dla snippetow kodu |
| `@angular/pwa` | latest | NOWY - Progressive Web App support |

## Architektura komponentow

```
AppComponent (root)
├── Toolbar (Material)
├── Sidenav (Material, mobile)
├── Router Outlet
│   ├── HomePage (lazy)
│   │   ├── Hero Section
│   │   └── Feature Cards
│   ├── WorkExperiencePage (lazy)
│   │   ├── Loading Spinner
│   │   ├── Error Alert
│   │   └── Timeline List
│   ├── AboutPage (lazy)
│   │   ├── Architecture Diagram
│   │   └── Tech Stack Grid
│   ├── BlogListPage (lazy) [TODO]
│   ├── BlogPostPage (lazy) [TODO]
│   └── AdminModule (lazy, guarded) [TODO - Sprint 5]
│       ├── WorkExperienceFormPage
│       ├── CVManagerPage
│       └── ApplicationTrackerPage
└── Footer
```

## Zaleznosci

- **Wymaga:** Task 4.1 (Content API endpointy), Task 4.2 (Nginx routing do Angular)
- **Blokuje:** Task 5.1 (Angular RBAC rozbudowuje te aplikacje)

## Notatki techniczne

- Lazy loading routes zmniejszaja poczatkowy bundle size - kazda strona jest ladowana na zadanie.
- Angular Material uzywa systemu tematow (theming) - mozna latwo zmienic kolorystyke calej aplikacji.
- `ApiService` uzywa `HttpClient` z Angular - w przyszlosci nalezy dodac `HttpInterceptor` ktory automatycznie dolacza JWT token z Keycloak do kazdego requestu.
- Environment config (`environment.ts` / `environment.prod.ts`) pozwala na rozne base URL dla dev i produkcji. W produkcji URL jest relatywny (pusty string) bo Angular jest serwowany przez ten sam Nginx.
- Angular 21 wprowadza Signals i nowe control flow (`@if`, `@for`) - rozwazyc migracje z klasycznych `*ngIf` / `*ngFor`.
