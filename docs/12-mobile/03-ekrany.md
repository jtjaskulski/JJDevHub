# 03. Ekrany telefonu

Poprzedni numer to [logowanie](02-logowanie.md). Następny to [powiadomienia](04-powiadomienia.md).

Ten numer dodaje ekrany ze słownika: dom, CV, technologia, kursy, kurs, lekcja, notatki, notatka, kontakt studenta, logowanie, rejestracja, O aplikacji i dwie skrzynki właściciela. Nie ma edytora CV ani notatki. O aplikacji pokazuje `mobile-vX.Y.Z` i sześć wierszy z `GET /versions`.

## Po co

Szkielet z jednym napisem nie jest aplikacją. Lista ekranów ze słownika zamyka zakres telefonu: to samo do czytania co strona, bez intranetowego edytora. Ekran O aplikacji zamyka wersję tak, jak stopka zamyka ją w webie.

## Dlaczego ta technologia

### Te same DTO

Ekrany używają `api-client` z workspace. Nie ma drugiego fetcha. Problem, który zamyka, to inny kształt lekcji na telefonie. Odpada kopia klienta. Koszt: telefon ciągnie typy webu. To jest cel.

### HTML

Telefon nie wstawia HTML tak jak przeglądarka, jeśli komponent natywny nie ma DOM. Treść lekcji: albo widok webowy z tym samym `TrustedHtml` i DOMPurify, albo tekst bez tagów. Widok webowy z sanitacją jest bliższy stronie. Wybór: `react-native-webview` tylko jeśli licencja jest MIT albo Apache-2.0 i sanitacja idzie przed `html`. Jeśli to ciągnie za dużo, pokazujemy tekst po zdjęciu tagów w funkcji czystej, testowanej, a nie surowy HTML. Ten numer wybiera funkcję `htmlToPlainText` po DOMPurify w środowisku, które go ma, albo po wcześniejszym polu, jeśli API kiedyś da tekst. Dziś API daje `html`. Funkcja usuwa tagi po sanitacji i zostawia treść. Nie renderuje `script`. Test na `<script>` daje brak tego napisu w wyniku albo sam tekst bez tagu. Nie używamy webview w tym numerze, żeby nie mieć drugiej przeglądarki bez testu Playwright.

### Skrzynki

`StudentInboxScreen` i `RecruiterInboxScreen` są dostępne tylko dla `owner`. Student ich nie widzi w nawigacji i głęboki link go nie wpuszcza. Odświeżanie listy przy wejściu na ekran. SSE na telefonie nie jest wymagane, bo push budzi aplikację w następnym numerze. Wolno użyć tego samego streamu, jeśli klient HTTP umie długie połączenie bez dokładania biblioteki spoza licencji. Ten numer nie wymaga SSE. Wchodzi na ekran i robi `GET`.

### O aplikacji

Sześć wierszy, kolejność ze słownika, pusty napis to `version.missing`. Wersja aplikacji z `version.ts`.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| ekran | komponent ze słownika, nie adres URL |
| `htmlToPlainText` | treść bez tagów |
| głęboki link | nie omija strażnika roli |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/clients/mobile/src/screens/` | ekrany ze słownika |
| `src/clients/mobile/src/htmlToPlainText.ts` | cięcie tagów |
| `src/clients/mobile/src/OwnerGate.tsx` | skrzynki |
| `src/clients/mobile/src/screens/AboutScreen.tsx` | wersja |

Nawigacja: stos. Nazwy ekranów zgodne ze słownikiem: `HomeScreen`, `CvScreen`, `TechnologyScreen`, `CoursesScreen`, `CourseScreen`, `LessonScreen`, `NotesScreen`, `NoteScreen`, `StudentContactScreen`, `LoginScreen`, `RegisterScreen`, `AboutScreen`, `StudentInboxScreen`, `RecruiterInboxScreen`.

## Plan

### 1. Czytanie

Każdy ekran treści ma stan pusty, błąd i treść, te same klucze co web. Nie trzeba osobnego testu na każdy, jeśli jeden wspólny komponent stanu jest przetestowany i ekrany go używają.

### 2. Lekcja

`LessonScreen` pokazuje wynik `htmlToPlainText`. Test funkcji na `<script>alert(1)</script>` nie zawiera `alert(1)` jako wykonalnego tagu; tekst `alert(1)` może zostać po zdjęciu tagu. Asercja: wynik nie zawiera `<script`.

### 3. Strażnik

`owner` wchodzi do skrzynki. `student` dostaje odesłanie do domu.

### Co jest do czego

| Ekran | Czego nie ma |
| --- | --- |
| `LessonScreen` | edycji |
| `AboutScreen` | siódmej usługi |
| skrzynki | formularza wysyłki |

## Testy

Nazwa: `plain_text_removes_script_tag`.

| Arrange | Act | Assert |
| --- | --- | --- |
| HTML z tagiem script | `htmlToPlainText` | brak `<script` |

Nazwa: `about_lists_six_services`.

```bash
cd src/clients/mobile && pnpm exec vitest run
```

## Checkpoint

```bash
cd src/clients/mobile && pnpm exec vitest run
```

Lista ekranów w nawigacji pokrywa słownik. `grep` edytora notatki w `mobile` nic nie znajduje.

## Umiesz wyjaśnić

Dlaczego nie ma edytora?

Bo intranet zostaje na webie. Telefon jest do czytania, ukończenia lekcji, formularza studenta i skrzynek właściciela. Edytor na małym ekranie to druga implementacja `If-Match`.

Dlaczego nie webview?

Bo dokładamy silnik HTML bez testu przeglądarki. Tekst po zdjęciu tagów nie wykona skryptu. Wygląd jest uboższy. Treść zostaje.

Dlaczego sześć wierszy?

Bo `GET /versions` ma sześć pól. Telefon nie dopisuje `mobile` do tej listy. `mobile` jest nad listą, z bundla.

Czemu student nie wchodzi do skrzynki głębokim linkiem?

Bo nawigacja nie jest autoryzacją. Strażnik jest na ekranie. Samo ukrycie przycisku nie wystarcza.

Dlaczego te same klucze `empty.list`?

Bo pusty kurs i pusta notatka mają ten sam sens co na stronie. Osobny klucz tylko dla telefonu rozjedzie tłumaczenie.

## Zasady

- Nazwy ekranów są ze słownika.
- Brak edytora sekcji, notatki i kursu.
- Brak `<script` w wyniku funkcji treści.
- Skrzynka ma strażnika `owner`.
- About nie dopisuje siódmej usługi.
- Ekran nie ma własnego klienta HTTP.

## Poza zakresem

- Powiadomienia są w [04-powiadomienia.md](04-powiadomienia.md).
- Wygląd ruchu nie jest wymagany.
- Publikacja do sklepu nie jest tym numerem.

## Źródła

- Słownik, tabela ekranów telefonu. Wiążące są nazwy i brak edytora. Dotyczy nawigacji. Ze słownika nie bierzemy adresów URL webu jako tras natywnych.
- Numer klienta OpenAPI. Wiążący jest jeden klient. Dotyczy braku drugiego fetcha. Ze źródła nie bierzemy wołania portu usługi.
- [React Native text](https://reactnative.dev/docs/text). Wiążące jest to, że `Text` nie wykonuje HTML. Dotyczy wyboru funkcji zamiast webview. Ze źródła nie bierzemy wstawiania HTML do `Text`.

## Gotowe gdy

- Test tagu `script` przechodzi.
- About pokazuje wersję `mobile-v` i sześć nazw.
- Student nie zostaje na skrzynce.
- W `mobile` nie ma edytora notatki.
- Vitest przechodzi bez emulatora.
