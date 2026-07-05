using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRVS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentFullNameToAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StudentFullName",
                table: "syllabus_assignments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StudentFullName",
                table: "syllabus_assignments");
        }
    }
}
