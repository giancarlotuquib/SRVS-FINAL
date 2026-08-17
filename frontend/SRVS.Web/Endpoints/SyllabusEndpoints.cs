using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRVS.Application.Abstractions;
using SRVS.Application.Models;
using SRVS.Domain.Entities;
using SRVS.Application.Services;
using SRVS.Web.Data;
using SRVS.Web.DTOs;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SRVS.Web.Endpoints;

public static class SyllabusEndpoints
{
    public static IEndpointRouteBuilder MapSyllabusEndpoints(this IEndpointRouteBuilder app)
    {
        // Download endpoints
        var downloads = app.MapGroup("/syllabi")
            .WithTags("Syllabi Downloads")
            .RequireAuthorization();

        downloads.MapGet("/{syllabusDocumentId}/view", async (
            string syllabusDocumentId,
            HttpContext httpContext,
            ApplicationDbContext dbContext,
            ISyllabusSearchService syllabusSearchService,
            ISyllabusFileStorage syllabusFileStorage,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();

            var document = await FindSyllabusByIdOr5DigitAsync(dbContext, syllabusDocumentId, cancellationToken);
            if (document is null) return Results.NotFound(new ErrorResponse { Error = "Syllabus not found." });

            var accessibleDoc = await syllabusSearchService.GetAccessibleDocumentAsync(document.Id, user.Role, user.Id, cancellationToken);
            if (accessibleDoc is null) return Results.NotFound(new ErrorResponse { Error = "Access denied or syllabus not found." });

            return await CreateFileResultAsync(syllabusFileStorage, accessibleDoc.CurrentStoragePath, accessibleDoc.CurrentFileName, asAttachment: false, cancellationToken);
        })
        .WithName("ViewSyllabus")
        .WithSummary("View syllabus document inline")
        .WithDescription("Displays the syllabus document file in browser. Accepts 5-digit document ID (e.g. 12345) or GUID.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized);

        downloads.MapGet("/{syllabusDocumentId}/download", async (
            string syllabusDocumentId,
            HttpContext httpContext,
            ApplicationDbContext dbContext,
            ISyllabusSearchService syllabusSearchService,
            ISyllabusFileStorage syllabusFileStorage,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();

            var document = await FindSyllabusByIdOr5DigitAsync(dbContext, syllabusDocumentId, cancellationToken);
            if (document is null) return Results.NotFound(new ErrorResponse { Error = "Syllabus not found." });

            var accessibleDoc = await syllabusSearchService.GetAccessibleDocumentAsync(document.Id, user.Role, user.Id, cancellationToken);
            if (accessibleDoc is null) return Results.NotFound(new ErrorResponse { Error = "Access denied or syllabus not found." });

            if (!await syllabusFileStorage.ExistsAsync(accessibleDoc.CurrentStoragePath, cancellationToken))
            {
                return Results.NotFound(new ErrorResponse { Error = "Syllabus storage file missing." });
            }

            return await CreateFileResultAsync(syllabusFileStorage, accessibleDoc.CurrentStoragePath, accessibleDoc.CurrentFileName, asAttachment: true, cancellationToken);
        })
        .WithName("DownloadSyllabus")
        .WithSummary("Download syllabus file")
        .WithDescription("Downloads syllabus document file. Accepts 5-digit document ID (e.g. 12345) or GUID.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized);

        downloads.MapGet("/versions/{versionId}/view", async (
            string versionId,
            HttpContext httpContext,
            ApplicationDbContext dbContext,
            ISyllabusFileStorage syllabusFileStorage,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();

            var document = await FindSyllabusByIdOr5DigitAsync(dbContext, versionId, cancellationToken);
            if (document is null) return Results.NotFound(new ErrorResponse { Error = "Syllabus version not found." });

            var hasAccess = await CanAccessSyllabusAsync(dbContext, document, user, cancellationToken);
            if (!hasAccess) return Results.Forbid();

            return await CreateFileResultAsync(syllabusFileStorage, document.CurrentStoragePath, document.CurrentFileName, asAttachment: false, cancellationToken);
        })
        .WithName("ViewSyllabusVersion")
        .WithSummary("View syllabus version inline")
        .WithDescription("Views specific version of a syllabus document. Accepts 5-digit document ID (e.g. 12345) or GUID.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        downloads.MapGet("/versions/{versionId}/download", async (
            string versionId,
            HttpContext httpContext,
            ApplicationDbContext dbContext,
            ISyllabusFileStorage syllabusFileStorage,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();

            var document = await FindSyllabusByIdOr5DigitAsync(dbContext, versionId, cancellationToken);
            if (document is null) return Results.NotFound(new ErrorResponse { Error = "Syllabus version not found." });

            var hasAccess = await CanAccessSyllabusAsync(dbContext, document, user, cancellationToken);
            if (!hasAccess) return Results.Forbid();

            if (!await syllabusFileStorage.ExistsAsync(document.CurrentStoragePath, cancellationToken))
            {
                return Results.NotFound(new ErrorResponse { Error = "Syllabus version file missing." });
            }

            return await CreateFileResultAsync(syllabusFileStorage, document.CurrentStoragePath, document.CurrentFileName, asAttachment: true, cancellationToken);
        })
        .WithName("DownloadSyllabusVersion")
        .WithSummary("Download syllabus version file")
        .WithDescription("Downloads specific version of a syllabus document. Accepts 5-digit document ID (e.g. 12345) or GUID.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // API endpoints
        var api = app.MapGroup("/api/syllabi")
            .WithTags("Syllabi")
            .RequireAuthorization();

        api.MapGet("/search", async (
            [FromQuery] string? term,
            [FromQuery] int maxResults,
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager,
            ISyllabusSearchService syllabusSearchService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var results = await syllabusSearchService.SearchAsync(new SyllabusSearchRequest(term, null, maxResults <= 0 ? 100 : maxResults), user.Role, user.Id, cancellationToken);
            return Results.Ok(results);
        })
        .WithName("SearchSyllabi")
        .WithSummary("Search syllabus repository")
        .WithDescription("Searches syllabi accessible to the logged-in user by keyword, course code, or course title.")
        .Produces<SyllabusSearchResults>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        // Get syllabus versions by GUID or 5-digit DocumentId
        api.MapGet("/{syllabusDocumentId}/versions", async (
            string syllabusDocumentId,
            HttpContext httpContext,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();

            var document = await FindSyllabusByIdOr5DigitAsync(dbContext, syllabusDocumentId, cancellationToken);
            if (document is null) return Results.NotFound(new ErrorResponse { Error = "Syllabus document not found." });

            var hasAccess = await CanAccessSyllabusAsync(dbContext, document, user, cancellationToken);
            if (!hasAccess) return Results.Forbid();

            var versions = new List<SyllabusVersionResponse>
            {
                new SyllabusVersionResponse
                {
                    Id = document.Id,
                    VersionNumber = document.CurrentVersionNumber,
                    FileName = document.CurrentFileName,
                    UploadedAtUtc = document.SubmittedAtUtc ?? document.CreatedAtUtc,
                    UploadedByName = document.InstructorId,
                    ChangeSummary = document.LatestChangeSummary
                }
            };

            return Results.Ok(versions);
        })
        .WithName("GetSyllabusVersions")
        .WithSummary("Get syllabus version history")
        .WithDescription("Retrieves version history list for a syllabus document. Accepts 5-digit document ID (e.g. 12345) or GUID.")
        .Produces<List<SyllabusVersionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<SyllabusDocument?> FindSyllabusByIdOr5DigitAsync(ApplicationDbContext dbContext, string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var trimmed = id.Trim();
        if (Guid.TryParse(trimmed, out var guid))
        {
            return await dbContext.SyllabusDocuments.FirstOrDefaultAsync(d => d.Id == guid, cancellationToken);
        }

        var allDocs = await dbContext.SyllabusDocuments.ToListAsync(cancellationToken);
        return allDocs.FirstOrDefault(s =>
            s.Id.ToString().Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
            Math.Abs(s.Id.GetHashCode() % 90000 + 10000).ToString("D5").Equals(trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<IResult> CreateFileResultAsync(
        ISyllabusFileStorage syllabusFileStorage,
        string storagePath,
        string fileName,
        bool asAttachment,
        CancellationToken cancellationToken)
    {
        if (!await syllabusFileStorage.ExistsAsync(storagePath, cancellationToken))
        {
            return Results.NotFound(new ErrorResponse { Error = "Syllabus document file not found." });
        }

        var memoryStream = new MemoryStream();
        await using (var fileStream = await syllabusFileStorage.OpenReadAsync(storagePath, cancellationToken))
        {
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
        }
        memoryStream.Position = 0;

        var contentType = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };

        return asAttachment
            ? Results.File(memoryStream, contentType, fileName, enableRangeProcessing: true)
            : Results.File(memoryStream, contentType, enableRangeProcessing: true);
    }

    private static async Task<bool> CanAccessSyllabusAsync(
        ApplicationDbContext dbContext,
        SyllabusDocument document,
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (user.Role != SRVS.Domain.Enums.UserRoleType.Student)
        {
            return SyllabusAccessPolicy.CanDownload(document, user.Role, user.Id, user.DepartmentName);
        }

        if (!string.Equals(document.DepartmentName, user.DepartmentName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!document.IsPublished || document.Status != SRVS.Domain.Enums.SyllabusStatus.Approved)
        {
            return false;
        }

        return await dbContext.SyllabusAssignments.AnyAsync(
            assignment => assignment.StudentId == user.Id
                && assignment.SyllabusDocId == document.Id
                && assignment.IsActive
                && assignment.DeletedAt == null,
            cancellationToken);
    }
}
