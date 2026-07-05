using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Entities;
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
    /// <param name="actorUserId">The user ID of the admin performing the action</param>
    /// <param name="actorName">The display name of the admin performing the action</param>
    /// <returns>The updated ApplicationUser, or null if user not found</returns>
    public async Task<ApplicationUser?> UpdateUserStatusAsync(string userId, UserAccountStatus newStatus, string actorUserId, string actorName)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return null;
        }

        user.AccountStatus = newStatus;

        var actionType = newStatus switch
        {
            UserAccountStatus.Active => AuditActionType.AccountActivated,
            UserAccountStatus.Suspended => AuditActionType.AccountDeactivated,
            UserAccountStatus.Deleted => AuditActionType.AccountDeleted,
            _ => AuditActionType.RoleUpdated
        };

        var actionDesc = newStatus switch
        {
            UserAccountStatus.Active => $"Activated user account '{user.Email}'.",
            UserAccountStatus.Suspended => $"Deactivated user account '{user.Email}'.",
            UserAccountStatus.Deleted => $"Soft-deleted user account '{user.Email}'.",
            _ => $"Updated status of user '{user.Email}' to {newStatus}."
        };

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            UserId = actorUserId,
            UserDisplayName = actorName,
            ActionType = actionType,
            ResultStatus = AuditResultStatus.Success,
            Description = actionDesc,
            EntityType = nameof(ApplicationUser),
            EntityId = user.Id
        });

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
