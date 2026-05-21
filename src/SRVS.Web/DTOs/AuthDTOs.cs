using System.ComponentModel.DataAnnotations;
using SRVS.Domain.Enums;
using System.Text.Json.Serialization;

namespace SyllabusRepository.DTOs;

public class LoginRequest
{
    [Required]
    [JsonPropertyName("schoolId")]
    public string SchoolId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("role")]
    public UserRoleType Role { get; set; }
}

public class RegisterRequest
{
    [Required(ErrorMessage = "First name is required.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "School ID is required.")]
    [RegularExpression(@"^(?:\d{5}|\d{10})$", ErrorMessage = "School ID must be exactly 5 digits for Admin, Department Head, and Faculty, or 10 digits for Student.")]
    [JsonPropertyName("schoolId")]
    public string SchoolId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("role")]
    public UserRoleType Role { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$", ErrorMessage = "Password must contain at least one uppercase letter, one number, and one special character.")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [JsonPropertyName("confirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid format.")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$", ErrorMessage = "Password must contain at least one uppercase letter, one number, and one special character.")]
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [JsonPropertyName("confirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
