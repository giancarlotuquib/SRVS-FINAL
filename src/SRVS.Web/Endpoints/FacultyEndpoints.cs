using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;
using SRVS.Web.Data;

namespace SRVS.Web.Endpoints;

public static class FacultyEndpoints
{
    public static void MapFacultyEndpoints(this WebApplication app)
    {
        var facultyGroup = app.MapGroup("/api/faculty").WithTags("Faculty").RequireAuthorization();

        facultyGroup.MapGet("/syllabi", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var syllabi = await dbContext.SyllabusDocuments
                .Where(s => s.OwnerUserId == user.Id)
                .Select(s => new { s.Id, s.CourseCode, s.CourseTitle, s.AcademicYear, s.Semester, s.CurrentVersionNumber, s.SubmittedAtUtc, s.WorkflowStatus })
                .ToListAsync();

            return Results.Ok(syllabi);
        });

        facultyGroup.MapPost("/syllabi", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            // Placeholder for syllabus creation
            return Results.Ok(new { message = "Syllabus creation endpoint (placeholder)." });
        });

        facultyGroup.MapGet("/syllabi/{id:guid}", async (Guid id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var syllabus = await dbContext.SyllabusDocuments
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerUserId == user.Id);
            if (syllabus is null) return Results.NotFound();

            return Results.Ok(new { syllabus.Id, syllabus.CourseCode, syllabus.CourseTitle, syllabus.AcademicYear, syllabus.Semester, syllabus.CurrentVersionNumber, syllabus.WorkflowStatus });
        });

        facultyGroup.MapPut("/syllabi/{id:guid}", async (Guid id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var syllabus = await dbContext.SyllabusDocuments
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerUserId == user.Id);
            if (syllabus is null) return Results.NotFound();

            // Placeholder for syllabus update
            return Results.Ok(new { message = "Syllabus update endpoint (placeholder)." });
        });

        facultyGroup.MapDelete("/syllabi/{id:guid}", async (Guid id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var syllabus = await dbContext.SyllabusDocuments
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerUserId == user.Id);
            if (syllabus is null) return Results.NotFound();

            // Placeholder for syllabus deletion
            return Results.Ok(new { message = "Syllabus deletion endpoint (placeholder)." });
        });
    }
}
