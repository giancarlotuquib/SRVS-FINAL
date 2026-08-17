using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRVS.Application.Abstractions;
using SRVS.Application.Models;
using SRVS.Domain.Entities;
using SRVS.Web.Data;
using SRVS.Web.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SRVS.Web.Endpoints;

public static class DeptHeadEndpoints
{
    public static void MapDeptHeadEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/depthead").WithTags("DeptHead").RequireAuthorization();

        // 1. Get active students in department with syllabus assignment status
        group.MapGet("/students", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var students = await dbContext.Users
                .Where(u => u.Role == SRVS.Domain.Enums.UserRoleType.Student && u.AccountStatus == SRVS.Domain.Enums.UserAccountStatus.Active)
                .Select(u => new StudentResponse
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    FullName = u.FullName,
                    SchoolId = u.Id,
                    Email = u.Email ?? string.Empty,
                    AssignedSyllabusId = dbContext.SyllabusAssignments
                        .Where(a => a.StudentId == u.Id && a.IsActive)
                        .Select(a => (Guid?)a.SyllabusDocId)
                        .FirstOrDefault(),
                    AssignedSyllabusTitle = dbContext.SyllabusAssignments
                        .Where(a => a.StudentId == u.Id && a.IsActive)
                        .Join(dbContext.SyllabusDocuments, a => a.SyllabusDocId, s => s.Id, (_, s) => s.CourseTitle)
                        .FirstOrDefault(),
                    Status = dbContext.SyllabusAssignments.Any(a => a.StudentId == u.Id && a.IsActive) ? "Assigned" : "Unassigned"
                })
                .ToListAsync();

            return Results.Ok(students);
        })
        .WithName("GetDeptHeadStudents")
        .WithSummary("Get department students")
        .WithDescription("Retrieves all active student accounts along with their current syllabus assignment status.")
        .Produces<List<StudentResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 2. Get department syllabi optionally filtered by status
        group.MapGet("/syllabi", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, [FromQuery] int? status) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var query = dbContext.SyllabusDocuments
                .Where(s => !string.IsNullOrEmpty(s.OwnerUserId) && s.DepartmentName == user.DepartmentName);

            if (status.HasValue)
            {
                var stat = (SRVS.Domain.Enums.SyllabusStatus)status.Value;
                query = query.Where(s => s.Status == stat);
            }

            var syllabiList = await query
                .Select(s => new SyllabusListResponse
                {
                    Id = s.Id,
                    SubjectCode = s.CourseCode,
                    SubjectTitle = s.CourseTitle,
                    FacultyName = dbContext.Users.Where(u => u.Id == s.OwnerUserId).Select(u => u.FullName).FirstOrDefault() ?? "Faculty Member",
                    AcademicYear = s.AcademicYear,
                    Semester = s.Semester,
                    UploadedAt = s.SubmittedAtUtc ?? DateTimeOffset.MinValue,
                    Status = s.Status
                })
                .ToListAsync();

            var syllabi = syllabiList.OrderByDescending(s => s.UploadedAt).ToList();

            return Results.Ok(syllabi);
        })
        .WithName("GetDeptHeadSyllabi")
        .WithSummary("Get department syllabi")
        .WithDescription("Retrieves all department syllabi, optionally filtered by status (0=Draft, 1=Submitted, 2=Approved, 3=Rejected).")
        .Produces<List<SyllabusListResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 3. Create / Insert syllabus (JSON metadata)
        group.MapPost("/syllabi", async ([FromBody] CreateSyllabusRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            if (string.IsNullOrWhiteSpace(request.CourseCode) || string.IsNullOrWhiteSpace(request.CourseTitle))
            {
                return Results.BadRequest(new ErrorResponse { Error = "CourseCode and CourseTitle are required." });
            }

            var doc = new SyllabusDocument
            {
                CourseCode = request.CourseCode.Trim(),
                CourseTitle = request.CourseTitle.Trim(),
                AcademicYear = string.IsNullOrWhiteSpace(request.AcademicYear) ? "2025-2026" : request.AcademicYear.Trim(),
                Semester = string.IsNullOrWhiteSpace(request.Semester) ? "First Semester" : request.Semester.Trim(),
                DepartmentName = user.DepartmentName,
                InstructorId = string.IsNullOrWhiteSpace(request.InstructorId) ? user.Id : request.InstructorId.Trim(),
                OwnerUserId = user.Id,
                Status = SRVS.Domain.Enums.SyllabusStatus.Approved,
                CurrentVersionNumber = 1,
                CurrentFileName = request.FileName ?? $"{request.CourseCode.Trim()}_Syllabus.pdf",
                CurrentStoragePath = $"syllabi/{Guid.NewGuid()}_{request.FileName ?? "document.pdf"}",
                IsPublished = true,
                ReviewedAtUtc = DateTimeOffset.UtcNow,
                ReviewedByUserId = user.Id
            };

            dbContext.SyllabusDocuments.Add(doc);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/api/depthead/syllabi/{doc.Id}", new SyllabusDetailResponse
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
            });
        })
        .WithName("CreateDeptHeadSyllabus")
        .WithSummary("Create and insert a syllabus")
        .WithDescription("Allows Department Head to insert or create an approved syllabus directly into the department repository.")
        .Produces<SyllabusDetailResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 4. Upload and Insert syllabus document (Multipart Form File Upload)
        group.MapPost("/syllabi/upload", async (
            [FromForm] UploadSyllabusFormRequest form,
            HttpContext httpContext,
            ISyllabusWorkflowService workflowService,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            if (form.File is null || form.File.Length == 0)
            {
                return Results.BadRequest(new ErrorResponse { Error = "A valid syllabus file is required." });
            }

            await using var stream = form.File.OpenReadStream();
            var upsertRequest = new SyllabusDraftUpsertRequest(
                SyllabusDocumentId: null,
                CourseCode: form.CourseCode,
                CourseTitle: form.CourseTitle,
                AcademicYear: string.IsNullOrWhiteSpace(form.AcademicYear) ? "2025-2026" : form.AcademicYear.Trim(),
                Semester: string.IsNullOrWhiteSpace(form.Semester) ? "First Semester" : form.Semester.Trim(),
                InstructorId: string.IsNullOrWhiteSpace(form.InstructorId) ? user.Id : form.InstructorId.Trim(),
                ChangeSummary: string.IsNullOrWhiteSpace(form.ChangeSummary) ? "Uploaded and inserted by Department Head" : form.ChangeSummary.Trim(),
                FileStream: stream,
                OriginalFileName: form.File.FileName,
                UploadedByUserId: user.Id,
                UploadedByName: user.FullName,
                DepartmentName: user.DepartmentName);

            var doc = await workflowService.SaveDraftAsync(upsertRequest, cancellationToken);
            doc.Status = SRVS.Domain.Enums.SyllabusStatus.Approved;
            doc.IsPublished = true;
            doc.ReviewedAtUtc = DateTimeOffset.UtcNow;
            doc.ReviewedByUserId = user.Id;
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/depthead/syllabi/{doc.Id}", new SyllabusDetailResponse
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
            });
        })
        .DisableAntiforgery()
        .WithName("UploadDeptHeadSyllabus")
        .WithSummary("Upload and insert a syllabus document file")
        .WithDescription("Allows Department Head to upload a syllabus PDF/DOCX file and insert an approved syllabus directly.")
        .Produces<SyllabusDetailResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 5. Get pending syllabi for review
        group.MapGet("/syllabi/pending", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var deptSyllabi = await dbContext.SyllabusDocuments
                .Where(s => s.DepartmentName == user.DepartmentName)
                .Select(s => new { s.Id, s.CourseCode, s.Status, s.OwnerUserId })
                .ToListAsync();

            var pendingSyllabiList = await dbContext.SyllabusDocuments
                .Where(s => s.Status == SRVS.Domain.Enums.SyllabusStatus.Submitted && !string.IsNullOrEmpty(s.OwnerUserId) && s.DepartmentName == user.DepartmentName)
                .Select(s => new SyllabusPendingResponse
                {
                    Id = s.Id,
                    CourseCode = s.CourseCode,
                    CourseTitle = s.CourseTitle,
                    FacultyName = dbContext.Users.Where(u => u.Id == s.OwnerUserId).Select(u => u.FullName).FirstOrDefault() ?? "Faculty Member",
                    AcademicYear = s.AcademicYear,
                    Semester = s.Semester,
                    CurrentVersionNumber = s.CurrentVersionNumber,
                    CurrentFileName = s.CurrentFileName,
                    SubmittedAtUtc = s.SubmittedAtUtc
                })
                .ToListAsync();

            var pendingSyllabi = pendingSyllabiList.OrderByDescending(s => s.SubmittedAtUtc).ToList();

            return Results.Ok(new { 
                TotalSyllabiInSystem = deptSyllabi.Count,
                AllSyllabiInSystem = deptSyllabi,
                TotalSyllabiInDept = deptSyllabi.Count,
                AllSyllabiInDept = deptSyllabi,
                PendingCount = pendingSyllabi.Count,
                PendingSyllabi = pendingSyllabi
            });
        })
        .WithName("GetDeptHeadPendingSyllabi")
        .WithSummary("Get pending syllabi for review")
        .WithDescription("Retrieves a summary and list of pending syllabus submissions awaiting review by the Department Head.")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 6. Approve submitted syllabus (by GUID or 5-digit DocumentId)
        group.MapPut("/syllabi/{syllabusId}/approve", async (string syllabusId, [FromBody] ReviewSyllabusRequest? request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var allDeptSyllabi = await dbContext.SyllabusDocuments.Where(s => s.DepartmentName == user.DepartmentName).ToListAsync();
            var syllabus = FindSyllabusByIdOr5Digit(allDeptSyllabi, syllabusId);

            if (syllabus is null) return Results.NotFound(new ErrorResponse { Error = "Syllabus not found." });
            
            if (syllabus.Status != SRVS.Domain.Enums.SyllabusStatus.Submitted)
                return Results.BadRequest(new ErrorResponse { Error = "Only submitted syllabi can be approved." });

            using var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
                syllabus.Status = SRVS.Domain.Enums.SyllabusStatus.Approved;
                syllabus.IsPublished = true;
                syllabus.ReviewedAtUtc = DateTimeOffset.UtcNow;
                syllabus.ReviewedByUserId = user.Id;
                syllabus.ReviewerRemarks = request?.Remarks;

                dbContext.SyllabusDocuments.Update(syllabus);
                await dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new ApproveRejectResponse { Message = "Syllabus approved successfully.", SyllabusId = syllabus.Id });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        })
        .WithName("ApproveSyllabus")
        .WithSummary("Approve a submitted syllabus")
        .WithDescription("Approves a faculty-submitted syllabus and publishes it to department students. Accepts 5-digit document ID (e.g. 12345) or GUID.")
        .Produces<ApproveRejectResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 7. Reject submitted syllabus (by GUID or 5-digit DocumentId)
        group.MapPut("/syllabi/{syllabusId}/reject", async (string syllabusId, [FromBody] ReviewSyllabusRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var allDeptSyllabi = await dbContext.SyllabusDocuments.Where(s => s.DepartmentName == user.DepartmentName).ToListAsync();
            var syllabus = FindSyllabusByIdOr5Digit(allDeptSyllabi, syllabusId);

            if (syllabus is null) return Results.NotFound(new ErrorResponse { Error = "Syllabus not found." });
            
            if (syllabus.Status != SRVS.Domain.Enums.SyllabusStatus.Submitted)
                return Results.BadRequest(new ErrorResponse { Error = "Only submitted syllabi can be rejected." });

            if (string.IsNullOrWhiteSpace(request?.Remarks))
                return Results.BadRequest(new ErrorResponse { Error = "Remarks are required for rejection." });

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

                return Results.Ok(new ApproveRejectResponse { Message = "Syllabus rejected successfully.", SyllabusId = syllabus.Id });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        })
        .WithName("RejectSyllabus")
        .WithSummary("Reject a submitted syllabus")
        .WithDescription("Rejects a faculty-submitted syllabus with reviewer feedback. Accepts 5-digit document ID (e.g. 12345) or GUID.")
        .Produces<ApproveRejectResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 8. Assign syllabus to a student
        group.MapPost("/assign", async ([FromBody] AssignRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            var student = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.StudentId);
            if (student is null) return Results.BadRequest(new ErrorResponse { Error = "Student not found." });
            if (student.Role != SRVS.Domain.Enums.UserRoleType.Student || student.AccountStatus != SRVS.Domain.Enums.UserAccountStatus.Active)
            {
                return Results.BadRequest(new ErrorResponse { Error = "Student is not an active student account." });
            }

            var allSyllabi = await dbContext.SyllabusDocuments.ToListAsync();
            var syllabus = FindSyllabusByIdOr5Digit(allSyllabi, request.SyllabusId);

            if (syllabus is null) return Results.BadRequest(new ErrorResponse { Error = "Syllabus not found." });
            if (syllabus.Status is not (SRVS.Domain.Enums.SyllabusStatus.Approved or SRVS.Domain.Enums.SyllabusStatus.Submitted))
            {
                return Results.BadRequest(new ErrorResponse { Error = "Only approved or submitted syllabi can be assigned." });
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

                var fiveDigitId = Math.Abs(syllabus.Id.GetHashCode() % 90000 + 10000).ToString("D5");

                var newAssignment = new SyllabusAssignment
                {
                    StudentId = student.Id,
                    StudentFullName = student.FullName,
                    SyllabusId = fiveDigitId,
                    SyllabusDocId = syllabus.Id,
                    DepartmentName = syllabus.DepartmentName,
                    CourseCode = syllabus.CourseCode,
                    CourseTitle = syllabus.CourseTitle,
                    Semester = syllabus.Semester,
                    AcademicYear = syllabus.AcademicYear,
                    AssignedBy = user.Id,
                    AssignedAt = syllabus.CourseCode,
                    AssignedAtDate = now,
                    IsActive = true
                };

                dbContext.SyllabusAssignments.Add(newAssignment);
                await dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new AssignmentResponse
                {
                    Id = newAssignment.Id,
                    StudentFullName = student.FullName,
                    SchoolId = student.Id,
                    SyllabusTitle = syllabus.CourseTitle,
                    SubjectCode = syllabus.CourseCode,
                    AssignedAt = newAssignment.AssignedAtDate,
                    AssignedBy = user.FullName
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        })
        .WithName("AssignSyllabusToStudent")
        .WithSummary("Assign syllabus to a student")
        .WithDescription("Assigns an approved syllabus to a specific student account. Accepts 5-digit document ID (e.g. 12345) or GUID.")
        .Produces<AssignmentResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 9. Bulk assign syllabus to multiple students
        group.MapPost("/assign/bulk", async ([FromBody] BulkAssignRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != SRVS.Domain.Enums.UserRoleType.DepartmentHead) return Results.Forbid();

            if (request.StudentIds == null || request.StudentIds.Count == 0)
            {
                return Results.BadRequest(new ErrorResponse { Error = "Select at least one student." });
            }

            var allSyllabi = await dbContext.SyllabusDocuments.ToListAsync();
            var syllabus = FindSyllabusByIdOr5Digit(allSyllabi, request.SyllabusId);

            if (syllabus is null) return Results.BadRequest(new ErrorResponse { Error = "Syllabus not found." });
            if (syllabus.Status is not (SRVS.Domain.Enums.SyllabusStatus.Approved or SRVS.Domain.Enums.SyllabusStatus.Submitted))
            {
                return Results.BadRequest(new ErrorResponse { Error = "Only approved or submitted syllabi can be assigned." });
            }

            var students = await dbContext.Users
                .Where(u => request.StudentIds.Contains(u.Id) && u.Role == SRVS.Domain.Enums.UserRoleType.Student && u.AccountStatus == SRVS.Domain.Enums.UserAccountStatus.Active)
                .ToListAsync();

            using var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var now = DateTimeOffset.UtcNow;
                var fiveDigitId = Math.Abs(syllabus.Id.GetHashCode() % 90000 + 10000).ToString("D5");

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
                        StudentFullName = student.FullName,
                        SyllabusId = fiveDigitId,
                        SyllabusDocId = syllabus.Id,
                        DepartmentName = syllabus.DepartmentName,
                        CourseCode = syllabus.CourseCode,
                        CourseTitle = syllabus.CourseTitle,
                        Semester = syllabus.Semester,
                        AcademicYear = syllabus.AcademicYear,
                        AssignedBy = user.Id,
                        AssignedAt = syllabus.CourseCode,
                        AssignedAtDate = now,
                        IsActive = true
                    });
                }

                await dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new BulkAssignResponse { Message = $"Syllabus successfully assigned to {students.Count} student(s).", Count = students.Count });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        })
        .WithName("BulkAssignSyllabusToStudents")
        .WithSummary("Bulk assign syllabus to students")
        .WithDescription("Assigns an approved syllabus to multiple students simultaneously. Accepts 5-digit document ID (e.g. 12345) or GUID.")
        .Produces<BulkAssignResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static SyllabusDocument? FindSyllabusByIdOr5Digit(IEnumerable<SyllabusDocument> syllabi, string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var trimmed = id.Trim();
        if (Guid.TryParse(trimmed, out var guid))
        {
            return syllabi.FirstOrDefault(s => s.Id == guid);
        }
        return syllabi.FirstOrDefault(s =>
            s.Id.ToString().Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
            Math.Abs(s.Id.GetHashCode() % 90000 + 10000).ToString("D5").Equals(trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
