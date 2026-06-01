using SRVS.Application.Models;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;

namespace SRVS.Application.Abstractions;

public interface ISyllabusSearchService
{
    Task<SyllabusSearchResults> SearchAsync(SyllabusSearchRequest request, UserRoleType role, Guid? departmentId, string userId, CancellationToken cancellationToken = default);

    Task<SyllabusDocument?> GetAccessibleDocumentAsync(Guid syllabusDocumentId, UserRoleType role, Guid? departmentId, string userId, CancellationToken cancellationToken = default);
}