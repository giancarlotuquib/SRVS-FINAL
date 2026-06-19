using SRVS.Domain.Enums;

namespace SRVS.Application.Models;

public sealed record RegistrationReviewQuery(
    IReadOnlyList<PendingUserDto> Requests,
    int PendingCount,
    int ApprovedCount,
    int RejectedCount);