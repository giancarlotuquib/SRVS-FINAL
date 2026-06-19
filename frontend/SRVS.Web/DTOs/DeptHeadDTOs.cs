using System;
using System.Collections.Generic;

namespace SRVS.Web.DTOs;

public class StudentResponse
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string SchoolId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid? AssignedSyllabusId { get; set; }
    public string? AssignedSyllabusTitle { get; set; }
    public string Status { get; set; } = string.Empty; // "Assigned" or "Unassigned"
}

public class SyllabusListResponse
{
    public Guid Id { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectTitle { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public SRVS.Domain.Enums.SyllabusStatus Status { get; set; }
}

public class AssignRequest
{
    public string StudentId { get; set; } = string.Empty;
    public Guid SyllabusId { get; set; }
}

public class BulkAssignRequest
{
    public List<string> StudentIds { get; set; } = new();
    public Guid SyllabusId { get; set; }
}

public class AssignmentResponse
{
    public Guid Id { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string SchoolId { get; set; } = string.Empty;
    public string SyllabusTitle { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public DateTimeOffset AssignedAt { get; set; }
    public string AssignedBy { get; set; } = string.Empty; // DeptHead Id or name
}

public class SyllabusPendingResponse
{
    public Guid Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public int CurrentVersionNumber { get; set; }
    public string CurrentFileName { get; set; } = string.Empty;
    public DateTimeOffset? SubmittedAtUtc { get; set; }
}

public class ReviewSyllabusRequest
{
    public string? Remarks { get; set; }
}
