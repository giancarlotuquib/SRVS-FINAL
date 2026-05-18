using SRVS.Domain.Enums;

namespace SRVS.Domain.Entities;

public class RegistrationRequest : EntityBase
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string InstitutionalId { get; set; } = string.Empty;

    public UserRoleType RequestedRole { get; set; }

    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

    public string? ReviewRemarks { get; set; }

    public string? ReviewedByUserId { get; set; }

    public DateTimeOffset? ReviewedAtUtc { get; set; }
}