# 09. Health i odporność

Poprzedni numer to [ślady](08-slady-i-metryki.md). Następny folder zaczyna się od [kont](../05-tozsamosc/01-konta-i-zaproszenia.md).

Ten numer rozdziela probe live i ready, dokłada sprawdzenie Postgresa i Kafki oraz circuit breaker na produce w relayu. Pad brokera nie restartuje procesu w kółko. Pad bazy zapisuje ready na czerwono i zostawia live zielone, żeby proces dało się odróżnić od martwego.

## Po co

Jeden endpoint `/health`, który sprawdza wszystko, przy leżącej Kafce restartuje kontener, który i tak nie naprawi Kafki restartem. Pętla restartów zamyka też HTTP, które mogłoby zwrócić `503` z sensem. Dwa probe zamykają tę pętlę. Live mówi: proces żyje. Ready mówi: wolno mu dać ruch albo wolno mu publikować.

Produce bez limitu czasu wisi na martwym brokerze i trzyma blokadę `FOR UPDATE`. Timeout i circuit breaker zamykają wiszenie.

## Dlaczego ta technologia

### Dwa endpointy, które już są

ServiceDefaults mapuje `/health/live` i `/health/ready`. Ten numer dodaje checki z tagami. `self` zostaje na `live`. Npgsql i Kafka dostają tag `ready` i nie dostają tagu `live`. Problem, który zamyka, to restart kontenera przy awarii zależności. Odpada jeden check „all”. Koszt: trzeba pamiętać, żeby nowy check bazy nie dostał tagu `live`.

Na compose nie ma kubeleta. Mimo to endpointy są, bo faza klastra ich użyje, a na compose deploy i tak woła `live` po starcie. Ready jest dla człowieka i dla przyszłego probe. Relay nie wystawia publicznie niczego poza tymi dwoma ścieżkami na porcie wewnętrznym. Nie idzie przez bramę.

### `AspNetCore.HealthChecks.NpgSql` i Kafka

Check bazy używa connection stringa zapisu i `SELECT 1`. Check Kafki używa bootstrap servers. Oba mają timeout dwóch sekund. Problem, który zamyka, to własny socket w każdym projekcie. Odpada check, który robi `Migrate` albo `Produce` testowej wiadomości. Koszt: paczki health checks w wersjach z tabeli technologii.

Projektor sprawdza `learning_read` i Kafkę. API sprawdza `learning_write`. Relay sprawdza `learning_write` i Kafkę. Żaden nie sprawdza bazy sąsiada.

### Polly v8 na produce

Wokół `ProduceAsync` jest strategiczny pipeline: timeout 5 sekund i circuit breaker. Próg: 50 procent porażek w oknie 30 sekund, minimum 10 wywołań, przerwa 15 sekund. Liczby są w `Relay:Resilience` i mają test, który nie czeka 30 sekund: test używa `FakeTimeProvider`, jeśli pipeline na to pozwala, albo małego okna w teście przez opcje. Problem, który zamyka, to relay, który przy martwym brokerze robi tysiąc prób na sekundę i trzyma blokady. Odpada `catch` i natychmiastowy retry bez przerwy. Koszt: przez 15 sekund po otwarciu obwodu outbox stoi. Wiersze zostają. To jest lepsze niż burza.

Gdy obwód jest otwarty, pętla nie bierze partii. Nie bierze `FOR UPDATE`, skoro i tak nie wyśle. Sprawdzenie stanu obwodu jest przed `SELECT`.

`AddStandardResilienceHandler` zostaje na `HttpClient` w ServiceDefaults. Nie obejmuje Kafki. Kafka nie jest `HttpClient`. Osobny pipeline jest uczciwszy niż upychanie producenta w handler HTTP.

### Wyjątek produce a `attempts`

Timeout i otwarty obwód zwiększają `attempts` tak samo jak błąd sieci w numerze relayu. Nie ustawiają `published_at`.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| liveness | proces żyje, nie restartować go za karę za bazę |
| readiness | wolno dać mu pracę, która potrzebuje zależności |
| circuit breaker | po serii porażek wołanie pada od razu, bez sieci |
| timeout | granica czasu jednego produce |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Learning.Infrastructure/Health/LearningHealthChecks.cs` | rejestracja Npgsql i Kafki |
| `Learning.Relay/PublishResilience.cs` | pipeline Polly |
| `Learning.Relay/OutboxPublisher.cs` | nie bierze partii, gdy obwód otwarty |
| `Learning.Api/Program.cs`, relay, projektor | `MapDefaultEndpoints` już jest; dopinają checki |
| `tests/Learning.Relay.Tests/CircuitTests.cs` | otwarty obwód nie woła producenta |

## Plan

### 1. Tagi

Check `learning-write` tag `ready`. Check `kafka` tag `ready`. Check `self` tag `live`. `AddLearningPersistence` rejestruje check bazy. Fabryka Kafki rejestruje check brokera. Host, który nie ma Kafki, nie rejestruje checka Kafki: API na tym etapie nie produkuje samo, ale i tak nie musi być ready od brokera, żeby przyjąć `POST`. `POST` zapisze outbox bez brokera. Dlatego API nie dostaje checka Kafki. Relay dostaje. Projektor dostaje, bo bez brokera nie ma pracy, i dostaje check `learning-read`.

To jest ważny podział. API ready przy leżącej Kafce zostaje zielone. Inaczej brama przestanie wysyłać ruch, choć zapis do outboxa działa.

### 2. Pipeline

`ResiliencePipeline` z timeoutem i breaker. `OutboxPublisher` na początku obiegu pyta, czy wolno. Jeśli nie, czeka interwał i nie otwiera transakcji.

### 3. Test obwodu

Producent testowy rzuca. Po progu pipeline zwraca `BrokenCircuitException` bez wołania producenta. Asercja: licznik wołań producenta stoi w miejscu przy kolejnych próbach, dopóki czas nie minie. Czas przesuwa `FakeTimeProvider`, jeśli konstrukcja Polly 8 na to pozwala w teście. Jeśli nie pozwala bez zegara ściennego, test ustawia okno na kilkaset milisekund przez opcje i czeka tyle. Czekanie jest w teście integracyjnym albo jednostkowym z małym oknem, nie w `unit-tests` przez 30 sekund. Ten test jest jednostkowy z oknem 50 ms.

### Co jest do czego

| Check | Live | Ready |
| --- | --- | --- |
| `self` | tak | nie osobno; ready i tak wymaga, żeby proces odpowiedział |
| Postgres zapisu | nie | tak, na API i relayu |
| Postgres odczytu | nie | tak, na projektorze |
| Kafka | nie | tak, na relayu i projektorze; nie na API |

## Testy

Nazwa: `Open_circuit_does_not_call_producer`.

| Arrange | Act | Assert |
| --- | --- | --- |
| producent, który rzuca, po przekroczeniu progu | kolejne wywołanie pipeline | wyjątek obwodu, licznik wołań nie rośnie |

Nazwa: `Live_stays_ok_when_database_check_fails`. Host z checkiem bazy podstawionym porażką. `GET /health/live` zwraca `200`. `GET /health/ready` zwraca `503`.

```bash
dotnet test tests/Learning.Relay.Tests tests/Learning.Api.Tests --nologo --filter "FullyQualifiedName~Open_circuit|FullyQualifiedName~Live_stays_ok"
```

## Checkpoint

```bash
dotnet test tests/Learning.Relay.Tests --nologo --filter "FullyQualifiedName~Open_circuit"
aspire run
```

Zatrzymanie Kafki: `GET` live API zwraca `200`, ready API zwraca `200`, ready relayu zwraca `503`. `POST` lekcji w Development nadal daje `201`. Wiersz outboxa czeka. Po starcie Kafki i po przerwie obwodu wiersz schodzi.

## Umiesz wyjaśnić

Dlaczego API nie jest czerwone, gdy leży Kafka?

Bo `POST` nie potrzebuje brokera. Potrzebuje bazy zapisu. Gotowość API od Kafki zatrzymałaby publikację kursu dokładnie wtedy, gdy outbox ma ją uratować.

Dlaczego live nie sprawdza bazy?

Bo restart procesu nie podnosi bazy. Kubelet, gdy dojdzie, zabiłby kontener w pętli. Ready zdejmuje ruch. Live zostawia proces przy życiu.

Co daje circuit breaker ponad timeout?

Timeout ucina jedną próbę. Breaker ucina kolejne, zanim otworzą połączenie i zanim wezmą blokadę wierszy. Bez tego każda partia czeka pięć sekund i trzyma `FOR UPDATE`.

Dlaczego nie `AddStandardResilienceHandler` na Kafkę?

Bo to handler `HttpClient`. Producent Kafki nie jest `HttpClient`. Osobny pipeline jest wprost. HTTP między usługami, gdy dojdzie, użyje handlera ze ServiceDefaults.

Czemu projektor sprawdza odczyt, a relay zapis?

Bo każdy proces dotyka tej bazy, bez której nie ma pracy. Sprawdzanie obu baz w każdym procesie poszerza awarię: pad odczytu zdejmowałby relay, który odczytu nie używa.

## Zasady

- Check bazy i brokera nie ma tagu `live`.
- API nie sprawdza Kafki w ready.
- Otwarty obwód nie wykonuje `SELECT ... FOR UPDATE`.
- Timeout produce nie ustawia `published_at`.
- Liczby progu są w konfiguracji, nie w trzech miejscach w kodzie.
- Health nie zwraca connection stringa ani treści wyjątku.

## Poza zakresem

- Probe w manifeście klastra są w [02-manifesty.md](../13-kubernetes/02-manifesty.md). Endpointy z tego numeru są tym, co probe zawoła.
- HPA jest w [07-skalowanie.md](../13-kubernetes/07-skalowanie.md).
- Retry HTTP do sąsiedniej usługi nie występuje, bo Learning nie woła sąsiada po HTTP.

## Źródła

- [Health checks](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks). Wiążące są tagi i dwa endpointy. Dotyczy podziału live i ready. Ze źródła nie bierzemy jednego `/health` bez tagów jako probe restartu.
- [Polly v8 circuit breaker](https://www.pollydocs.org/strategies/circuit-breaker.html). Wiążące są próg, okno i przerwa. Dotyczy produce. Ze źródła nie bierzemy retry bez limitu wewnątrz otwartego obwodu.
- [Kubernetes probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/). Wiążące jest to, że liveness zabija, a readiness zdejmuje ruch. Dotyczy decyzji, czego nie dawać na live. Ze źródła nie bierzemy probe, które woła endpoint zależny od bazy, jako liveness. Klaster dojdzie później, mylny probe byłby już w kodzie.

## Gotowe gdy

- Test obwodu pokazuje brak wołania producenta po otwarciu.
- Live API jest `200` przy czerwonym checku bazy w teście.
- Ready API przy leżącej Kafce na działającym środowisku zostaje `200`, ready relayu jest `503`.
- `POST` przy leżącej Kafce zapisuje outbox.
- Odpowiedź health nie zawiera hasła.
