using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JJDevHub.News.Infrastructure.Read.Migrations;

/// <inheritdoc />
public partial class _20261021064031_NewsReadInitial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ArticleSummaries",
            columns: table => new
            {
                ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                Body = table.Column<string>(type: "text", nullable: false),
                AuthorIdentityGuid = table.Column<string>(type: "text", nullable: true),
                CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevisedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ArticleSummaries", x => x.ArticleId);
            });

        migrationBuilder.CreateTable(
            name: "Checkpoints",
            columns: table => new
            {
                ProjectionName = table.Column<string>(type: "text", nullable: false),
                Position = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Checkpoints", x => x.ProjectionName);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ArticleSummaries_CreatedOn",
            table: "ArticleSummaries",
            column: "CreatedOn",
            descending: new bool[0]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ArticleSummaries");

        migrationBuilder.DropTable(
            name: "Checkpoints");
    }
}
