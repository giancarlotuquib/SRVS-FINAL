using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRVS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSyllabusAssignmentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_syllabus_assignments_SyllabusId",
                table: "syllabus_assignments");

            migrationBuilder.AlterColumn<string>(
                name: "SyllabusId",
                table: "syllabus_assignments",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedAt",
                table: "syllabus_assignments",
                type: "text",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AssignedAtDate",
                table: "syllabus_assignments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "SyllabusDocId",
                table: "syllabus_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_syllabus_assignments_SyllabusDocId",
                table: "syllabus_assignments",
                column: "SyllabusDocId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_syllabus_assignments_SyllabusDocId",
                table: "syllabus_assignments");

            migrationBuilder.DropColumn(
                name: "AssignedAtDate",
                table: "syllabus_assignments");

            migrationBuilder.DropColumn(
                name: "SyllabusDocId",
                table: "syllabus_assignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "SyllabusId",
                table: "syllabus_assignments",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AssignedAt",
                table: "syllabus_assignments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_syllabus_assignments_SyllabusId",
                table: "syllabus_assignments",
                column: "SyllabusId");
        }
    }
}
