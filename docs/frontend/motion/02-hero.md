# Hero na home

Jedna scena scrolla na ciemnym hero strony głównej. Dotyczy wyłącznie klienta web. Telefon opisuje [reanimated.md](../reanimated.md).

## Po co

Na `/pl` i `/en` pierwsza sekcja home zostaje przyklejona na czas jednego przewinięcia. W tej klatce imię maleje, a rola i dwa ostatnie rozdziały doświadczenia dochodzą do pełnej czytelności. Po zejściu ze sceny strona jedzie dalej zwykłym scrollem: lista wykładów i notatki.

Drugiej przypiętej sekwencji nie ma. CV, wykłady, kompendium, notatki, login i rejestracja zostają zwykłymi stronami.

Gdy przeglądarka zgłasza `prefers-reduced-motion: reduce`, hero jest statyczne: ta sama treść, jedna wysokość ekranu, bez drogi przewijania i bez stylów spiętych ze scrollem.

Wejścia sekcji pod hero i zmiana widoku między trasami są w [01-przejscia.md](01-przejscia.md).

## Wersje i pakiety

Biblioteka to [`motion`](https://www.npmjs.com/package/motion) `14.1.0`. Import idzie z `motion/react`. React w `src/Clients/web` to `19.2.3`, menedżer pakietów to pnpm.

Ta scena używa `motion`, `useScroll`, `useTransform` i `useReducedMotion`. `AnimateView` z `motion/react-animate-view` wymaga Reacta 19.3 i tu nie wchodzi.

`MotionConfig reducedMotion` wyłącza animacje z propa `animate`. Wartości podane w `style` przez `useTransform` zostają aktywne, więc reduced motion jest tu jawną gałęzią renderu, a nie samym kontekstem.

Liczby czasu i koloru biorą się z [tokens.ts](../../../src/Clients/shared/theme/tokens.ts). Kontrakt palety jest w [motyw.md](../motyw.md). Ta scena czyta:

- `color.darkChapter.background` (`#000000`) i `color.darkChapter.foreground` (`#f5f5f7`) — pole hero
- `duration.heroPin` (`1200`) — baza długości drogi scrolla
- `space[5]` (`24`) — przesunięcie roli
- `space[6]` (`32`) — przesunięcie rozdziału

`duration.heroFade` i `easing.hero` zostają przy przejściach czasowych z [01-przejscia.md](01-przejscia.md). Tu zegarem jest pozycja scrolla, więc krzywa easingu na scrub nie wchodzi.

Skala końcowa imienia to `0.55`. To jedna liczba tej sceny. Zmiana skali ląduje w `tokens.ts` obok `duration.heroPin`, a nie w klasie.

## Ścieżki w repo

| Ścieżka | Rola |
| --- | --- |
| `src/Clients/web/package.json` | zależność `motion` |
| `src/Clients/web/src/app/pages/home/HomePage.tsx` | jedyne miejsce, które montuje scenę |
| `src/Clients/web/src/app/pages/home/HomeHero.tsx` | scena: scroll, reduced motion, układ |
| `src/Clients/web/src/app/pages/home/HeroChapter.tsx` | jeden rozdział doświadczenia (`article`) |
| `src/Clients/shared/theme/tokens.ts` | kolory, droga scrolla, przesunięcia |
| `src/Clients/web/src/content/pl.json`, `en.json` | brew, lead, etykiety linków |
| `cv.json` w bundlu | imię, rola, doświadczenie |

`HomePage` renderuje `HomeHero`, potem wykłady i notatki. Treść JSON-a opisuje [react/03-tresc-i-auth.md](../react/03-tresc-i-auth.md). Znaczniki opisuje [html/01-semantyka.md](../html/01-semantyka.md).

## Tutorial

Z katalogu klienta web:

```bash
cd src/Clients/web
pnpm add motion@14.1.0
```

Scena ma dwie warstwy. Zewnętrzna `section` jest torem: przy pełnym ruchu jej wysokość to jeden ekran plus droga. Wewnętrzna klatka jest `sticky` przy `top: 0` i ma co najmniej `100dvh`. `useScroll` patrzy na sekcję z offsetem `['start start', 'end end']`: postęp `0` jest wtedy, gdy góra sekcji styka się z górą okna, postęp `1` wtedy, gdy dół sekcji styka się z dołem okna.

Długość drogi ponad ekran to `tokens.duration.heroPin * 2.5` pikseli. Przy bazie `1200` daje to `3000` px. Komponent liczy tę wysokość z tokenu. Liczba `3000` nie jest wpisywana w klasie ani w CSS.

Postęp mapuje `useTransform`. Poza podanym zakresem Motion trzyma wartość skrajną.

| Warstwa | Zmiana | Postęp |
| --- | --- | --- |
| `h1` z imieniem | `scale` od `1` do `0.55`, `transformOrigin` `center top` | `0` → `0.40` |
| rola | `opacity` od `0` do `1`, `y` od `tokens.space[5]` do `0` | `0.15` → `0.55` |
| pierwszy rozdział | `opacity` od `0` do `1`, `y` od `tokens.space[6]` do `0` | `0.40` → `0.75` |
| drugi rozdział | `opacity` od `0` do `1`, `y` od `tokens.space[6]` do `0` | `0.60` → `1` |

Brew, nagłówek „teraz”, lead i oba linki są widoczne przez cały postęp. Rozdziały to `cv.experience.slice(0, 2)`. Hooki dwóch rozdziałów są wołane zawsze, także gdy wpis jest jeden albo zero: drugi styl po prostu nie trafia na element.

`useReducedMotion()` zwraca `true`, `false` albo `null` zanim media query jest znane. Scena scrolla działa wyłącznie przy `false`. Przy `true` i `null` renderuje się hero statyczne, żeby pierwszy paint i test w jsdom pokazywały treść.

```tsx
import { tokens } from '@jjdevhub/theme';
import { motion, useReducedMotion, useScroll, useTransform } from 'motion/react';
import { useRef } from 'react';
import { Link } from 'react-router';

import type { CvLocaleContent, HomeContent, LocaleCode } from '../../content/content.model';
import { HeroChapter } from './HeroChapter';

const nameEndScale = 0.55;

type HomeHeroProps = {
  locale: LocaleCode;
  home: HomeContent;
  cv: CvLocaleContent;
};

export function HomeHero({ locale, home, cv }: HomeHeroProps) {
  const chapters = cv.experience.slice(0, 2);
  const sceneRef = useRef<HTMLElement>(null);
  const reduceMotion = useReducedMotion();
  const { scrollYProgress } = useScroll({
    target: sceneRef,
    offset: ['start start', 'end end'],
  });

  const nameScale = useTransform(scrollYProgress, [0, 0.4], [1, nameEndScale]);
  const roleOpacity = useTransform(scrollYProgress, [0.15, 0.55], [0, 1]);
  const roleY = useTransform(scrollYProgress, [0.15, 0.55], [tokens.space[5], 0]);
  const firstOpacity = useTransform(scrollYProgress, [0.4, 0.75], [0, 1]);
  const firstY = useTransform(scrollYProgress, [0.4, 0.75], [tokens.space[6], 0]);
  const secondOpacity = useTransform(scrollYProgress, [0.6, 1], [0, 1]);
  const secondY = useTransform(scrollYProgress, [0.6, 1], [tokens.space[6], 0]);

  const scene = reduceMotion === false;
  const chapterMotion = [
    { opacity: firstOpacity, y: firstY },
    { opacity: secondOpacity, y: secondY },
  ];
  const runway = `calc(100dvh + ${tokens.duration.heroPin * 2.5}px)`;

  return (
    <section
      ref={sceneRef}
      aria-label={cv.name}
      style={scene ? { minHeight: runway } : { minHeight: '100dvh' }}
    >
      <div className={scene ? 'sticky top-0 flex min-h-dvh flex-col justify-center' : 'flex min-h-dvh flex-col justify-center'}>
        <p>{home.eyebrow}</p>
        <motion.h1
          style={
            scene
              ? { scale: nameScale, transformOrigin: 'center top' }
              : { scale: nameEndScale, transformOrigin: 'center top' }
          }
        >
          {cv.name}
        </motion.h1>
        <motion.p style={scene ? { opacity: roleOpacity, y: roleY } : undefined}>{cv.role}</motion.p>
        <div>
          <h2>{home.currentTitle}</h2>
          <p>{home.currentLead}</p>
          <div>
            {chapters.map((item, index) => (
              <HeroChapter
                key={`${item.title}|${item.period}`}
                item={item}
                style={scene ? chapterMotion[index] : undefined}
              />
            ))}
          </div>
        </div>
        <p>
          <Link to={`/${locale}/cv`}>{home.ctaCv}</Link>
          <Link to={`/${locale}/courses`}>{home.ctaCourses}</Link>
        </p>
      </div>
    </section>
  );
}
```

`HeroChapter` zostaje osobnym plikiem. Renderuje `motion.article` i przyjmuje opcjonalny `style` ze sceny. Przy hero statycznym `style` jest `undefined`, a tekst rozdziału jest od razu widoczny.

Na `section` siadają klasy tła i tekstu ciemnego rozdziału z mapy tokenów: [tailwind/02-motyw.md](../tailwind/02-motyw.md). Typografię opisuje [tailwind/04-typografia.md](../tailwind/04-typografia.md), odstęp kolumny [tailwind/03-uklad.md](../tailwind/03-uklad.md). W przykładzie zostały klasy klatki: `sticky`, `top-0`, `min-h-dvh`.

Sprawdzenie w dev (`pnpm start` w `src/Clients/web`, port 4200):

1. Otwórz `/pl`. Ciemne hero wypełnia pierwszy ekran, imię jest duże, rola i rozdziały jeszcze dochodzą.
2. Przewiń w obrębie drogi. Klatka zostaje, imię maleje do `0.55`, rola i dwa rozdziały wchodzą w kolejności z tabeli.
3. Przewiń dalej. Sekcje wykładów i notatek jadą zwykłym scrollem, na jasnym płótnie.
4. W narzędziach przeglądarki włącz `prefers-reduced-motion: reduce` i odśwież `/pl`. Imię, rola, lead i rozdziały są widoczne bez przewijania, a sekcja hero ma wysokość jednego ekranu.

## Przypadki użycia

### Home, pełny ruch

Użytkownik wchodzi na `/pl` albo `/en`. Shell zostaje nad sceną. Hero czyta brew, tytuł i lead z JSON-a języka oraz imię, rolę i doświadczenie z CV w bundlu.

Jedno przewinięcie prowadzi przez tabelę postępu. Link „Zobacz CV” prowadzi do `/${locale}/cv`, link wykładów do `/${locale}/courses`. Oba są w klatce od początku sceny. Animacja samej zmiany trasy jest w [01-przejscia.md](01-przejscia.md).

Gdy doświadczenie ma jeden wpis, schodzi tylko pierwszy rozdział. Gdy lista jest pusta, zostają imię, rola, blok „teraz” i linki. Scena i tak ma jedną drogę.

### Home, reduced motion

`useReducedMotion()` nie jest `false`. Sekcja ma `min-height: 100dvh` i nie dostaje drogi `heroPin * 2.5`. Imię stoi w skali `0.55` z początkiem transformacji u góry na środku. Rola i rozdziały mają krycie `1` i `y` równe `0`. `scrollYProgress` nie trafia do `style`.

Ten sam statyczny układ jest na `/pl` i `/en`. Podstrony nie dostają własnej wersji tej sceny.

## Zasady wyglądu

Hero jest ciemnym polem: tło `#000000`, tekst `#f5f5f7`, krój Inter. Płótno `#f5f5f7` i atrament `#1d1d1f` zaczynają się pod sceną. Akcent `#0071e3` zostaje przy kontrolkach jasnych sekcji; na hero linki są kolorem pierwszego planu ciemnego pola.

Jeden `h1` to imię. Brew jest akapitem nad nim. Blok „teraz” ma `h2`. Każdy rozdział jest `article` z `h3`. Klatka ma duży oddech ze skali `space` i wąską kolumnę na środku. Rozdział może dostać `radius.card` i lekkie rozjaśnienie tego samego ciemnego pola, bez obrysu.

Ruch ogranicza się do `scale`, `opacity` i `y` warstw z tabeli. Reszta klatki stoi.

## Poza zakresem

- Wejście listy wykładów, notatek i przejście na szczegół: [01-przejscia.md](01-przejscia.md).
- Przesunięcie hero względem offsetu listy na telefonie: [reanimated.md](../reanimated.md). Ten plik nie opisuje Reanimated ani NativeWind.
- Katalog klas odstępu, siatki i typografii: pliki w [tailwind/](../tailwind/).
- Pełna tabela tokenów: [motyw.md](../motyw.md).
- Przypięta sekwencja na CV, wykładach, kompendium, notatkach, loginie i rejestracji.
- GSAP, ScrollTrigger i Lenis w tej scenie. Hero czyta scroll dokumentu przez `useScroll`.

## Gotowe gdy

- `motion@14.1.0` jest zależnością `src/Clients/web`, a `HomeHero` importuje z `motion/react`.
- Scena jest zamontowana tylko z `HomePage`, na `/pl` i `/en`.
- Przy pełnym ruchu jedna klatka zostaje na drodze `duration.heroPin * 2.5` pikseli i przechodzi skalę imienia oraz wejście roli i co najwyżej dwóch rozdziałów według tabeli postępu.
- Przy `prefers-reduced-motion: reduce` hero jest statyczne: treść widoczna od razu, skala imienia `0.55`, brak dodatkowej wysokości toru.
- Podstrony nie mają drugiej przypiętej sekwencji.
- Implementacja telefonu zostaje w [reanimated.md](../reanimated.md) i nie jest powtórzona w tym pliku.
