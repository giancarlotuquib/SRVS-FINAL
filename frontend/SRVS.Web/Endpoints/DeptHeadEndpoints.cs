using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Entities;
using SRVS.Web.DTOs;
using SRVS.Web.Data;

namespace SRVS.Web.Endpoints;

public static class DeptHeadEndpoints
{
    public static void MapDeptHeadEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/depthead").WithTags("DeptHead").RequireAuthorization();

        group.MapGet("/students", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var students = await dbContext.Users
                .Where(u => u.Role == SRVS.Domain.Enums.UserRoleType.Viewer && u.AccountStatus == SRVS.Domain.Enums.UserAccountStatus.Active)
                .Select(u => new StudentResponse
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    SchoolId = u.InstitutionalId,
                    Email = u.Email ?? string.Empty,
                    AssignedSyllabusId = null,
                    AssignedSyllabusTitle = null,
                    Status = "Active"
                })
                .ToListAsync();

            return Results.Ok(students);
        });

        group.MapGet("/syllabi", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, int? status) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var query = dbContext.SyllabusDocuments
                .Where(s => !string.IsNullOrEmpty(s.OwnerUserId));

            if (status.HasValue)
            {
                var stat = (SRVS.Domain.Enums.SyllabusStatus)status.Value;
                query = query.Where(s => s.Status == stat);
            }

            var syllabi = await query
                .OrderByDescending(s => s.SubmittedAtUtc ?? s.UpdatedAtUtc)
                .Select(s => new SyllabusListResponse
                {
                    Id = s.Id,
                    SubjectCode = s.CourseCode,
                    SubjectTitle = s.CourseTitle,
                    FacultyName = dbContext.Users.Where(u => u.Id == s.OwnerUserId).Select(u => u.FullName).FirstOrDefault() ?? s.InstructorName,
                    AcademicYear = s.AcademicYear,
                    Semester = s.Semester,
                    UploadedAt = s.SubmittedAtUtc ?? DateTimeOffset.MinValue,
                    Status = s.Status
                })
                .ToListAsync();

            return Results.Ok(syllabi);
        });

        group.MapGet("/syllabi/pending", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            // Debug: Get all syllabi in the system
            var allSyllabi = await dbContext.SyllabusDocuments
                .Select(s => new { s.Id, s.CourseCode, s.Status, s.OwnerUserId })
                .ToListAsync();

            var pendingSyllabi = await dbContext.SyllabusDocuments
                .Where(s => s.Status == SRVS.Domain.Enums.SyllabusStatus.Submitted && !string.IsNullOrEmpty(s.OwnerUserId))
                .OrderByDescending(s => s.SubmittedAtUtc ?? s.UpdatedAtUtc)
                .Select(s => new SyllabusPendingResponse
                {
                    Id = s.Id,
                    CourseCode = s.CourseCode,
                    CourseTitle = s.CourseTitle,
                    FacultyName = dbContext.Users.Where(u => u.Id == s.OwnerUserId).Select(u => u.FullName).FirstOrDefault() ?? s.InstructorName,
                    AcademicYear = s.AcademicYear,
                    Semester = s.Semester,
                    CurrentVersionNumber = s.CurrentVersionNumber,
                    CurrentFileName = s.CurrentFileName,
                    SubmittedAtUtc = s.SubmittedAtUtc
                })
                .ToListAsync();

            return Results.Ok(new { 
                TotalSyllabiInSystem = allSyllabi.Count,
                AllSyllabiInSystem = allSyllabi,
                TotalSyllabiInDept = allSyllabi.Count,
                AllSyllabiInDept = allSyllabi,
                PendingCount = pendingSyllabi.Count,
                PendingSyllabi = pendingSyllabi
            });
        });

        group.MapPut("/syllabi/{syllabusId:guid}/approve", async (Guid syllabusId, ReviewSyllabusRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var syllabus = await dbContext.SyllabusDocuments.FirstOrDefaultAsync(s => s.Id == syllabusId);
            if (syllabus is null) return Results.NotFound(new { error = "Syllabus not found." });
            
            // Only Submitted status can be approved
            if (syllabus.Status != SRVS.Domain.Enums.SyllabusStatus.Submitted)
                return Results.BadRequest(new { error = "Only submitted syllabi can be approved." });

            using var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
                syllabus.Status = SRVS.Domain.Enums.SyllabusStatus.Approved;
                syllabus.IsPublished = true;
                syllabus.ReviewedAtUtc = DateTimeOffset.UtcNow;
                syllabus.ReviewedByUserId = user.Id;
                syllabus.ReviewerRemarks = request.Remarks;

                dbContext.SyllabusDocuments.Update(syllabus);
                await dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new { message = "Syllabus approved successfully.", syllabusId = syllabus.Id });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });

        group.MapPut("/syllabi/{syllabusId:guid}/reject", async (Guid syllabusId, ReviewSyllabusRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var syllabus = await dbContext.SyllabusDocuments.FirstOrDefaultAsync(s => s.Id == syllabusId);
            if (syllabus is null) return Results.NotFound(new { error = "Syllabus not found." });
            
            // Only Submitted status can be rejected
            if (syllabus.Status != SRVS.Domain.Enums.SyllabusStatus.Submitted)
                return Results.BadRequest(new { error = "Only submitted syllabi can be rejected." });

            // Remarks are required for rejection
            if (string.IsNullOrWhiteSpace(request.Remarks))
                return Results.BadRequest(new { error = "Remarks are required for rejection." });

            using var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
                syllabus.Status = SRVS.Domain.Enums.SyllabusStatus.Rejected;
                syllabus.IsPublished = false;
                syllabus.ReviewedAtUtc = DateTimeOffset.UtcNow;
                syllabus.ReviewedByUserId = user.Id;
                syllabus.ReviewerRemarks = request.Remarks;

                dbContext.SyllabusDocuments.Update(syllabus);
                await dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new { message = "Syllabus rejected successfully.", syllabusId = syllabus.Id });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });


    }
}
