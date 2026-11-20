# 01. Motyw

Poprzedni numer to [wydajność](../10-testy/03-wydajnosciowe.md). Następny to [szkielet webu](02-szkielet.md).

Ten numer stawia liczby wyglądu w jednym pliku. Web i telefon biorą je stąd. W komponencie nie ma heksa. Nie ma drugiej palety.

## Po co

Kolor wpisany w przycisku i drugi raz w stopce rozjadą się przy pierwszej zmianie. Jeden obiekt `tokens` zamyka paletę, odstępy, promień, skalę pisma i czasy. Telefon nie dostaje własnych heksów „bo natywnie wygląda inaczej”. Ma wyglądać jak strona.

## Dlaczego ta technologia

### Jeden plik TypeScript

`src/clients/shared/theme/tokens.ts` eksportuje obiekt. Problem, który zamyka, to zmienne CSS w jednym kliencie i stałe w drugim. Odpada plik `scss` i odpada Tamagui. Koszt: Tailwind i NativeWind tylko mapują te liczby na klasy. Mapowanie jest w numerach szkieletu, nie tu. Tu są liczby.

### Paleta

Tło `#f3efe6`, atrament `#1a1814`, akcent `#9a3412`. Krój Inter. Problem, który zamyka, to drugi zestaw „ciemny” wpisany w komponencie obok jasnego. Odpada ciemny motyw jako osobne heksy w tym numerze. Koszt: jeden wygląd. Jeśli ciemny motyw kiedykolwiek dojdzie, będzie drugim obiektem w tym pliku, nie heksem w przycisku. Na razie go nie ma.

### Czas

`tokens.duration` trzyma milisekundy ruchu. Problem, który zamyka, to `300` w jednym komponencie i `250` w drugim. Odpada magiczna liczba w `transition`. Koszt: ruch webu i telefonu, gdy dojdzie, czyta tę samą liczbę. Ten numer nie dodaje biblioteki ruchu.

### Test liczb

Test bez przeglądarki czyta obiekt i sprawdza trzy heksy oraz to, że w pliku nie ma drugiego tła. Test jest w `src/clients/shared/theme/tokens.test.ts` i leci vitestem, gdy vitest dojdzie w szkielecie. Na tym numerze test może być tym samym plikiem uruchamianym przez `pnpm exec vitest`, jeśli szkielet pakietu już jest. Żeby nie wyprzedzać Vite, ten numer dodaje minimalny `package.json` workspace tylko dla shared, albo czeka z runnerem do następnego numeru i tu zostawia test jako plik, który następny numer wpina. Decyzja: workspace `src/clients/pnpm-workspace.yaml` powstaje tu, vitest tu, bo inaczej liczby nie mają checka. Web jako aplikacja jest w następnym numerze.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| `tokens` | jedyny obiekt liczb wyglądu |
| heks | zapis koloru, tylko w `tokens.ts` |
| `duration` | czas ruchu w milisekundach |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/clients/pnpm-workspace.yaml` | `shared/theme`, później web i mobile |
| `src/clients/shared/theme/tokens.ts` | paleta, odstępy, pismo, czasy |
| `src/clients/shared/theme/tokens.test.ts` | trzy heksy |
| `src/clients/package.json` | prywatny korzeń workspace, menedżer pnpm |

Lockfile `pnpm-lock.yaml` jest w `src/clients` i jest commitowany. Dependabot npm dla tego katalogu wchodzi teraz, jeśli numer zależności zostawił komentarz zamiast wpisu.

## Plan

### 1. Obiekt

Pola: `color.canvas`, `color.ink`, `color.accent`, `space`, `radius`, `font.family` równe `Inter`, `duration.fast` i `duration.base`. Wartości odstępów są liczbami w pikselach, nie losowym zestawem.

### 2. Test

Asercja heksów ze słownika. Asercja, że `JSON.stringify` obiektu zawiera `#f3efe6` dokładnie raz.

### 3. Zakaz

W tym numerze nie ma pliku `*.scss` i nie ma komponentu React. `grep` heksa po `src/clients` poza `tokens.ts` jest pusty.

### Co jest do czego

| Pole | Kto je później czyta |
| --- | --- |
| kolor | klasy `bg-canvas`, `text-ink`, `bg-accent` |
| `duration` | ruch, gdy dojdzie |
| krój | `@fontsource-variable/inter` w numerze szkieletu |

## Testy

Nazwa: `tokens_match_the_palette`.

| Arrange | Act | Assert |
| --- | --- | --- |
| import `tokens` | odczyt pól | tło, atrament i akcent równe słownikowi |

```bash
cd src/clients && pnpm exec vitest run shared/theme/tokens.test.ts
```

## Checkpoint

```bash
cd src/clients && pnpm exec vitest run
```

Komenda kończy się kodem zero. `find src/clients -name '*.scss'` nic nie zwraca.

## Umiesz wyjaśnić

Dlaczego heks nie stoi w komponencie?

Bo drugi heks tego samego koloru nie zaktualizuje się razem z pierwszym. Test łapie paletę w jednym pliku. Komponent użyje klasy, która wskazuje na tę liczbę.

Dlaczego nie ma ciemnego motywu?

Bo nie ma drugiej palety w słowniku. Dodanie jej w komponencie obok jasnej zrobi dwa produkty. Jak dojdzie, dojdzie jako drugi obiekt w `tokens.ts`.

Po co czasy w motywie, skoro nie ma jeszcze ruchu?

Żeby pierwszy komponent ruchu nie wpisał `300` z głowy. Liczba jest wcześniej niż biblioteka.

Dlaczego pnpm, a nie drugi lockfile npm?

Bo web i telefon mają dzielić `shared`. Dwa menedżery to dwa drzewa zależności. Workspace pnpm jest jednym drzewem.

Co wolno zmienić bez zmiany testu?

Nazwy klas Tailwinda, których jeszcze nie ma. Nie wolno zmienić heksa bez zmiany testu i słownika. Słownik i test idą w tym samym scaleniu.

## Zasady

- Heks palety jest tylko w `tokens.ts`.
- Brak `*.scss`.
- Brak drugiej palety.
- Krój nazywa się Inter.
- Lockfile pnpm jest w git.
- Test motywu nie odpala przeglądarki.

## Poza zakresem

- HTML i trasy są w [02-szkielet.md](02-szkielet.md).
- Tailwind jest w tym samym numerze szkieletu, nie tutaj, żeby motyw nie zależał od jednego klienta.
- NativeWind jest w [01-szkielet-expo.md](../12-mobile/01-szkielet-expo.md).

## Źródła

- [pnpm workspace](https://pnpm.io/workspaces). Wiążące jest jedno drzewo pakietów. Dotyczy `src/clients`. Ze źródła nie bierzemy osobnego `package-lock.json` w webie.
- Słownik, sekcja wyglądu. Wiążące są trzy heksy i Inter. Dotyczy treści `tokens.ts`. Ze słownika nie bierzemy klasy Tailwinda jako miejsca, w którym stoi heks.

## Gotowe gdy

- Vitest palety przechodzi.
- Poza `tokens.ts` nie ma heksa palety w `src/clients`.
- Nie ma pliku scss.
- Workspace pnpm istnieje i ma lockfile.
- Obiekt ma `duration`.
