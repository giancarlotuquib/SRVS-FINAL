using Microsoft.AspNetCore.Identity;
using SRVS.Domain.Enums;

namespace SRVS.Web.Data;

public class ApplicationUser : IdentityUser
{
	public string FullName { get; set; } = string.Empty;

	public string InstitutionalId { get; set; } = string.Empty;

	public UserRoleType Role { get; set; } = UserRoleType.Viewer;

	public UserAccountStatus AccountStatus { get; set; } = UserAccountStatus.PendingApproval;

	public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? LastLoginAtUtc { get; set; }
}