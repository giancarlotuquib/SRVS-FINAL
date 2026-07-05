using SRVS.Domain.Enums;

namespace SRVS.Web.DTOs;

// ──── Admin DTOs ────

public class AdminUserResponse
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string SchoolId { get; set; } = string.Empty;
    public UserRoleType Role { get; set; }
    public UserAccountStatus AccountStatus { get; set; }
}

public class RegistrationResponse
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string SchoolId { get; set; } = string.Empty;
    public UserRoleType RequestedRole { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public UserAccountStatus Status { get; set; }
}

// ──── Auth DTOs ────

public class AuthUserResponse
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string SchoolId { get; set; } = string.Empty;
    public UserRoleType Role { get; set; }
    public UserAccountStatus AccountStatus { get; set; }
}

public class LoginResponse
{
    public string Message { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FullName { get; set; } = string.Empty;
    public UserRoleType Role { get; set; }
}

public class MessageResponse
{
    public string Message { get; set; } = string.Empty;
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
}

// ──── Syllabus Version DTOs ────

public class SyllabusVersionResponse
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; set; }
    public string? UploadedByName { get; set; }
    public string? ChangeSummary { get; set; }
}

// ──── DeptHead additional DTOs ────

public class ApproveRejectResponse
{
    public string Message { get; set; } = string.Empty;
    public Guid SyllabusId { get; set; }
}

public class BulkAssignResponse
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string FileStorage { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}
