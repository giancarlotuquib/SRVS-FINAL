BEGIN;

-- ==============================================================================
-- 1. SYLLABUS ASSIGNMENTS RELATIONSHIPS
--    These keys connect each assignment to a student, the person who assigned it, 
--    and the syllabus document itself.
-- ==============================================================================

-- A. Link the assignment to the Student (User)
ALTER TABLE syllabus_assignments
    ADD CONSTRAINT "FK_syllabus_assignments_users_StudentId" 
    FOREIGN KEY ("StudentId") 
    REFERENCES users("Id") 
    ON DELETE CASCADE;

-- B. Link the assignment to the Assigner (User)
ALTER TABLE syllabus_assignments
    ADD CONSTRAINT "FK_syllabus_assignments_users_AssignedBy" 
    FOREIGN KEY ("AssignedBy") 
    REFERENCES users("Id") 
    ON DELETE NO ACTION;

-- C. Link the assignment to the specific Syllabus Document
ALTER TABLE syllabus_assignments
    ADD CONSTRAINT "FK_syllabus_assignments_syllabi_SyllabusDocId" 
    FOREIGN KEY ("SyllabusDocId") 
    REFERENCES syllabi("Id") 
    ON DELETE CASCADE;


-- ==============================================================================
-- 2. SYLLABI RELATIONSHIPS
--    These keys connect the syllabus to its owner and the reviewer.
-- ==============================================================================

-- A. Link the syllabus to its Owner (User)
ALTER TABLE syllabi
    ADD CONSTRAINT "FK_syllabi_users_OwnerUserId" 
    FOREIGN KEY ("OwnerUserId") 
    REFERENCES users("Id") 
    ON DELETE CASCADE;

-- B. Link the syllabus to its Reviewer (User)
--    If the reviewer is deleted, the ID becomes NULL (keeps the syllabus intact).
ALTER TABLE syllabi
    ADD CONSTRAINT "FK_syllabi_users_ReviewedByUserId" 
    FOREIGN KEY ("ReviewedByUserId") 
    REFERENCES users("Id") 
    ON DELETE SET NULL;


-- ==============================================================================
-- 3. SYLLABUS VERSIONS RELATIONSHIPS
--    These keys connect version history to the person who uploaded the file.
-- ==============================================================================

-- A. Link the syllabus version to the Uploader (User)
ALTER TABLE syllabus_versions
    ADD CONSTRAINT "FK_syllabus_versions_users_UploadedByUserId" 
    FOREIGN KEY ("UploadedByUserId") 
    REFERENCES users("Id") 
    ON DELETE NO ACTION;


-- ==============================================================================
-- 4. AUDIT LOGS RELATIONSHIPS
--    These keys connect action logs to the user who performed the action.
-- ==============================================================================

-- A. Link the audit log entry to the User
--    If the user is deleted, we keep the log but clear the user ID.
ALTER TABLE audit_logs
    ADD CONSTRAINT "FK_audit_logs_users_UserId" 
    FOREIGN KEY ("UserId") 
    REFERENCES users("Id") 
    ON DELETE SET NULL;

COMMIT;
