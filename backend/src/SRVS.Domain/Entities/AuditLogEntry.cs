using SRVS.Domain.Enums;

namespace SRVS.Domain.Entities;

public class AuditLogEntry : EntityBase
{
    public string? UserId { get; set; }

    public string? UserDisplayName { get; set; }

    public AuditActionType ActionType { get; set; }

    public AuditResultStatus ResultStatus { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? IpAddress { get; set; }
}
