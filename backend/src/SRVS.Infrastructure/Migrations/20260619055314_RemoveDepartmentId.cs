using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRVS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDepartmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseAssignments_Departments_DepartmentId",
                table: "CourseAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrationRequests_Departments_DepartmentId",
                table: "RegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SyllabusDocuments_Departments_DepartmentId",
                table: "SyllabusDocuments");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_SyllabusDocuments_DepartmentId_Status",
                table: "SyllabusDocuments");

            migrationBuilder.DropIndex(
                name: "IX_RegistrationRequests_DepartmentId",
                table: "RegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_CourseAssignments_DepartmentId",
                table: "CourseAssignments");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "SyllabusDocuments");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "RegistrationRequests");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "CourseAssignments");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusDocuments_Status",
                table: "SyllabusDocuments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyllabusDocuments_Status",
                table: "SyllabusDocuments");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "SyllabusDocuments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "RegistrationRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "CourseAssignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusDocuments_DepartmentId_Status",
                table: "SyllabusDocuments",
                columns: new[] { "DepartmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequests_DepartmentId",
                table: "RegistrationRequests",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_DepartmentId",
                table: "CourseAssignments",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseAssignments_Departments_DepartmentId",
                table: "CourseAssignments",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrationRequests_Departments_DepartmentId",
                table: "RegistrationRequests",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SyllabusDocuments_Departments_DepartmentId",
                table: "SyllabusDocuments",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
