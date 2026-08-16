using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;
using SRVS.Web.Data;
using SRVS.Web.DTOs;

namespace SRVS.Web.Endpoints;

public static class FacultyEndpoints
{
    public static void MapFacultyEndpoints(this WebApplication app)
    {
        var facultyGroup = app.MapGroup("/api/faculty").WithTags("Faculty").RequireAuthorization();

        // 1. Get all syllabi owned by logged-in faculty
        facultyGroup.MapGet("/syllabi", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var syllabi = await dbContext.SyllabusDocuments
                .Where(s => s.OwnerUserId == user.Id && s.DepartmentName == user.DepartmentName)
                .OrderByDescending(s => s.UpdatedAtUtc ?? s.CreatedAtUtc)
                .Select(s => new SyllabusDetailResponse
                {
                    Id = s.Id,
                    CourseCode = s.CourseCode,
                    CourseTitle = s.CourseTitle,
                    AcademicYear = s.AcademicYear,
                    Semester = s.Semester,
                    DepartmentName = s.DepartmentName,
                    InstructorId = s.InstructorId,
                    CurrentVersionNumber = s.CurrentVersionNumber,
                    Status = s.Status,
                    CurrentFileName = s.CurrentFileName,
                    ReviewerRemarks = s.ReviewerRemarks,
                    SubmittedAtUtc = s.SubmittedAtUtc,
                    CreatedAtUtc = s.CreatedAtUtc
                })
                .ToListAsync();

            return Results.Ok(syllabi);
        })
        .Produces<List<SyllabusDetailResponse>>(StatusCodes.Status200OK)
        .WithName("GetFacultySyllabi");

        // 2. Create new syllabus draft
        facultyGroup.MapPost("/syllabi", async (CreateSyllabusRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var doc = new SyllabusDocument
            {
                CourseCode = request.CourseCode.Trim(),
                CourseTitle = request.CourseTitle.Trim(),
                AcademicYear = request.AcademicYear.Trim(),
                Semester = request.Semester.Trim(),
                DepartmentName = user.DepartmentName,
                InstructorId = string.IsNullOrWhiteSpace(request.InstructorId) ? user.Id : request.InstructorId.Trim(),
                OwnerUserId = user.Id,
                Status = SyllabusStatus.Draft,
                CurrentVersionNumber = 1,
                CurrentFileName = request.FileName ?? $"{request.CourseCode.Trim()}_Syllabus.pdf",
                CurrentStoragePath = $"syllabi/{Guid.NewGuid()}_{request.FileName ?? "document.pdf"}",
                IsPublished = false
            };

            dbContext.SyllabusDocuments.Add(doc);
            await dbContext.SaveChangesAsync();

            var response = new SyllabusDetailResponse
            {
                Id = doc.Id,
                CourseCode = doc.CourseCode,
                CourseTitle = doc.CourseTitle,
                AcademicYear = doc.AcademicYear,
                Semester = doc.Semester,
                DepartmentName = doc.DepartmentName,
                InstructorId = doc.InstructorId,
                CurrentVersionNumber = doc.CurrentVersionNumber,
                Status = doc.Status,
                CurrentFileName = doc.CurrentFileName,
                ReviewerRemarks = doc.ReviewerRemarks,
                SubmittedAtUtc = doc.SubmittedAtUtc,
                CreatedAtUtc = doc.CreatedAtUtc
            };

            return Results.Created($"/api/faculty/syllabi/{doc.Id}", response);
        })
        .Produces<SyllabusDetailResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("CreateSyllabus");

        // 3. Get single syllabus details by ID
        facultyGroup.MapGet("/syllabi/{id:guid}", async (Guid id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var s = await dbContext.SyllabusDocuments
                .FirstOrDefaultAsync(doc => doc.Id == id && doc.OwnerUserId == user.Id);

            if (s is null) return Results.NotFound(new { error = "Syllabus not found." });

            return Results.Ok(new SyllabusDetailResponse
            {
                Id = s.Id,
                CourseCode = s.CourseCode,
                CourseTitle = s.CourseTitle,
                AcademicYear = s.AcademicYear,
                Semester = s.Semester,
                DepartmentName = s.DepartmentName,
                InstructorId = s.InstructorId,
                CurrentVersionNumber = s.CurrentVersionNumber,
                Status = s.Status,
                CurrentFileName = s.CurrentFileName,
                ReviewerRemarks = s.ReviewerRemarks,
                SubmittedAtUtc = s.SubmittedAtUtc,
                CreatedAtUtc = s.CreatedAtUtc
            });
        })
        .Produces<SyllabusDetailResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("GetFacultySyllabusById");

        // 4. Update syllabus details (Draft / Rejected)
        facultyGroup.MapPut("/syllabi/{id:guid}", async (Guid id, UpdateSyllabusRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var s = await dbContext.SyllabusDocuments
                .FirstOrDefaultAsync(doc => doc.Id == id && doc.OwnerUserId == user.Id);

            if (s is null) return Results.NotFound(new { error = "Syllabus not found." });

            if (!string.IsNullOrWhiteSpace(request.CourseCode)) s.CourseCode = request.CourseCode.Trim();
            if (!string.IsNullOrWhiteSpace(request.CourseTitle)) s.CourseTitle = request.CourseTitle.Trim();
            if (!string.IsNullOrWhiteSpace(request.AcademicYear)) s.AcademicYear = request.AcademicYear.Trim();
            if (!string.IsNullOrWhiteSpace(request.Semester)) s.Semester = request.Semester.Trim();
            if (!string.IsNullOrWhiteSpace(request.InstructorId)) s.InstructorId = request.InstructorId.Trim();
            if (!string.IsNullOrWhiteSpace(request.FileName)) s.CurrentFileName = request.FileName.Trim();
            if (!string.IsNullOrWhiteSpace(request.ChangeSummary)) s.LatestChangeSummary = request.ChangeSummary.Trim();

            s.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync();

            return Results.Ok(new SyllabusDetailResponse
            {
                Id = s.Id,
                CourseCode = s.CourseCode,
                CourseTitle = s.CourseTitle,
                AcademicYear = s.AcademicYear,
                Semester = s.Semester,
                DepartmentName = s.DepartmentName,
                InstructorId = s.InstructorId,
                CurrentVersionNumber = s.CurrentVersionNumber,
                Status = s.Status,
                CurrentFileName = s.CurrentFileName,
                ReviewerRemarks = s.ReviewerRemarks,
                SubmittedAtUtc = s.SubmittedAtUtc,
                CreatedAtUtc = s.CreatedAtUtc
            });
        })
        .Produces<SyllabusDetailResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("UpdateFacultySyllabus");

        // 5. Submit syllabus for Department Head review
        facultyGroup.MapPost("/syllabi/{id:guid}/submit", async (Guid id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var s = await dbContext.SyllabusDocuments
                .FirstOrDefaultAsync(doc => doc.Id == id && doc.OwnerUserId == user.Id);

            if (s is null) return Results.NotFound(new { error = "Syllabus not found." });

            s.Status = SyllabusStatus.Submitted;
            s.SubmittedAtUtc = DateTimeOffset.UtcNow;
            s.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync();

            return Results.Ok(new SyllabusDetailResponse
            {
                Id = s.Id,
                CourseCode = s.CourseCode,
                CourseTitle = s.CourseTitle,
                AcademicYear = s.AcademicYear,
                Semester = s.Semester,
                DepartmentName = s.DepartmentName,
                InstructorId = s.InstructorId,
                CurrentVersionNumber = s.CurrentVersionNumber,
                Status = s.Status,
                CurrentFileName = s.CurrentFileName,
                ReviewerRemarks = s.ReviewerRemarks,
                SubmittedAtUtc = s.SubmittedAtUtc,
                CreatedAtUtc = s.CreatedAtUtc
            });
        })
        .Produces<SyllabusDetailResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("SubmitSyllabusForReview");

        // 6. Delete draft syllabus
        facultyGroup.MapDelete("/syllabi/{id:guid}", async (Guid id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var s = await dbContext.SyllabusDocuments
                .FirstOrDefaultAsync(doc => doc.Id == id && doc.OwnerUserId == user.Id);

            if (s is null) return Results.NotFound(new { error = "Syllabus not found." });

            if (s.Status == SyllabusStatus.Approved)
            {
                return Results.BadRequest(new { error = "Approved syllabi cannot be deleted." });
            }

            dbContext.SyllabusDocuments.Remove(s);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { message = "Syllabus deleted successfully." });
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("DeleteFacultySyllabus");
    }
}
