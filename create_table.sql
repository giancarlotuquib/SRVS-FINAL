-- ============================================================
-- SRVS (Syllabus Repository & Versioning System)
-- PostgreSQL Schema for ERD in pgAdmin4 (Multi-Engineering Department)
-- ============================================================

-- Drop all existing tables (including old orphan syllabus_versions)
DROP TABLE IF EXISTS syllabus_assignments CASCADE;
DROP TABLE IF EXISTS syllabus_versions CASCADE;
DROP TABLE IF EXISTS syllabi CASCADE;
DROP TABLE IF EXISTS audit_logs CASCADE;
DROP TABLE IF EXISTS users CASCADE;

-- Optional drop of legacy identity schema if present
DROP SCHEMA IF EXISTS identity CASCADE;

-- ============================================================
-- 1. USERS
-- ============================================================
CREATE TABLE users (
    "Id"                BIGINT          NOT NULL,
    "UserName"          TEXT,
    "Email"             TEXT,
    "PasswordHash"      TEXT,
    "FirstName"         TEXT            NOT NULL DEFAULT '',
    "LastName"          TEXT            NOT NULL DEFAULT '',
    "FullName"          TEXT            NOT NULL DEFAULT '',
    "DepartmentName"    TEXT            NOT NULL DEFAULT 'Computer Engineering',
    "Role"              TEXT            NOT NULL DEFAULT 'Student',
    "AccountStatus"     TEXT            NOT NULL DEFAULT 'PendingApproval',
    "CreatedAtUtc"      DATE            NOT NULL DEFAULT CURRENT_DATE,
    "LastLoginAtUtc"    DATE,
    CONSTRAINT "PK_users" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_users_DepartmentName" ON users ("DepartmentName");

-- ============================================================
-- 2. SYLLABUS DOCUMENTS
-- ============================================================
CREATE TABLE syllabi (
    "Id"                    UUID            NOT NULL DEFAULT gen_random_uuid(),
    "CourseCode"            TEXT            NOT NULL DEFAULT '',
    "CourseTitle"           TEXT            NOT NULL DEFAULT '',
    "AcademicYear"          TEXT            NOT NULL DEFAULT '',
    "Semester"              TEXT            NOT NULL DEFAULT '',
    "DepartmentName"        TEXT            NOT NULL DEFAULT 'Computer Engineering',
    "InstructorId"          BIGINT          NOT NULL,
    "OwnerUserId"           BIGINT          NOT NULL,
    "Status"                TEXT            NOT NULL DEFAULT 'Draft',
    "CurrentVersionNumber"  INTEGER         NOT NULL DEFAULT 1,
    "LatestChangeSummary"   TEXT,
    "ReviewerRemarks"       TEXT,
    "CurrentFileName"       TEXT            NOT NULL DEFAULT '',
    "CurrentStoragePath"    TEXT            NOT NULL DEFAULT '',
    "IsPublished"           BOOLEAN         NOT NULL DEFAULT FALSE,
    "SubmittedAtUtc"        DATE,
    "ReviewedAtUtc"         DATE,
    "ReviewedByUserId"      BIGINT,
    "CreatedAtUtc"          DATE            NOT NULL DEFAULT CURRENT_DATE,
    "UpdatedAtUtc"          DATE,
    CONSTRAINT "PK_syllabi" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_syllabi_InstructorId" FOREIGN KEY ("InstructorId") REFERENCES users ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_syllabi_OwnerUserId" FOREIGN KEY ("OwnerUserId") REFERENCES users ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_syllabi_ReviewedByUserId" FOREIGN KEY ("ReviewedByUserId") REFERENCES users ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_syllabi_DepartmentName" ON syllabi ("DepartmentName");
CREATE INDEX "IX_syllabi_Status" ON syllabi ("Status");

-- ============================================================
-- 3. SYLLABUS ASSIGNMENTS
-- ============================================================
CREATE TABLE syllabus_assignments (
    "Id"                UUID            NOT NULL DEFAULT gen_random_uuid(),
    "StudentId"         BIGINT          NOT NULL,
    "StudentFullName"   TEXT            NOT NULL,
    "SyllabusDocId"     UUID            NOT NULL,
    "DepartmentName"    TEXT            NOT NULL DEFAULT 'Computer Engineering',
    "CourseCode"        TEXT            NOT NULL DEFAULT '',
    "CourseTitle"       TEXT            NOT NULL DEFAULT '',
    "Semester"          TEXT            NOT NULL DEFAULT '',
    "AcademicYear"      TEXT            NOT NULL DEFAULT '',
    "AssignedBy"        BIGINT          NOT NULL,
    "AssignedAtDate"    DATE            NOT NULL DEFAULT CURRENT_DATE,
    "IsActive"          BOOLEAN         NOT NULL DEFAULT TRUE,
    "DeletedAt"         DATE,
    "CreatedAtUtc"      DATE            NOT NULL DEFAULT CURRENT_DATE,
    "UpdatedAtUtc"      DATE,
    CONSTRAINT "PK_syllabus_assignments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_syllabus_assignments_StudentId" FOREIGN KEY ("StudentId") REFERENCES users ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_syllabus_assignments_AssignedBy" FOREIGN KEY ("AssignedBy") REFERENCES users ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_syllabus_assignments_SyllabusDocId" FOREIGN KEY ("SyllabusDocId") REFERENCES syllabi ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_syllabus_assignments_StudentId_IsActive" ON syllabus_assignments ("StudentId", "IsActive");
CREATE INDEX "IX_syllabus_assignments_SyllabusDocId" ON syllabus_assignments ("SyllabusDocId");
CREATE INDEX "IX_syllabus_assignments_DepartmentName" ON syllabus_assignments ("DepartmentName");

-- ============================================================
-- 4. AUDIT LOGS
-- ============================================================
CREATE TABLE audit_logs (
    "Id"                UUID            NOT NULL DEFAULT gen_random_uuid(),
    "UserId"            BIGINT,
    "UserDisplayName"   TEXT,
    "ActionType"        TEXT            NOT NULL DEFAULT 'LoginAttempt',
    "ResultStatus"      TEXT            NOT NULL DEFAULT 'Success',
    "Description"       TEXT            NOT NULL DEFAULT '',
    "EntityType"        TEXT,
    "EntityId"          TEXT,
    "IpAddress"         TEXT,
    "CreatedAtUtc"      DATE            NOT NULL DEFAULT CURRENT_DATE,
    "UpdatedAtUtc"      DATE,
    CONSTRAINT "PK_audit_logs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_audit_logs_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE SET NULL
);
