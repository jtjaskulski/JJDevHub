# Angular — klient web

Aplikacja to Angular **21** w [src/Clients/web](../../src/Clients/web). Package manager: **pnpm** (`packageManager` w `package.json` i w [angular.json](../../src/Clients/web/angular.json)).

Login i register nadal wołają API przez `/api`. Strony treści (`/:lang/...`) czytają tylko JSON z bundla — bez HTTP do API.

## Bootstrap

[main.ts](../../src/Clients/web/src/main.ts) przed `bootstrapApplication`:

1. Importuje `cssCustomProperties()` z `@jjdevhub/theme`.
2. Ustawia każdą parę na `document.documentElement` (`style.setProperty`).
3. Startuje `App` z [app.config.ts](../../src/Clients/web/src/app/app.config.ts).

Globalne style: Inter Variable + [styles.scss](../../src/Clients/web/src/styles.scss) (lista w `angular.json` → `styles`).

## Konfiguracja routera

```ts
provideRouter(routes, withViewTransitions())
```

`withViewTransitions()` włącza natywne View Transitions API przy zmianie trasy. Nie ma `@angular/animations` w zależnościach. Wejścia/wyjścia elementów list: atrybuty `animate.enter` / `animate.leave` — [przejscia-widoku.md](przejscia-widoku.md).

HTTP client + interceptor auth zostają dla `/login` i `/register`. `API_BASE_URL` to pusty string (proxy / nginx dokleja `/api`).

## Trasy

Definicja: [app.routes.ts](../../src/Clients/web/src/app/app.routes.ts).

- `''` → `redirectTo: 'pl'`
- `login`, `register` — `guestGuard`, **bez** prefiksu języka
- `:lang` — `langGuard`, dzieci: home, cv, courses (+ `:slug`), compendium (+ `:slug`), notes (+ `:slug`)
- `**` → `pl`

`langGuard` ([lang.guard.ts](../../src/Clients/web/src/app/routing/lang.guard.ts)) przepuszcza tylko `pl` i `en`; inaczej `UrlTree` na `/pl`.

## Shell

Komponent root: [app.ts](../../src/Clients/web/src/app/app.ts) / [app.html](../../src/Clients/web/src/app/app.html).

- Sticky header: brand `JJDevHub`, linki z `ContentService.site().nav`, przyciski PL/EN
- `<router-outlet />` w `main.shell-main`
- Footer z nazwą brandu

`switchLocale` rozbija URL, podmienia pierwszy segment jeśli to locale (albo dokłada locale), zachowuje query i hash.

W `ngOnInit` woła `LenisService.start()` — jedna instancja na cały cykl życia aplikacji.

## Treść

[ContentService](../../src/Clients/web/src/app/content/content.service.ts) importuje `pl.json`, `en.json` i wygenerowany `cv.json`. Locale bierze z pierwszego segmentu URL (signal + `NavigationEnd`). Computed: `site`, `cv`, `content`. Lookup po slug: `courseBySlug`, `compendiumBySlug`, `noteBySlug`.

Model typów: [content.model.ts](../../src/Clients/web/src/app/content/content.model.ts). Podział plików: [tresc.md](tresc.md).

## Motions

| Mechanizm | Gdzie |
| --- | --- |
| Lenis | [lenis.service.ts](../../src/Clients/web/src/app/motion/lenis.service.ts) |
| GSAP + ScrollTrigger | [gsap-setup.ts](../../src/Clients/web/src/app/motion/gsap-setup.ts), użyte na [home.ts](../../src/Clients/web/src/app/pages/home/home.ts) |
| View transitions + CSS enter/leave | [przejscia-widoku.md](przejscia-widoku.md) |

## Zależności wyglądu

`"@jjdevhub/theme": "file:../shared/theme"` w [package.json](../../src/Clients/web/package.json). Docker buduje z kontekstu `src/Clients`, żeby ta ścieżka się rozwiązała ([Dockerfile](../../src/Clients/web/Dockerfile)).
