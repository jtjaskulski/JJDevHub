# 03. Kafka w trybie KRaft

Poprzedni numer to [outbox](02-outbox.md). Następny to [relay](04-relay.md).

Ten numer stawia brokera `apache/kafka:4.4.0` i tworzy topiki. `auto.create.topics.enable` jest wyłączone. Aplikacja jeszcze nie produkuje. Jest proces, lista topiców i test, że nieznany topic nie powstaje sam.

## Po co

Outbox bez brokera zostaje tabelą. Broker bez jawnych topiców przyjmie literówkę w nazwie i utworzy drugi temat, którego nikt nie konsumuje. Wyłączenie auto-tworzenia zamyka tę dziurę. KRaft zamyka drugą: nie ma ZooKeepera do pilnowania obok jednej usługi.

Obraz Confluent nie wchodzi. Domyślny kontener z `Aspire.Hosting.Kafka` wskazuje właśnie na niego. Dlatego broker jest zwykłym kontenerem, a nie integracją hostingu Aspire.

## Dlaczego ta technologia

### `apache/kafka` i KRaft

Jeden proces, rola broker i controller, bez ZooKeepera. Problem, który zamyka, to drugi system koordynacji przy jednym brokerze na jednej maszynie. Odpada klaster trzech brokerów: nie mamy trzech maszyn, a udawanie trzech na jednej VM nie daje odporności na pad dysku. Koszt: jedna awaria brokera zatrzymuje fakty. Zapis HTTP kursu i tak już przeszedł. Relay dogoni po powrocie brokera, bo wiersze outboxa czekają.

Obraz jest przypięty do `4.4.0`. Nie ma `:latest`.

### Topic z pliku, nie z kodu aplikacji

Kontener jednorazowy `kafka-init` używa `kafka-topics.sh --create --if-not-exists`. Problem, który zamyka, to temat tworzony przy pierwszym `Produce`, zależnie od tego, który proces wstanie pierwszy. Odpada auto-create. Koszt: nowy topic to zmiana skryptu i numeru, nie niespodzianka w logu.

Na starcie są: `learning.catalog` (3 partycje), `learning.integration` (3 partycje, `cleanup.policy=compact`), `news.articles`, `identity.registration`, `contact.integration`, `audit.events` (1 partycja) oraz `learning.catalog.dlq` (1 partycja). Topiki usług, których jeszcze nie ma, powstają teraz, tak jak powstały puste bazy. Retencja domyślna poza tematami kompaktowanymi. DLQ nie jest kompaktowany.

Klucz kompaktowania to slug, ustawiany przez producenta później. Sam temat ma politykę `compact`. Bez klucza kompaktowanie nie ma czego trzymać. Relay poda klucz. Ten numer o tym przypomina w zasadach i nie udaje, że kompaktowanie działa bez klucza.

### AppHost

`AddContainer("kafka", "apache/kafka", "4.4.0")` z zmiennymi KRaft: `KAFKA_NODE_ID`, `KAFKA_PROCESS_ROLES=broker,controller`, listenery `PLAINTEXT://kafka:9092` i controller. Drugi kontener `kafka-init` zależy od health brokera i kończy się sukcesem. Nazwy zmiennych biorą się z dokumentacji obrazu `apache/kafka` dla trybu KRaft, nie z obrazu Confluent. Jeśli nazwa zmiennej w `4.4.0` różni się od pamięci, wygrywa dokumentacja obrazu przypiętego w tym numerze, a specyfikacja jest poprawiana w tym samym scaleniu. Nie mieszamy `KAFKA_ADVERTISED_LISTENERS` ze składni Confluent, jeśli obraz Apache ma inną.

Port na hoście laptopa: `9092`. Na maszynie port nie jest publikowany na `0.0.0.0`. Zostaje w sieci compose. Tunel nie dostaje 9092.

### Klient jeszcze nie

Paczka `Confluent.Kafka` wchodzi do `Directory.Packages.props` w tym numerze, bo następny numer jej użyje, albo dopiero w relayu. Żeby nie zostawiać paczki bez kodu, ten numer dodaje ją w projekcie testowym, który łączy się z kontenera Testcontainers i woła `GetMetadata` dla `learning.catalog`. Paczka klienta jest Apache-2.0. Obraz serwera Apache jest osobną decyzją i też jest Apache-2.0. To nie jest ten sam byt co obraz Confluent.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| KRaft | tryb Kafki bez ZooKeepera, z kontrolerem w kworum |
| partycja | kawałek topicu; kolejność jest w partycji, nie między nimi |
| compaction | zostaje ostatnia wartość klucza |
| DLQ | temat na wiadomość, której konsument nie umie przetworzyć |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `deploy/kafka/create-topics.sh` | `kafka-topics.sh --create --if-not-exists` |
| `src/apphost/JJDevHub.AppHost/AppHost.cs` | kontenery `kafka` i `kafka-init` |
| `tests/JJDevHub.Integration.Tests/KafkaTopicsTests.cs` | metadata, `Category=Integration` |

Test używa `Testcontainers.Kafka` w trybie KRaft, a potem ten sam skrypt albo te same wywołania Admin API. Obraz testu ma być zgodny z produktem. Jeśli `Testcontainers.Kafka` w przypiętej wersji nie umie wskazać `apache/kafka:4.4.0`, test ustawia obraz wprost. Nie zostawiamy domyślnego obrazu testu, jeśli jest to Confluent.

## Plan

### 1. Skrypt topiców

Dla każdego topicu ze słownika jedna linia `--create`. `learning.integration` ma `--config cleanup.policy=compact`. `learning.catalog` ma `3` partycje. DLQ ma `1`. Skrypt jest idempotentny.

### 2. Kontenery

Broker ma healthcheck, który init respektuje. Init ma `restart: "no"`. Compose z publish nie publikuje `9092` na adres maszyny inny niż localhost laptopa. W publish na maszynę `ports` dla Kafki nie występuje. Usługi w tej samej sieci łączą się z `kafka:9092`.

### 3. Test

Admin client pobiera metadata `learning.catalog` i oczekuje trzech partycji. Próba metadanych topicu `typo.topic` kończy się błędem nieznanego topicu, nie utworzeniem go. To jest asercja wyłączonego auto-create: testowy broker ma tę flagę na `false`.

### Co jest do czego

| Topic | Partycje | Polityka |
| --- | --- | --- |
| `learning.catalog` | 3 | delete, zwykła retencja |
| `learning.integration` | 3 | compact |
| `learning.catalog.dlq` | 1 | delete |
| pozostałe z słownika | jak w słowniku | delete, dopóki ich numer nie powie inaczej |

`audit.events` ma jedną partycję, bo porządek audytu per encja i tak trzyma klucz, a wolumen jest mały.

## Testy

Nazwa: `Catalog_topic_has_three_partitions_and_unknown_topic_is_not_created`.

| Arrange | Act | Assert |
| --- | --- | --- |
| broker KRaft po skrypcie, auto-create wyłączone | metadata `learning.catalog` | 3 partycje |
| ten sam broker | metadata `typo.topic` | błąd nieznanego topicu |

```bash
dotnet test tests/JJDevHub.Integration.Tests --nologo --filter "FullyQualifiedName~Catalog_topic_has_three"
```

## Checkpoint

```bash
aspire run
docker exec "$(docker ps -qf name=kafka)" /opt/kafka/bin/kafka-topics.sh --bootstrap-server localhost:9092 --list
```

Ścieżka `kafka-topics.sh` w obrazie `apache/kafka:4.4.0` może być inna. Checkpoint używa ścieżki z dokumentacji tego tagu. Lista zawiera `learning.catalog` i `learning.integration`. Nie zawiera topicu, którego nie ma w skrypcie.

Z zewnątrz maszyny port `9092` nie odpowiada. W konfiguracji tunelu go nie ma.

## Umiesz wyjaśnić

Dlaczego nie ma ZooKeepera?

Bo KRaft trzyma metadane w kworum kontrolerów. Przy jednym węźle i tak jest jeden proces. Drugi system do tego jednego procesu nie dodaje odporności.

Dlaczego auto-create jest wyłączone?

Bo literówka w nazwie topicu cicho tworzy drugi strumień. Konsument słucha właściwej nazwy i nie widzi ruchu. Wolimy błąd producenta niż cichy temat.

Czym compaction różni się od retencji czasowej?

Retencja czasowa usuwa stare segmenty niezależnie od klucza. Compaction zostawia ostatnią wartość każdego klucza. Dlatego stan technologii dla Cv przeżywa, a pośrednie nazwy nie muszą.

Dlaczego obraz Aspire Kafka odpada?

Bo domyślny obraz tej integracji jest od Confluent i jego licencja nie jest na liście. Klient `Confluent.Kafka` zostaje: to paczka Apache-2.0. Serwer jest obrazem Apache.

Co daje trzy partycje przy jednym konsumencie?

Na razie niewiele: jeden konsument czyta wszystkie. Przy drugim podzie z [skalowania](../13-kubernetes/07-skalowanie.md) partycje rozłożą się na dwa procesy. Kolejność w jednym slugu zostaje, bo klucz jest slugiem i slug trafia zawsze na tę samą partycję.

## Zasady

- Obraz to `apache/kafka:4.4.0`, bez `:latest` i bez obrazu Confluent.
- `auto.create.topics.enable=false`.
- Nowy topic powstaje w `create-topics.sh`, nie w kodzie przy błędzie.
- Port `9092` nie jest publiczny.
- `learning.integration` jest jedynym kompaktowanym topicem na tym numerze.
- `Aspire.Hosting.Kafka` nie wchodzi do AppHosta.

## Poza zakresem

- Producent jest w [04-relay.md](04-relay.md).
- Konsument i DLQ w użyciu są w [05-projektor-i-inbox.md](05-projektor-i-inbox.md). Temat DLQ tylko powstaje.
- Zmiana liczby partycji istniejącego topicu nie jest procedurą tego numeru. Jest operacją świadomą, bo klucz zostaje, a kolejność między nowymi partycjami nie jest tą samą kolejnością.

## Źródła

- [KRaft](https://kafka.apache.org/documentation/#kraft). Wiążące jest działanie bez ZooKeepera. Dotyczy jednego węzła. Ze źródła nie bierzemy trzech kontrolerów na jednej VM jako „produkcyjnego kworum”.
- [Topic configuration](https://kafka.apache.org/documentation/#topicconfigs). Wiążące są `cleanup.policy` i liczba partycji przy tworzeniu. Dotyczy `learning.integration`. Ze źródła nie bierzemy kompaktowania topicu faktów jednorazowych: przyjęcie zapytania ma zostać każdą dostawą, nie ostatnim kluczem.
- [Confluent.Kafka license](https://github.com/confluentinc/confluent-kafka-dotnet/blob/master/LICENSE). Wiążące jest Apache-2.0 paczki klienta. Dotyczy tego, że klient zostaje, a obraz serwera Confluent nie. Ze strony obrazu `confluentinc/confluent-local` nie bierzemy licencji Community jako wyjątku.

## Gotowe gdy

- `aspire run` podnosi brokera i init kończy się sukcesem.
- Lista topiców zgadza się ze skryptem.
- Test nieznanego topicu nie tworzy go.
- Port 9092 nie jest w tunelu i nie jest na `0.0.0.0` maszyny.
- W AppHost nie ma `AddKafka` z pakietu `Aspire.Hosting.Kafka`.
