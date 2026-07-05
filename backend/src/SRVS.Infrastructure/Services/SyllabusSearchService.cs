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
    public async Task<SyllabusSearchResults> SearchAsync(SyllabusSearchRequest request, UserRoleType role, string userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var documents = await BuildScopedQuery(dbContext, role, userId, request.Status)
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
                string.Empty,
                document.AcademicYear,
                document.Semester,
                document.InstructorName,
                document.CurrentVersionNumber,
                document.Status,
                CanAccessDocument(document, role, userId, isAssignedViewerDocument: role == UserRoleType.Student),
                GetVisibilityLabel(document, role),
                document.LatestChangeSummary))
            .ToList();

        return new SyllabusSearchResults(items, totalCount);
    }

    public async Task<SyllabusDocument?> GetAccessibleDocumentAsync(Guid syllabusDocumentId, UserRoleType role, string userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var document = await dbContext.SyllabusDocuments
            .FirstOrDefaultAsync(item => item.Id == syllabusDocumentId, cancellationToken);

        if (document is null)
        {
            return null;
        }

        var isAssignedViewerDocument = role == UserRoleType.Student
            && await dbContext.SyllabusAssignments.AnyAsync(
                assignment => assignment.StudentId == userId
                    && assignment.SyllabusDocId == document.Id
                    && assignment.IsActive
                    && assignment.DeletedAt == null,
                cancellationToken);

        return CanAccessDocument(document, role, userId, isAssignedViewerDocument) ? document : null;
    }

    private static IQueryable<SyllabusDocument> BuildScopedQuery(ApplicationDbContext dbContext, UserRoleType role, string userId, SyllabusStatus? statusFilter)
    {
        var query = dbContext.SyllabusDocuments.AsQueryable();

        return role switch
        {
            UserRoleType.Admin => query,
            UserRoleType.DepartmentHead => query,
            UserRoleType.Educator => query.Where(document => document.OwnerUserId == userId),
            UserRoleType.Student => query.Where(document =>
                document.Status == SyllabusStatus.Approved
                && document.IsPublished
                && dbContext.SyllabusAssignments.Any(assignment =>
                    assignment.StudentId == userId
                    && assignment.SyllabusDocId == document.Id
                    && assignment.IsActive
                    && assignment.DeletedAt == null)),
            _ => query.Where(document => document.Status == SyllabusStatus.Approved && document.IsPublished)
        };
    }

    private static bool CanAccessDocument(SyllabusDocument document, UserRoleType role, string userId, bool isAssignedViewerDocument)
    {
        return role switch
        {
            UserRoleType.Admin => true,
            UserRoleType.DepartmentHead => true,
            UserRoleType.Educator => document.OwnerUserId == userId,
            UserRoleType.Student => document.IsPublished && document.Status == SyllabusStatus.Approved && isAssignedViewerDocument,
            _ => false
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
                document.InstructorName.Contains(term, StringComparison.OrdinalIgnoreCase));
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
            UserRoleType.DepartmentHead => document.Status == SyllabusStatus.Submitted ? "Pending review" : "Department head access",
            UserRoleType.Educator => document.OwnerUserId == string.Empty ? "Department scope" : "Owner/department scope",
            UserRoleType.Student => "Assigned",
            _ => "Read-only"
        };
    }
}
