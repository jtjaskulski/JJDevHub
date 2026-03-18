# Transactional Outbox — Kafka decoupling (backlog plan)

## Problem

Today, domain event handlers run inside `SaveChangesAsync` **before** PostgreSQL commit:

1. Handlers upsert MongoDB and publish to Kafka.
2. Then EF commits the write model.

If **Kafka or MongoDB fails**, the transaction rolls back — good.  
If handlers **succeed** but **PostgreSQL commit fails** (timeout, constraint, etc.), the read model and downstream consumers can see events that never persisted in the write DB — **inconsistent**.

Running handlers **after** commit fixes PG-first consistency but breaks the opposite case (PG committed, Kafka down). True decoupling needs **at-least-once delivery** aligned with DB commit.

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
