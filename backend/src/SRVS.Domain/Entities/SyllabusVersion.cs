using SRVS.Domain.Enums;

namespace SRVS.Domain.Entities;

public class SyllabusVersion : EntityBase
{
    public Guid SyllabusDocumentId { get; set; }

    public SyllabusDocument? SyllabusDocument { get; set; }

    public int VersionNumber { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public string UploadedByUserId { get; set; } = string.Empty;

    public string UploadedByName { get; set; } = string.Empty;

    public string ChangeSummary { get; set; } = string.Empty;

    public SyllabusStatus StatusSnapshot { get; set; }

    public DateTimeOffset UploadedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}