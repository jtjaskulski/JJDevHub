using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JJDevHub.News.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class NewsWriteOutbox : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
            name: "IX_OutboxMessages_CreatedOn",
            table: "OutboxMessages",
            column: "CreatedOn",
            filter: "\"ProcessedOn\" IS NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "InboxMessages");

        migrationBuilder.DropTable(
            name: "OutboxMessages");
    }
}
