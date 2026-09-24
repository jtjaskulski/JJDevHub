# Treść — JSON w webie

Treść stron jest w bundlu Angulara. Import przy buildzie, **bez** HTTP. Model: [content.model.ts](../../src/Clients/web/src/app/content/content.model.ts). Składanie: [ContentService](../../src/Clients/web/src/app/content/content.service.ts).

## Co jest w repo

Pliki lokalizacji (menu, home, kursy, kompendium, notatki) — **bez** imienia, e-maila, pracodawców ani edukacji:

- [src/Clients/web/src/content/pl.json](../../src/Clients/web/src/content/pl.json)
- [src/Clients/web/src/content/en.json](../../src/Clients/web/src/content/en.json)

Źródła kursów (linki w JSON, nie pełne `lekcja-*.md`):

| Slug | Skąd |
| --- | --- |
| `mobile` | sylabus [KursReactNativeCQRS](https://github.com/jtjaskulski/KursReactNativeCQRS), lab [PMAB2026Zaoczki](https://github.com/jtjaskulski/PMAB2026Zaoczki) — lista lekcji z URL do plików na GitHubie |
| `web` | [InternetBusinessApplicationsMVCSummer2026](https://github.com/jtjaskulski/InternetBusinessApplicationsMVCSummer2026) — rozdziały + sekcja „w planach” (Angular, Blazor) bez zmyślonych lekcji |

Kompendium: hasła (CQRS, Docker, React Native, …). Notatki: krótkie wpisy z opcjonalnym powiązaniem do lekcji/kursu.

## CV — tylko lokalnie

Kształt pliku: `{ pl, en }` → `CvLocaleContent` (name, role, email, experience, education, extras; akapity jako `string[]`).

| Plik | Git | Rola |
| --- | --- | --- |
| [cv.example.json](../../src/Clients/web/src/content/cv.example.json) | tak | placeholdery (`Imię Nazwisko`, `you@example.com`) — CI i świeży klon się budują |
| `cv.local.json` | **nie** | prawdziwe dane; trzeba położyć ręcznie |
| `cv.json` | **nie** | wynik skryptu; to **jedyny** plik importowany przez aplikację |

Ignore: root [.gitignore](../../.gitignore) i [web/.gitignore](../../src/Clients/web/.gitignore) — `cv.local.json` oraz `cv.json`.

Bez adresu, telefonu, prawa jazdy i klauzuli RODO w założeniu danych lokalnych.

## Skrypt przed start / build

[scripts/copy-cv.mjs](../../src/Clients/web/scripts/copy-cv.mjs):

1. Jeśli istnieje `cv.local.json` → kopiuje go do `cv.json`
2. W przeciwnym razie → kopiuje `cv.example.json`

Hooki w [package.json](../../src/Clients/web/package.json): `prestart`, `prebuild`, `pretest` i `pretest:ci` wołają ten skrypt. GitHub Actions odpala `pnpm test:ci` zanim zrobi `pnpm build`, więc bez `pretest:ci` runner nie ma `cv.json` i pada na imporcie. Na runnerze nie ma `cv.local.json`, więc skrypt bierze `cv.example.json`. `ContentService` robi `import cvFile from '../../content/cv.json'`.

## Serwer / obraz Dockera

Sam `git checkout` **nie** przywiezie `cv.local.json`. Na VM trzeba położyć plik lokalnie **przed** buildem obrazu weba — ta sama kategoria sekretów / lokalnych plików co `/etc/jjdevhub/api.env` z [proxmox.md](../proxmox.md) i [github.md](../github.md).

Ścieżka w drzewie źródłowym (w kontekście buildu `src/Clients`):

`web/src/content/cv.local.json`

Bez niego obraz zbuduje się z example (placeholdery w hero i na `/cv`). Dockerfile weba kopiuje katalog `web/` po `pnpm install` i robi `pnpm build` — skrypt `prebuild` musi wtedy znaleźć lokalny plik już w kontekście kopiowanym do obrazu (albo akceptujesz example w danym środowisku).

## Locale w runtime

`ContentService` trzyma `pl` / `en` z prefiksu URL. `site` i `cv` to osobne computed; `content` je łączy. Przełącznik języka w shellu zmienia tylko segment `:lang` — te same slugi tras.
