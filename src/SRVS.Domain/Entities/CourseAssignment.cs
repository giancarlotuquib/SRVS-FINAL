namespace SRVS.Domain.Entities;

public class CourseAssignment : EntityBase
{
    public Guid DepartmentId { get; set; }

    public Department? Department { get; set; }

    public string CourseCode { get; set; } = string.Empty;

    public string CourseTitle { get; set; } = string.Empty;

    public string InstructorUserId { get; set; } = string.Empty;

    public string InstructorName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}