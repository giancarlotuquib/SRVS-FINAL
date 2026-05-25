# SRVS Database Schema

## Overview

SRVS (Syllabus Repository and Verification System) uses Entity Framework Core with SQL Server as the database provider. The database consists of ASP.NET Core Identity tables for authentication and authorization, along with custom domain entities for syllabus management.

## Entity Relationship Diagram

```
┌─────────────────┐
│  Departments    │
├─────────────────┤
│ Id (PK)         │
│ Code            │
│ Name            │
│ IsActive        │
│ CreatedAtUtc    │
│ UpdatedAtUtc    │
└────────┬────────┘
         │
         │ 1
         │
         │ *
    ┌────┴─────────────────────────────────────────────────────┐
    │                                                          │
    │ *                                                        │ *
┌───┴────────┐                                        ┌────────┴────┐
│ Course     │                                        │ Syllabus    │
│ Assignment │                                        │ Documents   │
├────────────┤                                        ├─────────────┤
│ Id (PK)    │                                        │ Id (PK)     │
│ DeptId (FK)│                                        │ DeptId (FK) │
│ CourseCode │                                        │ CourseCode  │
│ CourseTitle│                                        │ CourseTitle │
│ Instructor │                                        │ AcademicYear│
│ IsActive   │                                        │ Semester    │
│ CreatedAt  │                                        │ Instructor  │
│ UpdatedAt  │                                        │ OwnerUserId │
└────────────┘                                        │ Status      │
                                                       │ IsPublished │
                                                       │ SubmittedAt │
                                                       │ ReviewedAt  │
                                                       │ CreatedAt   │
                                                       │ UpdatedAt   │
                                                       └──────┬──────┘
                                                              │
                                                              │ 1
                                                              │
                                                              │ *
                                                       ┌──────┴────────┐
                                                       │ Syllabus      │
                                                       │ Versions      │
                                                       ├───────────────┤
                                                       │ Id (PK)       │
                                                       │ DocId (FK)    │
                                                       │ VersionNumber │
                                                       │ FileName      │
                                                       │ StoragePath   │
                                                       │ UploadedBy    │
                                                       │ ChangeSummary │
                                                       │ UploadedAt    │
                                                       └───────────────┘

┌─────────────────┐         ┌─────────────────┐
│ ApplicationUser│         │ Registration    │
│ (IdentityUser) │         │ Requests       │
├─────────────────┤         ├─────────────────┤
│ Id (PK)         │         │ Id (PK)         │
│ Email           │         │ FullName        │
│ FullName        │         │ Email           │
│ InstitutionalId│         │ InstitutionalId │
│ Role            │         │ RequestedRole   │
│ DeptId (FK)     │         │ DeptId (FK)     │
│ AccountStatus   │         │ Status          │
│ CreatedAtUtc    │         │ ReviewRemarks   │
│ LastLoginAtUtc  │         │ ReviewedBy      │
└────────┬────────┘         │ ReviewedAt      │
         │                  │ CreatedAt       │
         │                  └─────────────────┘
         │
         │ *
    ┌────┴────────┐
    │ Syllabus    │
    │ Assignment  │
    ├─────────────┤
    │ Id (PK)     │
    │ StudentId(FK)│
    │ SyllabusId(FK)│
    │ AssignedBy   │
    │ AssignedAt   │
    │ IsActive     │
    │ DeletedAt    │
    └─────────────┘
```

## Tables

### 1. ApplicationUser (ASP.NET Core Identity)

Extends `IdentityUser` with custom properties for SRVS-specific user management.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | string (PK) | No | Unique identifier (inherited from IdentityUser) |
| UserName | string | No | Username (inherited from IdentityUser) |
| NormalizedUserName | string | No | Normalized username (inherited from IdentityUser) |
| Email | string | No | Email address (inherited from IdentityUser) |
| NormalizedEmail | string | No | Normalized email (inherited from IdentityUser) |
| EmailConfirmed | bool | No | Email confirmation status (inherited from IdentityUser) |
| PasswordHash | string | Yes | Password hash (inherited from IdentityUser) |
| SecurityStamp | string | No | Security stamp (inherited from IdentityUser) |
| ConcurrencyStamp | string | No | Concurrency stamp (inherited from IdentityUser) |
| PhoneNumber | string | Yes | Phone number (inherited from IdentityUser) |
| PhoneNumberConfirmed | bool | No | Phone number confirmed (inherited from IdentityUser) |
| TwoFactorEnabled | bool | No | Two-factor authentication enabled (inherited from IdentityUser) |
| LockoutEnd | DateTimeOffset? | Yes | Lockout end date (inherited from IdentityUser) |
| LockoutEnabled | bool | No | Lockout enabled (inherited from IdentityUser) |
| AccessFailedCount | int | No | Access failed count (inherited from IdentityUser) |
| **FullName** | string | No | User's full name (custom) |
| **InstitutionalId** | string | No | Institutional ID (5 digits for Admin/DeptHead/Educator, 10 digits for Student) |
| **Role** | UserRoleType | No | User role (Admin, DepartmentHead, Educator, Viewer) |
| **DepartmentId** | Guid? | Yes | Foreign key to Department |
| **AccountStatus** | UserAccountStatus | No | Account status (PendingApproval, Active, Suspended, Rejected, Deleted) |
| **CreatedAtUtc** | DateTimeOffset | No | Account creation timestamp |
| **LastLoginAtUtc** | DateTimeOffset? | Yes | Last login timestamp |

**Enums:**
- `UserRoleType`: Admin (0), DepartmentHead (1), Educator (2), Viewer (3)
- `UserAccountStatus`: PendingApproval (0), Active (1), Suspended (2), Rejected (3), Deleted (4)

---

### 2. Department

Represents academic departments in the institution.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | Guid (PK) | No | Unique identifier |
| Code | string | No | Department code (e.g., "CE", "CS") |
| Name | string | No | Department name (e.g., "Computer Engineering") |
| IsActive | bool | No | Active status |
| CreatedAtUtc | DateTimeOffset | No | Creation timestamp |
| UpdatedAtUtc | DateTimeOffset? | Yes | Last update timestamp |

**Relationships:**
- One-to-Many with CourseAssignment
- One-to-Many with UserDepartment
- One-to-Many with SyllabusDocument

---

### 3. CourseAssignment

Represents course assignments within departments.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | Guid (PK) | No | Unique identifier |
| DepartmentId | Guid (FK) | No | Foreign key to Department |
| CourseCode | string | No | Course code (e.g., "CE101") |
| CourseTitle | string | No | Course title |
| InstructorUserId | string | No | Instructor user ID |
| InstructorName | string | No | Instructor name |
| IsActive | bool | No | Active status |
| CreatedAtUtc | DateTimeOffset | No | Creation timestamp |
| UpdatedAtUtc | DateTimeOffset? | Yes | Last update timestamp |

**Relationships:**
- Many-to-One with Department

---

### 4. Subject

Represents academic subjects/courses (legacy table, may be merged with CourseAssignment).

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | int (PK) | No | Unique identifier |
| Code | string | No | Subject code |
| Name | string | No | Subject name |
| Description | string | No | Subject description |
| IsActive | bool | No | Active status |
| CreatedAtUtc | DateTimeOffset | No | Creation timestamp |
| UpdatedAtUtc | DateTimeOffset? | Yes | Last update timestamp |

---

### 5. SyllabusDocument

Represents syllabus documents with version tracking.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | Guid (PK) | No | Unique identifier |
| DepartmentId | Guid (FK) | No | Foreign key to Department |
| CourseAssignmentId | Guid? (FK) | Yes | Foreign key to CourseAssignment |
| CourseCode | string | No | Course code |
| CourseTitle | string | No | Course title |
| AcademicYear | string | No | Academic year (e.g., "2024-2025") |
| Semester | string | No | Semester (e.g., "Fall", "Spring") |
| InstructorName | string | No | Instructor name |
| OwnerUserId | string | No | Owner user ID (Educator) |
| Status | SyllabusStatus | No | Syllabus status (Draft, Submitted, Approved, Rejected) |
| CurrentVersionNumber | int | No | Current version number |
| LatestChangeSummary | string? | Yes | Latest change summary |
| ReviewerRemarks | string? | Yes | Reviewer remarks |
| CurrentFileName | string | No | Current file name |
| CurrentStoragePath | string | No | Current file storage path |
| IsPublished | bool | No | Published status |
| SubmittedAtUtc | DateTimeOffset? | Yes | Submission timestamp |
| ReviewedAtUtc | DateTimeOffset? | Yes | Review timestamp |
| ReviewedByUserId | string? | Yes | Reviewer user ID |
| CreatedAtUtc | DateTimeOffset | No | Creation timestamp |
| UpdatedAtUtc | DateTimeOffset? | Yes | Last update timestamp |

**Enums:**
- `SyllabusStatus`: Draft (0), Submitted (1), Approved (2), Rejected (3)

**Relationships:**
- Many-to-One with Department
- Many-to-One with CourseAssignment
- One-to-Many with SyllabusVersion

---

### 6. SyllabusVersion

Represents individual versions of syllabus documents.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | Guid (PK) | No | Unique identifier |
| SyllabusDocumentId | Guid (FK) | No | Foreign key to SyllabusDocument |
| VersionNumber | int | No | Version number |
| FileName | string | No | File name |
| StoragePath | string | No | File storage path |
| UploadedByUserId | string | No | Uploader user ID |
| UploadedByName | string | No | Uploader name |
| ChangeSummary | string | No | Change summary |
| StatusSnapshot | SyllabusStatus | No | Status snapshot at upload time |
| UploadedAtUtc | DateTimeOffset | No | Upload timestamp |
| CreatedAtUtc | DateTimeOffset | No | Creation timestamp |
| UpdatedAtUtc | DateTimeOffset? | Yes | Last update timestamp |

**Relationships:**
- Many-to-One with SyllabusDocument

---

### 7. SyllabusAssignment

Represents assignment of syllabi to students.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | Guid (PK) | No | Unique identifier |
| StudentId | string (FK) | No | Foreign key to ApplicationUser |
| SyllabusId | Guid (FK) | No | Foreign key to SyllabusDocument |
| AssignedBy | string | No | Assigner user ID (DeptHead) |
| AssignedAt | DateTimeOffset | No | Assignment timestamp |
| IsActive | bool | No | Active status |
| DeletedAt | DateTimeOffset? | Yes | Deletion timestamp |
| CreatedAtUtc | DateTimeOffset | No | Creation timestamp |
| UpdatedAtUtc | DateTimeOffset? | Yes | Last update timestamp |

**Relationships:**
- Many-to-One with ApplicationUser (Student)
- Many-to-One with SyllabusDocument

---

### 8. RegistrationRequest

Represents user registration requests awaiting approval.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | Guid (PK) | No | Unique identifier |
| FullName | string | No | Full name |
| Email | string | No | Email address |
| InstitutionalId | string | No | Institutional ID |
| RequestedRole | UserRoleType | No | Requested role |
| DepartmentId | Guid? (FK) | Yes | Foreign key to Department |
| Status | RegistrationStatus | No | Registration status (Pending, Approved, Rejected) |
| ReviewRemarks | string? | Yes | Review remarks |
| ReviewedByUserId | string? | Yes | Reviewer user ID |
| ReviewedAtUtc | DateTimeOffset? | Yes | Review timestamp |
| CreatedAtUtc | DateTimeOffset | No | Creation timestamp |
| UpdatedAtUtc | DateTimeOffset? | Yes | Last update timestamp |

**Enums:**
- `RegistrationStatus`: Pending (0), Approved (1), Rejected (2)

**Relationships:**
- Many-to-One with Department

---

### 9. AuditLogEntry

Represents audit trail for system actions.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | Guid (PK) | No | Unique identifier |
| UserId | string? | Yes | User ID who performed action |
| UserDisplayName | string? | Yes | User display name |
| ActionType | AuditActionType | No | Action type |
| ResultStatus | AuditResultStatus | No | Result status (Success, Failed, Warning) |
| Description | string | No | Action description |
| EntityType | string? | Yes | Entity type affected |
| EntityId | string? | Yes | Entity ID affected |
| IpAddress | string? | Yes | IP address of requester |
| CreatedAtUtc | DateTimeOffset | No | Creation timestamp |
| UpdatedAtUtc | DateTimeOffset? | Yes | Last update timestamp |

**Enums:**
- `AuditActionType`: LoginAttempt (0), LoginSuccess (1), LoginFailure (2), Logout (3), PasswordResetRequested (4), PasswordResetCompleted (5), RegistrationSubmitted (6), RegistrationApproved (7), RegistrationRejected (8), SyllabusUploaded (9), SyllabusSubmitted (10), SyllabusApproved (11), SyllabusRejected (12), SyllabusRestored (13), RoleUpdated (14), AccountDeactivated (15), AccountActivated (16), AccountDeleted (17)
- `AuditResultStatus`: Success (0), Failed (1), Warning (2)

---

### 10. NotificationEntry

Represents user notifications.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | Guid (PK) | No | Unique identifier |
| RecipientUserId | string | No | Recipient user ID |
| Type | NotificationType | No | Notification type |
| Title | string | No | Notification title |
| Message | string | No | Notification message |
| RelatedEntityType | string? | Yes | Related entity type |
| RelatedEntityId | string? | Yes | Related entity ID |
| IsRead | bool | No | Read status |
| ReadAtUtc | DateTimeOffset? | Yes | Read timestamp |
| CreatedAtUtc | DateTimeOffset | No | Creation timestamp |
| UpdatedAtUtc | DateTimeOffset? | Yes | Last update timestamp |

**Enums:**
- `NotificationType`: RegistrationApproved (0), RegistrationRejected (1), SubmissionAlert (2), ApprovalAlert (3), RejectionAlert (4), RevisionUploaded (5), RestorationNotice (6), SystemMessage (7)

---

### 11. UserDepartment

Represents user-department relationships (junction table).

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | Guid (PK) | No | Unique identifier |
| UserId | string (FK) | No | Foreign key to ApplicationUser |
| DepartmentId | Guid (FK) | No | Foreign key to Department |
| CreatedAtUtc | DateTimeOffset | No | Creation timestamp |
| UpdatedAtUtc | DateTimeOffset? | Yes | Last update timestamp |

**Relationships:**
- Many-to-One with ApplicationUser
- Many-to-One with Department

---

## ASP.NET Core Identity Tables

The following tables are automatically created by ASP.NET Core Identity:

- `AspNetUsers` - User accounts (mapped to ApplicationUser)
- `AspNetRoles` - User roles
- `AspNetUserRoles` - User-role associations
- `AspNetUserClaims` - User claims
- `AspNetUserLogins` - External logins
- `AspNetUserTokens` - User tokens
- `AspNetRoleClaims` - Role claims

## Indexes

### Recommended Indexes

```sql
-- ApplicationUser
CREATE INDEX IX_ApplicationUser_Role ON AspNetUsers(Role);
CREATE INDEX IX_ApplicationUser_DepartmentId ON AspNetUsers(DepartmentId);
CREATE INDEX IX_ApplicationUser_InstitutionalId ON AspNetUsers(InstitutionalId);
CREATE INDEX IX_ApplicationUser_AccountStatus ON AspNetUsers(AccountStatus);

-- SyllabusDocument
CREATE INDEX IX_SyllabusDocument_DepartmentId ON SyllabusDocuments(DepartmentId);
CREATE INDEX IX_SyllabusDocument_OwnerUserId ON SyllabusDocuments(OwnerUserId);
CREATE INDEX IX_SyllabusDocument_CourseCode ON SyllabusDocuments(CourseCode);
CREATE INDEX IX_SyllabusDocument_Status ON SyllabusDocuments(Status);

-- SyllabusAssignment
CREATE INDEX IX_SyllabusAssignment_StudentId ON SyllabusAssignments(StudentId);
CREATE INDEX IX_SyllabusAssignment_SyllabusId ON SyllabusAssignments(SyllabusId);
CREATE INDEX IX_SyllabusAssignment_IsActive ON SyllabusAssignments(IsActive);

-- RegistrationRequest
CREATE INDEX IX_RegistrationRequest_Status ON RegistrationRequests(Status);
CREATE INDEX IX_RegistrationRequest_DepartmentId ON RegistrationRequests(DepartmentId);

-- AuditLogEntry
CREATE INDEX IX_AuditLogEntry_UserId ON AuditLogEntries(UserId);
CREATE INDEX IX_AuditLogEntry_ActionType ON AuditLogEntries(ActionType);
CREATE INDEX IX_AuditLogEntry_CreatedAtUtc ON AuditLogEntries(CreatedAtUtc);

-- NotificationEntry
CREATE INDEX IX_NotificationEntry_RecipientUserId ON NotificationEntries(RecipientUserId);
CREATE INDEX IX_NotificationEntry_IsRead ON NotificationEntries(IsRead);
```

## Constraints

### Foreign Key Constraints

```sql
-- Department
ALTER TABLE CourseAssignments ADD FK_CourseAssignments_Departments_DepartmentId FOREIGN KEY (DepartmentId) REFERENCES Departments(Id);
ALTER TABLE UserDepartments ADD FK_UserDepartments_Departments_DepartmentId FOREIGN KEY (DepartmentId) REFERENCES Departments(Id);
ALTER TABLE SyllabusDocuments ADD FK_SyllabusDocuments_Departments_DepartmentId FOREIGN KEY (DepartmentId) REFERENCES Departments(Id);
ALTER TABLE RegistrationRequests ADD FK_RegistrationRequests_Departments_DepartmentId FOREIGN KEY (DepartmentId) REFERENCES Departments(Id);

-- ApplicationUser
ALTER TABLE UserDepartments ADD FK_UserDepartments_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id);
ALTER TABLE SyllabusAssignments ADD FK_SyllabusAssignments_AspNetUsers_StudentId FOREIGN KEY (StudentId) REFERENCES AspNetUsers(Id);

-- SyllabusDocument
ALTER TABLE SyllabusVersions ADD FK_SyllabusVersions_SyllabusDocuments_SyllabusDocumentId FOREIGN KEY (SyllabusDocumentId) REFERENCES SyllabusDocuments(Id);
ALTER TABLE SyllabusAssignments ADD FK_SyllabusAssignments_SyllabusDocuments_SyllabusId FOREIGN KEY (SyllabusId) REFERENCES SyllabusDocuments(Id);
```

## Data Access Patterns

### Common Queries

1. **Get active syllabi by department**
```csharp
var syllabi = await dbContext.SyllabusDocuments
    .Where(s => s.DepartmentId == deptId && s.Status == SyllabusStatus.Approved)
    .Include(s => s.Versions)
    .ToListAsync();
```

2. **Get student's assigned syllabi**
```csharp
var assignments = await dbContext.SyllabusAssignments
    .Where(a => a.StudentId == userId && a.IsActive)
    .Include(a => a.SyllabusDocument)
    .ThenInclude(s => s.Versions)
    .ToListAsync();
```

3. **Get pending registrations**
```csharp
var pending = await dbContext.RegistrationRequests
    .Where(r => r.Status == RegistrationStatus.Pending)
    .Include(r => r.Department)
    .ToListAsync();
```

4. **Get audit logs by user**
```csharp
var logs = await dbContext.AuditLogEntries
    .Where(a => a.UserId == userId)
    .OrderByDescending(a => a.CreatedAtUtc)
    .ToListAsync();
```

## Migration Strategy

### Creating Migrations

```bash
dotnet ef migrations add AddNewEntity
dotnet ef database update
```

### Rollback Strategy

```bash
dotnet ef database update <previous-migration>
dotnet ef migrations remove
```

## Performance Considerations

1. **File Storage**: Syllabus files are stored in the file system (`App_Data/syllabi`), not in the database. Only file paths are stored in the database.

2. **Soft Deletes**: Use `IsActive` flags and `DeletedAt` timestamps instead of hard deletes for audit trail purposes.

3. **Pagination**: Implement pagination for large result sets, especially for syllabi lists and audit logs.

4. **Caching**: Consider caching frequently accessed data like department lists and course assignments.

5. **Indexing**: Ensure proper indexes on foreign keys and frequently queried columns.

## Security Considerations

1. **Row-Level Security**: Implement proper authorization checks at the application level to ensure users can only access data they're permitted to see.

2. **Sensitive Data**: Institutional IDs and personal information should be handled with care and logged appropriately.

3. **Audit Trail**: All critical actions should be logged in the AuditLogEntry table for compliance and troubleshooting.

4. **File Access**: File system access to syllabus files should be validated against database permissions.
