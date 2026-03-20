# Task 3.2: Stworzenie KafkaEventBus (Producer) i DomainEventHandler

| Pole | Wartosc |
|------|---------|
| Sprint | 3 - Event-Driven Sync |
| Status | DONE |
| Priorytet | High |
| Estymacja | 5 story points |
| Powiazane pliki | `src/Services/JJDevHub.Content/JJDevHub.Content.Infrastructure/Messaging/` (KafkaEventBus, OutboxPublisherHostedService, KafkaProducerConfiguration), `src/Services/JJDevHub.Shared.Kernel/Messaging/` |

## Opis

`KafkaEventBus` to implementacja interfejsu `IEventBus` z Shared.Kernel — **bezposredni** producer (topic, klucz, JSON) z tymi samymi ustawieniami co publisher outboxa. W praktyce **sciezka produkcyjna** z Application do Kafka idzie przez **Transactional Outbox**: handler zapisuje wiadomosc do PostgreSQL w tej samej transakcji co agregat, a `OutboxPublisherHostedService` w tle publikuje na Kafka po commicie. Dzieki temu awaria Kafki nie blokuje ani nie cofa zapisu biznesowego.

Kazdy typ Integration Event trafia na oddzielny topic (nazwa topicu = nazwa typu). Producer jest skonfigurowany z `Acks.All` i idempotencja (`KafkaProducerConfiguration` — wspolne dla `KafkaEventBus` i outbox publishera).

### Co jest zaimplementowane

**KafkaEventBus** (implementuje `IEventBus`):
- Konfiguracja przez `KafkaProducerConfiguration.CreateProducerConfig(IConfiguration)` — sekcja `Kafka:BootstrapServers` (default: `localhost:29092` — bootstrap z hosta; w Docker: `kafka:9092`)
- Producer config: `Acks = Acks.All`, `EnableIdempotence = true`
- `PublishAsync<T>(T integrationEvent)`:
  - Topic = `typeof(T).Name` (np. `WorkExperienceCreatedIntegrationEvent`)
  - Key = event `Id` (Guid jako string)
  - Value = JSON serializacja eventu
  - Loguje sukces (partition, offset) lub error
- `IDisposable` z `Flush()` przed `Dispose()`

**OutboxPublisherHostedService** (Infrastructure):
- Ten sam `ProducerConfig` co `KafkaEventBus` (wspolny helper)
- Odczyt `content.outbox_messages` (`FOR UPDATE SKIP LOCKED`), `ProduceAsync` z `event_type` / `message_key` / `payload` (semantyka jak wyzej: topic = typ eventu, klucz = Id)
- Konfiguracja: `Outbox:PublisherEnabled`, `Outbox:PollIntervalMs`, `Outbox:BatchSize`, `Outbox:ErrorBackoffMs`

**Kontrakty w Shared.Kernel:**
- `IEventBus.PublishAsync<T>(T integrationEvent) where T : IntegrationEvent`
- `IntegrationEvent` - bazowy record z `Id`, `OccurredOn`

**Uzycie w Domain Event Handlers (Application):**
- `WorkExperienceCreatedDomainEventHandler` -> `IOutboxWriter.Enqueue(WorkExperienceCreatedIntegrationEvent, ...)`
- `WorkExperienceUpdatedDomainEventHandler` -> `IOutboxWriter.Enqueue(WorkExperienceUpdatedIntegrationEvent, ...)`
- `WorkExperienceDeletedDomainEventHandler` -> `IOutboxWriter.Enqueue(WorkExperienceDeletedIntegrationEvent, ...)`

`IEventBus` / `KafkaEventBus` sa zarejestrowane w DI (Singleton) na potrzeby bezposredniej publikacji, testow lub przyszlych serwisow — obecnie handlery nie wstrzykuja `IEventBus`.

## Kryteria akceptacji

- [x] KafkaEventBus implementuje IEventBus
- [x] Producer z Acks.All i idempotencja (at-least-once delivery) — rowniez dla outbox publishera
- [x] Topic name = nazwa typu Integration Event
- [x] Key = event Id dla partitioning
- [x] JSON serializacja eventow (w outbox: payload zapisany przy enqueue)
- [x] Logowanie sukcesu i bledow
- [x] Graceful shutdown z Flush() przed Dispose()
- [x] Rejestracja w DI jako Singleton (`IEventBus` -> `KafkaEventBus`)

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `Confluent.Kafka` | 2.13.0 | JJDevHub.Content.Infrastructure | Oficjalny .NET klient Apache Kafka (producer/consumer) |

## Przeplyw eventow

```
[Agregat] emituje DomainEvent
    → [ContentDbContext.SaveChangesAsync] dispatches via MediatR
        → [DomainEventHandler] (Application layer)
            → Upsert do MongoDB (IWorkExperienceReadStore)
            → Enqueue Integration Event (IOutboxWriter) → wiersz w content.outbox_messages (ta sama transakcja co EF)
    → [OutboxPublisherHostedService] (po commicie)
            → Confluent.Kafka Producer (wspolna konfiguracja z KafkaEventBus)
                → Topic: "WorkExperienceCreatedIntegrationEvent"
                → Key: event.Id (message_key)
                → Value: JSON (payload)
```

**Alternatywa (bezposrednia):** dowolny kod moze wywolac `IEventBus.PublishAsync` -> `KafkaEventBus` -> Kafka (bez outbox).

## Zaleznosci

- **Wymaga:** Task 1.3 (IEventBus, IntegrationEvent), Task 3.1 (Kafka w Docker)
- **Blokuje:** Task 3.3 (Consumer potrzebuje eventow na topicach)

## Notatki techniczne

- `Acks.All` wymaga potwierdzenia zapisu od wszystkich replik (ISR). W single-broker setup oznacza to potwierdzenie od jednego brokera.
- `EnableIdempotence = true` zapewnia, ze retransmisje nie tworza duplikatow na brokerze (exactly-once semantics na poziomie producera).
- `KafkaEventBus` jest Singleton — jeden producer na cale lifetime aplikacji dla `IEventBus`. `OutboxPublisherHostedService` utrzymuje **osobny** producer w hosted service (drugi egzemplarz polaczenia); konfiguracja jest wspoldzielona przez `KafkaProducerConfiguration`.
- Nazwy topicow sa generowane dynamicznie z typu eventu. Kafka automatycznie tworzy topic jesli nie istnieje (auto.create.topics.enable).
- Transactional Outbox oddziela zapis PostgreSQL od publikacji na Kafka; jesli Kafka jest chwilowo niedostepna, wiadomosci zostaja w outbox i sa ponawiane po odzyskaniu brokera.
