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
        var department = await dbContext.Departments
            .FirstOrDefaultAsync(item => item.Id == request.DepartmentId && item.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Selected department is not available.");

        SyllabusDocument document;
        var isNewDocument = request.SyllabusDocumentId is null;
        if (request.SyllabusDocumentId is null)
        {
            document = new SyllabusDocument
            {
                DepartmentId = department.Id,
                CourseCode = request.CourseCode.Trim(),
                CourseTitle = request.CourseTitle.Trim(),
                AcademicYear = request.AcademicYear.Trim(),
                Semester = request.Semester.Trim(),
                InstructorName = request.InstructorName.Trim(),
                OwnerUserId = request.UploadedByUserId,
                Status = SyllabusStatus.Draft,
                IsPublished = false
            };
        }
        else
        {
            document = await dbContext.SyllabusDocuments
                .Include(item => item.Versions)
                .FirstOrDefaultAsync(item => item.Id == request.SyllabusDocumentId.Value, cancellationToken)
                ?? throw new InvalidOperationException("The requested syllabus could not be found.");
        }

        if (document.Id != Guid.Empty && document.Status is SyllabusStatus.Submitted or SyllabusStatus.Approved)
        {
            throw new InvalidOperationException("Submitted or approved syllabi cannot be overwritten.");
        }

        var nextVersionNumber = document.Versions.Count == 0 ? 1 : document.Versions.Max(version => version.VersionNumber) + 1;
        var extension = Path.GetExtension(request.OriginalFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".pdf";
        }

        var fileName = SyllabusFileNaming.BuildVersionedFileName(document.CourseCode, document.Semester, nextVersionNumber, extension);
        await using var storageStream = request.FileStream;
        var storagePath = await fileStorage.SaveAsync(storageStream, fileName, cancellationToken);

        document.DepartmentId = department.Id;
        document.CourseCode = request.CourseCode.Trim();
        document.CourseTitle = request.CourseTitle.Trim();
        document.AcademicYear = request.AcademicYear.Trim();
        document.Semester = request.Semester.Trim();
        document.InstructorName = request.InstructorName.Trim();
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

        document.Versions.Add(new SyllabusVersion
        {
            VersionNumber = nextVersionNumber,
            FileName = fileName,
            StoragePath = storagePath,
            UploadedByUserId = request.UploadedByUserId,
            UploadedByName = request.UploadedByName,
            ChangeSummary = request.ChangeSummary.Trim(),
            StatusSnapshot = SyllabusStatus.Draft,
            UploadedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            UserId = request.UploadedByUserId,
            UserDisplayName = request.UploadedByName,
            ActionType = AuditActionType.SyllabusUploaded,
            ResultStatus = AuditResultStatus.Success,
            Description = $"Uploaded version V{nextVersionNumber} for course {document.CourseCode}.",
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
            Description = $"Submitted syllabus {document.CourseCode} for review.",
            EntityType = nameof(SyllabusDocument),
            EntityId = document.Id.ToString()
        });

        foreach (var recipient in await FindDepartmentHeadsAsync(document.DepartmentId, cancellationToken))
        {
            dbContext.NotificationEntries.Add(new NotificationEntry
            {
                RecipientUserId = recipient.Id,
                Type = NotificationType.SubmissionAlert,
                Title = "Syllabus submitted for review",
                Message = $"{document.CourseCode} is ready for review in {document.Semester}.",
                RelatedEntityType = nameof(SyllabusDocument),
                RelatedEntityId = document.Id.ToString()
            });
        }

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
            Description = $"Approved syllabus {document.CourseCode}.",
            EntityType = nameof(SyllabusDocument),
            EntityId = document.Id.ToString()
        });

        dbContext.NotificationEntries.Add(new NotificationEntry
        {
            RecipientUserId = document.OwnerUserId,
            Type = NotificationType.ApprovalAlert,
            Title = "Syllabus approved",
            Message = $"Your syllabus for {document.CourseCode} has been approved and published.",
            RelatedEntityType = nameof(SyllabusDocument),
            RelatedEntityId = document.Id.ToString()
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
            Description = $"Rejected syllabus {document.CourseCode}.",
            EntityType = nameof(SyllabusDocument),
            EntityId = document.Id.ToString()
        });

        dbContext.NotificationEntries.Add(new NotificationEntry
        {
            RecipientUserId = document.OwnerUserId,
            Type = NotificationType.RejectionAlert,
            Title = "Syllabus rejected",
            Message = $"Your syllabus for {document.CourseCode} was rejected: {feedback.Trim()}",
            RelatedEntityType = nameof(SyllabusDocument),
            RelatedEntityId = document.Id.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SyllabusDocument> RestoreVersionAsync(Guid syllabusVersionId, string actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        var version = await dbContext.SyllabusVersions
            .Include(item => item.SyllabusDocument)!
            .ThenInclude(item => item!.Versions)
            .FirstOrDefaultAsync(item => item.Id == syllabusVersionId, cancellationToken)
            ?? throw new InvalidOperationException("The selected version could not be found.");

        var document = version.SyllabusDocument ?? throw new InvalidOperationException("The selected version is not attached to a syllabus.");
        await using var stream = await fileStorage.OpenReadAsync(version.StoragePath, cancellationToken);
        var restoredVersionNumber = document.Versions.Max(item => item.VersionNumber) + 1;
        var restoredFileName = SyllabusFileNaming.BuildVersionedFileName(document.CourseCode, document.Semester, restoredVersionNumber, Path.GetExtension(version.FileName));
        var restoredStoragePath = await fileStorage.SaveAsync(stream, restoredFileName, cancellationToken);

        document.CurrentVersionNumber = restoredVersionNumber;
        document.CurrentFileName = restoredFileName;
        document.CurrentStoragePath = restoredStoragePath;
        document.Status = SyllabusStatus.Draft;
        document.IsPublished = false;
        document.LatestChangeSummary = $"Restored content from Version {version.VersionNumber}";
        document.UpdatedAtUtc = DateTimeOffset.UtcNow;

        document.Versions.Add(new SyllabusVersion
        {
            VersionNumber = restoredVersionNumber,
            FileName = restoredFileName,
            StoragePath = restoredStoragePath,
            UploadedByUserId = actorUserId,
            UploadedByName = actorName,
            ChangeSummary = $"Restored content from Version {version.VersionNumber}",
            StatusSnapshot = SyllabusStatus.Draft,
            UploadedAtUtc = DateTimeOffset.UtcNow
        });

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            UserId = actorUserId,
            UserDisplayName = actorName,
            ActionType = AuditActionType.SyllabusRestored,
            ResultStatus = AuditResultStatus.Success,
            Description = $"Restored syllabus {document.CourseCode} from version {version.VersionNumber}.",
            EntityType = nameof(SyllabusDocument),
            EntityId = document.Id.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return document;
    }

    private async Task<SyllabusDocument> LoadDocumentAsync(Guid syllabusDocumentId, CancellationToken cancellationToken)
    {
        return await dbContext.SyllabusDocuments
            .Include(item => item.Versions)
            .FirstOrDefaultAsync(item => item.Id == syllabusDocumentId, cancellationToken)
            ?? throw new InvalidOperationException("The requested syllabus could not be found.");
    }

    private async Task<List<ApplicationUser>> FindDepartmentHeadsAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .Where(user => user.DepartmentId == departmentId && user.Role == UserRoleType.DepartmentHead && user.AccountStatus == UserAccountStatus.Active)
            .ToListAsync(cancellationToken);
    }
}
