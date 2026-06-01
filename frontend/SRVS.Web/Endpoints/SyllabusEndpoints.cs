using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SRVS.Application.Abstractions;
using SRVS.Application.Models;
using SRVS.Domain.Entities;
using SRVS.Application.Services;
using SRVS.Web.Data;
using System.IO;

namespace SRVS.Web.Endpoints;

public static class SyllabusEndpoints
{
    public static IEndpointRouteBuilder MapSyllabusEndpoints(this IEndpointRouteBuilder app)
    {
        // Download endpoints
        var downloads = app.MapGroup("/syllabi")
            .WithTags("Syllabi Downloads")
            .RequireAuthorization();

        downloads.MapGet("/{syllabusDocumentId:guid}/view", async (
            Guid syllabusDocumentId,
            HttpContext httpContext,
            ISyllabusSearchService syllabusSearchService,
            ISyllabusFileStorage syllabusFileStorage,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var document = await syllabusSearchService.GetAccessibleDocumentAsync(syllabusDocumentId, user.Role, user.DepartmentId, user.Id, cancellationToken);
            if (document is null)
            {
                return Results.NotFound();
            }

            return await CreateFileResultAsync(syllabusFileStorage, document.CurrentStoragePath, document.CurrentFileName, asAttachment: false, cancellationToken);
        })
        .WithName("ViewSyllabus");

        downloads.MapGet("/{syllabusDocumentId:guid}/download", async (
            Guid syllabusDocumentId,
            HttpContext httpContext,
            ISyllabusSearchService syllabusSearchService,
            ISyllabusFileStorage syllabusFileStorage,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var document = await syllabusSearchService.GetAccessibleDocumentAsync(syllabusDocumentId, user.Role, user.DepartmentId, user.Id, cancellationToken);
            if (document is null)
            {
                return Results.NotFound();
            }

            if (!await syllabusFileStorage.ExistsAsync(document.CurrentStoragePath, cancellationToken))
            {
                return Results.NotFound();
            }

            return await CreateFileResultAsync(syllabusFileStorage, document.CurrentStoragePath, document.CurrentFileName, asAttachment: true, cancellationToken);
        })
        .WithName("DownloadSyllabus");

        downloads.MapGet("/versions/{versionId:guid}/view", async (
            Guid versionId,
            HttpContext httpContext,
            ApplicationDbContext dbContext,
            ISyllabusFileStorage syllabusFileStorage,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();

            var version = await dbContext.SyllabusVersions
                .Include(v => v.SyllabusDocument)
                .FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);

            if (version is null || version.SyllabusDocument is null) return Results.NotFound();

            var hasAccess = SyllabusAccessPolicy.CanDownload(version.SyllabusDocument, user.Role, user.DepartmentId, user.Id);
            if (!hasAccess) return Results.Forbid();

            return await CreateFileResultAsync(syllabusFileStorage, version.StoragePath, version.FileName, asAttachment: false, cancellationToken);
        })
        .WithName("ViewSyllabusVersion");

        downloads.MapGet("/versions/{versionId:guid}/download", async (
            Guid versionId,
            HttpContext httpContext,
            ApplicationDbContext dbContext,
            ISyllabusFileStorage syllabusFileStorage,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();

            var version = await dbContext.SyllabusVersions
                .Include(v => v.SyllabusDocument)
                .FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);
                
            if (version is null || version.SyllabusDocument is null) return Results.NotFound();

            // Basic permission check - admin, dept head of same dept, or owner
            var hasAccess = SyllabusAccessPolicy.CanDownload(version.SyllabusDocument, user.Role, user.DepartmentId, user.Id);

            if (!hasAccess) return Results.Forbid();

            if (!await syllabusFileStorage.ExistsAsync(version.StoragePath, cancellationToken))
            {
                return Results.NotFound();
            }

            return await CreateFileResultAsync(syllabusFileStorage, version.StoragePath, version.FileName, asAttachment: true, cancellationToken);
        })
        .WithName("DownloadSyllabusVersion");

        // API endpoints
        var api = app.MapGroup("/api/syllabi")
            .WithTags("Syllabi")
            .RequireAuthorization();

        api.MapGet("/search", async (
            string? term,
            int maxResults,
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

            var results = await syllabusSearchService.SearchAsync(new SyllabusSearchRequest(term, null, maxResults <= 0 ? 100 : maxResults), user.Role, user.DepartmentId, user.Id, cancellationToken);
            return Results.Ok(results);
        })
        .WithName("SearchSyllabi");

        // Get syllabus versions
        api.MapGet("/{syllabusDocumentId:guid}/versions", async (
            Guid syllabusDocumentId,
            HttpContext httpContext,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();

            var document = await dbContext.SyllabusDocuments
                .Include(d => d.Versions)
                .FirstOrDefaultAsync(d => d.Id == syllabusDocumentId, cancellationToken);
            
            if (document is null) return Results.NotFound();

            var hasAccess = SyllabusAccessPolicy.CanDownload(document, user.Role, user.DepartmentId, user.Id);
            if (!hasAccess) return Results.Forbid();

            var versions = document.Versions
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new
                {
                    v.Id,
                    v.VersionNumber,
                    v.FileName,
                    v.UploadedAtUtc,
                    v.UploadedByName,
                    v.ChangeSummary
                })
                .ToList();

            return Results.Ok(versions);
        })
        .WithName("GetSyllabusVersions");

        return app;
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
            return Results.NotFound();
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
}
