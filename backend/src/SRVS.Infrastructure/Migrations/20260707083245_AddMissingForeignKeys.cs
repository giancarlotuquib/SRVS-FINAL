using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRVS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "AspNetUserPasskeys",
                newName: "AspNetUserPasskeys",
                newSchema: "identity");

            migrationBuilder.CreateIndex(
                name: "IX_syllabus_versions_UploadedByUserId",
                table: "syllabus_versions",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_syllabus_assignments_AssignedBy",
                table: "syllabus_assignments",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_syllabi_OwnerUserId",
                table: "syllabi",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_syllabi_ReviewedByUserId",
                table: "syllabi",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_UserId",
                table: "audit_logs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_audit_logs_users_UserId",
                table: "audit_logs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_syllabi_users_OwnerUserId",
                table: "syllabi",
                column: "OwnerUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_syllabi_users_ReviewedByUserId",
                table: "syllabi",
                column: "ReviewedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_syllabus_assignments_syllabi_SyllabusDocId",
                table: "syllabus_assignments",
                column: "SyllabusDocId",
                principalTable: "syllabi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_syllabus_assignments_users_AssignedBy",
                table: "syllabus_assignments",
                column: "AssignedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_syllabus_assignments_users_StudentId",
                table: "syllabus_assignments",
                column: "StudentId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_syllabus_versions_users_UploadedByUserId",
                table: "syllabus_versions",
                column: "UploadedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_logs_users_UserId",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_syllabi_users_OwnerUserId",
                table: "syllabi");

            migrationBuilder.DropForeignKey(
                name: "FK_syllabi_users_ReviewedByUserId",
                table: "syllabi");

            migrationBuilder.DropForeignKey(
                name: "FK_syllabus_assignments_syllabi_SyllabusDocId",
                table: "syllabus_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_syllabus_assignments_users_AssignedBy",
                table: "syllabus_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_syllabus_assignments_users_StudentId",
                table: "syllabus_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_syllabus_versions_users_UploadedByUserId",
                table: "syllabus_versions");

            migrationBuilder.DropIndex(
                name: "IX_syllabus_versions_UploadedByUserId",
                table: "syllabus_versions");

            migrationBuilder.DropIndex(
                name: "IX_syllabus_assignments_AssignedBy",
                table: "syllabus_assignments");

            migrationBuilder.DropIndex(
                name: "IX_syllabi_OwnerUserId",
                table: "syllabi");

            migrationBuilder.DropIndex(
                name: "IX_syllabi_ReviewedByUserId",
                table: "syllabi");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_UserId",
                table: "audit_logs");

            migrationBuilder.RenameTable(
                name: "AspNetUserPasskeys",
                schema: "identity",
                newName: "AspNetUserPasskeys");
        }
    }
}
