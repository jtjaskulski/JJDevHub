# 02. Szkielet webu

Poprzedni numer to [motyw](01-motyw.md). Następny to [klient OpenAPI](03-klient-openapi.md).

Ten numer stawia aplikację Vite, React i TypeScript. Są trasy ze słownika, bramka języka i stopka `web-vX.Y.Z`. Nie ma jeszcze danych z API. Puste strony pokazują klucz chromu, nie treść kursu.

## Po co

Bez jednego routera każdy ekran wymyśli adres. Adres jest kontraktem: `/:lang/courses/:slug` nie może znaczyć raz listy, raz lekcji. Stopka zamyka pytanie, który bundle jest otwarty, tak jak `GET /versions` zamyka je dla usług. Na tym numerze stopka pokazuje tylko numer webu. Sześć wierszy usług dojdzie, gdy będzie klient HTTP.

## Dlaczego ta technologia

### Vite i React

`react`, `react-dom`, `vite`, TypeScript. Problem, który zamyka, to brak miejsca na trasy, dopóki „kiedyś będzie frontend”. Odpada drugi framework UI. Koszt: aplikacja jest klientem publicznym. Nie ma sekretu w `import.meta.env` poza adresem bramy, który i tak jest publiczny.

### React Router

Trasy z słownika. `LangGate` wpuszcza tylko `pl` i `en`. Inny segment idzie na `/pl`. `/login`, `/register` i `/intranet` są zadeklarowane przed `/:lang`, żeby słowo `login` nie zostało językiem. Problem, który zamyka, to `login` jako kod kultury. Odpada ręczne `if` w każdym ekranie. Koszt: kolejność tras ma test.

### Chrom

`src/clients/shared/i18n/pl.json` i `en.json` mają te same klucze co słownik. `resolveLocale` zwraca `pl` albo `en`. Nieznany kod daje `pl`. Problem, który zamyka, to napis wkomponowany po polsku w przycisku i po angielsku w drugim pliku, który nie istnieje. Odpada biblioteka i18n z formatem, którego nie da się porównać testem kluczy. Wystarczy JSON i funkcja. Koszt: brak odmiany przez liczby, dopóki jej nie dopiszemy. Na starcie napisy są całymi zdaniami.

### Wersja

`VITE_WEB_VERSION` wstawiana przy buildzie. W Development wolno jej być `web-v0.0.0`. W obrazie jest tagiem `web-vX.Y.Z`. Stopka czyta zmienną, nie woła API o własny numer. Problem, który zamyka, to numer wpisany w kodzie na stałe. Odpada pobieranie wersji webu z `GET /versions`: ten endpoint nie zwraca pola `web`. Zwraca usługi. Pole webu jest w bundlu, bo bundle jest wersją.

### Tailwind

Ten numer podpina Tailwind 4 i mapuje `bg-canvas`, `text-ink`, `bg-accent` na `tokens`. Jeden plik `src/clients/web/src/styles.css`. Problem, który zamyka, to klasa z heksem w nawiasie. Odpada drugi plik CSS. Koszt: konfiguracja motywu w CSS `@theme`. Heks w `@theme` pochodzi z komentarza „równe tokens”, a test i tak pilnuje `tokens.ts`. Żeby nie było dwóch źródeł, `@theme` czyta wartości wygenerowane albo powtórzone. Dwa źródła są złe. Wybór: klasy w CSS używają zmiennych ustawianych w `styles.css` jednorazowo z tych samych heksów, a test `grep` heksa po `src/clients/web` poza dozwoloną listą pada. Dozwolona lista to `styles.css` i `tokens.ts`. Komponent nie jest na liście. Lepszy wybór, jeden heks: tylko `tokens.ts`, a `styles.css` nie zawiera heksa, tylko `var` ustawiane skryptem. To jest kruche. Praktycznie: heks wolno powtórzyć w `styles.css` i test porównuje, że zestaw heksów w `styles.css` jest podzbiorem `tokens.ts`. Rozjazd pada. Komponent z heksem pada osobno.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| `LangGate` | wpuszcza `pl` i `en` |
| chrom | napisy z `pl.json` i `en.json` |
| `web-vX.Y.Z` | wersja bundla w stopce |
| `Shell` | belka, treść, stopka |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/clients/web/` | aplikacja Vite |
| `src/clients/web/src/routes.tsx` | drzewo tras |
| `src/clients/web/src/LangGate.tsx` | język |
| `src/clients/web/src/Shell.tsx` | układ |
| `src/clients/shared/i18n/pl.json` | napisy |
| `src/clients/shared/i18n/en.json` | te same klucze |
| `src/clients/shared/i18n/locale.ts` | `resolveLocale` |
| `src/clients/web/src/styles.css` | jedyne wejście CSS |

Komponent jest funkcją, eksportem nazwanym, jeden na plik. Nie ma `React.FC`.

## Plan

### 1. Trasy

Zgodnie ze słownikiem, także puste `/intranet` i `/login`. Puste znaczy: widać klucz `nav.login` albo tytuł trasy, nie dane.

### 2. Język

Przełącznik na stronie publicznej podmienia pierwszy segment i zostawia resztę ścieżki. Na `/login` i `/intranet` nie rusza adresu. Test klika przełącznik na `/pl/courses` i oczekuje `/en/courses`.

### 3. Stopka

Render stopki zawiera `web-v0.0.0` w teście, bo tak ustawiona jest zmienna. Tekst klucza `footer.versionLabel` jest obok.

### 4. Test kluczy

Test czyta oba JSON i porównuje zbiory kluczy. Brak klucza w jednym pliku pada.

### Co jest do czego

| Element | Kiedy widać |
| --- | --- |
| `LangGate` | każdy adres z językiem |
| stopka | każdy ekran w `Shell` |
| przełącznik | strona publiczna |

## Testy

Nazwa: `unknown_lang_redirects_to_pl`.

| Arrange | Act | Assert |
| --- | --- | --- |
| wejście na `/de/courses` | render routera | lokalizacja `/pl` |

Nazwa: `footer_shows_baked_web_version`.

```bash
cd src/clients/web && pnpm exec vitest run
```

## Checkpoint

```bash
cd src/clients/web && pnpm exec vitest run
pnpm exec tsc --noEmit
```

`pnpm dev` otwiera stronę. Adres `/pl` pokazuje belkę. Adres `/xx` ląduje na `/pl`. W stopce widać `web-v`.

## Umiesz wyjaśnić

Dlaczego `login` nie jest segmentem języka?

Bo trasa `/:lang` połknęłaby `/login` i odesłała na `/pl`. Trasy bez języka są zadeklarowane wcześniej. Test to trzyma.

Dlaczego wersja webu nie pochodzi z `GET /versions`?

Bo ten endpoint mówi o usługach. Bundle przeglądarki jest wersją plików, które właśnie się załadowały. Wpisuje się go przy buildzie. Usługi dojdą obok, nie zamiast.

Skąd napis przycisku?

Z `pl.json` albo `en.json`, klucz ze słownika. Nie z literału w komponencie. Test kluczy łapie brak tłumaczenia.

Dlaczego jeden plik CSS?

Bo drugi plik staje się drugim motywem. `@import` poza `styles.css` nie jest dozwolony jako nowe wejście.

Co z heksem w `styles.css`?

Wolno mu być tylko jeśli test zgodności z `tokens.ts` przechodzi. W pliku komponentu heksa nie ma wcale.

## Zasady

- Trasy ze słownika, bez nowego adresu „na chwilę”.
- `React.FC` nie występuje.
- Jeden plik CSS.
- Klucze chromu są w obu językach.
- Wersja webu jest z zmiennej builda.
- Komponent nie woła `fetch`.

## Poza zakresem

- Wywołania API są w [03-klient-openapi.md](03-klient-openapi.md).
- Logowanie PKCE jest w [04-logowanie-pkce.md](04-logowanie-pkce.md).
- Treść kursu jest w [05-tresc-publiczna.md](05-tresc-publiczna.md).

## Źródła

- [React](https://react.dev/). Wiążące są komponenty funkcyjne. Dotyczy zakazu `React.FC`. Ze źródła nie bierzemy klasy komponentu.
- [Vite](https://vite.dev/guide/). Wiążące są `import.meta.env` i build. Dotyczy `VITE_WEB_VERSION`. Ze źródła nie bierzemy sekretu w zmiennej z prefiksem `VITE_`.
- [React Router](https://reactrouter.com/). Wiążąca jest kolejność tras. Dotyczy `LangGate` i `/login`. Ze źródła nie bierzemy adresu spoza słownika.
- Słownik, sekcja ekranów i kluczy chromu. Wiążąca jest lista. Dotyczy `routes.tsx` i JSON. Ze słownika nie bierzemy piątego języka.

## Gotowe gdy

- Vitest tras i kluczy przechodzi.
- `/de` kończy się na `/pl`.
- Stopka pokazuje wersję z zmiennej.
- `tsc --noEmit` zwraca zero.
- W komponentach nie ma `fetch` i nie ma heksa.
