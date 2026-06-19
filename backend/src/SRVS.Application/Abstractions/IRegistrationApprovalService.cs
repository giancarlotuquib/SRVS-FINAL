using SRVS.Application.Models;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;

namespace SRVS.Application.Abstractions;

public interface IRegistrationApprovalService
{
    Task<RegistrationReviewQuery> GetQueueAsync(UserRoleType? callerRole = null, string? search = null, CancellationToken cancellationToken = default);
    Task<PendingUserDto> GetRegistrationRequestAsync(string userId, CancellationToken cancellationToken = default);

    Task ApproveAsync(string targetUserId, string reviewerUserId, string reviewerName, UserRoleType reviewerRole, CancellationToken cancellationToken = default);

    Task RejectAsync(string targetUserId, string reviewerUserId, string reviewerName, string reviewRemarks, CancellationToken cancellationToken = default);

    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}