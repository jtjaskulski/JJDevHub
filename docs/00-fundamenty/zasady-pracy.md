# Zasady pracy

Ten plik mówi, jak numer wchodzi do repozytorium. Słownik jest w [slownik.md](slownik.md). Szablon sekcji jest w [szablon-numeru.md](szablon-numeru.md).

## Jeden numer

W toku jest jeden numer. Numer jest jednym pull requestem. Następny numer startuje, gdy poprzedni ma zielone checki, spełnione `## Gotowe gdy` i wiersz w [postep.md](postep.md).

Pull request, który realizuje numer, nie przekracza 800 zmienionych linii poza plikami wyłączonymi w [01-bramki/02-galezie.md](../01-bramki/02-galezie.md). Gdy się nie mieści, numer był za duży i specyfikację dzieli się, zanim powstanie reszta kodu. Nie omija się limitu, łącząc dwa numery „bo i tak są małe”.

Biblioteka współdzielona powstaje, gdy potrzebuje jej druga usługa. Do tego czasu kod mieszka w projekcie usługi, która go pierwsza używa. Wyjątkiem jest `JJDevHub.Shared.Kernel`: język `Result` i wartość `Entity` są kontraktem błędów od pierwszej usługi, bo później nie da się ich przenieść bez zmiany każdego testu.

## Gałąź i tytuł

Gałąź robocza wychodzi z `develop`. Prefiks i kebab-case są w numerze gałęzi. Tytuł pull requestu do `develop` jest squashem i staje się commitem. `feat` i `fix` mają zakres z listy komponentów, `infra` albo `shared`.

Promocja `develop` na `release` i `release` na `main` jest merge commitem. Nie ma gałęzi wyciętej z `main`.

## Checki

Check z [01-bramki](../01-bramki/01-solucja.md) jest wymagany od pierwszego pull requestu po numerze, który go dodaje. Późniejszy numer nie dodaje drugiej bramki na ten sam problem. Dokłada kod, a istniejący job przestaje kończyć się pustym sukcesem.

Job wymagany przez ruleset zawsze się uruchamia. Filtr `paths` na poziomie `on` jest zabroniony. Brak materiału jest jawnym logiem `no material` i kodem wyjścia zero. Pominięty job nie jest sukcesem.

## Sekret

W gicie nie ma wartości sekretu, tokenu, hasła ani connection stringa z hasłem. `appsettings.json` ma puste miejsce tam, gdzie klucz w ogóle istnieje. Na laptopie wartość jest w user secrets. Na maszynie, do numeru OpenBao, jest w `/etc/jjdevhub/compose.env` o uprawnieniu `600`. Po OpenBao wartość jest w OpenBao, a proces dostaje poświadczenie na godzinę.

Log nie zawiera ciała żądania, tokenu, treści notatki ani treści zapytania. Odpowiedź `/health` nie zawiera connection stringa ani ciała wyjątku.

## Test w numerze

Każdy numer, który dokłada kod, dokłada test. Test jednostkowy nie podnosi kontenera. Test integracyjny ma cechę `Category=Integration` i sam podnosi kontener. Test obciążenia ma cechę `Category=Performance` i nie leci w zwykłym pull requeście na `develop` dłużej niż jego własny job: job `performance` jest wymagany, ale kończy się sukcesem bez cechy w drzewie, a po pojawieniu się testu wykonuje go z progiem z numeru testów.

Filtr lokalny bez kontenerów i bez obciążenia:

```text
dotnet test --filter "Category!=Integration&Category!=Performance"
```

## Wersja i deploy

Tag komponentu powstaje na `main`. Obraz na maszynę wchodzi tylko z tagu. Rollback jest tym samym jobem z poprzednim tagiem tego samego komponentu. Nie ma tagu `:latest` w compose ani w manifeście klastra.

Każda faza, która produkuje proces, kończy się wdrożeniem tego procesu na maszynę. Faza bramek kończy się wymaganymi checkami, bo jeszcze nie ma procesu.

## Język plików

Pliki specyfikacji są po polsku. Nazwy typów, topiców, checków i komend zostają w brzmieniu, w jakim występują w kodzie. Termin z `## Pojęcia` nie dostaje polskiego synonimu w następnym zdaniu.

## Zamknięcie numeru

Przed wpisem do `postep.md`:

- Nazwy są takie jak w słowniku.
- Nowa baza jest w tabeli baz albo numer jej nie dodaje.
- Nowy pakiet ma licencję z [technologie.md](technologie.md) i wiersz w tabeli pakietów tego pliku.
- Sekret, jeśli numer go potrzebuje, nie leży w pliku gita.
- Zdarzenie integracyjne ma nazwę `segment.fakt.vN`.
- Usługa nie dostaje connection stringa do bazy innej usługi.
- `## Umiesz wyjaśnić` da się powiedzieć bez czytania numeru z ekranu.
