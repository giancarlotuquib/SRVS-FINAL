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
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[SyllabusAssignments]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [SyllabusAssignments] (
                        [Id] uniqueidentifier NOT NULL,
                        [StudentId] nvarchar(450) NOT NULL,
                        [SyllabusId] uniqueidentifier NOT NULL,
                        [AssignedBy] nvarchar(450) NOT NULL,
                        [AssignedAt] datetimeoffset NOT NULL,
                        [IsActive] bit NOT NULL,
                        [DeletedAt] datetimeoffset NULL,
                        [CreatedAtUtc] datetimeoffset NOT NULL,
                        [UpdatedAtUtc] datetimeoffset NULL,
                        CONSTRAINT [PK_SyllabusAssignments] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SyllabusAssignments_AspNetUsers_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_SyllabusAssignments_AspNetUsers_AssignedBy] FOREIGN KEY ([AssignedBy]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_SyllabusAssignments_SyllabusDocuments_SyllabusId] FOREIGN KEY ([SyllabusId]) REFERENCES [SyllabusDocuments] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_SyllabusAssignments_StudentId_IsActive] ON [SyllabusAssignments] ([StudentId], [IsActive]);
                    CREATE INDEX [IX_SyllabusAssignments_SyllabusId] ON [SyllabusAssignments] ([SyllabusId]);
                    CREATE INDEX [IX_SyllabusAssignments_AssignedBy] ON [SyllabusAssignments] ([AssignedBy]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[SyllabusAssignments]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [SyllabusAssignments];
                END
                """);
        }
    }
}
