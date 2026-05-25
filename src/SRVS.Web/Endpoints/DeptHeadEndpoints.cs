using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Entities;
using SyllabusRepository.DTOs;
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

            var deptId = user.DepartmentId;
            var students = await dbContext.Users
                .Where(u => u.Role == SRVS.Domain.Enums.UserRoleType.Viewer && u.DepartmentId == deptId && u.AccountStatus == SRVS.Domain.Enums.UserAccountStatus.Active)
                .Select(u => new StudentResponse
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    SchoolId = u.InstitutionalId,
                    Email = u.Email,
                    Department = dbContext.Departments.Where(d => d.Id == u.DepartmentId).Select(d => d.Name).FirstOrDefault() ?? string.Empty,
                    AssignedSyllabusId = dbContext.SyllabusAssignments.Where(a => a.StudentId == u.Id && a.IsActive).Select(a => a.SyllabusId).FirstOrDefault(),
                    AssignedSyllabusTitle = dbContext.SyllabusAssignments
                        .Where(a => a.StudentId == u.Id && a.IsActive)
                        .Join(dbContext.SyllabusDocuments, a => a.SyllabusId, s => s.Id, (a, s) => s.CourseTitle)
                        .FirstOrDefault(),
                    Status = dbContext.SyllabusAssignments.Any(a => a.StudentId == u.Id && a.IsActive) ? "Assigned" : "Unassigned"
                })
                .ToListAsync();

            // Normalize AssignedSyllabusId default(Guid) -> null
            foreach (var s in students)
            {
                if (s.AssignedSyllabusId == Guid.Empty) s.AssignedSyllabusId = null;
            }

            return Results.Ok(students);
        });

        group.MapGet("/syllabi", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var deptId = user.DepartmentId;
            var syllabi = await dbContext.SyllabusDocuments
                .Where(s => s.DepartmentId == deptId && !string.IsNullOrEmpty(s.OwnerUserId))
                .Select(s => new SyllabusListResponse
                {
                    Id = s.Id,
                    SubjectCode = s.CourseCode,
                    SubjectTitle = s.CourseTitle,
                    FacultyName = s.InstructorName,
                    AcademicYear = s.AcademicYear,
                    Semester = s.Semester,
                    UploadedAt = s.SubmittedAtUtc ?? DateTimeOffset.MinValue
                })
                .ToListAsync();

            return Results.Ok(syllabi);
        });

        group.MapPost("/assign", async (AssignRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            // Validate student
            var student = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.StudentId);
            if (student is null) return Results.BadRequest(new { error = "Student not found." });
            if (student.DepartmentId != user.DepartmentId) return Results.Forbid();

            // Validate syllabus
            var syllabus = await dbContext.SyllabusDocuments.FirstOrDefaultAsync(s => s.Id == request.SyllabusId);
            if (syllabus is null) return Results.BadRequest(new { error = "Syllabus not found." });
            if (syllabus.DepartmentId != user.DepartmentId) return Results.Forbid();

            using var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
                // Deactivate current
                var existing = await dbContext.SyllabusAssignments.Where(a => a.StudentId == student.Id && a.IsActive).ToListAsync();
                foreach (var e in existing)
                {
                    e.IsActive = false;
                    e.DeletedAt = DateTimeOffset.UtcNow;
                    dbContext.SyllabusAssignments.Update(e);
                }

                var assignment = new SyllabusAssignment
                {
                    StudentId = student.Id,
                    SyllabusId = syllabus.Id,
                    AssignedBy = user.Id,
                    AssignedAt = DateTimeOffset.UtcNow,
                    IsActive = true
                };
                dbContext.SyllabusAssignments.Add(assignment);

                await dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                var response = new AssignmentResponse
                {
                    Id = assignment.Id,
                    StudentFullName = student.FullName,
                    SchoolId = student.InstitutionalId,
                    SyllabusTitle = syllabus.CourseTitle,
                    SubjectCode = syllabus.CourseCode,
                    AssignedAt = assignment.AssignedAt,
                    AssignedBy = user.FullName ?? user.Id
                };

                return Results.Ok(response);
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

            var syllabus = await dbContext.SyllabusDocuments.FirstOrDefaultAsync(s => s.Id == request.SyllabusId);
            if (syllabus is null) return Results.BadRequest(new { error = "Syllabus not found." });
            if (syllabus.DepartmentId != user.DepartmentId) return Results.Forbid();

            var students = await dbContext.Users.Where(u => request.StudentIds.Contains(u.Id) && u.DepartmentId == user.DepartmentId).ToListAsync();

            using var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var now = DateTimeOffset.UtcNow;
                var created = 0;
                foreach (var student in students)
                {
                    var existing = await dbContext.SyllabusAssignments.Where(a => a.StudentId == student.Id && a.IsActive).ToListAsync();
                    foreach (var e in existing)
                    {
                        e.IsActive = false;
                        e.DeletedAt = now;
                        dbContext.SyllabusAssignments.Update(e);
                    }

                    var assignment = new SyllabusAssignment
                    {
                        StudentId = student.Id,
                        SyllabusId = syllabus.Id,
                        AssignedBy = user.Id,
                        AssignedAt = now,
                        IsActive = true
                    };
                    dbContext.SyllabusAssignments.Add(assignment);
                    created++;
                }

                await dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new { message = $"Syllabus successfully assigned to {created} students.", count = created });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });

        group.MapGet("/assignments", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var deptId = user.DepartmentId;
            var assignments = await dbContext.SyllabusAssignments
                .Where(a => a.IsActive)
                .Join(dbContext.Users, a => a.StudentId, u => u.Id, (a, u) => new { a, u })
                .Join(dbContext.SyllabusDocuments, au => au.a.SyllabusId, s => s.Id, (au, s) => new AssignmentResponse
                {
                    Id = au.a.Id,
                    StudentFullName = au.u.FullName,
                    SchoolId = au.u.InstitutionalId,
                    SyllabusTitle = s.CourseTitle,
                    SubjectCode = s.CourseCode,
                    AssignedAt = au.a.AssignedAt,
                    AssignedBy = au.a.AssignedBy
                })
                .ToListAsync();

            return Results.Ok(assignments);
        });

        group.MapDelete("/assign/{id:guid}", async (Guid id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var assignment = await dbContext.SyllabusAssignments.FirstOrDefaultAsync(a => a.Id == id && a.IsActive);
            if (assignment is null) return Results.NotFound();

            var student = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == assignment.StudentId);
            if (student is null) return Results.NotFound();
            if (student.DepartmentId != user.DepartmentId) return Results.Forbid();

            assignment.IsActive = false;
            assignment.DeletedAt = DateTimeOffset.UtcNow;
            dbContext.SyllabusAssignments.Update(assignment);
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        });

        // Courses endpoints
        group.MapGet("/courses", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var deptId = user.DepartmentId;
            var courses = await dbContext.CourseAssignments
                .Where(c => c.DepartmentId == deptId && c.IsActive)
                .Select(c => new { c.Id, c.CourseCode, c.CourseTitle, c.InstructorName })
                .ToListAsync();

            return Results.Ok(courses);
        });

        group.MapGet("/courses/{courseId:guid}/syllabi", async (Guid courseId, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var course = await dbContext.CourseAssignments.FirstOrDefaultAsync(c => c.Id == courseId);
            if (course is null) return Results.NotFound();
            if (course.DepartmentId != user.DepartmentId) return Results.Forbid();

            var syllabi = await dbContext.SyllabusDocuments
                .Where(s => s.CourseCode == course.CourseCode && s.DepartmentId == user.DepartmentId)
                .Select(s => new { s.Id, s.CourseCode, s.CourseTitle, s.AcademicYear, s.Semester, s.SubmittedAtUtc })
                .ToListAsync();

            return Results.Ok(syllabi);
        });

        // Student course management
        group.MapPost("/students/{studentId:guid}/courses", async (Guid studentId, AssignRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var student = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == studentId);
            if (student is null) return Results.NotFound();
            if (student.DepartmentId != user.DepartmentId) return Results.Forbid();

            var syllabus = await dbContext.SyllabusDocuments.FirstOrDefaultAsync(s => s.Id == request.SyllabusId);
            if (syllabus is null) return Results.BadRequest(new { error = "Syllabus not found." });
            if (syllabus.DepartmentId != user.DepartmentId) return Results.Forbid();

            using var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await dbContext.SyllabusAssignments.Where(a => a.StudentId == student.Id && a.IsActive).ToListAsync();
                foreach (var e in existing)
                {
                    e.IsActive = false;
                    e.DeletedAt = DateTimeOffset.UtcNow;
                    dbContext.SyllabusAssignments.Update(e);
                }

                var assignment = new SyllabusAssignment
                {
                    StudentId = student.Id,
                    SyllabusId = syllabus.Id,
                    AssignedBy = user.Id,
                    AssignedAt = DateTimeOffset.UtcNow,
                    IsActive = true
                };
                dbContext.SyllabusAssignments.Add(assignment);

                await dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new { message = "Course assigned successfully." });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });

        group.MapDelete("/students/{studentId:guid}/courses/{courseId:guid}", async (Guid studentId, Guid courseId, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var student = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == studentId);
            if (student is null) return Results.NotFound();
            if (student.DepartmentId != user.DepartmentId) return Results.Forbid();

            var course = await dbContext.CourseAssignments.FirstOrDefaultAsync(c => c.Id == courseId);
            if (course is null) return Results.NotFound();

            var assignment = await dbContext.SyllabusAssignments
                .FirstOrDefaultAsync(a => a.StudentId == studentId && a.SyllabusId == courseId && a.IsActive);
            if (assignment is null) return Results.NotFound();

            assignment.IsActive = false;
            assignment.DeletedAt = DateTimeOffset.UtcNow;
            dbContext.SyllabusAssignments.Update(assignment);
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        });

        group.MapGet("/students/{studentId:guid}/syllabi", async (Guid studentId, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var student = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == studentId);
            if (student is null) return Results.NotFound();
            if (student.DepartmentId != user.DepartmentId) return Results.Forbid();

            var syllabi = await dbContext.SyllabusAssignments
                .Where(a => a.StudentId == studentId && a.IsActive)
                .Join(dbContext.SyllabusDocuments, a => a.SyllabusId, s => s.Id, (a, s) => new { s.Id, s.CourseCode, s.CourseTitle, s.AcademicYear, s.Semester })
                .ToListAsync();

            return Results.Ok(syllabi);
        });
    }
}
