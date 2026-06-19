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
        var query = dbContext.RegistrationRequests
            .AsNoTracking()
            .Include(request => request.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(request =>
                request.FullName.Contains(term) ||
                request.Email.Contains(term) ||
                request.InstitutionalId.Contains(term));
        }

        if (callerRole == UserRoleType.DepartmentHead)
        {
            // DepartmentHead can only approve Faculty (Educator) registrations
            query = query.Where(r => r.RequestedRole == UserRoleType.Educator);
        }
        else if (callerRole == UserRoleType.Admin)
        {
            // Admin can approve DepartmentHead, Faculty (Educator) and Student (Viewer) registrations
            query = query.Where(r => r.RequestedRole == UserRoleType.DepartmentHead
                                     || r.RequestedRole == UserRoleType.Viewer
                                     || r.RequestedRole == UserRoleType.Educator);
        }

        var pending = await query
            .Where(request => request.Status == RegistrationStatus.Pending)
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var pendingCount = await dbContext.RegistrationRequests.CountAsync(request => request.Status == RegistrationStatus.Pending, cancellationToken);
        var approvedCount = await dbContext.RegistrationRequests.CountAsync(request => request.Status == RegistrationStatus.Approved, cancellationToken);
        var rejectedCount = await dbContext.RegistrationRequests.CountAsync(request => request.Status == RegistrationStatus.Rejected, cancellationToken);

        return new RegistrationReviewQuery(pending, pendingCount, approvedCount, rejectedCount);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.RegistrationRequests.CountAsync(request => request.Status == RegistrationStatus.Pending, cancellationToken);
    }

    public async Task ApproveAsync(Guid registrationRequestId, string reviewerUserId, string reviewerName, UserRoleType reviewerRole, Guid? reviewerDepartmentId, CancellationToken cancellationToken = default)
    {
        var request = await LoadRequestAsync(registrationRequestId, cancellationToken);
        if (request.Status != RegistrationStatus.Pending)
        {
            throw new InvalidOperationException("This request has already been reviewed.");
        }

        var user = await FindUserByEmailAsync(request.Email, cancellationToken)
            ?? throw new InvalidOperationException("The linked user account could not be found.");

        request.Status = RegistrationStatus.Approved;
        request.ReviewRemarks = "Approved by system administrator.";
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedAtUtc = DateTimeOffset.UtcNow;

        user.AccountStatus = UserAccountStatus.Active;
        user.LastLoginAtUtc ??= null;

        if (reviewerRole == UserRoleType.DepartmentHead && (user.Role == UserRoleType.Educator || user.Role == UserRoleType.Viewer))
        {
            user.DepartmentId = reviewerDepartmentId;
        }


        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid registrationRequestId, string reviewerUserId, string reviewerName, string reviewRemarks, CancellationToken cancellationToken = default)
    {
        var request = await LoadRequestAsync(registrationRequestId, cancellationToken);
        if (request.Status != RegistrationStatus.Pending)
        {
            throw new InvalidOperationException("This request has already been reviewed.");
        }

        var user = await FindUserByEmailAsync(request.Email, cancellationToken)
            ?? throw new InvalidOperationException("The linked user account could not be found.");

        request.Status = RegistrationStatus.Rejected;
        request.ReviewRemarks = reviewRemarks.Trim();
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedAtUtc = DateTimeOffset.UtcNow;

        user.AccountStatus = UserAccountStatus.Rejected;


        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<RegistrationRequest> LoadRequestAsync(Guid registrationRequestId, CancellationToken cancellationToken)
    {
        return await dbContext.RegistrationRequests
            .FirstOrDefaultAsync(request => request.Id == registrationRequestId, cancellationToken)
            ?? throw new InvalidOperationException("The selected registration request could not be found.");
    }

    public async Task<RegistrationRequest> GetRegistrationRequestAsync(Guid registrationRequestId, CancellationToken cancellationToken = default)
    {
        return await dbContext.RegistrationRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == registrationRequestId, cancellationToken)
            ?? throw new InvalidOperationException("Registration request not found.");
    }

    private Task<ApplicationUser?> FindUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return userManager.FindByEmailAsync(email);
    }
}