using SRVS.Domain.Enums;

namespace SRVS.Application.Models;

public sealed record SyllabusSearchItem(
    Guid Id,
    string CourseCode,
    string CourseTitle,
    string DepartmentName,
    string AcademicYear,
    string Semester,
    string InstructorName,
    int CurrentVersionNumber,
    SyllabusStatus Status,
    bool CanDownload,
    string VisibilityLabel,
    string? LatestChangeSummary);