# 06. SonarQube

Poprzedni numer to [zależności](05-zaleznosci.md). Następny to [łańcuch dostaw](07-lancuch-dostaw.md).

Ten numer dodaje check `sonarqube`. Bramka jakości stoi na SonarQube Community Build, w kontenerze, który job podnosi sam. Wynik nie zależy od kliknięć w interfejsie. Próg dotyczy nowego kodu: zero nowych blockerów, zero nowych critical, sto procent przejrzanych hotspotów, duplikacja co najwyżej trzy procent, pokrycie co najmniej osiemdziesiąt procent, gdy są linie do pokrycia.

## Po co

CodeQL szuka wzorców bezpieczeństwa. Nie mierzy, czy nowa metoda ma test, ani czy ten sam blok wklejono trzeci raz. Bez progu pokrycia pierwszy projekt wchodzi bez testu i później próg jest „na potem”. Na potem nie następuje.

Serwer klikany ręcznie rozjeżdża się między przebiegami: ktoś zmieni próg w interfejsie i git o tym nie wie. Dlatego bramka jest plikiem w repozytorium, a kontener jest świeży.

## Dlaczego ta technologia

### Community Build w jobie

Obraz `sonarqube:community`. Licencja serwera jest LGPL i serwer nie jest linkowany do aplikacji. Problem, który zamyka, to brak bramki jakości na pustym koncie chmurowym. Odpada hostowany serwer jako jedyne źródło werdyktu, bo jego stan nie jest w git. Koszt: job jest cięższy. Czeka na health kontenera, ładuje bramkę z pliku i dopiero skanuje.

Na maszynie, od [numeru maszyny](../02-szkielet-i-deploy/04-maszyna.md), może stać ten sam obraz do ręcznego obejrzenia. Werdykt pull requestu i tak liczy kontener z joba. Adres na maszynie nie luzuje progu.

### Próg na nowym kodzie

Nowy kod to diff pull requestu. Problem, który zamyka, to dziedziczenie długu: stary plik bez testu nie blokuje poprawki literówki, ale nowa linia bez pokrycia blokuje. Odpada próg na całym repozytorium od pierwszego dnia, który zmusza do pisania testów do wygenerowanego kodu. Koszt: trzeba umieć pokazać skanerowi, co jest nowe. Robi to parametr pull requestu w wywołaniu skanera, nie ręczna lista plików.

Pokrycie liczy się, gdy są linie do pokrycia. Sam plik workflow albo sam markdown nie ma linii C# i nie oblewa progu osiemdziesięciu procent.

### Token

Token serwera powstaje w jobie, na świeżym kontenerze, i ginie z kontenerem. Nie ma sekretu `SONAR_TOKEN` w ustawieniach repozytorium dla tego przebiegu. Log nie drukuje tokenu. `set +x` wokół kroku, który go wystawia skanerowi.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| quality gate | zestaw warunków, po których skan jest czerwony albo zielony |
| nowy kod | linie dodane w pull requeście |
| hotspot | miejsce, które wymaga decyzji, nie jest od razu błędem |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `.github/workflows/sonarqube.yml` | job `sonarqube` |
| `deploy/sonar/quality-gate.json` | progi, ładowane do kontenera |
| `sonar-project.properties` | klucz projektu `jjdevhub`, katalogi źródeł i testów |

## Plan

### 1. Bramka w pliku

`deploy/sonar/quality-gate.json` opisuje warunki na nowym kodzie:

- nowe blockery: `0`
- nowe critical: `0`
- przejrzane hotspoty nowego kodu: `100%`
- gęstość duplikacji nowego kodu: co najwyżej `3`
- pokrycie nowego kodu: co najmniej `80`, warunek pomijany, gdy nie ma linii do pokrycia

Job tworzy projekt i bramkę z tego pliku przy każdym przebiegu. Zmiana progu jest zmianą pliku.

### 2. Job

`runs-on: ubuntu-latest`. Usługa kontenerowa albo krok `docker run` z obrazem `sonarqube:community`. Job czeka, aż `api/system/status` zwróci `UP`. Potem zakłada użytkownika jednorazowego, wgrywa bramkę, uruchamia `dotnet sonarscanner` albo skaner CLI, gdy nie ma jeszcze `.csproj`.

Gdy nie ma `*.cs`, `*.ts` ani `*.tsx`, job nie podnosi kontenera. Loguje `no material` i zwraca kod `0`. Podnoszenie JVM bez źródeł trwa kilka minut i nic nie mówi.

Gdy źródła są, skaner dostaje klucz `jjdevhub` i parametry pull requestu (`sonar.pullrequest.key`, gałąź, baza). Padnięcie bramki to kod `1`.

### 3. Pokrycie

Projekt testowy zbiera pokrycie w formacie, który skaner czyta (`coverage.cobertura.xml` albo raport, który w dniu numeru przyjmuje skaner dla xunit.v3). Job `unit-tests` już produkuje test. Ten job uruchamia testy jeszcze raz z pokryciem albo pobiera artefakt z `unit-tests`. Pobieranie artefaktu jest lepsze, żeby nie mieć dwóch wyników testu. `unit-tests` wgrywa artefakt pokrycia, `sonarqube` go czyta. Gdy artefakt nie powstał, bo nie było testów, a źródła są, pokrycie jest puste i próg pada. To jest cel: źródło bez testu nie przechodzi.

Wyjątek: sam katalog `tests/` albo sam workflow, bez produkcji, zostaje przy `no material` albo przy braku linii do pokrycia. Skrypt rozstrzyga to jawnie w logu.

### 4. Ruleset

Nazwa `sonarqube` wchodzi do trzech rulesetów.

### Co jest do czego

| Plik | Kto go woła |
| --- | --- |
| `quality-gate.json` | job, przy starcie kontenera |
| `sonar-project.properties` | skaner |
| artefakt pokrycia | skaner, z joba `unit-tests` |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| drzewo bez źródeł | warunek joba | `no material`, kod `0` |
| plik bramki | parser JSON | pięć warunków z tego numeru |

Pełny przebieg kontenera jest checkiem na pull requeście, nie suchym testem lokalnym. Lokalnie wystarcza `docker pull` obrazu raz, przy pierwszym źródle, i jeden przebieg ręczny opisany w checkpointcie.

## Checkpoint

```bash
test -f .github/workflows/sonarqube.yml
test -f deploy/sonar/quality-gate.json
python3 -c "import json; g=json.load(open('deploy/sonar/quality-gate.json')); assert g['new_blocker']==0 and g['new_coverage']==80"
actionlint .github/workflows/sonarqube.yml
```

Na drzewie bez źródeł workflow, odpalony lokalnie tym samym warunkiem co job, wypisuje `no material`.

## Umiesz wyjaśnić

Dlaczego kontener jest świeży, a nie jeden serwer na lata?

Bo próg ma być w git. Serwer, w którym ktoś kliknął inny próg, okłamałby check. Świeży kontener ładuje plik przy każdym przebiegu.

Czemu pokrycie jest na nowym kodzie, nie na całym?

Żeby stary dług nie blokował każdej poprawki i żeby nowa linia nie wchodziła bez testu. Osiemdziesiąt procent dotyczy linii, które ten pull request dodał.

Co z markdownem tej specyfikacji?

Nie ma linii do pokrycia. Próg pokrycia się nie włącza. Blockery C# też nie mają skąd się wziąć. Job i tak raportuje status.

Gdzie jest token?

Powstaje w jobie i znika z kontenerem. Nie ma go w ustawieniach repozytorium i nie ma go w logu.

Dlaczego to nie zastępuje CodeQL?

Inny rodzaj znaleziska. Skan bezpieczeństwa kodu zostaje. Tu jest pokrycie, duplikacja, blocker i hotspot.

## Zasady

- Próg nie jest luzowany w interfejsie.
- Obraz to `sonarqube:community`, bez edycji płatnej.
- Token nie wchodzi do logu ani do git.
- Job pull requestu nie używa runnera `jjdevhub`.
- Wyłączenie reguły w kodzie komentarzem nie zastępuje zmiany `quality-gate.json`.

## Poza zakresem

- Miejsce kontenera na maszynie do oglądania jest w [04-maszyna.md](../02-szkielet-i-deploy/04-maszyna.md). Werdykt zostaje w tym jobie.
- Testy, które dają pokrycie, dochodzą z numerami kodu. Piramida jest w [10-testy](../10-testy/01-integracyjne.md).

## Źródła

- [Quality gates](https://docs.sonarsource.com/sonarqube-community-build/quality-standards-administration/managing-quality-gates/). Wiążące są warunki na nowym kodzie. Dotyczy pięciu progów z tego numeru. Ze źródła nie bierzemy bramki wyklikanej i niezacommitowanej.
- [SonarScanner for .NET](https://docs.sonarsource.com/sonarqube-community-build/analyzing-source-code/scanners/dotnet/sonarqube-scanner-for-dotnet). Wiążące jest rozpoczęcie skanu przed buildem i zakończenie po teście, oraz parametry pull requestu. Dotyczy joba, gdy są projekty. Ze źródła nie bierzemy tokenu trzymanego w sekrecie repozytorium, skoro kontener jest świeży i token może powstać w przebiegu.
- [Analysis scope](https://docs.sonarsource.com/sonarqube-community-build/project-administration/setting-analysis-scope/excluding-files-based-on-path-patterns/). Wiążące jest wyłączenie `**/Migrations/**` i wygenerowanego klienta. Dotyczy tego, żeby generator nie zjadał progu duplikacji. Ze źródła nie bierzemy wyłączenia `src/services`.

## Gotowe gdy

- `deploy/sonar/quality-gate.json` ma pięć progów z tego numeru.
- Na drzewie bez źródeł job loguje `no material` i zwraca kod zero.
- Trzy rulesety wymieniają `sonarqube`.
- W workflow nie ma wartości tokenu.
- Katalogi migracji i wygenerowanego klienta są poza liczeniem duplikacji.
