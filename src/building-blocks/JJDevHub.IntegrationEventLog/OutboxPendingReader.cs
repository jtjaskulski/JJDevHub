using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JJDevHub.IntegrationEventLog
{

    internal static class OutboxPendingReader
    {
        public static async Task<List<OutboxMessage>> LoadAsync(
            DbContext context,
            int maxAttempts,
            int batchSize,
            CancellationToken cancellationToken)
        {
            await using var command = CreateCommand(context, maxAttempts, batchSize);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await ReadAllAsync(reader, cancellationToken);
        }

        private static DbCommand CreateCommand(DbContext context, int maxAttempts, int batchSize)
        {
            var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            if (transaction is null)
            {
                throw new InvalidOperationException("Outbox processor requires an active transaction.");
            }

            var command = context.Database.GetDbConnection().CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                SELECT * FROM "OutboxMessages"
                WHERE "ProcessedOn" IS NULL AND "Attempts" < @maxAttempts
                ORDER BY "CreatedOn"
                LIMIT @batchSize
                FOR UPDATE SKIP LOCKED
                """;
            AddParameter(command, "maxAttempts", maxAttempts);
            AddParameter(command, "batchSize", batchSize);
            return command;
        }

        private static async Task<List<OutboxMessage>> ReadAllAsync(
            DbDataReader reader,
            CancellationToken cancellationToken)
        {
            var pending = new List<OutboxMessage>();
            while (await reader.ReadAsync(cancellationToken))
            {
                pending.Add(Read(reader));
            }

            return pending;
        }

        private static void AddParameter(DbCommand command, string name, int value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        private static OutboxMessage Read(DbDataReader reader) =>
            new()
            {
                EventId = reader.GetGuid(reader.GetOrdinal("EventId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                PartitionKey = reader.GetString(reader.GetOrdinal("PartitionKey")),
                Payload = ReadText(reader, "Payload"),
                OccurredOn = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("OccurredOn")),
                CreatedOn = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedOn")),
                ProcessedOn = ReadProcessedOn(reader),
                Attempts = reader.GetInt32(reader.GetOrdinal("Attempts")),
                LastError = ReadOptionalText(reader, "LastError"),
            };

        private static DateTimeOffset? ReadProcessedOn(DbDataReader reader)
        {
            var ordinal = reader.GetOrdinal("ProcessedOn");
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return reader.GetFieldValue<DateTimeOffset>(ordinal);
        }

        private static string ReadText(DbDataReader reader, string name) =>
            reader.GetValue(reader.GetOrdinal(name)).ToString() ?? "";

        private static string? ReadOptionalText(DbDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return reader.GetValue(ordinal).ToString();
        }
    }
}
