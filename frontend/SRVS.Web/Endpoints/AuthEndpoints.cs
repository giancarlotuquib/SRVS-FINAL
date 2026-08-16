using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SRVS.Application.Services;
using SRVS.Web.Components.Account;
using Microsoft.AspNetCore.Identity;
using SRVS.Domain.Enums;
using SRVS.Domain.Entities;
using SRVS.Web.Data;
using SyllabusRepository.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SRVS.Web.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/api/auth").WithTags("Auth").DisableAntiforgery();

        app.MapPost("/Account/Login", async ([FromForm] LoginRequest request, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext dbContext) =>
        {
            if (string.IsNullOrWhiteSpace(request.SchoolId) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.Redirect("/Account/Login?error=" + Uri.EscapeDataString("School ID and password are required."));
            }

            var user = await userManager.Users.FirstOrDefaultAsync(candidate => candidate.Id == request.SchoolId.Trim());
            if (user is null)
            {
                dbContext.AuditLogEntries.Add(new AuditLogEntry
                {
                    ActionType = AuditActionType.LoginFailure,
                    ResultStatus = AuditResultStatus.Failed,
                    Description = $"Failed login attempt: User with School ID '{request.SchoolId}' not found."
                });
                await dbContext.SaveChangesAsync();
                return Results.Redirect("/Account/Login?error=" + Uri.EscapeDataString("Invalid School ID or password."));
            }

            if (user.AccountStatus != UserAccountStatus.Active)
            {
                dbContext.AuditLogEntries.Add(new AuditLogEntry
                {
                    UserId = user.Id,
                    UserDisplayName = user.FullName,
                    ActionType = AuditActionType.LoginFailure,
                    ResultStatus = AuditResultStatus.Failed,
                    Description = $"Failed login attempt: User '{user.Email}' is not active ({user.AccountStatus}).",
                    EntityType = nameof(ApplicationUser),
                    EntityId = user.Id
                });
                await dbContext.SaveChangesAsync();
                return Results.Redirect("/Account/Login?error=" + Uri.EscapeDataString("Account is not active."));
            }

            var result = await signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                user.LastLoginAtUtc = DateTimeOffset.UtcNow;
                await userManager.UpdateAsync(user);

                // Ensure the user is fully signed-in so the auth cookie is issued in this response.
                await signInManager.SignInAsync(user, isPersistent: false);

                dbContext.AuditLogEntries.Add(new AuditLogEntry
                {
                    UserId = user.Id,
                    UserDisplayName = user.FullName,
                    ActionType = AuditActionType.LoginSuccess,
                    ResultStatus = AuditResultStatus.Success,
                    Description = $"User '{user.Email}' logged in successfully.",
                    EntityType = nameof(ApplicationUser),
                    EntityId = user.Id
                });
                await dbContext.SaveChangesAsync();

                var destination = user.Role == UserRoleType.Admin
                    ? "/admin/dashboard"
                    : SRVS.Application.Services.DashboardRouteResolver.GetRoute(user.Role);

                return Results.Redirect(destination);
            }

            dbContext.AuditLogEntries.Add(new AuditLogEntry
            {
                UserId = user.Id,
                UserDisplayName = user.FullName,
                ActionType = AuditActionType.LoginFailure,
                ResultStatus = AuditResultStatus.Failed,
                Description = $"Failed login attempt for user '{user.Email}': Invalid password.",
                EntityType = nameof(ApplicationUser),
                EntityId = user.Id
            });
            await dbContext.SaveChangesAsync();

            if (result.IsLockedOut)
            {
                return Results.Redirect("/Account/Lockout");
            }

            return Results.Redirect("/Account/Login?error=" + Uri.EscapeDataString("Invalid School ID or password."));
        }).DisableAntiforgery();

        // Register
        authGroup.MapPost("/register", async (RegisterRequest request, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext) =>
        {
            // Normalise SchoolId: trim whitespace. The validator enforces digit-only input.
            request.SchoolId = request.SchoolId?.Trim() ?? string.Empty;

            // Do not allow Admin self-registration
            if (request.Role == UserRoleType.Admin)
            {
                return Results.BadRequest(new { error = "Admin accounts cannot be self-registered." });
            }

            if (!request.Role.HasValue || request.Role.Value is not (UserRoleType.DepartmentHead or UserRoleType.Educator or UserRoleType.Student))
            {
                return Results.BadRequest(new { error = "Invalid role selected." });
            }

            if (!InstitutionalIdRules.IsValid(request.Role.Value, request.SchoolId))
            {
                return Results.BadRequest(new { error = GetSchoolIdValidationMessage(request.Role.Value) });
            }

            var normalizedSchoolId = request.SchoolId ?? string.Empty;

            // Check for existing InstitutionalId (SchoolId)
            var existingUserById = await userManager.Users.FirstOrDefaultAsync(u => u.Id == normalizedSchoolId);
            if (existingUserById != null)
            {
                return Results.Conflict(new { error = "A user with this School ID already exists." });
            }

            var existingUserByEmail = await userManager.FindByEmailAsync(request.Email);
            if (existingUserByEmail != null)
            {
                return Results.Conflict(new { error = "A user with this email already exists." });
            }

            var user = new ApplicationUser
            {
                Id = normalizedSchoolId,
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                FullName = $"{request.FirstName} {request.LastName}".Trim(),
                DepartmentName = string.IsNullOrWhiteSpace(request.DepartmentName) ? "Computer Engineering" : request.DepartmentName.Trim(),
                Role = request.Role.Value,
                AccountStatus = UserAccountStatus.PendingApproval,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                dbContext.AuditLogEntries.Add(new AuditLogEntry
                {
                    UserId = user.Id,
                    UserDisplayName = user.FullName,
                    ActionType = AuditActionType.RegistrationSubmitted,
                    ResultStatus = AuditResultStatus.Success,
                    Description = $"User '{user.Email}' submitted registration request.",
                    EntityType = nameof(ApplicationUser),
                    EntityId = user.Id
                });
                await dbContext.SaveChangesAsync();

                return Results.Ok(new { message = "Account created successfully. Your registration is pending approval." });
            }

            return Results.BadRequest(result.Errors);
        });



        authGroup.MapPost("/login", async ([FromBody] LoginRequest request, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext dbContext) =>
        {
            // Model validation attributes ensure required fields are present.
            if (string.IsNullOrWhiteSpace(request.SchoolId) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "SchoolId and Password are required." });
            }


            var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == request.SchoolId);
            if (user == null)
            {
                dbContext.AuditLogEntries.Add(new AuditLogEntry
                {
                    ActionType = AuditActionType.LoginFailure,
                    ResultStatus = AuditResultStatus.Failed,
                    Description = $"API Login Failure: School ID '{request.SchoolId}' not found."
                });
                await dbContext.SaveChangesAsync();
                return Results.Unauthorized();
            }

            if (user.AccountStatus != UserAccountStatus.Active)
            {
                dbContext.AuditLogEntries.Add(new AuditLogEntry
                {
                    UserId = user.Id,
                    UserDisplayName = user.FullName,
                    ActionType = AuditActionType.LoginFailure,
                    ResultStatus = AuditResultStatus.Failed,
                    Description = $"API Login Failure: User '{user.Email}' is not active ({user.AccountStatus}).",
                    EntityType = nameof(ApplicationUser),
                    EntityId = user.Id
                });
                await dbContext.SaveChangesAsync();
                return Results.Json(new { error = "Account is not active." }, statusCode: 403);
            }

            var signInResult = await signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: false);
            if (signInResult.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false);

                dbContext.AuditLogEntries.Add(new AuditLogEntry
                {
                    UserId = user.Id,
                    UserDisplayName = user.FullName,
                    ActionType = AuditActionType.LoginSuccess,
                    ResultStatus = AuditResultStatus.Success,
                    Description = $"API Login Success for user '{user.Email}'.",
                    EntityType = nameof(ApplicationUser),
                    EntityId = user.Id
                });
                await dbContext.SaveChangesAsync();

                return Results.Ok(new { message = "Signed in." });
            }

            dbContext.AuditLogEntries.Add(new AuditLogEntry
            {
                UserId = user.Id,
                UserDisplayName = user.FullName,
                ActionType = AuditActionType.LoginFailure,
                ResultStatus = AuditResultStatus.Failed,
                Description = $"API Login Failure for user '{user.Email}': Invalid password.",
                EntityType = nameof(ApplicationUser),
                EntityId = user.Id
            });
            await dbContext.SaveChangesAsync();
            return Results.Unauthorized();
        });
                

        // Reset password custom logic
        authGroup.MapPost("/reset-password", async (ResetPasswordRequest request, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                dbContext.AuditLogEntries.Add(new AuditLogEntry
                {
                    ActionType = AuditActionType.PasswordResetCompleted,
                    ResultStatus = AuditResultStatus.Failed,
                    Description = $"Failed password reset request: email '{request.Email}' not found."
                });
                await dbContext.SaveChangesAsync();
                return Results.NotFound(new { error = "Email not found." });
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (result.Succeeded)
            {
                dbContext.AuditLogEntries.Add(new AuditLogEntry
                {
                    UserId = user.Id,
                    UserDisplayName = user.FullName,
                    ActionType = AuditActionType.PasswordResetCompleted,
                    ResultStatus = AuditResultStatus.Success,
                    Description = $"Password reset completed for user '{user.Email}'.",
                    EntityType = nameof(ApplicationUser),
                    EntityId = user.Id
                });
                await dbContext.SaveChangesAsync();
                return Results.Ok(new { message = "Password changed successfully." });
            }

            dbContext.AuditLogEntries.Add(new AuditLogEntry
            {
                UserId = user.Id,
                UserDisplayName = user.FullName,
                ActionType = AuditActionType.PasswordResetCompleted,
                ResultStatus = AuditResultStatus.Failed,
                Description = $"Password reset failed for user '{user.Email}': {string.Join(", ", result.Errors.Select(e => e.Description))}",
                EntityType = nameof(ApplicationUser),
                EntityId = user.Id
            });
            await dbContext.SaveChangesAsync();
            return Results.BadRequest(result.Errors);
        });

        // Refresh token endpoint
        authGroup.MapPost("/refresh-token", (RefreshTokenRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Results.BadRequest(new { error = "Refresh token is required." });
            }
            return Results.Ok(new { token = request.RefreshToken, expiresAt = DateTimeOffset.UtcNow.AddHours(24) });
        })
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // Logout endpoint
        authGroup.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok(new { message = "Signed out successfully." });
        })
        .Produces<object>(StatusCodes.Status200OK);

        // Forgot password endpoint
        authGroup.MapPost("/forgot-password", async (ForgotPasswordRequest request, UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Results.BadRequest(new { error = "Email address is required." });
            }

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Results.Ok(new { message = "If your email is registered, a password reset link has been sent." });
            }

            return Results.Ok(new { message = "If your email is registered, a password reset link has been sent." });
        })
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // Validate token endpoint
        authGroup.MapGet("/validate-token", (HttpContext httpContext) =>
        {
            return Results.Ok(new { valid = httpContext.User.Identity?.IsAuthenticated ?? false });
        })
        .Produces<object>(StatusCodes.Status200OK);

        // Get current user (me)
        authGroup.MapGet("/me", async (HttpContext httpContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            return Results.Ok(new { user.Id, user.Email, user.FullName, user.InstitutionalId, user.Role, user.AccountStatus });
        }).RequireAuthorization();
    }

    private static string GetSchoolIdValidationMessage(UserRoleType role) => role switch
    {
        UserRoleType.Student => "Students must use a 10-digit School ID.",
        UserRoleType.Educator => "Faculty must use a 5-digit School ID.",
        UserRoleType.DepartmentHead => "Department Heads must use a 5-digit School ID.",
        _ => "Invalid role selected."
    };
}
