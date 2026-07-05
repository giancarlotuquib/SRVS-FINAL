CREATE TABLE IF NOT EXISTS syllabus_assignments (
    "Id" uuid NOT NULL,
    "StudentId" text NOT NULL,
    "SyllabusId" uuid NOT NULL,
    "AssignedBy" text NOT NULL,
    "AssignedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_syllabus_assignments" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_syllabus_assignments_StudentId_IsActive" ON syllabus_assignments ("StudentId", "IsActive");
CREATE INDEX IF NOT EXISTS "IX_syllabus_assignments_SyllabusId" ON syllabus_assignments ("SyllabusId");
