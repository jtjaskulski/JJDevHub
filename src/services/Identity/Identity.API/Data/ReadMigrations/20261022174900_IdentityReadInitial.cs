using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JJDevHub.Api.Data.ReadMigrations;

/// <inheritdoc />
public partial class _20261022174900_IdentityReadInitial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AccountList",
            columns: table => new
            {
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Roles = table.Column<string[]>(type: "text[]", nullable: false),
                CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccountList", x => x.AccountId);
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

        migrationBuilder.CreateTable(
            name: "InviteList",
            columns: table => new
            {
                InviteId = table.Column<Guid>(type: "uuid", nullable: false),
                Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ExpiresOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                MaxUses = table.Column<int>(type: "integer", nullable: false),
                UsedCount = table.Column<int>(type: "integer", nullable: false),
                RevokedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InviteList", x => x.InviteId);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AccountList");

        migrationBuilder.DropTable(
            name: "Checkpoints");

        migrationBuilder.DropTable(
            name: "InviteList");
    }
}
