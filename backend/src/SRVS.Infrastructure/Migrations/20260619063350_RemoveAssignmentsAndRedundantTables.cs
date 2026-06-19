using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRVS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAssignmentsAndRedundantTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SyllabusDocuments_CourseAssignments_CourseAssignmentId",
                table: "SyllabusDocuments");

            migrationBuilder.DropTable(
                name: "CourseAssignments");

            migrationBuilder.DropTable(
                name: "RegistrationRequests");

            migrationBuilder.DropTable(
                name: "SyllabusAssignments");

            migrationBuilder.DropIndex(
                name: "IX_SyllabusDocuments_CourseAssignmentId",
                table: "SyllabusDocuments");

            migrationBuilder.DropColumn(
                name: "CourseAssignmentId",
                table: "SyllabusDocuments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CourseAssignmentId",
                table: "SyllabusDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CourseAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseCode = table.Column<string>(type: "text", nullable: false),
                    CourseTitle = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InstructorName = table.Column<string>(type: "text", nullable: false),
                    InstructorUserId = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    InstitutionalId = table.Column<string>(type: "text", nullable: false),
                    RequestedRole = table.Column<int>(type: "integer", nullable: false),
                    ReviewRemarks = table.Column<string>(type: "text", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyllabusAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SyllabusId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyllabusAssignments_AspNetUsers_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SyllabusAssignments_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SyllabusAssignments_SyllabusDocuments_SyllabusId",
                        column: x => x.SyllabusId,
                        principalTable: "SyllabusDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusDocuments_CourseAssignmentId",
                table: "SyllabusDocuments",
                column: "CourseAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusAssignments_AssignedBy",
                table: "SyllabusAssignments",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusAssignments_StudentId_IsActive",
                table: "SyllabusAssignments",
                columns: new[] { "StudentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusAssignments_SyllabusId",
                table: "SyllabusAssignments",
                column: "SyllabusId");

            migrationBuilder.AddForeignKey(
                name: "FK_SyllabusDocuments_CourseAssignments_CourseAssignmentId",
                table: "SyllabusDocuments",
                column: "CourseAssignmentId",
                principalTable: "CourseAssignments",
                principalColumn: "Id");
        }
    }
}
