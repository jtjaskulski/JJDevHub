# 01. Testy integracyjne

Poprzedni numer to [asystent](../09-rozszerzenia-api/02-asystent.md). Następny to [kontrakt](02-kontraktowe.md).

Ten numer spina test, który podnosi prawdziwy Postgres i prawdziwą Kafkę oraz, osobno, test, który podnosi AppHost. Nie zastępuje testów jednostkowych z wcześniejszych numerów. Domka dziury: „u mnie handler przechodzi w pamięci, a migracja nie wstaje”.

## Po co

Testy przy numerach sprawdzają jedną regułę. Z czasem przybywa ich dużo i nikt nie sprawdza, czy brama, Learning i broker w ogóle wstają razem. Ten numer dodaje jeden scenariusz od `POST` do `GET` przez outbox i projektor oraz jeden scenariusz AppHosta. Job `integration-tests` już istnieje. Tu przestaje być zbiorem przypadków i dostaje ścieżkę produktu.

## Dlaczego ta technologia

### Testcontainers

Postgres `postgres:18` i Kafka w trybie KRaft, obraz zgodny z produktem. Problem, który zamyka, to test na bazie dewelopera, która ma stare migracje. Odpada wspólna baza na runnerze współdzielona między przebiegami. Koszt: czas. Dlatego te fakty mają `Category=Integration` i nie lecą w `unit-tests`.

### `Aspire.Hosting.Testing`

`DistributedApplicationTestingBuilder` podnosi AppHost na czas testu. Problem, który zamyka, to test, który sam składa kontenery inaczej niż `aspire run` i przechodzi, gdy AppHost jest zepsuty. Odpada wyłącznie ręczne `aspire run` jako dowód. Koszt: cięższy test. Jest jeden, nie dwadzieścia. Nazywa się `Apphost_starts_gateway_and_learning`. Sprawdza health bramy i Learning, nie cały katalog.

### Czas

`FakeTimeProvider` zostaje w testach jednostkowych sagi i tokenu. W teście integracyjnym zegar jest prawdziwy, a timeout oczekiwania na projekcję jest jawny, na przykład 20 sekund, z komunikatem „projekcja nie doszła”. Problem, który zamyka, to `Thread.Sleep(5000)` bez asercji. Odpada sen jako jedyny sposób. Pętla czyta `GET`, aż jest `200` albo mija timeout.

### Dane

Każdy test tworzy własne slugs z `Guid`, żeby kolejność testów nie miała znaczenia. Nie ma jednej migawki „baza demo” współdzielonej bez czyszczenia. Kontener i tak jest nowy na klasę albo na fakt, według czasu. Wolimy kontener na klasę.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| ścieżka produktu | `POST` i później `GET` przez szynę |
| AppHost testowy | ten sam skład co laptop, na czas faktu |
| timeout projekcji | granica czekania na dokument, nie sen na sztywno |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `tests/JJDevHub.Integration.Tests/LessonPublishPathTests.cs` | od publikacji do `GET` |
| `tests/JJDevHub.Integration.Tests/ApphostSmokeTests.cs` | health przez testing host |
| `tests/JJDevHub.Integration.Tests/JJDevHub.Integration.Tests.csproj` | Testcontainers i `Aspire.Hosting.Testing` |

Paczka testowa Aspire jest w `Directory.Packages.props` w linii 13.6.5.

## Plan

### 1. Ścieżka lekcji

Kontenery, migracja `learning_write` i `learning_read`, broker, topic, start relayu i projektora w procesie testu albo jako hosty. `POST` lekcji publicznej jako właściciel z tokenem testowym podpisanym kluczem testu, nie kluczem maszyny. Czekanie na `GET`. Asercja: tytuł i brak `<script`, jeśli body je miało.

Token testowy: test wystawia JWT kluczem, który Learning w tym hoście ma w konfiguracji testowej. Nie woła produkcyjnego Identity, chyba że scenariusz jest właśnie o Identity. Ten scenariusz jest o Learning. Identity ma własny fakt, jeśli jeszcze nie ma pełnej ścieżki PKCE. Ma ją z numeru OpenIddict. Nie powielamy.

### 2. AppHost

Testing builder, `WaitForHealthy` na bramie albo pętla na `/health/live`. Asercja `200`. Test nie publikuje na tunel.

### 3. Filtr

Oba mają `Category=Integration`. Nie mają `Category=Performance`.

### Co jest do czego

| Test | Czego nie sprawdza |
| --- | --- |
| ścieżka lekcji | wyglądu webu |
| smoke AppHosta | reguły pustego tytułu, bo to test jednostkowy |

## Testy

Nazwa: `Published_lesson_is_readable_through_projection`.

| Arrange | Act | Assert |
| --- | --- | --- |
| puste bazy i broker | `POST` i pętla `GET` | `200` i tytuł z dokumentu, zanim minie timeout |

```bash
dotnet test tests/JJDevHub.Integration.Tests --nologo --filter "Category=Integration&FullyQualifiedName~Published_lesson_is_readable"
```

## Checkpoint

```bash
dotnet test tests/JJDevHub.Integration.Tests --nologo --filter "Category=Integration"
```

Job na pull requeście wykonuje ten filtr i nie loguje `no material`, jeśli w drzewie są te fakty. Lokalnie ten sam filtr przechodzi bez ustawionego sekretu maszyny.

## Umiesz wyjaśnić

Dlaczego nie wszystko jest integracją?

Bo kontener nie mówi, która reguła padła, gdy pada sieć. Reguła zostaje w teście bez Dockera. Integracja sprawdza, że skład się dogaduje.

Czym testing host różni się od Testcontainers w teście ręcznie?

Testing host podnosi to, co jest w `AppHost.cs`. Ręczny kontener może mieć inny obraz albo inny port. Smoke łapie rozjazd składu.

Dlaczego pętla, a nie sen?

Sen mija i test czyta za wcześnie albo czeka zawsze pełne pięć sekund. Pętla kończy się w momencie sukcesu i ma twardy koniec.

Skąd token właściciela bez pełnego PKCE w tym teście?

Z klucza testowego i claimu `role=owner`. Sprawdzamy projekcję, nie logowanie. Logowanie ma własny test. Mieszanie obu daje dwa powody padnięcia.

Dlaczego slug jest losowy?

Bo równoległe fakty na jednym kontenerze nie mogą wpadać na unikalność `linq` zostawioną przez sąsiada. Izolacja danych jest częścią testu.

## Zasady

- Integracja nie używa bazy z laptopa dewelopera.
- Obraz Postgresa to `postgres:18`.
- Timeout projekcji jest w asercji, z komunikatem.
- Test nie czyta `/etc/jjdevhub/compose.env`.
- `unit-tests` nadal wyklucza `Category=Integration`.
- Jeden smoke AppHosta, nie kopia każdego faktu jednostkowego.

## Poza zakresem

- Kontrakt Pact jest w [02-kontraktowe.md](02-kontraktowe.md).
- Próg czasu odpowiedzi jest w [03-wydajnosciowe.md](03-wydajnosciowe.md).
- Przeglądarka jest w [07-playwright.md](../11-web/07-playwright.md).

## Źródła

- [Testcontainers](https://dotnet.testcontainers.org/). Wiążące jest podnoszenie kontenera na czas testu i obraz przypięty tagiem. Dotyczy Postgresa i Kafki. Ze źródła nie bierzemy `latest`.
- [Aspire.Hosting.Testing](https://aspire.dev/testing/integrations-overview/). Wiążące jest odpalenie AppHosta w teście. Dotyczy smoke. Ze źródła nie bierzemy testu, który wymaga działającego już `aspire run` na laptopie.
- Numer projektora. Wiążące jest okno między `POST` a `GET`. Dotyczy pętli oczekiwania. Ze źródła nie bierzemy asercji, że `GET` w tej samej milisekundzie jest `200`.

## Gotowe gdy

- Ścieżka publikacji lekcji przechodzi na czystych kontenerach.
- Smoke AppHosta dostaje `200` na health bramy.
- Oba fakty mają `Category=Integration`.
- Filtr jednostkowy ich nie uruchamia.
- Test nie potrzebuje sekretu z maszyny.
