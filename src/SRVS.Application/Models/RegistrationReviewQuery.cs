using SRVS.Domain.Entities;

namespace SRVS.Application.Models;

public sealed record RegistrationReviewQuery(
    IReadOnlyList<RegistrationRequest> Requests,
    int PendingCount,
    int ApprovedCount,
    int RejectedCount);