# Task 3.1: Postawienie Apache Kafka i Zookeeper w Dockerze

| Pole | Wartosc |
|------|---------|
| Sprint | 3 - Event-Driven Sync |
| Status | DONE |
| Priorytet | High |
| Estymacja | 3 story points |
| Powiazane pliki | `infra/docker/docker-compose.yml` |

## Opis

Apache Kafka sluzy jako event bus (message broker) w architekturze CQRS. Gdy agregat domenowy emituje Integration Event po zapisie do PostgreSQL, event jest publikowany na Kafka topic. Kafka zapewnia trwalosc, ordering i at-least-once delivery zdarzen. Zookeeper koordynuje klaster Kafka (wymagany w wersji Confluent Platform).

### Co jest zaimplementowane

Oba kontenery sa w pelni skonfigurowane w `docker-compose.yml`:

**Zookeeper:**
- Obraz: `confluentinc/cp-zookeeper:latest`
- Port kliencki: 2181
- Container: `jjdevhub-zookeeper`

**Kafka:**
- Obraz: `confluentinc/cp-kafka:latest`
- Port: `9092:9092`
- Container: `jjdevhub-kafka`
- `KAFKA_BROKER_ID: 1`
- `KAFKA_ZOOKEEPER_CONNECT: jjdevhub-zookeeper:2181`
- `KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://jjdevhub-kafka:9092`
- `KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1` (single broker)
- Zalezy od: Zookeeper

## Kryteria akceptacji

- [x] Zookeeper uruchomiony w Docker
- [x] Kafka broker uruchomiony i polaczony z Zookeeper
- [x] Port 9092 dostepny z sieci Docker (`jjdevhub-net`)
- [x] Automatyczne tworzenie topicow (auto.create.topics.enable)
- [x] Konfiguracja single-broker (replication factor = 1)

## Wymagane pakiety NuGet

Brak - to jest zadanie infrastrukturalne (Docker). Pakiety klienckie Kafka sa w Task 3.2.

## Konfiguracja docker-compose

```yaml
zookeeper:
  image: confluentinc/cp-zookeeper:latest
  container_name: jjdevhub-zookeeper
  environment:
    ZOOKEEPER_CLIENT_PORT: 2181
    ZOOKEEPER_TICK_TIME: 2000
  networks:
    - jjdevhub-net

kafka:
  image: confluentinc/cp-kafka:latest
  container_name: jjdevhub-kafka
  depends_on:
    - zookeeper
  ports:
    - "9092:9092"
  environment:
    KAFKA_BROKER_ID: 1
    KAFKA_ZOOKEEPER_CONNECT: jjdevhub-zookeeper:2181
    KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://jjdevhub-kafka:9092
    KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
  networks:
    - jjdevhub-net
```

## Zaleznosci

- **Wymaga:** Docker i Docker Compose
- **Blokuje:** Task 3.2 (KafkaEventBus producer), Task 3.3 (Kafka consumer)

## Notatki techniczne

- Single broker z replication factor 1 jest wystarczajacy dla developmentu. Na produkcji nalezy uzyc minimum 3 brokerow z replication factor 3.
- Confluent Platform zawiera Zookeeper, ale nowe wersje Kafka (KRaft) nie wymagaja Zookeeper. Mozna rozwazyc migracje na KRaft w przyszlosci.
- Auto-create topics jest wlaczone domyslnie. Topic jest tworzony automatycznie gdy producer wysle pierwszy event. Nazwy topicow = nazwy typow Integration Event (np. `WorkExperienceCreatedIntegrationEvent`).
- Port 9092 jest wystawiony na hosta dla narzedzi diagnostycznych (np. `kafkacat`, Kafka UI). W produkcji port nie powinien byc wystawiony publicznie.
- Dokumentacja: https://docs.confluent.io/platform/current/installation/docker/
