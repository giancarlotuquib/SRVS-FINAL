using Microsoft.AspNetCore.Identity;
using SRVS.Domain.Enums;

namespace SRVS.Web.Data;

public class ApplicationUser : IdentityUser
{
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;

	[System.ComponentModel.DataAnnotations.Schema.NotMapped]
	public string InstitutionalId
	{
		get => Id;
		set => Id = value;
	}

	public UserRoleType Role { get; set; } = UserRoleType.Student;

	public UserAccountStatus AccountStatus { get; set; } = UserAccountStatus.PendingApproval;

	public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? LastLoginAtUtc { get; set; }
}