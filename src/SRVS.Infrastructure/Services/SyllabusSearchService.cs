using Microsoft.EntityFrameworkCore;
using SRVS.Application.Abstractions;
using SRVS.Application.Models;
using SRVS.Application.Services;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;
using SRVS.Web.Data;

namespace SRVS.Infrastructure.Services;

public class SyllabusSearchService(IDbContextFactory<ApplicationDbContext> dbContextFactory) : ISyllabusSearchService
{
    public async Task<SyllabusSearchResults> SearchAsync(SyllabusSearchRequest request, UserRoleType role, Guid? departmentId, string userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var documents = await BuildScopedQuery(dbContext, role, departmentId, userId)
            .Include(document => document.Department)
            .ToListAsync(cancellationToken);

        var orderedDocuments = documents
            .OrderByDescending(document => document.UpdatedAtUtc ?? document.CreatedAtUtc)
            .ToList();

        var filtered = ApplySearchFilters(orderedDocuments, request);

        var totalCount = filtered.Count;
        var items = filtered
            .Take(Math.Max(1, request.MaxResults))
            .Select(document => new SyllabusSearchItem(
                document.Id,
                document.CourseCode,
                document.CourseTitle,
                document.Department?.Name ?? string.Empty,
                document.AcademicYear,
                document.Semester,
                document.InstructorName,
                document.CurrentVersionNumber,
                document.Status,
                SyllabusAccessPolicy.CanDownload(document, role, departmentId, userId),
                GetVisibilityLabel(document, role),
                document.LatestChangeSummary))
            .ToList();

        return new SyllabusSearchResults(items, totalCount);
    }

    public async Task<SyllabusDocument?> GetAccessibleDocumentAsync(Guid syllabusDocumentId, UserRoleType role, Guid? departmentId, string userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var document = await dbContext.SyllabusDocuments
            .Include(item => item.Department)
            .FirstOrDefaultAsync(item => item.Id == syllabusDocumentId, cancellationToken);

        return document is not null && SyllabusAccessPolicy.CanView(document, role, departmentId, userId) ? document : null;
    }

    private static IQueryable<SyllabusDocument> BuildScopedQuery(ApplicationDbContext dbContext, UserRoleType role, Guid? departmentId, string userId)
    {
        var ceDepartment = dbContext.Departments
            .FirstOrDefault(d => d.Code == "CE" || d.Name.Contains("Computer Engineering"));

        var query = dbContext.SyllabusDocuments.AsQueryable();
        var effectiveDepartmentId = departmentId ?? ceDepartment?.Id;

        return role switch
        {
            UserRoleType.Admin => query,
            UserRoleType.DepartmentHead => ApplyDepartmentScope(query, effectiveDepartmentId),
            UserRoleType.Educator => ApplyEducatorScope(query, effectiveDepartmentId, userId),
            UserRoleType.Viewer => ApplyDepartmentScope(query, effectiveDepartmentId)
                .Where(document => document.Status == SyllabusStatus.Approved && document.IsPublished),
            _ => ApplyDepartmentScope(query, effectiveDepartmentId)
                .Where(document => document.Status == SyllabusStatus.Approved && document.IsPublished)
        };
    }

    private static IQueryable<SyllabusDocument> ApplyDepartmentScope(IQueryable<SyllabusDocument> query, Guid? departmentId)
    {
        return departmentId is null
            ? query
            : query.Where(document => document.DepartmentId == departmentId.Value);
    }

    private static IQueryable<SyllabusDocument> ApplyEducatorScope(IQueryable<SyllabusDocument> query, Guid? departmentId, string userId)
    {
        return departmentId is null
            ? query.Where(document => document.OwnerUserId == userId)
            : query.Where(document => document.OwnerUserId == userId || document.DepartmentId == departmentId.Value);
    }

    private static IReadOnlyList<SyllabusDocument> ApplySearchFilters(IEnumerable<SyllabusDocument> documents, SyllabusSearchRequest request)
    {
        var query = documents;

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            var term = request.Term.Trim();
            query = query.Where(document =>
                document.CourseCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                document.CourseTitle.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                document.AcademicYear.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                document.Semester.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                document.InstructorName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (document.Department is not null && document.Department.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (document.Department is not null && document.Department.Code.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (request.Status is not null)
        {
            query = query.Where(document => document.Status == request.Status.Value);
        }

        return query.ToList();
    }

    private static string GetVisibilityLabel(SyllabusDocument document, UserRoleType role)
    {
        return role switch
        {
            UserRoleType.Admin => "Full access",
            UserRoleType.DepartmentHead => document.Status == SyllabusStatus.Submitted ? "Pending review" : "Department scope",
            UserRoleType.Educator => document.OwnerUserId == string.Empty ? "Department scope" : "Owner/department scope",
            UserRoleType.Viewer => "Published",
            _ => "Read-only"
        };
    }
}
