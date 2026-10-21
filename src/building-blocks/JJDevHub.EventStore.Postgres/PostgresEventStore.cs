using JJDevHub.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace JJDevHub.EventStore.Postgres
{
    public sealed class PostgresEventStore<TContext>(
        TContext context,
        IEventSerializer serializer) : IEventStore
        where TContext : DbContext
    {
        public async Task<Result<int>> AppendAsync(
            string streamId,
            int expectedVersion,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            string? identityGuid,
            CancellationToken cancellationToken)
        {
            if (domainEvents.Count == 0)
            {
                return Result<int>.Fail(ResultError.Invalid);
            }

            bool ownsTransaction = context.Database.CurrentTransaction is null;
            if (ownsTransaction)
            {
                await context.Database.BeginTransactionAsync(cancellationToken);
            }

            bool committed = false;
            try
            {
                var result = await AppendInTransaction(
                    streamId,
                    expectedVersion,
                    domainEvents,
                    identityGuid,
                    cancellationToken);
                if (ownsTransaction && result.Succeeded)
                {
                    await context.Database.CommitTransactionAsync(cancellationToken);
                }

                committed = result.Succeeded;
                return result;
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                return Result<int>.Fail(ResultError.Conflict);
            }
            finally
            {
                if (ownsTransaction && !committed)
                {
                    await context.Database.RollbackTransactionAsync(cancellationToken);
                }
            }
        }

        public async Task<EventStreamRead> LoadAsync(string streamId, CancellationToken cancellationToken)
        {
            var version = await context.Set<EventStream>()
                .AsNoTracking()
                .Where(stream => stream.StreamId == streamId)
                .Select(stream => (int?)stream.Version)
                .FirstOrDefaultAsync(cancellationToken) ?? 0;

            var records = await context.Set<EventRecord>()
                .AsNoTracking()
                .Where(record => record.StreamId == streamId)
                .OrderBy(record => record.Version)
                .ToListAsync(cancellationToken);

            return new EventStreamRead(version, ToStored(records));
        }

        public async Task<IReadOnlyList<StoredEvent>> ReadAllAsync(
            long afterPosition, int maxCount, CancellationToken cancellationToken)
        {
            var records = await context.Set<EventRecord>()
                .AsNoTracking()
                .Where(record => record.Position > afterPosition)
                .OrderBy(record => record.Position)
                .Take(maxCount)
                .ToListAsync(cancellationToken);

            return ToStored(records);
        }

        public async Task<long> ReadHeadPositionAsync(CancellationToken cancellationToken)
        {
            var head = await context.Set<EventRecord>()
                .AsNoTracking()
                .MaxAsync(record => (long?)record.Position, cancellationToken);
            return head ?? 0;
        }

        private async Task<Result<int>> AppendInTransaction(
            string streamId,
            int expectedVersion,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            string? identityGuid,
            CancellationToken cancellationToken)
        {
            await context.Database.ExecuteSqlAsync(
                $"SELECT pg_advisory_xact_lock({EventStoreLock.Key})",
                cancellationToken);

            var stream = await context.Set<EventStream>()
                .FirstOrDefaultAsync(item => item.StreamId == streamId, cancellationToken);
            if (IsConflict(stream, expectedVersion))
            {
                return Result<int>.Fail(ResultError.Conflict);
            }

            if (stream is null)
            {
                stream = new EventStream
                {
                    StreamId = streamId,
                    IdentityGuid = identityGuid,
                };
                context.Set<EventStream>().Add(stream);
            }

            AddRecords(streamId, expectedVersion, domainEvents);
            stream.Version = expectedVersion + domainEvents.Count;
            await context.SaveChangesAsync(cancellationToken);
            return Result<int>.Ok(stream.Version);
        }

        private void AddRecords(
            string streamId,
            int expectedVersion,
            IReadOnlyCollection<IDomainEvent> domainEvents)
        {
            int index = 0;
            foreach (var domainEvent in domainEvents)
            {
                var serialized = serializer.Serialize(domainEvent);
                context.Set<EventRecord>().Add(new EventRecord
                {
                    StreamId = streamId,
                    Version = expectedVersion + index + 1,
                    EventId = domainEvent.EventId,
                    EventType = serialized.Name,
                    Payload = serialized.Json,
                    DateOccurred = domainEvent.DateOccurred,
                });
                index++;
            }
        }

        private List<StoredEvent> ToStored(List<EventRecord> records)
        {
            var stored = new List<StoredEvent>(records.Count);
            foreach (var record in records)
            {
                var domainEvent = serializer.Deserialize(record.EventType, record.Payload);
                stored.Add(new StoredEvent(record.Position, record.StreamId, record.Version, domainEvent));
            }

            return stored;
        }

        private static bool IsConflict(EventStream? stream, int expectedVersion)
        {
            if (stream is null)
            {
                return expectedVersion != 0;
            }

            return stream.Version != expectedVersion;
        }

        private static bool IsUniqueViolation(DbUpdateException exception)
        {
            for (Exception? current = exception.InnerException; current is not null; current = current.InnerException)
            {
                if (current is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    return true;
                }
            }

            return false;
        }
    }
}