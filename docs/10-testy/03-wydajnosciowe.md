# 03. Test wydajności

Poprzedni numer to [kontrakt](02-kontraktowe.md). Następny folder zaczyna się od [motywu](../11-web/01-motyw.md).

Ten numer mierzy `GET` publicznej lekcji i `GET /versions`. Ustala próg czasu odpowiedzi. Narzędzie jest w repozytorium, na `HttpClient`, bez paczki o licencji innej niż dozwolona. Job `performance` przestaje być pusty.

## Po co

Test poprawności nie mówi, że lista kursów po zmianie SQL trwa sekundę. Próg zamyka regresję, którą widać liczbą, nie wrażeniem. Osobny job zamyka to, że ten test nie spowalnia każdego `unit-tests` i nie udaje testu jednostkowego.

## Dlaczego ta technologia

### Własny harness zamiast NBomber i k6

NBomber od linii 5 nie jest wolny produkcyjnie, a linii 4 nie przypinamy, żeby ominąć licencję. k6 jest na AGPL. Problem, który zamyka, to brak pomiaru w ogóle. Odpada „odpalę raz ręcznie”. Harness w `tests/JJDevHub.Performance.Tests` robi stałą liczbę żądań równolegle, liczy percentyl 95 i kończy się kodem `1`, gdy próg pęknie. Koszt: nie ma rozkładu tak ładnego jak w osobnym narzędziu. Jest liczba w logu i kod wyjścia, którego pilnuje check. To wystarcza jako bramka.

### Co mierzymy

Dwa cele. `GET /health/live` bramy: próg p95 poniżej 50 ms przy 200 żądaniach i współbieżności 20, na hoście testowym bez tunelu. `GET` publicznej lekcji z projekcji: p95 poniżej 200 ms przy 100 żądaniach i współbieżności 10, na kontenerze Postgresa. Problem, który zamyka, to pomiar przez tunel i Cloudflare, który mierzy sieć domu. Odpada próg na pełnej ścieżce z internetu jako warunek scalenia. Koszt: test nie mówi nic o tunelu. Tunel sprawdzamy ręcznie przy deployu, nie percentylem w CI.

### Dane

Lekcja jest wstawiona raz, przed pomiarem. Pomiar nie miesza się z migracją. Problem, który zamyka, to p95 obejmujące pierwszy cold start, który mylimy z regresją SQL. Pierwsze 10 żądań jest rozgrzewką i nie wchodzi do percentyla. Koszt: test jest dłuższy o rozgrzewkę. Jest jawna w kodzie.

### Zegar

`Stopwatch` na każde żądanie. Nie ma `DateTime.Now` do różnicy. Percentyl liczony z posortowanej listy. Przy parzystej liczbie bierzemy definicję zapisaną w teście i nie zmieniamy jej między przebiegami. Problem, który zamyka, to próg, który zależy od sposobu liczenia. Odpada średnia jako jedyny warunek: średnia ukrywa ogon.

### Obciążenie CI

Job `performance` jest wymagany. Timeout joba jest 15 minut. Test, który nie zdąży, pada. Nie ma `continue-on-error`. Problem, który zamyka, to pomiar, który jest „informacyjny” i nigdy nie blokuje. Odpada uruchamianie tego na każdym commicie dokumentacji w sposób, który i tak trwa, bo job i tak startuje. Gdy nie ma projektu testów wydajności, `no material`. Gdy jest, mierzy. Ten numer projekt dodaje, więc mierzy.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| p95 | czas, poniżej którego zmieściło się 95 procent żądań po rozgrzewce |
| rozgrzewka | żądania, które nie wchodzą do progu |
| harness | kod testu, który sam wysyła żądania |
| próg | liczba w teście, której przekroczenie jest czerwonym checkiem |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `tests/JJDevHub.Performance.Tests/GatewayLiveLatencyTests.cs` | próg bramy |
| `tests/JJDevHub.Performance.Tests/LessonReadLatencyTests.cs` | próg odczytu |
| `tests/JJDevHub.Performance.Tests/Percentile.cs` | jedna funkcja percentyla |

Oba fakty mają `Category=Performance`.

## Plan

### 1. Percentyl

Funkcja czysta, test jednostkowy bez cechy wydajności: dla znanej listy zwraca znaną wartość. Ten fakt leci w `unit-tests`, bo nie mierzy HTTP. Jest w tym samym projekcie i nie ma cechy `Performance`. Filtr joba wydajności bierze tylko cechę. Filtr jednostkowy bierze resztę. Projekt jest referencjonowany przez oba? Job `unit-tests` odpala `dotnet test` na solucji z filtrem kategorii, więc fakt percentyla w tym projekcie wejdzie do jednostkowych, a fakty HTTP nie. Dobrze.

### 2. Brama

Host w pamięci albo `WebApplicationFactory` bramy. 10 rozgrzewki, 200 pomiaru, współbieżność 20, próg 50 ms. Na wolnym runnerze 50 ms bywa ciasne. Jeśli trzy przebiegi na `ubuntu-latest` nie mieszczą się w 50, próg w tym numerze ustawiamy na wartość, która mieści się z zapasem, i zapisujemy ją w teście oraz tutaj. Startowa liczba to 100 ms p95, nie 50, właśnie dlatego, że runner nie jest maszyną deweloperską. 100 ms na `/health/live` w procesie testowym jest nadal regresją, gdy ktoś zrobi z health wyjście do bazy. Live nie dotyka bazy. Jeśli dotknie, czas skoczy i test padnie.

### 3. Lekcja

Kontener Postgresa, jeden dokument, Dapper, 10 rozgrzewki, 100 pomiaru, p95 300 ms. Próg jest luźniejszy, bo jest dysk kontenera. Nie mierzymy Kafki w tym teście. Dokument jest wstawiony SQL-em, bez czekania na projektor. To jest pomiar odczytu, nie pomiar szyny.

### Co jest do czego

| Test | Próg startowy |
| --- | --- |
| live bramy | p95 < 100 ms |
| odczyt lekcji | p95 < 300 ms |

Zmiana progu w górę wymaga zdania w pull requeście. Nie podnosi się go w ciszy.

## Testy

Nazwa: `Percentile95_of_known_sample_is_stable`.

| Arrange | Act | Assert |
| --- | --- | --- |
| lista 100 liczb 1..100 | funkcja p95 | wartość ustalona w teście i zapisana w nazwie asercji |

Nazwa wydajności: `Gateway_live_p95_is_under_threshold`.

```bash
dotnet test tests/JJDevHub.Performance.Tests --nologo --filter "Category=Performance"
```

## Checkpoint

```bash
dotnet test tests/JJDevHub.Performance.Tests --nologo --filter "Category!=Integration"
dotnet test tests/JJDevHub.Performance.Tests --nologo --filter "Category=Performance"
```

Pierwsze polecenie uruchamia tylko percentyl i przechodzi szybko. Drugie mierzy HTTP. Oba kończą się kodem zero na spokojnej maszynie. W logu jest wypisane p95, nie tylko „ok”.

## Umiesz wyjaśnić

Dlaczego nie NBomber?

Bo aktualna linia nie jest wolna produkcyjnie, a starszej nie przypinamy, żeby licencję obejść. Harness na `HttpClient` daje próg i kod wyjścia. Tego check potrzebuje.

Dlaczego nie średnia?

Bo dziewięć szybkich żądań i jedno dziesięciosekundowe daje znośną średnią. p95 tego nie ukryje. Przy stu żądaniach i tak patrzymy na ogon, nie na jeden błąd sieci: błąd HTTP jest osobną asercją „zero odpowiedzi innych niż 200”.

Czemu rozgrzewka nie wchodzi do progu?

Bo pierwsze żądanie dogrzewa kod i pulę połączeń. Mieszanie go z pomiarem robi test, który zależy od przypadkowego startu. Rozgrzewka jest jawna.

Dlaczego lekcja nie idzie przez Kafkę?

Bo wtedy mierzymy projektor i brokera, a nazwa testu mówi o odczycie. Szyna ma własny test poprawności. Tu jest SQL odczytu.

Co ten test mówi o produkcji?

Że nie zepsuliśmy kształtu odczytu i health w kodzie. Nie mówi, ile milisekund zajmie tunel z telefonu. Tego nie udajemy.

## Zasady

- Cecha `Performance` nie leci w `unit-tests`.
- Błąd HTTP w pomiarze jest porażką, nie punktem czasowym.
- Próg jest stałą w teście, podniesienie go jest widoczne w diffie.
- Nie ma zależności od NBomber i od k6.
- Pomiar nie idzie przez publiczny hostname.
- Log wypisuje p95.

## Poza zakresem

- Obciążenie klastra i HPA są w [07-skalowanie.md](../13-kubernetes/07-skalowanie.md). Ten test nie podnosi drugiego poda.
- Playwright mierzy ścieżkę człowieka, nie percentyl, w [07-playwright.md](../11-web/07-playwright.md).

## Źródła

- Numer technologii, tabela odrzuconych. Wiążące są licencje NBomber 5 i k6. Dotyczy własnego harnessu. Ze źródła nie bierzemy przypięcia NBomber 4.
- [Stopwatch](https://learn.microsoft.com/dotnet/api/system.diagnostics.stopwatch). Wiążące jest mierzenie odcinka czasu, nie odejmowanie `DateTime`. Dotyczy próbki. Ze źródła nie bierzemy jednego stopera na całą pętlę jako zamiennika percentyla.
- Numer health. Wiążące jest to, że live nie dotyka bazy. Dotyczy sensu progu 100 ms. Ze źródła nie bierzemy pomiaru ready z Postgresem jako „live”.

## Gotowe gdy

- Percentyl ma test na znanej liście.
- Job `performance` wykonuje oba pomiary i wypisuje p95.
- Przekroczenie progu kończy się kodem innym niż zero.
- W projekcie nie ma paczki NBomber ani k6.
- Żądanie inne niż `200` nie wchodzi do próbek jako sukces.
