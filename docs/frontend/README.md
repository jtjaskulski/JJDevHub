# Frontend JJDevHub

Specyfikacja klienta do późniejszej implementacji. Ten plik ustala kolejność pozostałych dokumentów, mapę stron, zasady i granice. Kod aplikacji w tym kroku zostaje bez zmian.

Łańcuch webu: React składa HTML5, Vite podaje jeden CSS, Tailwind v4 daje klasy, shadcn i Motion siedzą na tym samym HTML. Telefon ma osobne ekrany i te same liczby z tokenów.

## Po co

Generator kodu czyta najpierw ten plik, potem dokumenty w kolejności z sekcji „Kolejność plików”. Każdy kolejny plik dokłada jedną warstwę i zostawia sąsiada w spokoju.

Gruba technologia ma katalog z ponumerowanymi plikami. Cienka technologia ma jeden plik i linkuje do sąsiada, którego kontrakt już opisano.

## Wersje i pakiety

Klient web: `src/Clients/web`, menedżer **pnpm** `10.33.0`.

| Pakiet | Wersja w repo | Rola |
| --- | --- | --- |
| `react`, `react-dom` | `19.2.3` | komponenty |
| `react-router` | `^7.9.4` | trasy |
| `vite` | `^7.1.9` | dev i build |
| `@vitejs/plugin-react` | `^5.0.4` | plugin Reacta |
| `typescript` | `~5.9.2` | bramka typów |
| `vitest` | `^4.0.8` | testy |
| `@testing-library/react` | `^16.3.0` | test tego, co widać |
| `jsdom` | `^28.0.0` | środowisko testów |
| `@fontsource-variable/inter` | `^5.2.6` | krój Inter |
| `@jjdevhub/theme` | `file:../shared/theme` | tokeny |

Docelowe pakiety dopisują pliki katalogów. Ten spis podaje tylko nazwy: `tailwindcss` i `@tailwindcss/vite` (Tailwind v4), komponenty shadcn kopiowane do repo, `motion` (import z `motion/react`), NativeWind, React Native Reusables, Reanimated.

Telefon: React Native `0.84` w `src/Clients/mobile/JJDevHubMobile`. Ten sam React co web.

## Ścieżki w repo

| Ścieżka | Co tam leży |
| --- | --- |
| `docs/frontend/` | ta specyfikacja |
| `src/Clients/web/` | klient web, `pnpm` |
| `src/Clients/web/src/app/App.tsx` | drzewo tras |
| `src/Clients/web/src/app/Shell.tsx` | shell i przełącznik języka |
| `src/Clients/web/src/app/LangGate.tsx` | tylko `pl` i `en` |
| `src/Clients/web/src/content/pl.json` | treść po polsku, w bundlu |
| `src/Clients/web/src/content/en.json` | treść po angielsku, w bundlu |
| `src/Clients/web/src/content/cv.example.json` | placeholdery CV w gicie |
| `src/Clients/web/src/content/cv.local.json` | lokalne CV, poza gitem |
| `src/Clients/web/src/content/cv.json` | plik wygenerowany, poza gitem |
| `src/Clients/web/scripts/copy-cv.mjs` | kopiuje CV przed startem, buildem i testem |
| `src/Clients/web/vite.config.ts` | port `4200`, proxy, alias motywu |
| `src/Clients/web/src/styles.css` | docelowe jedyne wejście CSS |
| `src/Clients/web/src/components/ui/` | docelowe komponenty shadcn |
| `src/Clients/shared/theme/tokens.ts` | jedyny kontrakt liczb |
| `src/Clients/mobile/JJDevHubMobile/` | szkielet telefonu |
| `src/Clients/` | kontekst Dockera dla buildu weba |

Skrypt `copy-cv.mjs` bierze `cv.local.json`, a gdy go nie ma — `cv.example.json`, i zapisuje `cv.json`. Hooki `prestart`, `prebuild`, `pretest` i `pretest:ci` wołają ten skrypt.

## Kolejność plików

Czytanie idzie wzdłuż łańcucha. Numer w nazwie to porządek wewnątrz katalogu. Przed Tailwindem i przed NativeWindem zawsze [motyw.md](motyw.md).

### 1. React składa strony

| Plik | Zakres |
| --- | --- |
| [react/01-komponenty.md](react/01-komponenty.md) | React 19, jeden komponent na plik, nazwa pliku = komponent, JSX jako układ. Tutorial na shellu i karcie kursu. Rozwija `.cursor/rules/react.mdc`. |
| [react/02-trasy.md](react/02-trasy.md) | `react-router` 7, `BrowserRouter`, drzewo z `App.tsx`, shell, przełącznik języka. |
| [react/03-tresc-i-auth.md](react/03-tresc-i-auth.md) | JSON w bundlu, `cv.local.json`, `fetch` przy logowaniu i rejestracji. |
| [react/04-testy.md](react/04-testy.md) | Vitest, Testing Library, jsdom. Test widoku użytkownika. |

### 2. HTML5 w JSX

| Plik | Zakres |
| --- | --- |
| [html/01-semantyka.md](html/01-semantyka.md) | `header`, `nav`, `main`, `section`, `article`, `form`, `button`, jeden `h1`. Klasy układu zostają w `tailwind/`. |

### 3. Vite podaje jeden CSS

| Plik | Zakres |
| --- | --- |
| [vite/01-dev.md](vite/01-dev.md) | `pnpm`, `vite --port 4200`, proxy `/api`, `/health`, `/openapi`, `/scalar` na `localhost:5080`. |
| [vite/02-aliasy.md](vite/02-aliasy.md) | Dziś `@jjdevhub/theme` → `tokens.ts`. Docelowo `@/` na `src` pod shadcn, w `vite.config.ts` i `tsconfig.json`. |
| [vite/03-pluginy-i-build.md](vite/03-pluginy-i-build.md) | Plugin Reacta, docelowo `@tailwindcss/vite`. Bramka `pnpm build`: `tsc -p tsconfig.json --noEmit && vite build`. Docker z kontekstu `src/Clients`. |

### 4. Liczby

| Plik | Zakres |
| --- | --- |
| [motyw.md](motyw.md) | Jeden plik. Kontrakt `tokens.ts`: kolory, skala, odstępy, radius, czasy. Tailwind `@theme` i NativeWind tylko mapują te liczby. |

### 5. Tailwind v4 daje klasy

Wejście docelowe: `src/Clients/web/src/styles.css` z `@import "tailwindcss"`. Tailwind v4 zostaje przy CSS.

| Plik | Zakres |
| --- | --- |
| [tailwind/01-instalacja.md](tailwind/01-instalacja.md) | Pakiety, plugin, pierwsze klasy na shellu. |
| [tailwind/02-motyw.md](tailwind/02-motyw.md) | `@theme` z tokenów. Liczby zostają w `motyw.md`. |
| [tailwind/03-uklad.md](tailwind/03-uklad.md) | Odstępy, nav, siatka kart, szerokość artykułu. |
| [tailwind/04-typografia.md](tailwind/04-typografia.md) | Inter, skala, tracking, ciemny hero i jasne sekcje. |
| [tailwind/05-stany.md](tailwind/05-stany.md) | hover, focus-visible, disabled, pusto, błąd pola, `prefers-reduced-motion`. |
| [tailwind/06-css-zamiast-scss.md](tailwind/06-css-zamiast-scss.md) | Jeden `src/styles.css`. Zero nowych `*.scss`. `@apply` tylko wyjątkowo w tym jednym pliku. |

### 6. shadcn i Motion na tym samym HTML

| Plik | Zakres |
| --- | --- |
| [shadcn/01-init.md](shadcn/01-init.md) | `shadcn init` pod Vite, Tailwind v4, `components.json`, katalog `src/Clients/web/src/components/ui`, alias `@/`. |
| [shadcn/02-motyw.md](shadcn/02-motyw.md) | Zmienne shadcn spięte z `tokens.ts`. |
| [shadcn/03-komponenty.md](shadcn/03-komponenty.md) | button, input, dialog, pusty stan, nawigacja, formularz logowania. Późniejszy CRUD intranetu na tym samym zestawie. Komponenty zostają w repo. |
| [motion/01-przejscia.md](motion/01-przejscia.md) | Import z `motion/react`. Wejście sekcji i zmiana widoku. `prefers-reduced-motion` wyłącza ruch. |
| [motion/02-hero.md](motion/02-hero.md) | Jeden scroll na home, ciemny hero. Tylko web. Telefon opisuje `reanimated.md`. |

### 7. Telefon, osobne ekrany

| Plik | Zakres |
| --- | --- |
| [nativewind/01-instalacja.md](nativewind/01-instalacja.md) | NativeWind na szkielecie RN `0.84`. Te same liczby z `tokens.ts`. |
| [nativewind/02-ekrany.md](nativewind/02-ekrany.md) | Lista kursów klasami na telefonie. Ekran osobny od weba. |
| [reusables.md](reusables.md) | Jeden plik. React Native Reusables kopiowane do repo: przycisk, pole, karta, formularz logowania. Instalacja NativeWind zostaje w `nativewind/01-instalacja.md`. |
| [reanimated.md](reanimated.md) | Jeden plik. Przesunięcie hero względem offsetu listy i wyłączenie ruchu. Osobno od `motion/02-hero.md`. |

## Tutorial

Generator bierze jedną stronę, otwiera pliki łańcucha po kolei i dopisuje kod tylko w warstwie, którą dany plik opisuje.

Uruchomienie weba, gdy kod już powstanie:

```bash
cd src/Clients/web
pnpm install
pnpm start
```

Dev: http://localhost:4200. Proxy z `vite.config.ts` wysyła `/api`, `/health`, `/openapi` i `/scalar` na http://localhost:5080.

Bramka przed uznaniem widoku za gotowy:

```bash
cd src/Clients/web
pnpm test:ci
pnpm build
```

`pnpm build` to `tsc -p tsconfig.json --noEmit && vite build`. `pretest:ci` i `prebuild` najpierw kopiują CV.

Przykład: lista kursów na webie.

1. [react/02-trasy.md](react/02-trasy.md) — ścieżka `/:lang/courses`, `lang` to `pl` albo `en`.
2. [react/03-tresc-i-auth.md](react/03-tresc-i-auth.md) — lista z `pl.json` albo `en.json` w bundlu, bez HTTP.
3. [react/01-komponenty.md](react/01-komponenty.md) — karta kursu w osobnym pliku; pusta lista wraca wcześniej.
4. [html/01-semantyka.md](html/01-semantyka.md) — `main`, lista, jeden `h1`.
5. [motyw.md](motyw.md) — płótno, atrament, akcent, odstęp.
6. [tailwind/03-uklad.md](tailwind/03-uklad.md) i [tailwind/05-stany.md](tailwind/05-stany.md) — siatka kart i stan pusty.
7. [react/04-testy.md](react/04-testy.md) — test widzi pustą listę albo tytuły kursów.

Ta sama lista na telefonie czyta [nativewind/02-ekrany.md](nativewind/02-ekrany.md) i zostawia webowy scroll w [motion/02-hero.md](motion/02-hero.md).

## Mapa stron

`BrowserRouter` w webie. Shell owija całe drzewo. Dozwolone języki: `pl` i `en`. `/` oraz zły `:lang` i `*` idą na `/pl` (`Navigate` z `replace`).

Przełącznik w shellu podmienia pierwszy segment ścieżki (`pl` ↔ `en`) i zachowuje resztę, query oraz hash. Na `/login` i `/register` przełącznik nic nie zmienia: te ścieżki są poza językiem.

| URL | Ekran | Język w URL | Dane |
| --- | --- | --- | --- |
| `/` | przekierowanie na `/pl` | — | — |
| `/:lang` | home | `pl` \| `en` | `pl.json` / `en.json` |
| `/:lang/cv` | CV | `pl` \| `en` | `cv.json` z bundla |
| `/:lang/courses` | lista kursów | `pl` \| `en` | JSON w bundlu |
| `/:lang/courses/:slug` | szczegół kursu | `pl` \| `en` | JSON w bundlu |
| `/:lang/compendium` | kompendium | `pl` \| `en` | JSON w bundlu |
| `/:lang/compendium/:slug` | hasło kompendium | `pl` \| `en` | JSON w bundlu |
| `/:lang/notes` | lista notatek | `pl` \| `en` | JSON w bundlu |
| `/:lang/notes/:slug` | szczegół notatki | `pl` \| `en` | JSON w bundlu |
| `/login` | logowanie, trasa gościa | poza językiem | `fetch` na `/api` |
| `/register` | rejestracja, trasa gościa | poza językiem | `fetch` na `/api` |
| inny `:lang`, `*` | przekierowanie na `/pl` | — | — |

Wzorzec drzewa, który ma utrzymać [react/02-trasy.md](react/02-trasy.md):

```text
/                          → /pl
/login                     LoginPage w GuestRoute
/register                  RegisterPage w GuestRoute
/:lang                     LangGate (tylko pl, en)
  index                    HomePage
  cv                       CvPage
  courses                  CoursesPage
  courses/:slug            CourseDetailPage
  compendium               CompendiumPage
  compendium/:slug         CompendiumDetailPage
  notes                    NotesPage
  notes/:slug              NoteDetailPage
*                          → /pl
```

Nawigacja shellu prowadzi do home, CV, kursów, kompendium i notatek w bieżącym języku. Login i register zostają poza tym menu językowym.

Telefon powtarza produkt osobnymi ekranami. Pierwszy ekran w specyfikacji to lista kursów i jej pusty stan. Wspólne z webem są tokeny i kształt treści, a pliki ekranów zostają w `src/Clients/mobile/JJDevHubMobile`.

Intranet dochodzi później w tym samym kliencie web, gdy treść siedzi w API. CRUD używa komponentów z [shadcn/03-komponenty.md](shadcn/03-komponenty.md). Osobnej aplikacji intranetu specyfikacja nie przewiduje.

## Przypadki użycia

1. **Home.** `/:lang`, jedna przypięta klatka na `darkChapter`: imię, rola, linia stosu, pas „Teraz” i dwa rozdziały. Potem wykłady i notatki. `prefers-reduced-motion` zostawia klatkę statyczną. Web: [motion/02-hero.md](motion/02-hero.md). Telefon: [reanimated.md](reanimated.md).
2. **Lista kursów.** Siatka kart, duży oddech, mało ramek. Pusta lista to wczesny widok pustego stanu, opisany w teście.
3. **Szczegół notatki.** Artykuł: jeden `h1`, szerokość z [tailwind/03-uklad.md](tailwind/03-uklad.md), treść z JSON-a bundla.
4. **Logowanie.** `/login` poza językiem. Formularz z button i input shadcn. Błąd pola widać przy polu. Żądanie idzie `fetch` na `/api` przez proxy w dev.
5. **CV.** Strona `/:lang/cv` czyta wygenerowany `cv.json`. Autor trzyma prawdziwe dane w `cv.local.json` poza gitem.
6. **Lista kursów na telefonie.** Osobny ekran, klasy NativeWind, te same liczby. Scroll dokumentu i `backdrop-filter` zostają przy webie.
7. **Intranet później.** Ten sam klient, te same komponenty UI, gdy lista i formularz czytają API.

## Zasady

- Liczby kolorów, skali, odstępów, radiusu i czasów stoją w `src/Clients/shared/theme/tokens.ts`. Tailwind `@theme` i NativeWind mapują ten plik. Drugiej palety w klasach arbitralnych nie ma.
- Paleta jasna: płótno `#f3efe6`, atrament `#1a1814`, akcent `#9a3412`. Paleta ciemna: płótno `#141210`, atrament `#f3efe6`, akcent `#d4784a`. Krój: Inter. Pełna skala w [motyw.md](motyw.md). Masthead home używa `darkChapter`, nie pełnoekranowego teatru.
- Przełącznik trybu stoi w shellu obok języka. Zapis: `jjdevhub.theme`. Brak zapisu oznacza `prefers-color-scheme`. Oba tryby podmieniają te same zmienne CSS. Klas `dark:` nie ma.
- Ekrany weba i telefonu są osobne. Wspólny jest kontrakt tokenów.
- Strony treści czytają `pl.json`, `en.json` i wygenerowany `cv.json` z bundla. HTTP do API zostaje przy `/login` i `/register`, zwykłym `fetch`.
- `cv.local.json` i wygenerowany `cv.json` zostają poza gitem. W repo jest `cv.example.json`.
- Locale w URL: tylko `pl` i `en`. Login i register bez prefiksu języka.
- Jeden plik CSS weba: `src/styles.css`. Znaczniki HTML5 niosą klasy Tailwinda.
- `prefers-reduced-motion` wyłącza ruch na webie i na telefonie.
- Jeden komponent na plik, eksport nazwany, funkcja. Test sprawdza tekst, rolę i stan, które widzi użytkownik.
- Komponenty shadcn i Reusables leżą w repozytorium, w swoich katalogach klientów.

## Poza zakresem

Poza tą specyfikacją zostają:

- Tamagui
- wspólny parallax weba i telefonu (Motion i Reanimated opisują ruch osobno)
- drugi klient intranetu
- Sass, nowe pliki `*.scss` i drugie wejście CSS obok `src/styles.css`
- GSAP i Lenis
- druga przypięta sekwencja scrolla na podstronach
- `backdrop-filter` i scroll dokumentu jako model telefonu
- archiwa `docs/frontend-old` i `docs/frontend copy`
- powtórzenie kontraktu sąsiada (liczby w `motyw.md`, instalacja NativeWind w `nativewind/01-instalacja.md`, klasy układu w `tailwind/`)

## Gotowe gdy

- Generator ma tu pełną kolejność plików `01`–reszta wzdłuż łańcucha i wie, którego pliku nie powtarzać.
- Mapa URL zgadza się z drzewem `App.tsx`: `pl`/`en`, home, cv, courses, compendium, notes, login i register poza językiem, zły język na `/pl`.
- Zasady wskazują tokeny, paletę, osobne ekrany, JSON w bundlu, `cv.local.json` i `fetch` przy auth.
- Granice odcinają Tamagui, wspólny parallax, drugi klient intranetu i SCSS.
- Intranet ma zapisane miejsce: później, w tym samym kliencie web.
