namespace SRVS.Domain.Entities;

public class SyllabusAssignment : EntityBase
{
    public string StudentId { get; set; } = string.Empty;

    public Guid SyllabusId { get; set; }

    public string AssignedBy { get; set; } = string.Empty;

    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? DeletedAt { get; set; }
}
