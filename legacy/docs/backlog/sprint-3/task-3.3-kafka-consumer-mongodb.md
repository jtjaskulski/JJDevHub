# Task 3.3: Implementacja Consumera Zdarzen z Kafki + Upsert do MongoDB

| Pole | Wartosc |
|------|---------|
| Sprint | 3 - Event-Driven Sync |
| Status | DONE |
| Priorytet | High |
| Estymacja | 8 story points |
| Powiazane pliki | `src/Services/JJDevHub.Sync/`, `src/Services/JJDevHub.Content/JJDevHub.Content.Infrastructure/ReadStore/` |

## Opis

Sync Worker to BackgroundService ktory konsumuje Integration Events z Kafka i wykonuje operacje Upsert do MongoDB (tworzenie zdenormalizowanego read model). Aktualnie read model jest aktualizowany synchronicznie w Domain Event Handlers (w ramach tej samej transakcji co zapis do PostgreSQL). Sync Worker ma byc niezaleznym konsumentem ktory zapewnia eventual consistency nawet jesli bezposredni upsert w DomainEventHandler sie nie powiedzie.

### Co juz jest zrobione

**MongoDB Read Store (pelna implementacja):**
- `MongoWorkExperienceReadStore` implementuje `IWorkExperienceReadStore`
- Kolekcja: `work_experiences`
- Metody: `GetByIdAsync`, `GetAllAsync`, `GetPublicAsync`, `UpsertAsync` (ReplaceOne z IsUpsert), `DeleteAsync`
- `WorkExperienceDocument` z `[BsonId]` atrybutami
- `MongoDbSettings` z ConnectionString i DatabaseName

**Sync Worker:**
- `KafkaConsumerService` — group `jjdevhub-sync-worker`, manual commit, retry, DLT, health
- Topiki: WorkExperience, JobApplication, CurriculumVitae (Created/Updated/Deleted)
- Placeholder `Worker.cs` zostal zastapiony

### Co pozostalo

- Testy automatyczne Sync Workera
- Opcjonalnie: projekcja Mongo wylacznie z Kafki (obecnie dual-write: handler pre-commit + consumer)

## Kryteria akceptacji

- [x] Sync Worker konsumuje zdarzenia z Kafka topics
- [x] Subskrypcja na: `WorkExperienceCreatedIntegrationEvent`, `WorkExperienceUpdatedIntegrationEvent`, `WorkExperienceDeletedIntegrationEvent`
- [x] Subskrypcja na CurriculumVitae Created/Updated/Deleted oraz JobApplication
- [x] Deserializacja JSON eventow do odpowiednich typow
- [x] Operacje na MongoDB: Upsert (Create/Update), Delete
- [x] Consumer Group: `jjdevhub-sync-worker`
- [x] Retry logic z exponential backoff przy bledach MongoDB
- [x] Dead Letter Topic dla eventow ktore nie moga byc przetworzone
- [x] Health check reportujacy status consumera
- [x] Graceful shutdown z commit offsetow

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `Confluent.Kafka` | 2.13.0 | JJDevHub.Sync | Kafka consumer do odbioru Integration Events |
| `MongoDB.Driver` | 3.6.0 | JJDevHub.Sync | MongoDB driver do upsert read models |
| `Microsoft.Extensions.Hosting` | 10.0.1 | JJDevHub.Sync | BackgroundService hosting (juz zainstalowany) |

## Kroki implementacji

1. **Dodaj referencje projektowe i NuGet do Sync Worker:**
   ```xml
   <ProjectReference Include="..\JJDevHub.Shared.Kernel\JJDevHub.Shared.Kernel.csproj" />
   <PackageReference Include="Confluent.Kafka" Version="2.13.0" />
   <PackageReference Include="MongoDB.Driver" Version="3.6.0" />
   ```

2. **Stworz `KafkaConsumerService` (BackgroundService):**
   - Consumer config: `GroupId = "jjdevhub-sync-worker"`, `AutoOffsetReset = AutoOffsetReset.Earliest`, `EnableAutoCommit = false`
   - Subscribe na liste topicow (Integration Event names)
   - W petli `while (!stoppingToken.IsCancellationRequested)`: `consumer.Consume(stoppingToken)`

3. **Implementuj routing eventow:**
   ```csharp
   var topic = consumeResult.Topic;
   switch (topic)
   {
       case nameof(WorkExperienceCreatedIntegrationEvent):
           await HandleCreated(consumeResult.Message.Value);
           break;
       case nameof(WorkExperienceUpdatedIntegrationEvent):
           await HandleUpdated(consumeResult.Message.Value);
           break;
       case nameof(WorkExperienceDeletedIntegrationEvent):
           await HandleDeleted(consumeResult.Message.Value);
           break;
   }
   consumer.Commit(consumeResult);
   ```

4. **Zaimplementuj handlery per event typ:**
   - `HandleCreated` / `HandleUpdated`: deserializacja -> zbuduj read model -> `MongoCollection.ReplaceOneAsync` z `IsUpsert = true`
   - `HandleDeleted`: deserializacja -> `MongoCollection.DeleteOneAsync`

5. **Dodaj retry logic:**
   - Try/catch w petli consume
   - Exponential backoff: 1s, 2s, 4s, 8s, max 30s
   - Po max retries -> publish na Dead Letter Topic

6. **Dodaj graceful shutdown:**
   - `stoppingToken.Register(() => consumer.Close())`
   - Commit ostatnich offsetow przed zamknieciem

7. **Dodaj health check:**
   - Raportuj status consumera (connected, lagging, disconnected)

## Zaleznosci

- **Wymaga:** Task 3.1 (Kafka w Docker), Task 3.2 (Producer publikujacy eventy)
- **Blokuje:** Niezalezny (ale krytyczny dla eventual consistency)

## Notatki techniczne

- `EnableAutoCommit = false` z manualnym `Commit()` po przetworzeniu zapewnia at-least-once delivery. Jesli worker padnie przed commitem, event bedzie ponownie dostarczony.
- Consumer Group `jjdevhub-sync-worker` pozwala na skalowanie horizontalne - wiele instancji workera dzieli partycje miedzy soba.
- Obecnie DomainEventHandlers w Application layer rowniez aktualizuja MongoDB synchronicznie. Sync Worker sluzy jako "backup" - jesli synchroniczny upsert sie nie powiedzie, worker eventualnie przetworzy event z Kafka.
- Dead Letter Topic pozwala na reczna analiza eventow ktore nie mogly byc przetworzone (np. nieprawidlowy JSON, brakujace pole).
- Na przyszlosc: rozwazyc uzycie Schema Registry (Confluent) do walidacji formatu eventow.
