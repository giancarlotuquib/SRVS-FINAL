namespace SRVS.Application.Models;

public sealed record SyllabusDraftUpsertRequest(
    Guid? SyllabusDocumentId,
    string CourseCode,
    string CourseTitle,
    string AcademicYear,
    string Semester,
    string InstructorName,
    string ChangeSummary,
    Stream FileStream,
    string OriginalFileName,
    string UploadedByUserId,
    string UploadedByName);