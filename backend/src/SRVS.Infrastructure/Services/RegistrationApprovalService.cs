using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SRVS.Application.Abstractions;
using SRVS.Application.Models;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;
using SRVS.Web.Data;

namespace SRVS.Infrastructure.Services;

public class RegistrationApprovalService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IRegistrationApprovalService
{
    public async Task<RegistrationReviewQuery> GetQueueAsync(UserRoleType? callerRole = null, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.FullName.Contains(term) ||
                (u.Email != null && u.Email.Contains(term)) ||
                u.InstitutionalId.Contains(term));
        }

        if (callerRole == UserRoleType.DepartmentHead)
        {
            // DepartmentHead can only approve Faculty (Educator) registrations
            query = query.Where(u => u.Role == UserRoleType.Educator);
        }
        else if (callerRole == UserRoleType.Admin)
        {
            // Admin can approve any registrations, so no filter is applied
        }

        var pendingListRaw = await query
            .Where(u => u.AccountStatus == UserAccountStatus.PendingApproval)
            .Select(u => new PendingUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                InstitutionalId = u.InstitutionalId,
                Role = u.Role,
                Status = u.AccountStatus,
                CreatedAtUtc = u.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
            
        var pendingList = pendingListRaw.OrderByDescending(u => u.CreatedAtUtc).ToList();

        var pendingCount = await dbContext.Users.CountAsync(u => u.AccountStatus == UserAccountStatus.PendingApproval, cancellationToken);
        var approvedCount = await dbContext.Users.CountAsync(u => u.AccountStatus == UserAccountStatus.Active, cancellationToken);
        var rejectedCount = await dbContext.Users.CountAsync(u => u.AccountStatus == UserAccountStatus.Rejected, cancellationToken);

        return new RegistrationReviewQuery(pendingList, pendingCount, approvedCount, rejectedCount);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users.CountAsync(u => u.AccountStatus == UserAccountStatus.PendingApproval, cancellationToken);
    }

    public async Task ApproveAsync(string targetUserId, string reviewerUserId, string reviewerName, UserRoleType reviewerRole, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(targetUserId)
            ?? throw new InvalidOperationException("The user could not be found.");

        if (user.AccountStatus != UserAccountStatus.PendingApproval)
        {
            throw new InvalidOperationException("This user's account is not pending approval.");
        }

        user.AccountStatus = UserAccountStatus.Active;
        
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            dbContext.AuditLogEntries.Add(new AuditLogEntry
            {
                UserId = reviewerUserId,
                UserDisplayName = reviewerName,
                ActionType = AuditActionType.RegistrationApproved,
                ResultStatus = AuditResultStatus.Success,
                Description = $"Approved registration for user '{user.Email}' with role '{user.Role}'.",
                EntityType = nameof(ApplicationUser),
                EntityId = user.Id
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RejectAsync(string targetUserId, string reviewerUserId, string reviewerName, string reviewRemarks, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(targetUserId)
            ?? throw new InvalidOperationException("The user could not be found.");

        if (user.AccountStatus != UserAccountStatus.PendingApproval)
        {
            throw new InvalidOperationException("This user's account is not pending approval.");
        }

        user.AccountStatus = UserAccountStatus.Rejected;

        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            dbContext.AuditLogEntries.Add(new AuditLogEntry
            {
                UserId = reviewerUserId,
                UserDisplayName = reviewerName,
                ActionType = AuditActionType.RegistrationRejected,
                ResultStatus = AuditResultStatus.Success,
                Description = $"Rejected registration for user '{user.Email}'. Remarks: {reviewRemarks.Trim()}",
                EntityType = nameof(ApplicationUser),
                EntityId = user.Id
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<PendingUserDto> GetRegistrationRequestAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        return new PendingUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            InstitutionalId = user.InstitutionalId,
            Role = user.Role,
            Status = user.AccountStatus,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }
}