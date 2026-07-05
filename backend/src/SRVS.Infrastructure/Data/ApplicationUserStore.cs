using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SRVS.Web.Data;

/// <summary>
/// Custom user store that overrides the default email/username lookups
/// because we removed the NormalizedEmail and NormalizedUserName columns.
/// Queries are performed against the raw Email / UserName columns instead.
/// </summary>
public class ApplicationUserStore : UserStore<ApplicationUser, IdentityRole, ApplicationDbContext>
{
	public ApplicationUserStore(ApplicationDbContext context, IdentityErrorDescriber? describer = null)
		: base(context, describer)
	{
	}

	public override Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var lowerEmail = normalizedEmail.ToLowerInvariant();
		return Users.FirstOrDefaultAsync(u => u.Email.ToLower() == lowerEmail, cancellationToken);
	}

	public override Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var lowerName = normalizedUserName.ToLowerInvariant();
		return Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == lowerName, cancellationToken);
	}

	// Disable SecurityStamp read/write since the column no longer exists.
	public override Task<string?> GetSecurityStampAsync(ApplicationUser user, CancellationToken cancellationToken = default)
		=> Task.FromResult<string?>(string.Empty);

	public override Task SetSecurityStampAsync(ApplicationUser user, string stamp, CancellationToken cancellationToken = default)
		=> Task.CompletedTask;
}
