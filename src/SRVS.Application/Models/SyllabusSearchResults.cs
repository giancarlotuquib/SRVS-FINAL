namespace SRVS.Application.Models;

public sealed record SyllabusSearchResults(
    IReadOnlyList<SyllabusSearchItem> Items,
    int TotalCount);