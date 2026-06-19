using System;
using SRVS.Domain.Enums;

namespace SRVS.Application.Models;

public class PendingUserDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string InstitutionalId { get; set; } = string.Empty;
    public UserRoleType Role { get; set; }
    public UserAccountStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
