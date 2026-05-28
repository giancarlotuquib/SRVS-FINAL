using SRVS.Domain.Enums;
using System;

namespace SRVS.Domain.Entities;

public class ResetRequest : EntityBase
{
    public string UserId { get; set; } = string.Empty;
    public string SchoolId { get; set; } = string.Empty;
    public UserRoleType Role { get; set; }
    public ResetRequestStatus Status { get; set; } = ResetRequestStatus.Pending;
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
}

public enum ResetRequestStatus
{
    Pending,
    Approved,
    Rejected
}
