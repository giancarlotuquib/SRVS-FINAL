using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRVS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserRoleAndNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            // DATA MIGRATION: Migrate Viewer role to Student in database
            migrationBuilder.Sql("UPDATE users SET \"Role\" = 'Student' WHERE \"Role\" = 'Viewer';");

            // DATA MIGRATION: Populate FirstName and LastName by splitting FullName
            migrationBuilder.Sql("UPDATE users SET \"FirstName\" = split_part(\"FullName\", ' ', 1), \"LastName\" = CASE WHEN position(' ' in \"FullName\") > 0 THEN substring(\"FullName\" from position(' ' in \"FullName\") + 1) ELSE '' END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "users");
        }
    }
}
