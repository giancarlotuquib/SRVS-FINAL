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
    public async Task<RegistrationReviewQuery> GetQueueAsync(string? search = null, CancellationToken cancellationToken = default)
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

    public async Task ApproveAsync(Guid registrationRequestId, string reviewerUserId, string reviewerName, CancellationToken cancellationToken = default)
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

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            UserId = reviewerUserId,
            UserDisplayName = reviewerName,
            ActionType = AuditActionType.RegistrationApproved,
            ResultStatus = AuditResultStatus.Success,
            Description = $"Approved registration for {request.Email}.",
            EntityType = nameof(RegistrationRequest),
            EntityId = request.Id.ToString()
        });

        dbContext.NotificationEntries.Add(new NotificationEntry
        {
            RecipientUserId = user.Id,
            Type = NotificationType.RegistrationApproved,
            Title = "Registration approved",
            Message = "Your SRVS account is now active.",
            RelatedEntityType = nameof(RegistrationRequest),
            RelatedEntityId = request.Id.ToString()
        });

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

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            UserId = reviewerUserId,
            UserDisplayName = reviewerName,
            ActionType = AuditActionType.RegistrationRejected,
            ResultStatus = AuditResultStatus.Success,
            Description = $"Rejected registration for {request.Email}.",
            EntityType = nameof(RegistrationRequest),
            EntityId = request.Id.ToString()
        });

        dbContext.NotificationEntries.Add(new NotificationEntry
        {
            RecipientUserId = user.Id,
            Type = NotificationType.RegistrationRejected,
            Title = "Registration rejected",
            Message = string.IsNullOrWhiteSpace(reviewRemarks)
                ? "Your SRVS account request was rejected."
                : $"Your SRVS account request was rejected: {reviewRemarks.Trim()}",
            RelatedEntityType = nameof(RegistrationRequest),
            RelatedEntityId = request.Id.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<RegistrationRequest> LoadRequestAsync(Guid registrationRequestId, CancellationToken cancellationToken)
    {
        return await dbContext.RegistrationRequests
            .FirstOrDefaultAsync(request => request.Id == registrationRequestId, cancellationToken)
            ?? throw new InvalidOperationException("The selected registration request could not be found.");
    }

    private Task<ApplicationUser?> FindUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return userManager.FindByEmailAsync(email);
    }
}