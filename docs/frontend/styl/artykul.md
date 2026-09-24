# Artykuł — widok szczegółu

Szablony szczegółów dzielą ten sam układ „wąskiej kolumny”:

- kurs: [course-detail.scss](../../../src/Clients/web/src/app/pages/courses/course-detail.scss) — max-width `42rem`
- kompendium / notatka: max-width `680px` ([compendium-detail.scss](../../../src/Clients/web/src/app/pages/compendium/compendium-detail.scss), [note-detail.scss](../../../src/Clients/web/src/app/pages/notes/note-detail.scss))
- CV: `42rem` ([cv.scss](../../../src/Clients/web/src/app/pages/cv/cv.scss))

Padding pionowy jak na listach: `--space-9` góra, `--space-11` dół, boki `--space-6`.

## Anatomia

1. **Eyebrow** — link wstecz do listy (`--muted`)
2. **Nagłówek** — `h1` z `clamp` do `--font-h1`
3. **Lead / meta** — subtitle kursu, data notatki, e-mail CV; często `--muted` lub `--font-h4`
4. **Body** — akapity z JSON (`string[]`), `line-height` ~1.55, odstęp `--space-4` między `p`
5. **Bloki kursu** — lista lekcji (linki zewnętrzne do GitHuba), rozdziały, sekcja „w planach” z leadem

Paragrafy w `@for` często mają `animate.enter="list-enter"` przy pojawieniu się w DOM.

## Dane

Treść artykułu nie leci z API. Lookup: `ContentService.courseBySlug` / `compendiumBySlug` / `noteBySlug` albo `cv` dla strony CV. Brak sluga → pusty / brak wpisu według logiki komponentu (bez osobnego 404 w routerze).
