using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SRVS.Domain.Enums;
using SRVS.Web.Data;

namespace SRVS.Web.Components.Account;

internal sealed class ApplicationUserClaimsPrincipalFactory : IUserClaimsPrincipalFactory<ApplicationUser>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOptions<IdentityOptions> _options;

    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> options)
    {
        _userManager = userManager;
        _options = options;
    }

    public async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        // Create identity with the same auth type Identity uses for cookies
        var identity = new ClaimsIdentity(authenticationType: IdentityConstants.ApplicationScheme);

        // Standard user claims
        var email = user.Email;
        var userName = user.UserName;

        if (!string.IsNullOrWhiteSpace(userName))
        {
            identity.AddClaim(new Claim(_options.Value.ClaimsIdentity.UserNameClaimType, userName));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            identity.AddClaim(new Claim(_options.Value.ClaimsIdentity.EmailClaimType, email));
        }

        // Required for [Authorize(Roles="...")]
        var roleValue = user.Role switch
        {
            UserRoleType.Admin => "Admin",
            UserRoleType.DepartmentHead => "DepartmentHead",
            UserRoleType.Educator => "Educator",
            UserRoleType.Student => "Student",
            _ => user.Role.ToString()
        };
        identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));

        // Required: identity user id (used by UserManager.GetUserAsync)
        var userIdClaimType = _options.Value.ClaimsIdentity.UserIdClaimType;
        identity.AddClaim(new Claim(userIdClaimType, user.Id));

        // SecurityStamp (used by your IdentityRevalidatingAuthenticationStateProvider)
        if (!string.IsNullOrWhiteSpace(user.SecurityStamp))
        {
            var stampClaimType = _options.Value.ClaimsIdentity.SecurityStampClaimType;
            identity.AddClaim(new Claim(stampClaimType, user.SecurityStamp));
        }

        // Add any custom claims stored in Identity
        var existingUserClaims = await _userManager.GetClaimsAsync(user);
        foreach (var c in existingUserClaims)
        {
            identity.AddClaim(c);
        }

        return new ClaimsPrincipal(identity);
    }
}
