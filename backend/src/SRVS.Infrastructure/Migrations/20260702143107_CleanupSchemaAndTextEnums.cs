using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRVS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanupSchemaAndTextEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "users");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "users");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "users");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "users");

            migrationBuilder.DropColumn(
                name: "NormalizedUserName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                schema: "identity",
                table: "roles");

            // ── Convert Role: integer → text with value mapping ──
            migrationBuilder.Sql(
                """
                ALTER TABLE users ALTER COLUMN "Role" TYPE text USING
                  CASE "Role"
                    WHEN 0 THEN 'Admin'
                    WHEN 1 THEN 'DepartmentHead'
                    WHEN 2 THEN 'Educator'
                    WHEN 3 THEN 'Viewer'
                    ELSE 'Viewer'
                  END;
                """);

            // ── Convert AccountStatus: integer → text with value mapping ──
            migrationBuilder.Sql(
                """
                ALTER TABLE users ALTER COLUMN "AccountStatus" TYPE text USING
                  CASE "AccountStatus"
                    WHEN 0 THEN 'PendingApproval'
                    WHEN 1 THEN 'Active'
                    WHEN 2 THEN 'Suspended'
                    WHEN 3 THEN 'Rejected'
                    WHEN 4 THEN 'Deleted'
                    ELSE 'PendingApproval'
                  END;
                """);

            // ── Convert StatusSnapshot: integer → text with value mapping ──
            migrationBuilder.Sql(
                """
                ALTER TABLE syllabus_versions ALTER COLUMN "StatusSnapshot" TYPE text USING
                  CASE "StatusSnapshot"
                    WHEN 0 THEN 'Draft'
                    WHEN 1 THEN 'Submitted'
                    WHEN 2 THEN 'Approved'
                    WHEN 3 THEN 'Rejected'
                    ELSE 'Draft'
                  END;
                """);

            // ── Convert Status: integer → text with value mapping ──
            migrationBuilder.Sql(
                """
                ALTER TABLE syllabi ALTER COLUMN "Status" TYPE text USING
                  CASE "Status"
                    WHEN 0 THEN 'Draft'
                    WHEN 1 THEN 'Submitted'
                    WHEN 2 THEN 'Approved'
                    WHEN 3 THEN 'Rejected'
                    ELSE 'Draft'
                  END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "AccountStatus",
                table: "users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUserName",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StatusSnapshot",
                table: "syllabus_versions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "syllabi",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                schema: "identity",
                table: "roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                schema: "identity",
                table: "roles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "roles",
                column: "NormalizedName",
                unique: true);
        }
    }
}
