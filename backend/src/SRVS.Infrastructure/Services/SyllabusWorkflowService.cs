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
        if (request.SyllabusDocumentId is null)
        {
            document = new SyllabusDocument
            {

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


        return document;
    }

    public async Task SubmitAsync(Guid syllabusDocumentId, string actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        var document = await LoadDocumentAsync(syllabusDocumentId, cancellationToken);
        document.Status = SyllabusStatus.Submitted;
        document.SubmittedAtUtc = DateTimeOffset.UtcNow;
        document.UpdatedAtUtc = DateTimeOffset.UtcNow;



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


}
