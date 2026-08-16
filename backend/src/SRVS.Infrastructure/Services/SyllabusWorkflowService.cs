using Microsoft.EntityFrameworkCore;
using SRVS.Application.Abstractions;
using SRVS.Application.Models;
using SRVS.Application.Services;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;
using SRVS.Web.Data;

namespace SRVS.Infrastructure.Services;

public class SyllabusWorkflowService(
    ApplicationDbContext dbContext,
    ISyllabusFileStorage fileStorage) : ISyllabusWorkflowService
{
    public async Task<SyllabusDocument> SaveDraftAsync(SyllabusDraftUpsertRequest request, CancellationToken cancellationToken = default)
    {

        SyllabusDocument document;
        var isNewDocument = request.SyllabusDocumentId is null;
        var uploader = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UploadedByUserId, cancellationToken);
        var effectiveDepartment = !string.IsNullOrWhiteSpace(request.DepartmentName)
            ? request.DepartmentName.Trim()
            : (uploader?.DepartmentName ?? "Computer Engineering");

        if (request.SyllabusDocumentId is null)
        {
            document = new SyllabusDocument
            {

                CourseCode = request.CourseCode.Trim(),
                CourseTitle = request.CourseTitle.Trim(),
                AcademicYear = request.AcademicYear.Trim(),
                Semester = request.Semester.Trim(),
                DepartmentName = effectiveDepartment,
                InstructorId = string.IsNullOrWhiteSpace(request.InstructorId) ? request.UploadedByUserId : request.InstructorId.Trim(),
                OwnerUserId = request.UploadedByUserId,
                Status = SyllabusStatus.Draft,
                IsPublished = false
            };
        }
        else
        {
            document = await dbContext.SyllabusDocuments
                .FirstOrDefaultAsync(item => item.Id == request.SyllabusDocumentId.Value, cancellationToken)
                ?? throw new InvalidOperationException("The requested syllabus could not be found.");
        }

        if (document.Id != Guid.Empty && document.Status is SyllabusStatus.Submitted or SyllabusStatus.Approved)
        {
            throw new InvalidOperationException("Submitted or approved syllabi cannot be overwritten.");
        }

        var nextVersionNumber = document.CurrentVersionNumber + 1;
        var extension = Path.GetExtension(request.OriginalFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".pdf";
        }

        var fileName = SyllabusFileNaming.BuildVersionedFileName(document.CourseCode, document.Semester, nextVersionNumber, extension);
        await using var storageStream = request.FileStream;
        var storagePath = await fileStorage.SaveAsync(storageStream, fileName, cancellationToken);


        document.CourseCode = request.CourseCode.Trim();
        document.CourseTitle = request.CourseTitle.Trim();
        document.AcademicYear = request.AcademicYear.Trim();
        document.Semester = request.Semester.Trim();
        document.DepartmentName = effectiveDepartment;
        document.InstructorId = string.IsNullOrWhiteSpace(request.InstructorId) ? request.UploadedByUserId : request.InstructorId.Trim();
        document.Status = SyllabusStatus.Draft;
        document.IsPublished = false;
        document.LatestChangeSummary = request.ChangeSummary.Trim();
        document.CurrentVersionNumber = nextVersionNumber;
        document.CurrentFileName = fileName;
        document.CurrentStoragePath = storagePath;
        document.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (request.SyllabusDocumentId is null)
        {
            dbContext.SyllabusDocuments.Add(document);
        }

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            UserId = request.UploadedByUserId,
            UserDisplayName = request.UploadedByName,
            ActionType = AuditActionType.SyllabusUploaded,
            ResultStatus = AuditResultStatus.Success,
            Description = $"Saved draft for syllabus '{document.CourseCode}' (Version {nextVersionNumber}).",
            EntityType = nameof(SyllabusDocument),
            EntityId = document.Id.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return document;
    }

    public async Task SubmitAsync(Guid syllabusDocumentId, string actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        var document = await LoadDocumentAsync(syllabusDocumentId, cancellationToken);
        document.Status = SyllabusStatus.Submitted;
        document.SubmittedAtUtc = DateTimeOffset.UtcNow;
        document.UpdatedAtUtc = DateTimeOffset.UtcNow;

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            UserId = actorUserId,
            UserDisplayName = actorName,
            ActionType = AuditActionType.SyllabusSubmitted,
            ResultStatus = AuditResultStatus.Success,
            Description = $"Submitted syllabus '{document.CourseCode}' for review.",
            EntityType = nameof(SyllabusDocument),
            EntityId = document.Id.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(Guid syllabusDocumentId, string actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        var document = await LoadDocumentAsync(syllabusDocumentId, cancellationToken);
        document.Status = SyllabusStatus.Approved;
        document.IsPublished = true;
        document.ReviewerRemarks = null;
        document.ReviewedByUserId = actorUserId;
        document.ReviewedAtUtc = DateTimeOffset.UtcNow;
        document.UpdatedAtUtc = DateTimeOffset.UtcNow;

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            UserId = actorUserId,
            UserDisplayName = actorName,
            ActionType = AuditActionType.SyllabusApproved,
            ResultStatus = AuditResultStatus.Success,
            Description = $"Approved syllabus '{document.CourseCode}'.",
            EntityType = nameof(SyllabusDocument),
            EntityId = document.Id.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid syllabusDocumentId, string actorUserId, string actorName, string feedback, CancellationToken cancellationToken = default)
    {
        var document = await LoadDocumentAsync(syllabusDocumentId, cancellationToken);
        document.Status = SyllabusStatus.Rejected;
        document.IsPublished = false;
        document.ReviewerRemarks = feedback.Trim();
        document.ReviewedByUserId = actorUserId;
        document.ReviewedAtUtc = DateTimeOffset.UtcNow;
        document.UpdatedAtUtc = DateTimeOffset.UtcNow;

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            UserId = actorUserId,
            UserDisplayName = actorName,
            ActionType = AuditActionType.SyllabusRejected,
            ResultStatus = AuditResultStatus.Success,
            Description = $"Rejected syllabus '{document.CourseCode}'. Remarks: {feedback.Trim()}",
            EntityType = nameof(SyllabusDocument),
            EntityId = document.Id.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SyllabusDocument> RestoreVersionAsync(Guid syllabusVersionId, string actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.SyllabusDocuments
            .FirstOrDefaultAsync(item => item.Id == syllabusVersionId, cancellationToken)
            ?? throw new InvalidOperationException("The selected syllabus document could not be found.");

        document.Status = SyllabusStatus.Draft;
        document.IsPublished = false;
        document.LatestChangeSummary = $"Restored content for {document.CourseCode}";
        document.UpdatedAtUtc = DateTimeOffset.UtcNow;

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            UserId = actorUserId,
            UserDisplayName = actorName,
            ActionType = AuditActionType.SyllabusRestored,
            ResultStatus = AuditResultStatus.Success,
            Description = $"Restored syllabus '{document.CourseCode}' to version {document.CurrentVersionNumber}.",
            EntityType = nameof(SyllabusDocument),
            EntityId = document.Id.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return document;
    }

    private async Task<SyllabusDocument> LoadDocumentAsync(Guid syllabusDocumentId, CancellationToken cancellationToken)
    {
        return await dbContext.SyllabusDocuments
            .FirstOrDefaultAsync(item => item.Id == syllabusDocumentId, cancellationToken)
            ?? throw new InvalidOperationException("The requested syllabus could not be found.");
    }


}
