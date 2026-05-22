namespace SRVS.Domain.Entities;

public class SyllabusAssignment : EntityBase
{
    public string StudentId { get; set; } = string.Empty; // ApplicationUser.Id

    public Guid SyllabusId { get; set; } // SyllabusDocument.Id

    public string AssignedBy { get; set; } = string.Empty; // DeptHead user id

    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? DeletedAt { get; set; }
}
