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
                    AssignedSyllabusId = dbContext.SyllabusAssignments
                        .Where(a => a.StudentId == u.Id && a.IsActive)
                        .Select(a => (Guid?)a.SyllabusId)
                        .FirstOrDefault(),
                    AssignedSyllabusTitle = dbContext.SyllabusAssignments
                        .Where(a => a.StudentId == u.Id && a.IsActive)
                        .Join(dbContext.SyllabusDocuments, a => a.SyllabusId, s => s.Id, (_, s) => s.CourseTitle)
                        .FirstOrDefault(),
                    Status = dbContext.SyllabusAssignments.Any(a => a.StudentId == u.Id && a.IsActive) ? "Assigned" : "Unassigned"
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

        group.MapPost("/assign", async (AssignRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var student = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.StudentId);
            if (student is null) return Results.BadRequest(new { error = "Student not found." });
            if (student.Role != SRVS.Domain.Enums.UserRoleType.Viewer || student.AccountStatus != SRVS.Domain.Enums.UserAccountStatus.Active)
            {
                return Results.BadRequest(new { error = "Student is not an active student account." });
            }

            var syllabus = await dbContext.SyllabusDocuments.FirstOrDefaultAsync(s => s.Id == request.SyllabusId);
            if (syllabus is null) return Results.BadRequest(new { error = "Syllabus not found." });
            if (syllabus.Status is not (SRVS.Domain.Enums.SyllabusStatus.Approved or SRVS.Domain.Enums.SyllabusStatus.Submitted))
            {
                return Results.BadRequest(new { error = "Only approved or submitted syllabi can be assigned." });
            }

            using var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var now = DateTimeOffset.UtcNow;
                var existing = await dbContext.SyllabusAssignments
                    .Where(a => a.StudentId == student.Id && a.IsActive)
                    .ToListAsync();

                foreach (var assignment in existing)
                {
                    assignment.IsActive = false;
                    assignment.DeletedAt = now;
                }

                var newAssignment = new SyllabusAssignment
                {
                    StudentId = student.Id,
                    SyllabusId = syllabus.Id,
                    AssignedBy = user.Id,
                    AssignedAt = now,
                    IsActive = true
                };

                dbContext.SyllabusAssignments.Add(newAssignment);
                await dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new AssignmentResponse
                {
                    Id = newAssignment.Id,
                    StudentFullName = student.FullName,
                    SchoolId = student.InstitutionalId,
                    SyllabusTitle = syllabus.CourseTitle,
                    SubjectCode = syllabus.CourseCode,
                    AssignedAt = newAssignment.AssignedAt,
                    AssignedBy = user.FullName
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });

        group.MapPost("/assign/bulk", async (BulkAssignRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            if (request.StudentIds.Count == 0)
            {
                return Results.BadRequest(new { error = "Select at least one student." });
            }

            var syllabus = await dbContext.SyllabusDocuments.FirstOrDefaultAsync(s => s.Id == request.SyllabusId);
            if (syllabus is null) return Results.BadRequest(new { error = "Syllabus not found." });
            if (syllabus.Status is not (SRVS.Domain.Enums.SyllabusStatus.Approved or SRVS.Domain.Enums.SyllabusStatus.Submitted))
            {
                return Results.BadRequest(new { error = "Only approved or submitted syllabi can be assigned." });
            }

            var students = await dbContext.Users
                .Where(u => request.StudentIds.Contains(u.Id) && u.Role == SRVS.Domain.Enums.UserRoleType.Viewer && u.AccountStatus == SRVS.Domain.Enums.UserAccountStatus.Active)
                .ToListAsync();

            using var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var now = DateTimeOffset.UtcNow;
                foreach (var student in students)
                {
                    var existing = await dbContext.SyllabusAssignments
                        .Where(a => a.StudentId == student.Id && a.IsActive)
                        .ToListAsync();

                    foreach (var assignment in existing)
                    {
                        assignment.IsActive = false;
                        assignment.DeletedAt = now;
                    }

                    dbContext.SyllabusAssignments.Add(new SyllabusAssignment
                    {
                        StudentId = student.Id,
                        SyllabusId = syllabus.Id,
                        AssignedBy = user.Id,
                        AssignedAt = now,
                        IsActive = true
                    });
                }

                await dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new { message = $"Syllabus successfully assigned to {students.Count} student(s).", count = students.Count });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });

    }
}
