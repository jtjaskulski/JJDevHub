using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JJDevHub.Api.Data.Migrations;

/// <inheritdoc />
public partial class _20261022174856_IdentityEventStore : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AccountEmails",
            columns: table => new
            {
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccountEmails", x => x.Email);
            });

        migrationBuilder.CreateTable(
            name: "EventRecords",
            columns: table => new
            {
                Position = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                StreamId = table.Column<string>(type: "text", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                EventType = table.Column<string>(type: "text", nullable: false),
                Payload = table.Column<string>(type: "jsonb", nullable: false),
                DateOccurred = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventRecords", x => x.Position);
            });

        migrationBuilder.CreateTable(
            name: "EventStreams",
            columns: table => new
            {
                StreamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                IdentityGuid = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventStreams", x => x.StreamId);
            });

        migrationBuilder.CreateTable(
            name: "InboxMessages",
            columns: table => new
            {
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                Consumer = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                ProcessedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InboxMessages", x => new { x.EventId, x.Consumer });
            });

        migrationBuilder.CreateTable(
            name: "InviteCodeHashes",
            columns: table => new
            {
                CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                InviteId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InviteCodeHashes", x => x.CodeHash);
            });

        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                PartitionKey = table.Column<string>(type: "text", nullable: false),
                Payload = table.Column<string>(type: "jsonb", nullable: false),
                OccurredOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ProcessedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Attempts = table.Column<int>(type: "integer", nullable: false),
                LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxMessages", x => x.EventId);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AccountEmails_AccountId",
            table: "AccountEmails",
            column: "AccountId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EventRecords_EventId",
            table: "EventRecords",
            column: "EventId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EventRecords_StreamId_Version",
            table: "EventRecords",
            columns: new[] { "StreamId", "Version" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InviteCodeHashes_InviteId",
            table: "InviteCodeHashes",
            column: "InviteId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_CreatedOn",
            table: "OutboxMessages",
            column: "CreatedOn",
            filter: "\"ProcessedOn\" IS NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AccountEmails");

        migrationBuilder.DropTable(
            name: "EventRecords");

        migrationBuilder.DropTable(
            name: "EventStreams");

        migrationBuilder.DropTable(
            name: "InboxMessages");

        migrationBuilder.DropTable(
            name: "InviteCodeHashes");

        migrationBuilder.DropTable(
            name: "OutboxMessages");
    }
}
