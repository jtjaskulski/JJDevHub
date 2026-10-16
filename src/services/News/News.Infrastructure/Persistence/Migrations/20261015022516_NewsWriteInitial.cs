using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JJDevHub.News.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class NewsWriteInitial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "EventRecords");

        migrationBuilder.DropTable(
            name: "EventStreams");
    }
}
