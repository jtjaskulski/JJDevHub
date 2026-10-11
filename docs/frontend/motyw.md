# Motyw

Jeden obiekt `tokens` w `@jjdevhub/theme` jest źródłem kolorów, skali pisma, odstępów, promieni, czasów i krzywych. Tailwind v4 (`@theme inline`) i NativeWind tylko przekładają te pola na nazwy klas. Heks, piksel, `em`, milisekunda i `cubic-bezier` powstają w `tokens.ts`. Arkusz, konfiguracja telefonu i komponent ich nie powtarzają.

Łańcuch webu: React składa HTML5, Vite podaje jeden `src/styles.css`, Tailwind v4 daje klasy, shadcn i Motion siedzą na tym samym HTML. Telefon ma osobne ekrany i te same liczby.

## Wersje i pakiety

| Pakiet | Wersja w repo | Rola wobec tokenów |
| --- | --- | --- |
| `@jjdevhub/theme` | `0.0.0`, prywatny, `type: module` | Jedyny zapis liczb. Eksport `"."` wskazuje `./tokens.ts`. Zero importów Reacta, Tailwinda i NativeWind. |
| `web` | React `19.2.3`, Vite `^7.1.9`, pnpm `10.33.0` | Zależność `"@jjdevhub/theme": "file:../shared/theme"`. |
| `@fontsource-variable/inter` | `^5.2.6` (web) | Plik kroju, którego rodzina CSS nazywa się `Inter`, zgodnie z `tokens.font.family`. |
| `tailwindcss` + `@tailwindcss/vite` | Tailwind v4, docelowo | Klasy z `@theme inline`. Wpięcie pakietu opisuje [tailwind/01-instalacja.md](tailwind/01-instalacja.md). |
| NativeWind | szkielet `src/Clients/mobile/JJDevHubMobile`, React Native 0.84 | Te same nazwy klas. Wpięcie opisuje [nativewind/01-instalacja.md](nativewind/01-instalacja.md). |

Motyw nie wybiera między Tailwindem v3 a v4. Web jest na Tailwind v4. Telefon mapuje ten sam kontrakt: przy NativeWind opartym o Tailwind v4 jest to ten sam blok `@theme inline`; przy konfiguracji `theme.extend` wartościami są odwołania `var(--…)` albo pola `tokens`, nigdy wpisany drugi raz literał.

## Ścieżki w repo

- Źródło: [src/Clients/shared/theme/tokens.ts](../../src/Clients/shared/theme/tokens.ts). Obok niego [src/Clients/shared/theme/package.json](../../src/Clients/shared/theme/package.json).
- Alias Vite: `@jjdevhub/theme` → `src/Clients/shared/theme/tokens.ts` w [src/Clients/web/vite.config.ts](../../src/Clients/web/vite.config.ts). Alias i pole `exports` wskazują ten sam plik.
- Zależność weba: `"@jjdevhub/theme": "file:../shared/theme"` w [src/Clients/web/package.json](../../src/Clients/web/package.json).
- Zależność telefonu: `"@jjdevhub/theme": "file:../../shared/theme"` w pakiecie `src/Clients/mobile/JJDevHubMobile`.
- Wejście CSS weba: `src/Clients/web/src/styles.css` (`@import "tailwindcss"` i blok `@theme inline` z tego dokumentu).
- Wstrzyknięcie zmiennych na webie: `cssCustomProperties()` na `document.documentElement` przed pierwszym malowaniem, wołane z [src/Clients/web/src/main.tsx](../../src/Clients/web/src/main.tsx).
- Wstrzyknięcie na telefonie: ten sam rekord `cssCustomProperties()` na korzeniu drzewa przez `vars` NativeWind.
- Konsumenci liczb w JS (bez zamiany na string): Motion importuje `tokens.duration` i `tokens.easing`. Reanimated importuje te same pola. Zachowanie ruchu jest w [motion/](motion/) i [reanimated.md](reanimated.md).

Publiczny kontrakt modułu to trzy nazwy:

```ts
export const tokens: Tokens;
export type Tokens = typeof tokens;
export function cssCustomProperties(): Record<string, string>;
```

`tokens` jest `as const`. Funkcje `px`, `em`, `ms` i `cubicBezier` zostają prywatne w tym pliku. Jednostkę dokleja wyłącznie `cssCustomProperties()`.

## Kontrakt obiektu

Zmiana liczby odbywa się w obiekcie `tokens`. `cssCustomProperties()` czyta pola tego obiektu i dokleja jednostkę. Blok `@theme inline` dopisuje jedną linię `var(--…)`. Literał nie pojawia się w CSS ani w konfiguracji NativeWind.

Reguły jednostek serializera:

| Pole | Jednostka w obiekcie | String CSS |
| --- | --- | --- |
| `font.size.*`, `space.*`, `radius.*` | liczba | `` `${n}px` `` |
| `font.tracking.*` | liczba w `em` | `` `${n}em` `` |
| `duration.*` | milisekundy | `` `${n}ms` `` |
| `easing.*` | krotka `[x1, y1, x2, y2]` | `` `cubic-bezier(${x1}, ${y1}, ${x2}, ${y2})` `` |
| `color.*`, `font.family` | string | ten sam string |

`color.darkChapter.foreground` i `color.canvas` mają dziś ten sam heks `#f5f5f7`. To dwa pola. Zmiana jednego nie zmienia drugiego. `radius.card` i `radius.lg` mają dziś `18`. To dwa klucze: `card` jest promieniem karty, `lg` stopniem skali.

### `color`

| Ścieżka | Wartość | Właściwość CSS | Klasa |
| --- | --- | --- | --- |
| `color.canvas` | `#f5f5f7` | `--canvas` | `bg-canvas`, `text-canvas` |
| `color.white` | `#ffffff` | `--white` | `bg-white`, `text-white` |
| `color.ink` | `#1d1d1f` | `--ink` | `bg-ink`, `text-ink` |
| `color.muted` | `#6e6e73` | `--muted` | `text-muted`, `bg-muted` |
| `color.accent` | `#0071e3` | `--accent` | `bg-accent`, `text-accent` |
| `color.darkChapter.background` | `#000000` | `--dark-chapter-bg` | `bg-dark-chapter` |
| `color.darkChapter.foreground` | `#f5f5f7` | `--dark-chapter-fg` | `text-dark-chapter-fg` |

Przestrzeń koloru Tailwinda po resecie `--color-*: initial` zawiera tylko te siedem nazw. `bg-black` nie jest nazwą hero. Hero używa `bg-dark-chapter`. `bg-blue-600`, `bg-zinc-100`, `text-slate-500` i reszta domyślnej palety nie są tokenami JJDevHub.

### `font`

`font.family` to `'Inter'`. Właściwość CSS: `--font-family`. Klasa: `font-sans`.

Klucze `font.size` i `font.tracking` są te same: `display`, `h1`, `h2`, `h3`, `h4`, `body`, `small`, `caption`. Rozmiar i tracking to osobne mapowania. W obiekcie nie ma `line-height` ani grubości.

| Klucz | `font.size` (px) | Właściwość rozmiaru | Klasa rozmiaru | `font.tracking` (em) | Właściwość trackingu | Klasa trackingu |
| --- | --- | --- | --- | --- | --- | --- |
| `display` | 80 | `--font-display` | `text-display` | -0.015 | `--tracking-display` | `tracking-display` |
| `h1` | 56 | `--font-h1` | `text-h1` | -0.012 | `--tracking-h1` | `tracking-h1` |
| `h2` | 40 | `--font-h2` | `text-h2` | -0.01 | `--tracking-h2` | `tracking-h2` |
| `h3` | 28 | `--font-h3` | `text-h3` | -0.008 | `--tracking-h3` | `tracking-h3` |
| `h4` | 21 | `--font-h4` | `text-h4` | -0.005 | `--tracking-h4` | `tracking-h4` |
| `body` | 17 | `--font-body` | `text-body` | -0.014 | `--tracking-body` | `tracking-body` |
| `small` | 14 | `--font-small` | `text-small` | -0.006 | `--tracking-small` | `tracking-small` |
| `caption` | 12 | `--font-caption` | `text-caption` | 0 | `--tracking-caption` | `tracking-caption` |

`text-sm`, `text-base`, `text-xl` oraz `tracking-tight`, `tracking-normal` należą do domyślnej skali Tailwinda. Po resecie `--text-*: initial` i `--tracking-*: initial` zostają klucze z tabeli. Skład tych klas na hero i w artykule opisuje [tailwind/04-typografia.md](tailwind/04-typografia.md).

### `space`

Klucze `1`–`12`. Skala od `5` w górę nie jest wielokrotnością `4`. Domyślny mnożnik Tailwinda `--spacing: 0.25rem` dałby `p-5` = 20px, a token `space.5` to 24. Dlatego mnożnik jest wygaszony, a każdy klucz ma własną zmienną.

| Klucz | px | Właściwość CSS | Klasa (przykład) |
| --- | --- | --- | --- |
| `1` | 4 | `--space-1` | `p-1`, `gap-1` |
| `2` | 8 | `--space-2` | `p-2`, `gap-2` |
| `3` | 12 | `--space-3` | `p-3`, `gap-3` |
| `4` | 16 | `--space-4` | `p-4`, `gap-4` |
| `5` | 24 | `--space-5` | `p-5`, `gap-5` |
| `6` | 32 | `--space-6` | `p-6`, `gap-6` |
| `7` | 48 | `--space-7` | `p-7`, `gap-7` |
| `8` | 64 | `--space-8` | `p-8`, `gap-8` |
| `9` | 80 | `--space-9` | `p-9`, `gap-9` |
| `10` | 96 | `--space-10` | `p-10`, `gap-10` |
| `11` | 128 | `--space-11` | `p-11`, `gap-11` |
| `12` | 160 | `--space-12` | `p-12`, `gap-12` |

Ta sama liczba stoi za paddingiem, marginesem, `gap`, `w-*` i `h-*` danego klucza. Który klucz na nav, siatce i artykule — [tailwind/03-uklad.md](tailwind/03-uklad.md).

### `radius`

| Klucz | px | Właściwość CSS | Klasa |
| --- | --- | --- | --- |
| `sm` | 8 | `--radius-sm` | `rounded-sm` |
| `md` | 12 | `--radius-md` | `rounded-md` |
| `lg` | 18 | `--radius-lg` | `rounded-lg` |
| `card` | 18 | `--radius-card` | `rounded-card` |
| `pill` | 9999 | `--radius-pill` | `rounded-pill` |

Domyślne `rounded-sm` Tailwinda to `0.25rem` (4px), a token `sm` to 8px. Domyślne `rounded-lg` to `0.5rem` (8px), a token `lg` to 18px. Reset `--radius-*: initial` zostawia pięć klas z tabeli. Kapsuła to `rounded-pill` (`9999px` ze serializera).

### `duration`

Wartości to milisekundy. W JS zostają liczbą (`tokens.duration.reveal === 700`). W CSS stringiem z `ms`.

| Klucz | ms | Właściwość CSS | Klasa |
| --- | --- | --- | --- |
| `reveal` | 700 | `--duration-reveal` | `duration-reveal` |
| `revealFast` | 400 | `--duration-reveal-fast` | `duration-reveal-fast` |
| `heroPin` | 1200 | `--duration-hero-pin` | `duration-hero-pin` |
| `heroFade` | 600 | `--duration-hero-fade` | `duration-hero-fade` |
| `nav` | 250 | `--duration-nav` | `duration-nav` |

Domyślne `duration-150` (150ms) nie jest polem `tokens`. Czas z kontraktu jest jawną klasą albo polem `tokens.duration` w Motion i Reanimated.

### `easing`

Każda krzywa to readonly krotka czterech liczb `[x1, y1, x2, y2]`.

| Klucz | Krotka | Właściwość CSS | Klasa |
| --- | --- | --- | --- |
| `reveal` | `[0.25, 0.1, 0.25, 1]` | `--easing-reveal` | `ease-reveal` |
| `hero` | `[0.22, 1, 0.36, 1]` | `--easing-hero` | `ease-hero` |
| `standard` | `[0.4, 0, 0.2, 1]` | `--easing-standard` | `ease-standard` |

String CSS to `cubic-bezier(0.25, 0.1, 0.25, 1)`, `cubic-bezier(0.22, 1, 0.36, 1)` i `cubic-bezier(0.4, 0, 0.2, 1)`. Motion i Reanimated biorą krotkę z `tokens.easing`, a milisekundy z `tokens.duration`.

## Mapowanie `@theme inline`

Blok stoi w `src/Clients/web/src/styles.css` i — w tym samym brzmieniu — w wejściu CSS NativeWind. Słowo `inline` jest obowiązkowe: klasa dostaje `var(--canvas)`, a heks zostaje na właściwości zapisanej przez `cssCustomProperties()`. Bez `inline` Tailwind zadeklarowałby własną zmienną motywu i łatwo o pętlę tam, gdzie nazwa przestrzeni pokrywa się z właściwością dokumentu (`--tracking-display`, `--radius-sm`).

Reset `*: initial` w przestrzeniach poniżej wyłącza domyślną paletę, skalę pisma, tracking, mnożnik odstępów, promienie i czasy Tailwinda. Zostają nazwy z kontraktu. Breakpointy, cienie i `font-mono` ten reset nie rusza; nie są tokenami i nie zastępują palety.

```css
@import "tailwindcss";

@theme inline {
  --color-*: initial;
  --color-canvas: var(--canvas);
  --color-white: var(--white);
  --color-ink: var(--ink);
  --color-muted: var(--muted);
  --color-accent: var(--accent);
  --color-dark-chapter: var(--dark-chapter-bg);
  --color-dark-chapter-fg: var(--dark-chapter-fg);

  --font-sans: var(--font-family), sans-serif;

  --text-*: initial;
  --text-display: var(--font-display);
  --text-h1: var(--font-h1);
  --text-h2: var(--font-h2);
  --text-h3: var(--font-h3);
  --text-h4: var(--font-h4);
  --text-body: var(--font-body);
  --text-small: var(--font-small);
  --text-caption: var(--font-caption);

  --tracking-*: initial;
  --tracking-display: var(--tracking-display);
  --tracking-h1: var(--tracking-h1);
  --tracking-h2: var(--tracking-h2);
  --tracking-h3: var(--tracking-h3);
  --tracking-h4: var(--tracking-h4);
  --tracking-body: var(--tracking-body);
  --tracking-small: var(--tracking-small);
  --tracking-caption: var(--tracking-caption);

  --spacing: initial;
  --spacing-1: var(--space-1);
  --spacing-2: var(--space-2);
  --spacing-3: var(--space-3);
  --spacing-4: var(--space-4);
  --spacing-5: var(--space-5);
  --spacing-6: var(--space-6);
  --spacing-7: var(--space-7);
  --spacing-8: var(--space-8);
  --spacing-9: var(--space-9);
  --spacing-10: var(--space-10);
  --spacing-11: var(--space-11);
  --spacing-12: var(--space-12);

  --radius-*: initial;
  --radius-sm: var(--radius-sm);
  --radius-md: var(--radius-md);
  --radius-lg: var(--radius-lg);
  --radius-card: var(--radius-card);
  --radius-pill: var(--radius-pill);

  --ease-*: initial;
  --ease-reveal: var(--easing-reveal);
  --ease-hero: var(--easing-hero);
  --ease-standard: var(--easing-standard);

  --transition-duration-*: initial;
  --transition-duration-reveal: var(--duration-reveal);
  --transition-duration-reveal-fast: var(--duration-reveal-fast);
  --transition-duration-hero-pin: var(--duration-hero-pin);
  --transition-duration-hero-fade: var(--duration-hero-fade);
  --transition-duration-nav: var(--duration-nav);
}
```

`--font-sans` dokleja `, sans-serif` jako zapas, gdy plik Inter jeszcze nie zszedł. Rodzina marki zostaje `tokens.font.family`. W `@theme` nie ma `--font-display`: w Tailwindzie `--font-*` to rodzina, a rozmiar display jest `--text-display`.

Przestrzeń czasu to `--transition-duration-*`. Stąd klasa `duration-reveal`, a właściwość dokumentu zostaje `--duration-reveal`. Przestrzeń krzywej to `--ease-*`, właściwość dokumentu zostaje `--easing-*`.

Sprawdzenie, że w bloku nie ma drugiej palety:

```bash
rg -n "#[0-9a-fA-F]{3,8}|[0-9]+px|[0-9]+ms|cubic-bezier" src/Clients/web/src/styles.css
```

Po wpięciu motywu ta komenda nie znajduje trafień w `styles.css`. Trafienie heksa `#f5f5f7`, `#1d1d1f` albo `#0071e3` poza `tokens.ts` jest błędem kontraktu:

```bash
rg -n "#f5f5f7|#1d1d1f|#0071e3|#ffffff|#6e6e73|#000000" src/Clients
```

Jedyny oczekiwany plik z tymi heksami to `src/Clients/shared/theme/tokens.ts`.

## Mapowanie NativeWind

Telefon nie dostaje własnej palety, własnej skali ani własnego akcentu. Ekrany są osobne; liczby są wspólne.

1. Pakiet deklaruje `"@jjdevhub/theme": "file:../../shared/theme"`.
2. Korzeń aplikacji dostaje rekord serializera, zanim którykolwiek ekran się narysuje:

```tsx
import { cssCustomProperties } from '@jjdevhub/theme';
import { vars } from 'nativewind';
import { View } from 'react-native';

const themeVars = vars(cssCustomProperties());

export function ThemeRoot({ children }: { children: React.ReactNode }) {
  return <View style={themeVars}>{children}</View>;
}
```

3. Wejście CSS NativeWind zawiera ten sam blok `@theme inline` co web. Klasa `bg-accent` rozwiązuje się do `var(--accent)`, a `--accent` ma wartość `#0071e3`, bo `cssCustomProperties()` wpisało `tokens.color.accent`.
4. Tam, gdzie silnik NativeWind bierze liczbę w obiekcie stylu (konfiguracja `theme.extend` albo animacja), wartość jest polem `tokens`:

```ts
import { tokens } from '@jjdevhub/theme';

const accent = tokens.color.accent;
const buttonRadius = tokens.radius.pill;
const navMs = tokens.duration.nav;
```

Pola `colors`, `fontSize`, `letterSpacing`, `spacing`, `borderRadius` i czasy w konfiguracji telefonu nie zawierają stringów `'#0071e3'`, `'17px'`, `'24px'`. Albo `var(--nazwa)` z tabeli powyżej, albo odczyt `tokens`.

Klasa na telefonie nazywa się tak samo jak na webie: `bg-canvas`, `text-ink`, `text-muted`, `bg-accent`, `bg-dark-chapter`, `text-dark-chapter-fg`, `font-sans`, `text-body`, `tracking-body`, `p-5`, `gap-7`, `rounded-card`, `rounded-pill`, `duration-nav`, `ease-standard`.

Rodzina na telefonie nazywa się `Inter`, bo tak brzmi `tokens.font.family`. Sposób załadowania pliku fontu jest przy instalacji NativeWind.

## Tutorial

Cel: przycisk akcentu i jasna powierzchnia czytają `tokens`, a klasa arbitralna z heksem nie powstaje.

1. Zostaw liczby w `tokens.ts`. Nowa wartość wizualna to nowe pole obiektu, nowy klucz w `cssCustomProperties()` (z jednostką z tabeli reguł) i jedna linia `var(--…)` w `@theme inline` na webie i na telefonie.
2. Na webie wywołaj `cssCustomProperties()` na `documentElement` przed renderem. Na telefonie owiń drzewo w `ThemeRoot` z sekcji NativeWind.
3. Wklej blok `@theme inline` do `src/Clients/web/src/styles.css` i do CSS NativeWind. W bloku są wyłącznie `var(--…)` i resety `initial`.
4. Złóż przycisk klasami z kontraktu:

```tsx
<button
  type="button"
  className="bg-accent text-white rounded-pill px-5 py-3 text-small tracking-small duration-nav ease-standard"
>
  Dalej
</button>
```

`px-5` to 24px (`space.5`), `py-3` to 12px (`space.3`), `text-small` to 14px, `rounded-pill` to 9999px, tło to `#0071e3`, czas to 250ms, krzywa to `cubic-bezier(0.4, 0, 0.2, 1)`. Żadna z tych liczb nie stoi w `className`.

5. Odrzuć klasę, która wpisuje liczbę z obiektu jeszcze raz: `bg-[#0071e3]`, `text-[#1d1d1f]`, `text-[17px]`, `p-[24px]`, `rounded-[18px]`, `tracking-[-0.012em]`, `duration-[250ms]`.

Sprawdzenie arbitrów:

```bash
rg -n "bg-\[#|text-\[#|text-\[[0-9]+px\]|p-\[[0-9]+px\]|rounded-\[[0-9]+px\]|duration-\[[0-9]+ms\]" src/Clients/web/src src/Clients/mobile
```

Brak trafień znaczy, że komponenty mapują kontrakt, a nie kopiują paletę.

## Przypadki użycia

### Jasna strona

Tło strony to `bg-canvas` (`#f5f5f7`). Tekst to `text-ink` (`#1d1d1f`) i `font-sans`. Drugi plan (podpis, meta) to `text-muted` (`#6e6e73`). Karta leży na bieli: `bg-white`, `rounded-card` (18px), oddech z górnych kluczy skali (`p-7` = 48px, `gap-8` = 64px). Rozmiar akapitu to `text-body` (17px) z `tracking-body` (-0.014em). Obrysu w tokenach nie ma: kartę odcina biel od płótna i promień `card`.

### Ciemny hero

Hero na home używa pary `darkChapter`, nie osobnego trybu całej aplikacji. Tło: `bg-dark-chapter` (`#000000`). Tekst i drugi plan na hero: `text-dark-chapter-fg` (`#f5f5f7`, pole `color.darkChapter.foreground`). Tytuł: `text-display` (80px) i `tracking-display` (-0.015em). Akcent na hero zostaje `bg-accent` / `text-accent` (`#0071e3`). Czas wejścia i przypięcia czyta `tokens.duration.reveal` (700), `tokens.duration.heroFade` (600), `tokens.duration.heroPin` (1200) oraz `tokens.easing.hero`. Jeden scroll home opisuje [motion/02-hero.md](motion/02-hero.md); przesunięcie na liście telefonu opisuje [reanimated.md](reanimated.md).

### Ten sam akcent na webie i telefonie

Web:

```tsx
<button type="button" className="bg-accent text-white rounded-pill px-5 py-3">
  Zapisz
</button>
```

Telefon (osobny ekran, ta sama klasa):

```tsx
<Pressable className="bg-accent rounded-pill px-5 py-3">
  <Text className="text-white text-small tracking-small">Zapisz</Text>
</Pressable>
```

Oba wskazują `--accent` → `tokens.color.accent`. Telefon nie dostaje innego niebieskiego. Gdy styl musi być obiektem (Reanimated, `style`), źródłem jest `tokens.color.accent`, nie string wpisany w komponencie.

## Zasady wyglądu

- Płótno `#f5f5f7`, atrament `#1d1d1f`, akcent `#0071e3`, biel `#ffffff`, muted `#6e6e73`, krój `Inter`.
- Duży oddech: sekcje i hero biorą `space` od `7` w górę (48, 64, 80, 96, 128, 160). Gęsty rząd w komponencie bierze `1`–`6`.
- Mało ramek: w `tokens` nie ma koloru obrysu ani cienia. Separacja to płótno, biel, atrament i promień.
- Ciemny jest hero (`darkChapter`), reszta stron zostaje na `canvas` / `ink`.
- Karta używa `rounded-card`. Skala `sm` / `md` / `lg` zostaje dla kontrolek. Wspólne 18px nie łączy kluczy `card` i `lg` w jeden.
- Rozmiar i tracking idą w parze o tym samym kluczu (`text-h2` z `tracking-h2`).
- Jeden akcent na obu klientach. Intranet, gdy wejdzie do tego samego klienta, używa `bg-accent` i reszty tej mapy.

## Poza zakresem

- Instalacja Tailwinda, plugin Vite i zamiana wejścia CSS: [tailwind/01-instalacja.md](tailwind/01-instalacja.md).
- Użycie klas motywu na tle strony, przycisku i hero bez powtarzania tej tabeli liczb: [tailwind/02-motyw.md](tailwind/02-motyw.md).
- Nav, siatka kart, szerokość artykułu: [tailwind/03-uklad.md](tailwind/03-uklad.md).
- Skład Inter na hero, w tytule i w podpisie: [tailwind/04-typografia.md](tailwind/04-typografia.md).
- Hover, focus, disabled, pusto, błąd, `prefers-reduced-motion`: [tailwind/05-stany.md](tailwind/05-stany.md).
- Zejście z osobnych arkuszy do jednego `styles.css`: [tailwind/06-css-zamiast-scss.md](tailwind/06-css-zamiast-scss.md).
- Zmienne shadcn (`background`, `primary`, promień kontrolek) spięte z tymi tokenami, bez nowej palety: [shadcn/02-motyw.md](shadcn/02-motyw.md).
- Komendy instalacji NativeWind i lista ekranów telefonu: [nativewind/01-instalacja.md](nativewind/01-instalacja.md), [nativewind/02-ekrany.md](nativewind/02-ekrany.md).
- Przebieg animacji: [motion/01-przejscia.md](motion/01-przejscia.md), [motion/02-hero.md](motion/02-hero.md), [reanimated.md](reanimated.md).
- `line-height`, `font-weight`, cień, obrys, breakpoint. Dopóki nie ma ich w `tokens`, klasa ich nie wymyśla jako drugiej skali.
- Tamagui. Osobny klient intranetu. Druga paleta „na szybko” w klasie arbitralnej.

## Gotowe gdy

- Każde pole `tokens` ma w tym pliku ścieżkę, wartość, jednostkę, nazwę z `cssCustomProperties()`, linię `@theme inline` i klasę.
- `styles.css` weba i CSS NativeWind zawierają blok `@theme inline` z samymi `var(--…)` oraz resetami `initial` wymienionymi wyżej.
- `documentElement` (web) i `vars(cssCustomProperties())` (telefon) ustawiają właściwości, zanim UI się maluje.
- `p-5` to 24px, `text-body` to 17px, `rounded-lg` to 18px, `bg-accent` to `#0071e3` na obu klientach.
- Heksy palety występują w `src/Clients/shared/theme/tokens.ts`. Komponenty używają klas z tabel albo pól `tokens` w JS.
- Jasna strona, ciemny hero i przycisk akcentu na telefonie dają się złożyć z klas tego kontraktu, bez `bg-[#…]` i bez osobnego obiektu kolorów.
