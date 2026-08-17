using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SRVS.Domain.Enums;

namespace SRVS.Web.DTOs;

public class CreateSyllabusRequest
{
    [Required(ErrorMessage = "Course code is required.")]
    public string CourseCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course title is required.")]
    public string CourseTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Academic year is required.")]
    public string AcademicYear { get; set; } = string.Empty;

    [Required(ErrorMessage = "Semester is required.")]
    public string Semester { get; set; } = string.Empty;

    public string? InstructorId { get; set; }

    public string? FileName { get; set; }

    public string? ChangeSummary { get; set; }
}

public class UploadSyllabusFormRequest
{
    [Required(ErrorMessage = "Syllabus file is required.")]
    public IFormFile File { get; set; } = default!;

    [Required(ErrorMessage = "Course code is required.")]
    public string CourseCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course title is required.")]
    public string CourseTitle { get; set; } = string.Empty;

    public string? AcademicYear { get; set; }
    public string? Semester { get; set; }
    public string? InstructorId { get; set; }
    public string? ChangeSummary { get; set; }
}

public class UpdateSyllabusRequest
{
    public string? CourseCode { get; set; }
    public string? CourseTitle { get; set; }
    public string? AcademicYear { get; set; }
    public string? Semester { get; set; }
    public string? InstructorId { get; set; }
    public string? FileName { get; set; }
    public string? ChangeSummary { get; set; }
}

public class SyllabusDetailResponse
{
    public Guid Id { get; set; }
    public string DocumentId => Math.Abs(Id.GetHashCode() % 90000 + 10000).ToString("D5");
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string InstructorId { get; set; } = string.Empty;
    public int CurrentVersionNumber { get; set; }
    public SyllabusStatus Status { get; set; }
    public string? CurrentFileName { get; set; }
    public string? ReviewerRemarks { get; set; }
    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
