namespace SRVS.Domain.Entities;

public class SyllabusAssignment : EntityBase
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentFullName { get; set; } = string.Empty;

    public string SyllabusId { get; set; } = string.Empty;
    public Guid SyllabusDocId { get; set; }

    public string DepartmentName { get; set; } = "Computer Engineering";
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;

    public string AssignedBy { get; set; } = string.Empty;

    public string AssignedAt { get; set; } = string.Empty;
    public DateTimeOffset AssignedAtDate { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? DeletedAt { get; set; }
}
