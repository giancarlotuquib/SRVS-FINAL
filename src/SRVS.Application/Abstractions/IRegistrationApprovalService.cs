using SRVS.Application.Models;

namespace SRVS.Application.Abstractions;

public interface IRegistrationApprovalService
{
    Task<RegistrationReviewQuery> GetQueueAsync(string? search = null, CancellationToken cancellationToken = default);

    Task ApproveAsync(Guid registrationRequestId, string reviewerUserId, string reviewerName, CancellationToken cancellationToken = default);

    Task RejectAsync(Guid registrationRequestId, string reviewerUserId, string reviewerName, string reviewRemarks, CancellationToken cancellationToken = default);

    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}