-- ====================================================================
-- SRVS Database Update Script for pgAdmin4
-- Safe schema update for syllabus_assignments table without dropping data
-- ====================================================================

-- 1. Add Department, Course Code (Subject Code), Course Title (Subject Title), Semester, and Academic Year
ALTER TABLE syllabus_assignments ADD COLUMN IF NOT EXISTS "DepartmentName" TEXT NOT NULL DEFAULT 'Computer Engineering';
ALTER TABLE syllabus_assignments ADD COLUMN IF NOT EXISTS "CourseCode" TEXT NOT NULL DEFAULT '';
ALTER TABLE syllabus_assignments ADD COLUMN IF NOT EXISTS "CourseTitle" TEXT NOT NULL DEFAULT '';
ALTER TABLE syllabus_assignments ADD COLUMN IF NOT EXISTS "Semester" TEXT NOT NULL DEFAULT '';
ALTER TABLE syllabus_assignments ADD COLUMN IF NOT EXISTS "AcademicYear" TEXT NOT NULL DEFAULT '';

-- 2. Create index on DepartmentName for fast department-scoped queries
CREATE INDEX IF NOT EXISTS "IX_syllabus_assignments_DepartmentName" ON syllabus_assignments ("DepartmentName");

-- 3. Backfill existing student assignment records with course details from syllabi table
UPDATE syllabus_assignments sa
SET 
    "DepartmentName" = s."DepartmentName",
    "CourseCode"     = s."CourseCode",
    "CourseTitle"    = s."CourseTitle",
    "Semester"       = s."Semester",
    "AcademicYear"   = s."AcademicYear"
FROM syllabi s
WHERE sa."SyllabusDocId" = s."Id"
  AND (sa."CourseCode" = '' OR sa."CourseTitle" = '');
