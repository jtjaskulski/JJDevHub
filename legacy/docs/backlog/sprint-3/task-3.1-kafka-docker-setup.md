# Task 3.1: Postawienie Apache Kafka i Zookeeper w Dockerze

| Pole | Wartosc |
|------|---------|
| Sprint | 3 - Event-Driven Sync |
| Status | DONE |
| Priorytet | High |
| Estymacja | 3 story points |
| Powiazane pliki | `infra/docker/docker-compose.yml` |

## Opis

Apache Kafka sluzy jako event bus (message broker) w architekturze CQRS. Gdy agregat domenowy emituje Integration Event po zapisie do PostgreSQL, event jest publikowany na Kafka topic. Kafka zapewnia trwalosc, ordering i at-least-once delivery zdarzen. Zookeeper koordynuje klaster Kafka (w klasycznym trybie Confluent Platform 7.x).

### Co jest zaimplementowane

Oba kontenery sa w pelni skonfigurowane w `docker-compose.yml`. Obrazy sa **przypiete do 7.6.1** (nie `latest`): obrazy `latest` wymagaja KRaft i nie pasuja do obecnych zmiennych srodowiskowych.

**Zookeeper:**
- Obraz: `confluentinc/cp-zookeeper:7.6.1`
- Port kliencki: 2181 (wewnatrz sieci Docker)
- Serwis Compose: `zookeeper` (DNS w `jjdevhub-net`)
- Container: `jjdevhub-zookeeper`

**Kafka:**
- Obraz: `confluentinc/cp-kafka:7.6.1`
- Mapowanie portow host: `9092:9092`, `29092:29092`
- Container: `jjdevhub-kafka`
- Serwis Compose: `kafka`
- `KAFKA_BROKER_ID: 1`
- `KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181` (nazwa **serwisu** Compose, nie `container_name`)
- Dwie reklamowane nasluchiwacze: `PLAINTEXT://kafka:9092` (kontenery w `jjdevhub-net`) oraz `PLAINTEXT_HOST://localhost:29092` (aplikacje na hoscie, np. `dotnet run`)
- `KAFKA_LISTENER_SECURITY_PROTOCOL_MAP`, `KAFKA_INTER_BROKER_LISTENER_NAME` — jak w pliku compose
- `KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1` (single broker)
- Zalezy od: Zookeeper

## Kryteria akceptacji

- [x] Zookeeper uruchomiony w Docker
- [x] Kafka broker uruchomiony i polaczony z Zookeeper
- [x] Broker dostepny z sieci Docker na `kafka:9092` (`jjdevhub-net`)
- [x] Bootstrap z hosta dziala przez `localhost:29092` (mapowanie + `PLAINTEXT_HOST`)
- [x] Automatyczne tworzenie topicow (auto.create.topics.enable — domyslnie wlaczone)
- [x] Konfiguracja single-broker (replication factor = 1)

## Wymagane pakiety NuGet

Brak - to jest zadanie infrastrukturalne (Docker). Pakiety klienckie Kafka sa w Task 3.2.

## Konfiguracja docker-compose

Fragment zgodny z `infra/docker/docker-compose.yml` w repozytorium:

```yaml
zookeeper:
  image: confluentinc/cp-zookeeper:7.6.1
  container_name: jjdevhub-zookeeper
  environment:
    ZOOKEEPER_CLIENT_PORT: 2181
  networks:
    - jjdevhub-net

kafka:
  image: confluentinc/cp-kafka:7.6.1
  container_name: jjdevhub-kafka
  depends_on:
    - zookeeper
  ports:
    - "9092:9092"
    - "29092:29092"
  environment:
    KAFKA_BROKER_ID: 1
    KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
    KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:9092,PLAINTEXT_HOST://localhost:29092
    KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT
    KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
    KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
  networks:
    - jjdevhub-net
```

## Zaleznosci

- **Wymaga:** Docker i Docker Compose
- **Blokuje:** Task 3.2 (KafkaEventBus producer), Task 3.3 (Kafka consumer)

## Notatki techniczne

- Single broker z replication factor 1 jest wystarczajacy dla developmentu. Na produkcji nalezy uzyc minimum 3 brokerow z replication factor 3.
- Confluent Platform zawiera Zookeeper, ale nowsze linie obrazow `latest` przechodza na KRaft bez Zookeeper. Dlatego w repo uzywana jest wersja **7.6.1** z klasycznym brokerem + ZK.
- Auto-create topics jest wlaczone domyslnie. Topic jest tworzony automatycznie gdy producer wysle pierwszy event. Nazwy topicow = nazwy typow Integration Event (np. `WorkExperienceCreatedIntegrationEvent`).
- **Host vs Docker:** aplikacja na hoscie (`dotnet run`) powinna ustawic `Kafka:BootstrapServers` na `localhost:29092`. Kontenery w compose uzywaja `kafka:9092` (np. `Kafka__BootstrapServers` w `docker-compose.yml`). Polaczenie z hosta na samym `localhost:9092` zwraca metadane z adresem `kafka:9092`, ktorego host nie rozwiaze — stad drugi listener i port **29092**.
- Porty **9092** i **29092** na hoscie sluza do diagnostyki; w produkcji nie powinny byc publicznie wystawione.
- Dokumentacja: https://docs.confluent.io/platform/current/installation/docker/
