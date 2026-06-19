namespace SRVS.Domain.Entities;

public class Department : EntityBase
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<CourseAssignment> CourseAssignments { get; set; } = new List<CourseAssignment>();

    public ICollection<SyllabusDocument> SyllabusDocuments { get; set; } = new List<SyllabusDocument>();
}