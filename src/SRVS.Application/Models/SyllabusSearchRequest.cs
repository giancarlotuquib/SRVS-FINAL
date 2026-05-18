using SRVS.Domain.Enums;

namespace SRVS.Application.Models;

public sealed record SyllabusSearchRequest(
    string? Term = null,
    SyllabusStatus? Status = null,
    int MaxResults = 100);