using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;
using SRVS.Web.Data;

namespace SRVS.Web.Endpoints;

public static class StudentEndpoints
{
    public static void MapStudentEndpoints(this WebApplication app)
    {
        var studentGroup = app.MapGroup("/api/student").WithTags("Student").RequireAuthorization();

        studentGroup.MapGet("/syllabi", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Viewer) return Results.Forbid();

            var syllabi = await dbContext.SyllabusAssignments
                .Where(a => a.StudentId == user.Id && a.IsActive)
                .Join(dbContext.SyllabusDocuments, a => a.SyllabusId, s => s.Id, (a, s) => new 
                { 
                    s.Id, 
                    s.CourseCode, 
                    s.CourseTitle, 
                    s.AcademicYear, 
                    s.Semester,
                    s.InstructorName,
                    a.AssignedAt 
                })
                .ToListAsync();

            return Results.Ok(syllabi);
        });

        studentGroup.MapGet("/courses", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Viewer) return Results.Forbid();

            var courses = await dbContext.SyllabusAssignments
                .Where(a => a.StudentId == user.Id && a.IsActive)
                .Join(dbContext.SyllabusDocuments, a => a.SyllabusId, s => s.Id, (a, s) => new 
                { 
                    s.CourseCode, 
                    s.CourseTitle,
                    s.AcademicYear,
                    s.Semester 
                })
                .Distinct()
                .ToListAsync();

            return Results.Ok(courses);
        });

        studentGroup.MapGet("/profile", async (HttpContext httpContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Viewer) return Results.Forbid();

            return Results.Ok(new 
            { 
                user.Id, 
                user.Email, 
                user.FullName, 
                user.InstitutionalId, 
                user.Role, 
                user.AccountStatus,
                user.DepartmentId 
            });
        });
    }
}
