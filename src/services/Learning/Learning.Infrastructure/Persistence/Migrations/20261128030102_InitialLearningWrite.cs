using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialLearningWrite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.UniqueConstraint("ak_categories_slug", x => x.slug);
                });

            migrationBuilder.CreateTable(
                name: "technologies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_technologies", x => x.id);
                    table.UniqueConstraint("ak_technologies_slug", x => x.slug);
                    table.ForeignKey(
                        name: "fk_technologies_categories_category_slug",
                        column: x => x.category_slug,
                        principalTable: "categories",
                        principalColumn: "slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    technology_slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    visibility = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courses", x => x.id);
                    table.UniqueConstraint("ak_courses_slug", x => x.slug);
                    table.ForeignKey(
                        name: "fk_courses_technologies_technology_slug",
                        column: x => x.technology_slug,
                        principalTable: "technologies",
                        principalColumn: "slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lessons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "jsonb", nullable: false),
                    course_slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    visibility = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lessons", x => x.id);
                    table.UniqueConstraint("ak_lessons_slug", x => x.slug);
                    table.ForeignKey(
                        name: "fk_lessons_courses_course_slug",
                        column: x => x.course_slug,
                        principalTable: "courses",
                        principalColumn: "slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_progress",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    student_id = table.Column<string>(type: "text", nullable: false),
                    lesson_slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_progress", x => x.id);
                    table.ForeignKey(
                        name: "fk_student_progress_lessons_lesson_slug",
                        column: x => x.lesson_slug,
                        principalTable: "lessons",
                        principalColumn: "slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_courses_technology_slug",
                table: "courses",
                column: "technology_slug");

            migrationBuilder.CreateIndex(
                name: "ix_lessons_course_slug",
                table: "lessons",
                column: "course_slug");

            migrationBuilder.CreateIndex(
                name: "ix_student_progress_lesson_slug",
                table: "student_progress",
                column: "lesson_slug");

            migrationBuilder.CreateIndex(
                name: "ix_student_progress_student_id_lesson_slug",
                table: "student_progress",
                columns: new[] { "student_id", "lesson_slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_technologies_category_slug",
                table: "technologies",
                column: "category_slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "student_progress");

            migrationBuilder.DropTable(
                name: "lessons");

            migrationBuilder.DropTable(
                name: "courses");

            migrationBuilder.DropTable(
                name: "technologies");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}