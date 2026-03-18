# Transactional Outbox — Kafka decoupling (backlog plan)

## Problem (historical — partially resolved)

Domain event handlers run inside `SaveChangesAsync` within an explicit transaction. Aggregates are flushed first, then handlers dispatch. Kafka publishing has been moved to the transactional outbox (`IOutboxWriter.Enqueue` + `OutboxPublisherHostedService`), so Kafka is no longer a pre-commit side effect.

The remaining pre-commit side effect is **MongoDB**: handlers upsert the read model before the transaction commits. If PG commit fails after a successful MongoDB upsert, the read model can diverge until reconciled.

Running handlers **after** commit would fix PG-first consistency for MongoDB but breaks atomicity if outbox entries also need to be part of the commit. The current approach (explicit transaction: flush aggregates → dispatch handlers → flush outbox → commit) keeps outbox + aggregates atomic while accepting the MongoDB tradeoff.

## Goal

- **Atomicity:** Message publication is consistent with PostgreSQL commit.
- **Reliability:** Kafka outages do not lose events; retries are safe.
- **Optional:** Mongo read-model projection can stay synchronous with outbox commit in the same transaction, or move to a separate consumer (bigger change).

## Recommended approach: Transactional Outbox

1. **Outbox table** (same PostgreSQL DB, schema `content` or `messaging`):
   - Columns: `id`, `aggregate_type`, `aggregate_id`, `event_type`, `payload` (JSON), `created_utc`, `processed_utc` (nullable), optional `correlation_id`.
2. **On domain event (before commit):** Instead of calling `IEventBus` directly inside handlers, **append rows** to `outbox` in the same EF transaction as aggregate changes.
3. **Publisher worker** (background service or separate process):
   - Polls `processed_utc IS NULL` with `FOR UPDATE SKIP LOCKED` (or equivalent).
   - Publishes to Kafka, then marks row processed (same DB transaction per batch or per message).
4. **Idempotency:** Consumers use `event_id` / dedupe keys; Kafka producer keys by aggregate id for ordering per aggregate.

## MongoDB read model

| Option | Pros | Cons |
|--------|------|------|
| **A. Keep sync in handler, only Kafka via outbox** | Smaller change; read model still tied to commit boundary if handler runs pre-commit | Mongo still in failure domain with PG until commit |
| **B. Project Mongo from outbox consumer** | Single pipeline: outbox → Kafka + Mongo | Requires replay/compaction story; more moving parts |
| **C. Separate CDC / listener** | Clean separation | Infra-heavy |

**Pragmatic first step:** Outbox **only for Kafka**; keep current Mongo upsert in pre-commit handlers **or** move Mongo upsert to a consumer that reads the same outbox payload (Option B-lite: one consumer does Mongo + Kafka in order).

## Implementation phases

1. **Schema:** `OutboxMessage` entity + EF configuration + migration. *(Done: `content.outbox_messages` in `InitialCreate` migration.)*
2. **Dispatch change:** Replace `IEventBus.PublishAsync` in domain event handlers with `IOutboxWriter.Enqueue` (same transaction as aggregates). *(Done for work-experience events.)*
3. **Publisher:** `OutboxPublisherHostedService` in Infrastructure; polls with `FOR UPDATE SKIP LOCKED`, publishes per message then marks `processed_utc`. Config: `Outbox:PollIntervalMs`, `Outbox:BatchSize`, `Outbox:ErrorBackoffMs`, `Outbox:PublisherEnabled` (set `false` in integration tests). `IEventBus` / `KafkaEventBus` kept for ad-hoc publishes.
4. **Observability:** Metrics for lag (`now - created_utc` for unprocessed), DLQ or dead-letter table for poison messages.
5. **Cleanup:** Job to archive/delete processed outbox rows older than N days.

## Dependencies

- Kafka producer reliability (acks, retries).
- Task 3.2+ (`KafkaEventBus`) may evolve into outbox publisher + thin producer wrapper.

## References

- Audit: [audit-tasks-2.2-2.3.md](sprint-2/audit-tasks-2.2-2.3.md)  
- Original note: [task-2.3-command-handlers.md](sprint-2/task-2.3-command-handlers.md)
