-- ============================================================
-- SRVS (Syllabus Repository & Versioning System)
-- PostgreSQL Schema for ERD in pgAdmin4 (Clean Rebuild)
-- ============================================================

-- Drop existing tables if re-running script to ensure clean ERD generation
DROP TABLE IF EXISTS syllabus_assignments CASCADE;
DROP TABLE IF EXISTS syllabus_versions CASCADE;
DROP TABLE IF EXISTS syllabi CASCADE;
DROP TABLE IF EXISTS audit_logs CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS identity.user_roles CASCADE;
DROP TABLE IF EXISTS identity.user_claims CASCADE;
DROP TABLE IF EXISTS identity.user_logins CASCADE;
DROP TABLE IF EXISTS identity.user_tokens CASCADE;
DROP TABLE IF EXISTS identity.role_claims CASCADE;
DROP TABLE IF EXISTS identity.roles CASCADE;
DROP SCHEMA IF EXISTS identity CASCADE;

-- ============================================================
-- 1. USERS
-- ============================================================
CREATE TABLE users (
    "Id"                TEXT            NOT NULL,
    "UserName"          TEXT,
    "Email"             TEXT,
    "PasswordHash"      TEXT,
    "FirstName"         TEXT            NOT NULL DEFAULT '',
    "LastName"          TEXT            NOT NULL DEFAULT '',
    "FullName"          TEXT            NOT NULL DEFAULT '',
    "Role"              TEXT            NOT NULL DEFAULT 'Student',
    "AccountStatus"     TEXT            NOT NULL DEFAULT 'PendingApproval',
    "CreatedAtUtc"      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "LastLoginAtUtc"    TIMESTAMPTZ,
    CONSTRAINT "PK_users" PRIMARY KEY ("Id")
);

-- ============================================================
-- 2. SYLLABUS DOCUMENTS
-- ============================================================
CREATE TABLE syllabi (
    "Id"                    UUID            NOT NULL DEFAULT gen_random_uuid(),
    "CourseCode"            TEXT            NOT NULL DEFAULT '',
    "CourseTitle"           TEXT            NOT NULL DEFAULT '',
    "AcademicYear"          TEXT            NOT NULL DEFAULT '',
    "Semester"              TEXT            NOT NULL DEFAULT '',
    "InstructorName"        TEXT            NOT NULL DEFAULT '',
    "OwnerUserId"           TEXT            NOT NULL DEFAULT '',
    "Status"                TEXT            NOT NULL DEFAULT 'Draft',
    "CurrentVersionNumber"  INTEGER         NOT NULL DEFAULT 1,
    "LatestChangeSummary"   TEXT,
    "ReviewerRemarks"       TEXT,
    "CurrentFileName"       TEXT            NOT NULL DEFAULT '',
    "CurrentStoragePath"    TEXT            NOT NULL DEFAULT '',
    "IsPublished"           BOOLEAN         NOT NULL DEFAULT FALSE,
    "SubmittedAtUtc"        TIMESTAMPTZ,
    "ReviewedAtUtc"         TIMESTAMPTZ,
    "ReviewedByUserId"      TEXT,
    "CreatedAtUtc"          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc"          TIMESTAMPTZ,
    CONSTRAINT "PK_syllabi" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_syllabi_OwnerUserId" FOREIGN KEY ("OwnerUserId") REFERENCES users ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_syllabi_ReviewedByUserId" FOREIGN KEY ("ReviewedByUserId") REFERENCES users ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_syllabi_Status" ON syllabi ("Status");

-- ============================================================
-- 3. SYLLABUS VERSIONS
-- ============================================================
CREATE TABLE syllabus_versions (
    "Id"                    UUID            NOT NULL DEFAULT gen_random_uuid(),
    "SyllabusDocumentId"    UUID            NOT NULL,
    "VersionNumber"         INTEGER         NOT NULL,
    "FileName"              TEXT            NOT NULL DEFAULT '',
    "StoragePath"           TEXT            NOT NULL DEFAULT '',
    "UploadedByUserId"      TEXT            NOT NULL DEFAULT '',
    "UploadedByName"        TEXT            NOT NULL DEFAULT '',
    "ChangeSummary"         TEXT            NOT NULL DEFAULT '',
    "StatusSnapshot"        TEXT            NOT NULL DEFAULT 'Draft',
    "UploadedAtUtc"         TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedAtUtc"          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc"          TIMESTAMPTZ,
    CONSTRAINT "PK_syllabus_versions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_syllabus_versions_SyllabusDocumentId" FOREIGN KEY ("SyllabusDocumentId") REFERENCES syllabi ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_syllabus_versions_UploadedByUserId" FOREIGN KEY ("UploadedByUserId") REFERENCES users ("Id") ON DELETE RESTRICT
);

-- ============================================================
-- 4. SYLLABUS ASSIGNMENTS
-- ============================================================
CREATE TABLE syllabus_assignments (
    "Id"                UUID            NOT NULL DEFAULT gen_random_uuid(),
    "StudentId"         TEXT            NOT NULL,
    "StudentFullName"   TEXT            NOT NULL,
    "SyllabusDocId"     UUID            NOT NULL,
    "AssignedBy"        TEXT            NOT NULL,
    "AssignedAtDate"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "IsActive"          BOOLEAN         NOT NULL DEFAULT TRUE,
    "DeletedAt"         TIMESTAMPTZ,
    "CreatedAtUtc"      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc"      TIMESTAMPTZ,
    CONSTRAINT "PK_syllabus_assignments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_syllabus_assignments_StudentId" FOREIGN KEY ("StudentId") REFERENCES users ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_syllabus_assignments_AssignedBy" FOREIGN KEY ("AssignedBy") REFERENCES users ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_syllabus_assignments_SyllabusDocId" FOREIGN KEY ("SyllabusDocId") REFERENCES syllabi ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_syllabus_assignments_StudentId_IsActive" ON syllabus_assignments ("StudentId", "IsActive");
CREATE INDEX "IX_syllabus_assignments_SyllabusDocId" ON syllabus_assignments ("SyllabusDocId");

-- ============================================================
-- 5. AUDIT LOGS
-- ============================================================
CREATE TABLE audit_logs (
    "Id"                UUID            NOT NULL DEFAULT gen_random_uuid(),
    "UserId"            TEXT,
    "UserDisplayName"   TEXT,
    "ActionType"        TEXT            NOT NULL DEFAULT 'LoginAttempt',
    "ResultStatus"      TEXT            NOT NULL DEFAULT 'Success',
    "Description"       TEXT            NOT NULL DEFAULT '',
    "EntityType"        TEXT,
    "EntityId"          TEXT,
    "IpAddress"         TEXT,
    "CreatedAtUtc"      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc"      TIMESTAMPTZ,
    CONSTRAINT "PK_audit_logs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_audit_logs_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE SET NULL
);
