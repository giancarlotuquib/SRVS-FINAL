using Microsoft.EntityFrameworkCore;
using SRVS.Application.Abstractions;
using SRVS.Application.Models;
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
                CanDownload(document, role),
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

        return document is not null && IsVisibleToUser(document, role, departmentId, userId) ? document : null;
    }

    private static IQueryable<SyllabusDocument> BuildScopedQuery(ApplicationDbContext dbContext, UserRoleType role, Guid? departmentId, string userId)
    {
        var ceDepartment = dbContext.Departments
            .FirstOrDefault(d => d.Code == "CE" || d.Name.Contains("Computer Engineering"));

        var ceId = ceDepartment?.Id ?? Guid.Empty;
        var query = dbContext.SyllabusDocuments.Where(doc => doc.DepartmentId == ceId);

        return role switch
        {
            UserRoleType.Admin => query,
            UserRoleType.DepartmentHead => query.Where(document => document.DepartmentId == ceId),
            UserRoleType.Educator => query.Where(document => document.OwnerUserId == userId || document.DepartmentId == ceId),
            UserRoleType.Viewer => query.Where(document => document.Status == SyllabusStatus.Approved && document.IsPublished),
            _ => query.Where(document => document.Status == SyllabusStatus.Approved && document.IsPublished)
        };
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

    private static bool IsVisibleToUser(SyllabusDocument document, UserRoleType role, Guid? departmentId, string userId)
    {
        return role switch
        {
            UserRoleType.Admin => true,
            UserRoleType.DepartmentHead => document.DepartmentId == departmentId,
            UserRoleType.Educator => document.DepartmentId == departmentId || document.OwnerUserId == userId,
            UserRoleType.Viewer => document.Status == SyllabusStatus.Approved && document.IsPublished,
            _ => false
        };
    }

    private static bool CanDownload(SyllabusDocument document, UserRoleType role)
    {
        return role switch
        {
            UserRoleType.Admin => true,
            UserRoleType.DepartmentHead => document.Status is SyllabusStatus.Submitted or SyllabusStatus.Approved,
            UserRoleType.Educator => document.Status is SyllabusStatus.Draft or SyllabusStatus.Submitted or SyllabusStatus.Approved,
            UserRoleType.Viewer => document.Status == SyllabusStatus.Approved && document.IsPublished,
            _ => false
        };
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
