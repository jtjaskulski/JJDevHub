using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JJDevHub.Content.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCurriculumVitae : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "curriculum_vitae",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bio = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    work_experience_ids = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    modified_by_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_curriculum_vitae", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cv_educations",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    curriculum_vitae_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Institution = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FieldOfStudy = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Degree = table.Column<int>(type: "integer", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cv_educations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cv_educations_curriculum_vitae_curriculum_vitae_id",
                        column: x => x.curriculum_vitae_id,
                        principalSchema: "content",
                        principalTable: "curriculum_vitae",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cv_projects",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    curriculum_vitae_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    technologies = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cv_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cv_projects_curriculum_vitae_curriculum_vitae_id",
                        column: x => x.curriculum_vitae_id,
                        principalSchema: "content",
                        principalTable: "curriculum_vitae",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cv_skills",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    curriculum_vitae_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    skill_level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cv_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cv_skills_curriculum_vitae_curriculum_vitae_id",
                        column: x => x.curriculum_vitae_id,
                        principalSchema: "content",
                        principalTable: "curriculum_vitae",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cv_educations_curriculum_vitae_id",
                schema: "content",
                table: "cv_educations",
                column: "curriculum_vitae_id");

            migrationBuilder.CreateIndex(
                name: "IX_cv_projects_curriculum_vitae_id",
                schema: "content",
                table: "cv_projects",
                column: "curriculum_vitae_id");

            migrationBuilder.CreateIndex(
                name: "IX_cv_skills_curriculum_vitae_id",
                schema: "content",
                table: "cv_skills",
                column: "curriculum_vitae_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cv_educations",
                schema: "content");

            migrationBuilder.DropTable(
                name: "cv_projects",
                schema: "content");

            migrationBuilder.DropTable(
                name: "cv_skills",
                schema: "content");

            migrationBuilder.DropTable(
                name: "curriculum_vitae",
                schema: "content");
        }
    }
}
