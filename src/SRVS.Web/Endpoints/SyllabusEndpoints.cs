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

            var stream = await syllabusFileStorage.OpenReadAsync(document.CurrentStoragePath, cancellationToken);
            var contentType = Path.GetExtension(document.CurrentFileName).ToLowerInvariant() switch
            {
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };

            return Results.File(stream, contentType, document.CurrentFileName);
        })
        .WithName("DownloadSyllabus");

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

            var stream = await syllabusFileStorage.OpenReadAsync(version.StoragePath, cancellationToken);
            var contentType = Path.GetExtension(version.FileName).ToLowerInvariant() switch
            {
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };

            return Results.File(stream, contentType, version.FileName);
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

        return app;
    }
}
