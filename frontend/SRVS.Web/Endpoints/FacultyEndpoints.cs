using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRVS.Application.Abstractions;
using SRVS.Application.Models;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;
using SRVS.Web.Data;
using SRVS.Web.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
        .WithName("GetFacultySyllabi")
        .WithSummary("Get faculty syllabi")
        .WithDescription("Retrieves all syllabus documents created by or assigned to the logged-in faculty member.")
        .Produces<List<SyllabusDetailResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 2. Create new syllabus draft (JSON metadata)
        facultyGroup.MapPost("/syllabi", async ([FromBody] CreateSyllabusRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

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
        .WithName("CreateFacultySyllabus")
        .WithSummary("Create/Insert new syllabus draft (JSON)")
        .WithDescription("Allows faculty to create or insert a new draft syllabus record.")
        .Produces<SyllabusDetailResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 3. Upload syllabus document file and create draft (Multipart Form File Upload)
        facultyGroup.MapPost("/syllabi/upload", async (
            [FromForm] UploadSyllabusFormRequest form,
            HttpContext httpContext,
            ISyllabusWorkflowService workflowService,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

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
                ChangeSummary: string.IsNullOrWhiteSpace(form.ChangeSummary) ? "Initial syllabus upload" : form.ChangeSummary.Trim(),
                FileStream: stream,
                OriginalFileName: form.File.FileName,
                UploadedByUserId: user.Id,
                UploadedByName: user.FullName,
                DepartmentName: user.DepartmentName);

            var doc = await workflowService.SaveDraftAsync(upsertRequest, cancellationToken);

            return Results.Created($"/api/faculty/syllabi/{doc.Id}", new SyllabusDetailResponse
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
        .WithName("UploadFacultySyllabus")
        .WithSummary("Upload and insert syllabus document file")
        .WithDescription("Allows faculty member to upload a syllabus PDF/DOCX document file and create a draft syllabus record.")
        .Produces<SyllabusDetailResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 4. Get single syllabus details by ID or 5-digit DocumentId
        facultyGroup.MapGet("/syllabi/{id}", async (string id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var userSyllabi = await dbContext.SyllabusDocuments
                .Where(doc => doc.OwnerUserId == user.Id)
                .ToListAsync();

            var s = FindSyllabusByIdOr5Digit(userSyllabi, id);
            if (s is null) return Results.NotFound(new ErrorResponse { Error = "Syllabus not found." });

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
        .WithName("GetFacultySyllabusById")
        .WithSummary("Get faculty syllabus by ID")
        .WithDescription("Retrieves single syllabus details owned by faculty member. Accepts 5-digit document ID (e.g. 12345) or GUID.")
        .Produces<SyllabusDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 5. Update syllabus details (Draft / Rejected) by ID or 5-digit DocumentId
        facultyGroup.MapPut("/syllabi/{id}", async (string id, [FromBody] UpdateSyllabusRequest request, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var userSyllabi = await dbContext.SyllabusDocuments
                .Where(doc => doc.OwnerUserId == user.Id)
                .ToListAsync();

            var s = FindSyllabusByIdOr5Digit(userSyllabi, id);
            if (s is null) return Results.NotFound(new ErrorResponse { Error = "Syllabus not found." });

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
        .WithName("UpdateFacultySyllabus")
        .WithSummary("Update syllabus details")
        .WithDescription("Updates syllabus details for a draft or rejected syllabus. Accepts 5-digit document ID (e.g. 12345) or GUID.")
        .Produces<SyllabusDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 6. Submit syllabus for Department Head review (by ID or 5-digit DocumentId)
        facultyGroup.MapPost("/syllabi/{id}/submit", async (string id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var userSyllabi = await dbContext.SyllabusDocuments
                .Where(doc => doc.OwnerUserId == user.Id)
                .ToListAsync();

            var s = FindSyllabusByIdOr5Digit(userSyllabi, id);
            if (s is null) return Results.NotFound(new ErrorResponse { Error = "Syllabus not found." });

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
        .WithName("SubmitSyllabusForReview")
        .WithSummary("Submit syllabus for review")
        .WithDescription("Submits a draft syllabus to Department Head for review. Accepts 5-digit document ID (e.g. 12345) or GUID.")
        .Produces<SyllabusDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 7. Delete draft syllabus (by ID or 5-digit DocumentId)
        facultyGroup.MapDelete("/syllabi/{id}", async (string id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Educator) return Results.Forbid();

            var userSyllabi = await dbContext.SyllabusDocuments
                .Where(doc => doc.OwnerUserId == user.Id)
                .ToListAsync();

            var s = FindSyllabusByIdOr5Digit(userSyllabi, id);
            if (s is null) return Results.NotFound(new ErrorResponse { Error = "Syllabus not found." });

            if (s.Status == SyllabusStatus.Approved)
            {
                return Results.BadRequest(new ErrorResponse { Error = "Approved syllabi cannot be deleted." });
            }

            dbContext.SyllabusDocuments.Remove(s);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new MessageResponse { Message = "Syllabus deleted successfully." });
        })
        .WithName("DeleteFacultySyllabus")
        .WithSummary("Delete draft syllabus")
        .WithDescription("Deletes a draft or rejected syllabus document. Accepts 5-digit document ID (e.g. 12345) or GUID.")
        .Produces<MessageResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
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
