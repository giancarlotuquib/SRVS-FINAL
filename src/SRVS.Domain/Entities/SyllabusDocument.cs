using SRVS.Domain.Enums;

namespace SRVS.Domain.Entities;

public class SyllabusDocument : EntityBase
{
    public Guid DepartmentId { get; set; }

    public Department? Department { get; set; }

    public Guid? CourseAssignmentId { get; set; }

    public CourseAssignment? CourseAssignment { get; set; }

    public string CourseCode { get; set; } = string.Empty;

    public string CourseTitle { get; set; } = string.Empty;

    public string AcademicYear { get; set; } = string.Empty;

    public string Semester { get; set; } = string.Empty;

    public string InstructorName { get; set; } = string.Empty;

    public string OwnerUserId { get; set; } = string.Empty;

    public SyllabusStatus Status { get; set; } = SyllabusStatus.Draft;

    public int CurrentVersionNumber { get; set; } = 1;

    public string? LatestChangeSummary { get; set; }

    public string? ReviewerRemarks { get; set; }

    public string CurrentFileName { get; set; } = string.Empty;

    public string CurrentStoragePath { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTimeOffset? SubmittedAtUtc { get; set; }

    public DateTimeOffset? ReviewedAtUtc { get; set; }

    public string? ReviewedByUserId { get; set; }

    public ICollection<SyllabusVersion> Versions { get; set; } = new List<SyllabusVersion>();
}