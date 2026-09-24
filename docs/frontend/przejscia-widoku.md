# Przejścia widoku

Dwie warstwy, obie bez `@angular/animations`.

## 1. Router — View Transitions API

W [app.config.ts](../../src/Clients/web/src/app/app.config.ts):

```ts
provideRouter(routes, withViewTransitions())
```

Przy nawigacji Angular owija zmianę DOM w natywne view transition (gdy przeglądarka wspiera). Nie konfigurujemy tu własnych keyframesów przejścia trasy — domyślne zachowanie routera.

## 2. Elementy list — `animate.enter` / `animate.leave`

Szablony używają atrybutów Angulara na elementach w `@for`:

- `animate.enter="list-enter"`
- `animate.leave="list-leave"`

Klasy CSS są w [styles.scss](../../src/Clients/web/src/styles.scss):

| Klasa | Efekt | Czas / easing |
| --- | --- | --- |
| `.list-enter` | opacity 0→1, `translateY(--space-3)` → 0 | `--duration-reveal-fast`, `--easing-reveal` |
| `.list-leave` | odwrotnie, lekko w górę | `--duration-reveal-fast`, `--easing-standard` |

Występują m.in. na home (kafle, wiersze notatek), listach courses / notes / compendium, rozdziałach CV, paragrafach szczegółów.

## 3. Scroll reveal (nie router)

Klasa `.reveal` na sekcjach:

```css
animation-timeline: view();
animation-range: entry 0% cover 35%;
```

Keyframe `reveal-up`: fade + `translateY(--space-5)`. To nie jest View Transition — to scroll-driven animation dla treści poniżej foldu (CV, sekcje list, artykuły).

## Reduced motion

W `@media (prefers-reduced-motion: reduce)` animacje `.reveal`, `.list-enter` i `.list-leave` są wyłączone (`animation: none`). Lenis i scrub GSAP też — [lenis.md](lenis.md), [gsap.md](gsap.md).
