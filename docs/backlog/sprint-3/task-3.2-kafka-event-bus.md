# Task 3.2: Stworzenie KafkaEventBus (Producer) i DomainEventHandler

| Pole | Wartosc |
|------|---------|
| Sprint | 3 - Event-Driven Sync |
| Status | DONE |
| Priorytet | High |
| Estymacja | 5 story points |
| Powiazane pliki | `src/Services/JJDevHub.Content/JJDevHub.Content.Infrastructure/Messaging/KafkaEventBus.cs`, `src/Services/JJDevHub.Shared.Kernel/Messaging/` |

## Opis

`KafkaEventBus` to implementacja interfejsu `IEventBus` z Shared.Kernel. Producer wysyla Integration Events na Kafka topics po pomyslnym zapisie do PostgreSQL. Kazdy typ Integration Event trafia na oddzielny topic (nazwa topicu = nazwa typu). Producer jest skonfigurowany z `Acks.All` i idempotencja aby zapewnic dostarczenie wiadomosci.

### Co jest zaimplementowane

**KafkaEventBus** (implementuje `IEventBus`):
- Konfiguracja z `IConfiguration` - sekcja `Kafka:BootstrapServers` (default: `localhost:9092`)
- Producer config: `Acks = Acks.All`, `EnableIdempotence = true`
- `PublishAsync<T>(T integrationEvent)`:
  - Topic = `typeof(T).Name` (np. `WorkExperienceCreatedIntegrationEvent`)
  - Key = event `Id` (Guid jako string)
  - Value = JSON serializacja eventu
  - Loguje sukces (partition, offset) lub error
- `IDisposable` z `Flush()` przed `Dispose()`

**Kontrakty w Shared.Kernel:**
- `IEventBus.PublishAsync<T>(T integrationEvent) where T : IntegrationEvent`
- `IntegrationEvent` - bazowy record z `Id`, `OccurredOn`

**Uzycie w Domain Event Handlers (Application):**
- `WorkExperienceCreatedDomainEventHandler` -> publikuje `WorkExperienceCreatedIntegrationEvent`
- `WorkExperienceUpdatedDomainEventHandler` -> publikuje `WorkExperienceUpdatedIntegrationEvent`
- `WorkExperienceDeletedDomainEventHandler` -> publikuje `WorkExperienceDeletedIntegrationEvent`

## Kryteria akceptacji

- [x] KafkaEventBus implementuje IEventBus
- [x] Producer z Acks.All i idempotencja (at-least-once delivery)
- [x] Topic name = nazwa typu Integration Event
- [x] Key = event Id dla partitioning
- [x] JSON serializacja eventow
- [x] Logowanie sukcesu i bledow
- [x] Graceful shutdown z Flush() przed Dispose()
- [x] Rejestracja w DI jako Singleton

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
            → Publish na Kafka (IEventBus.PublishAsync)
                → [KafkaEventBus] (Infrastructure layer)
                    → Confluent.Kafka Producer
                        → Topic: "WorkExperienceCreatedIntegrationEvent"
                        → Key: event.Id
                        → Value: JSON
```

## Zaleznosci

- **Wymaga:** Task 1.3 (IEventBus, IntegrationEvent), Task 3.1 (Kafka w Docker)
- **Blokuje:** Task 3.3 (Consumer potrzebuje eventow na topicach)

## Notatki techniczne

- `Acks.All` wymaga potwierdzenia zapisu od wszystkich replik (ISR). W single-broker setup oznacza to potwierdzenie od jednego brokera.
- `EnableIdempotence = true` zapewnia, ze retransmisje nie tworza duplikatow na brokerze (exactly-once semantics na poziomie producera).
- KafkaEventBus jest zarejestrowany jako Singleton w DI - jeden producer na caly lifetime aplikacji. Producer jest thread-safe.
- Nazwy topicow sa generowane dynamicznie z typu eventu. Kafka automatycznie tworzy topic jesli nie istnieje (auto.create.topics.enable).
- Na przyszlosc: rozwazyc Transactional Outbox Pattern aby oddzielić zapis do PostgreSQL od publikacji na Kafka. Obecnie jesli Kafka jest niedostepna, cala transakcja jest rollbackowana.
