using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRVS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergeInstitutionalIdToId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstitutionalId",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstitutionalId",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
