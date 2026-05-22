using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRVS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyllabusAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                // Only create the new SyllabusAssignments table to avoid recreating existing schema
                migrationBuilder.CreateTable(
                    name: "SyllabusAssignments",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        StudentId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                        SyllabusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        AssignedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                        AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        IsActive = table.Column<bool>(type: "bit", nullable: false),
                        DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                        CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_SyllabusAssignments", x => x.Id);
                    });
            // Other existing tables/indexes are already present in DB; skipped to avoid conflicts
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyllabusAssignments");
        }
    }
}
