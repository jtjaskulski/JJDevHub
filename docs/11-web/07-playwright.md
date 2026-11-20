# 07. Playwright

Poprzedni numer to [intranet](06-intranet.md). Następny folder zaczyna się od [Expo](../12-mobile/01-szkielet-expo.md).

Ten numer uruchamia jedną ścieżkę w prawdziwej przeglądarce: gość otwiera lekcję publiczną, widzi stopkę z wersją webu i nie widzi skryptu z treści. Druga ścieżka, jeśli środowisko ma Identity, loguje właściciela do intranetu. Test nie zastępuje Vitest. Sprawdza to, czego jsdom nie widzi.

## Po co

Vitest renderuje komponenty bez silnika przeglądarki. Łatwo przejść test, w którym `TrustedHtml` w jsdom wygląda dobrze, a prawdziwy dokument i tak wykona atrybut. Playwright zamyka tę lukę na jednej ścieżce, nie na każdym przycisku. Jedna ścieżka utrzymana jest lepsza niż dwadzieścia pominiętych.

## Dlaczego ta technologia

### `@playwright/test`

Pakiet jest na Apache-2.0. Problem, który zamyka, to brak testu poza jsdom. Odpada Selenium i WebDriver z osobnym serwerem, dopóki Playwright wystarcza. Koszt: przeglądarka na runnerze. Job jest osobny albo jest krokiem po vitest, na `ubuntu-latest`, z instalacją przeglądarki z cache. Nie leci w `unit-tests` .NET. Ma własny skrypt `pnpm exec playwright test`. W CI jest job `web-e2e` albo krok w istniejącym workflow, który i tak buduje web. Nowa nazwa checka `web-e2e` wchodzi do rulesetów w tym samym scaleniu. Skoro rulesety wymagają checka, job zawsze startuje. Gdy nie ma katalogu `e2e`, loguje `no material`. Od tego numeru katalog jest i test leci.

### Środowisko

Test podnosi web `vite preview` i podstawia API przez `page.route`, albo łączy się z AppHostem. Podstawienie trasy jest stabilniejsze w CI: nie wymaga Kafki, żeby zobaczyć lekcję. Problem, który zamyka, to e2e, który pada, bo projektor nie zdążył, i nic nie mówi o HTML. Odpada wymaganie pełnego klastra. Koszt: test nie łapie rozjazdu proxy. Rozjazd proxy jest w teście .NET bramy. Tu jest przeglądarka i DOM.

Ścieżka z prawdziwym API jest oznaczona `Category` playwrighta `@live` i nie jest domyślna w CI. Domyślna jest `@mocked`.

### Asercje

Gość na `/pl/courses/przyklad/lekcja` widzi tytuł z podstawionej odpowiedzi. W DOM nie ma elementu `script` w treści lekcji, choć JSON miał `<script>`. Stopka zawiera `web-v`. Link do intranetu bez sesji prowadzi do logowania albo jest niewidoczny; wejście na `/intranet` kończy się na `/login`. Problem, który zamyka, to test, który tylko sprawdza status HTTP. Odpada asercja samego `200`.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| `@mocked` | API podstawione w przeglądarce |
| `@live` | opcjonalnie prawdziwy AppHost, nie w domyślnym CI |
| `web-e2e` | check pull requestu |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/clients/web/e2e/public-lesson.spec.ts` | gość i HTML |
| `src/clients/web/playwright.config.ts` | projekt przeglądarki, jeden silnik w CI |
| `.github/workflows/web-e2e.yml` | job `web-e2e` |

Jeden silnik, Chromium. Reszta silników nie jest warunkiem scalenia.

## Plan

### 1. Spec

Otwiera stronę, czeka na tytuł, sprawdza stopkę, sprawdza brak `script` w artykule treści, wchodzi na `/intranet` i oczekuje adresu logowania.

### 2. Job

`pnpm exec playwright test --grep-invert @live`. Artefakt zrzutu przy porażce jest w artefaktach przebiegu, nie w git.

### 3. Ruleset

Nazwa `web-e2e` obok pozostałych checków. Na drzewie bez `e2e` byłoby `no material`. Od tego commita test jest.

### Co jest do czego

| Asercja | Po co |
| --- | --- |
| tytuł | dane doszły do DOM |
| brak `script` | sanitacja w prawdziwym dokumencie |
| stopka | wersja bundla |
| `/intranet` | strażnik nie jest tylko w teście jednostkowym |

## Testy

Nazwa w pliku: `guest sees public lesson without script`.

| Arrange | Act | Assert |
| --- | --- | --- |
| trasa API lekcji zwraca HTML ze `script` | otwarcie adresu | tytuł widoczny, `locator('article script')` ma liczbę 0, stopka pasuje do `web-v` |

```bash
cd src/clients/web && pnpm exec playwright test --grep-invert @live
```

## Checkpoint

```bash
cd src/clients/web && pnpm exec playwright test --grep-invert @live
```

Kod wyjścia zero. W git nie ma zrzutów `test-results/`. Są w `.gitignore`.

## Umiesz wyjaśnić

Dlaczego nie każda strona ma scenariusz?

Bo każdy scenariusz trzeba utrzymywać, a większość reguł jest już w Vitest i w testach API. Przeglądarka ma złapać to, czego jsdom nie wykonuje: prawdziwy HTML i prawdziwe przekierowanie adresu.

Czemu API jest podstawione?

Bo inaczej test mierzy Kafkę i seed danych. To jest test innego numeru. Tu przeglądarka dostaje kontraktowy JSON i ma go narysować bezpiecznie.

Dlaczego jeden silnik?

Bo różnice silników nie są ryzykiem tego produktu na tym etapie. Chromium w CI wystarcza. Drugi silnik można dodać, gdy pojawi się konkretny błąd.

Co z testem `@live`?

Jest dla człowieka przed wydaniem webu. Nie blokuje pull requestu, bo wymaga składu. Nie jest pustą obietnicą w domyślnym grep.

Dlaczego zrzut nie wchodzi do git?

Bo jest wynikiem przebiegu, jak log. W git zostałby nieaktualny i duży.

## Zasady

- Domyślny CI nie używa `@live`.
- Asercja dotyczy DOM, nie tylko statusu.
- `test-results` jest ignorowany.
- Check nazywa się `web-e2e` i jest w rulesetach.
- Scenariusz nie loguje hasła.
- Nie dodajemy drugiego silnika jako warunku scalenia.

## Poza zakresem

- Telefon nie jest w Playwright. Ma własne testy w numerach Expo.
- Percentyl HTTP jest w teście wydajności, nie tu.
- Test logowania właściciela na żywym Identity jest `@live`.

## Źródła

- [Playwright](https://playwright.dev/docs/intro). Wiążące są asercje lokatorów i `page.route`. Dotyczy ścieżki gościa. Ze źródła nie bierzemy trzech przeglądarek jako bramki.
- [Playwright test annotations](https://playwright.dev/docs/test-annotations). Wiążące jest pomijanie `@live` w grep. Dotyczy CI. Ze źródła nie bierzemy `test.skip` bez napisu, dlaczego.
- Numer `TrustedHtml`. Wiążący jest brak skryptu w DOM. Dotyczy asercji. Ze źródła nie bierzemy wyłączenia DOMPurify w tym teście.

## Gotowe gdy

- `playwright test` bez `@live` przechodzi.
- W artykule lekcji nie ma elementu `script`.
- Wejście na `/intranet` bez sesji kończy się na logowaniu.
- `web-e2e` jest w rulesetach.
- Zrzuty nie są w git.
