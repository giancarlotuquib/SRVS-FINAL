using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Enums;
using SRVS.Web.Data;

namespace SRVS.Web.Components.Admin.Services;

/// <summary>
/// Service for handling user account actions (Activate, Deactivate, Delete).
/// Manages database operations and user status transitions.
/// </summary>
public class UserActionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public UserActionService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Updates a user's account status.
    /// </summary>
    /// <param name="userId">The ID of the user to update</param>
    /// <param name="newStatus">The new account status</param>
    /// <returns>The updated ApplicationUser, or null if user not found</returns>
    public async Task<ApplicationUser?> UpdateUserStatusAsync(string userId, UserAccountStatus newStatus)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return null;
        }

        user.AccountStatus = newStatus;
        await dbContext.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Gets all users excluding pending approval users, ordered by username.
    /// </summary>
    /// <returns>A list of users excluding pending approval</returns>
    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.Users
            .AsNoTracking()
            .Where(u => u.AccountStatus != UserAccountStatus.PendingApproval)
            .OrderBy(u => u.UserName)
            .ToListAsync();
    }

    /// <summary>
    /// Checks if an action is allowed for a user based on their current status.
    /// </summary>
    /// <param name="user">The user to check</param>
    /// <param name="targetStatus">The target status</param>
    /// <returns>True if the action is allowed, false otherwise</returns>
    public static bool IsActionAllowed(ApplicationUser user, UserAccountStatus targetStatus)
    {
        // Cannot set to same status
        if (user.AccountStatus == targetStatus)
        {
            return false;
        }

        // Activate: only allowed for non-active users
        if (targetStatus == UserAccountStatus.Active && user.AccountStatus == UserAccountStatus.Active)
        {
            return false;
        }

        // Deactivate: only allowed for active users
        if (targetStatus == UserAccountStatus.Suspended && user.AccountStatus != UserAccountStatus.Active)
        {
            return false;
        }

        return true;
    }
}
