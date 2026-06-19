using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRVS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveIdentityTablesToSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.RenameTable(
                name: "user_tokens",
                newName: "user_tokens",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_roles",
                newName: "user_roles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_logins",
                newName: "user_logins",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_claims",
                newName: "user_claims",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "roles",
                newName: "roles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "role_claims",
                newName: "role_claims",
                newSchema: "identity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "user_tokens",
                schema: "identity",
                newName: "user_tokens");

            migrationBuilder.RenameTable(
                name: "user_roles",
                schema: "identity",
                newName: "user_roles");

            migrationBuilder.RenameTable(
                name: "user_logins",
                schema: "identity",
                newName: "user_logins");

            migrationBuilder.RenameTable(
                name: "user_claims",
                schema: "identity",
                newName: "user_claims");

            migrationBuilder.RenameTable(
                name: "roles",
                schema: "identity",
                newName: "roles");

            migrationBuilder.RenameTable(
                name: "role_claims",
                schema: "identity",
                newName: "role_claims");
        }
    }
}
