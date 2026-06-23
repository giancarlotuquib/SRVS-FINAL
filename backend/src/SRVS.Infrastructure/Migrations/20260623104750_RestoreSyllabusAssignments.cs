using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRVS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestoreSyllabusAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "syllabus_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    SyllabusId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedBy = table.Column<string>(type: "text", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_syllabus_assignments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_syllabus_assignments_StudentId_IsActive",
                table: "syllabus_assignments",
                columns: new[] { "StudentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_syllabus_assignments_SyllabusId",
                table: "syllabus_assignments",
                column: "SyllabusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "syllabus_assignments");
        }
    }
}
