# 01. Szkielet Expo

Poprzedni numer to [Playwright](../11-web/07-playwright.md). Następny to [logowanie na telefonie](02-logowanie.md).

Ten numer stawia aplikację Expo w `src/clients/mobile`. Klasy wyglądu biorą liczby z `tokens`. Jest ekran pusty i ten sam `resolveLocale` co w webie. Nie ma jeszcze logowania ani listy kursów.

## Po co

Osobna paleta na telefonie zrobi drugi produkt. Expo zamyka budowanie bez utrzymywania natywnych projektów ręcznie od pierwszego dnia. Wspólne `tokens` i i18n zamykają drugi słownik napisów.

## Dlaczego ta technologia

### Expo

Pakiet `expo` jest na MIT. Problem, który zamyka, to ręczne projekty Xcode i Gradle, zanim jest jeden ekran. Odpada goły React Native bez Expo jako punkt startu. Koszt: jesteśmy w modelu Expo. Moduł, którego Expo nie ma, wymaga jawnej decyzji, nie dopiski w natywnym katalogu po cichu. Na tym numerze natywny katalog generuje Expo i nie jest poprawiany ręcznie.

### NativeWind

Klasy `bg-canvas`, `text-ink`, `bg-accent` wskazują na te same liczby. Problem, który zamyka, to `StyleSheet` z heksem. Odpada kopia `tokens` do obiektu stylu w każdym ekranie. Koszt: konfiguracja NativeWind czyta motyw. Heks w pliku ekranu jest błędem testu tak samo jak w webie.

### Język

`resolveLocale` przy starcie czyta język urządzenia. Nieznany daje `pl`. Przełącznik na telefonie nie jest segmentem URL, bo nie ma URL. Zapis wyboru jest w pamięci aplikacji. Problem, który zamyka, to telefon zawsze po polsku mimo języka urządzenia, albo zawsze w języku urządzenia bez wyjścia. Wybór użytkownika wygrywa z urządzeniem, gdy został zapisany.

### Wersja

`mobile-vX.Y.Z` wkompilowana przy buildzie, analogicznie do webu. Ekran O aplikacji dojdzie w numerze ekranów. Tu jest stała w module `version.ts`, testowana.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| Expo | toolchain React Native |
| NativeWind | klasy na komponentach natywnych |
| wybór języka | zapisany, ważniejszy niż język urządzenia |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/clients/mobile/` | aplikacja Expo |
| `src/clients/mobile/src/version.ts` | `mobile-vX.Y.Z` |
| `src/clients/mobile/src/locale.ts` | start języka |
| `src/clients/pnpm-workspace.yaml` | dopisany pakiet `mobile` |

Workspace już jest. Ten numer dopisuje pakiet.

## Plan

### 1. Aplikacja

`App` renderuje jeden ekran z klasą `bg-canvas` i tekstem `nav.home` z JSON. Nie duplikuje pliku JSON. Importuje `shared/i18n`.

### 2. Test

Vitest bez emulatora na `resolveLocale` i na tym, że wersja pasuje do `mobile-v`. Emulator nie jest w CI tego numeru.

### 3. Heks

`grep` heksa po `src/clients/mobile` poza konfiguracją motywu, która musi zgadzać się z `tokens`, jest pusty w plikach ekranów.

### Co jest do czego

| Plik | Po co |
| --- | --- |
| `tokens.ts` | liczby |
| NativeWind | klasy |
| `pl.json` | napisy, te same co web |

## Testy

Nazwa: `device_locale_unknown_is_pl`.

| Arrange | Act | Assert |
| --- | --- | --- |
| język urządzenia `de`, brak zapisu wyboru | funkcja startu | `pl` |

```bash
cd src/clients/mobile && pnpm exec vitest run
```

## Checkpoint

```bash
cd src/clients/mobile && pnpm exec vitest run
pnpm exec tsc --noEmit
```

`pnpm exec expo start` jest w dokumentacji numeru jako sprawdzenie ręczne, nie jako warunek CI. CI nie ma emulatora.

## Umiesz wyjaśnić

Dlaczego Expo, a nie od razu natywne projekty?

Bo pierwszy ekran nie potrzebuje własnej warstwy natywnej. Expo ją generuje. Ręczna edycja `android/` rozjedzie się z generacją.

Dlaczego ten sam JSON co web?

Bo napis `empty.list` ma być ten sam. Drugi plik tłumaczeń na telefonie zjedzie w pierwszym tygodniu.

Czym język telefonu różni się od segmentu URL?

Nie ma adresu. Jest zapis wyboru. Nieznany język urządzenia wpada w `pl`, tak jak zły segment URL.

Dlaczego test nie odpala emulatora?

Bo emulator w CI jest wolny i kruchy, a reguła języka jest czystą funkcją. Ekran z siecią dojdzie później i też najpierw na podstawionym kliencie.

Gdzie jest heks?

W `tokens.ts`. Ekran ma klasę. Konfiguracja NativeWind nie wprowadza trzeciego koloru.

## Zasady

- Brak heksa w pliku ekranu.
- Brak kopii `pl.json` w `mobile`.
- Wersja jest w module, nie w literale w komponencie rozrzuconym po ekranach.
- Natywne katalogi nie są ręcznie utrzymywane w tym numerze.
- Test startu języka nie potrzebuje urządzenia.
- Aplikacja nie woła jeszcze API.

## Poza zakresem

- PKCE jest w [02-logowanie.md](02-logowanie.md).
- Lista ekranów jest w [03-ekrany.md](03-ekrany.md).
- Push jest w [04-powiadomienia.md](04-powiadomienia.md).

## Źródła

- [Expo](https://docs.expo.dev/). Wiążące jest tworzenie aplikacji bez ręcznego projektu natywnego na start. Dotyczy katalogu `mobile`. Ze źródła nie bierzemy eject jako kroku tego numeru.
- [NativeWind](https://www.nativewind.dev/). Wiążące są klasy zamiast heksa w komponencie. Dotyczy `bg-canvas`. Ze źródła nie bierzemy drugiej palety.
- Słownik, `resolveLocale`. Wiążące jest `pl` dla nieznanego kodu. Dotyczy startu. Ze słownika nie bierzemy osobnych kluczy chromu tylko dla telefonu.

## Gotowe gdy

- Vitest języka i wersji przechodzi.
- Ekran startowy używa klasy i klucza chromu.
- W plikach ekranu nie ma heksa palety.
- `mobile` jest w workspace pnpm.
- `tsc` zwraca zero.
