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

namespace SRVS.Web.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/api/auth").WithTags("Auth").DisableAntiforgery();

        app.MapPost("/Account/Login", async ([FromForm] LoginRequest request, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) =>
        {
            if (string.IsNullOrWhiteSpace(request.SchoolId) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.Redirect("/Account/Login?error=" + Uri.EscapeDataString("School ID and password are required."));
            }

            if (!InstitutionalIdRules.IsValid(request.Role, request.SchoolId))
            {
                var message = request.Role == UserRoleType.Viewer
                    ? "Student School IDs must contain exactly 10 digits."
                    : "Admin, Department Head, and Faculty School IDs must contain exactly 5 digits.";

                return Results.Redirect("/Account/Login?error=" + Uri.EscapeDataString(message));
            }

            var user = await userManager.Users.FirstOrDefaultAsync(candidate => candidate.InstitutionalId == request.SchoolId.Trim());
            if (user is null || user.Role != request.Role)
            {
                return Results.Redirect("/Account/Login?error=" + Uri.EscapeDataString("Invalid School ID or password."));
            }

            var result = await signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                user.LastLoginAtUtc = DateTimeOffset.UtcNow;
                await userManager.UpdateAsync(user);

                // Ensure the user is fully signed-in so the auth cookie is issued in this response.
                await signInManager.SignInAsync(user, isPersistent: false);

                var destination = user.Role == UserRoleType.Admin
                    ? "/admin/dashboard"
                    : SRVS.Application.Services.DashboardRouteResolver.GetRoute(user.Role);

                return Results.Redirect(destination);
            }

            if (result.IsLockedOut)
            {
                return Results.Redirect("/Account/Lockout");
            }

            return Results.Redirect("/Account/Login?error=" + Uri.EscapeDataString("Invalid School ID or password."));
        }).DisableAntiforgery();

        // Register
        authGroup.MapPost("/register", async (RegisterRequest request, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext) =>
        {
            if (!InstitutionalIdRules.IsValid(request.Role, request.SchoolId))
            {
                return Results.BadRequest(new { error = "Institutional ID must match the selected role: 5 digits for Admin, Department Head, and Educator; 10 digits for Student." });
            }

            // Check for existing InstitutionalId (SchoolId)
            var existingUserById = await userManager.Users.FirstOrDefaultAsync(u => u.InstitutionalId == request.SchoolId);
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
                UserName = request.Email,
                Email = request.Email,
                FullName = $"{request.FirstName} {request.LastName}".Trim(),
                InstitutionalId = request.SchoolId,
                Role = request.Role, // assign role from registration request
                AccountStatus = UserAccountStatus.PendingApproval,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, request.Password);
            
            if (result.Succeeded)
            {
                // Create RegistrationRequest for admin approval
                var registrationRequest = new RegistrationRequest
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    InstitutionalId = user.InstitutionalId,
                    RequestedRole = user.Role,
                    Status = RegistrationStatus.Pending
                };
                dbContext.RegistrationRequests.Add(registrationRequest);
                await dbContext.SaveChangesAsync();
                
                return Results.Ok(new { message = "Account created successfully." });
            }
            
            return Results.BadRequest(result.Errors);
        });



        authGroup.MapPost("/login", async ([FromBody] LoginRequest request, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) =>
        {
            // Model validation attributes ensure required fields are present.
            if (string.IsNullOrWhiteSpace(request.SchoolId) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "SchoolId and Password are required." });
            }


            var user = await userManager.Users.FirstOrDefaultAsync(u => u.InstitutionalId == request.SchoolId);
            if (user == null) return Results.Unauthorized();

            if (user.Role != request.Role)
            {
                return Results.Json(new { correctRole = user.Role.ToString() }, statusCode: 403);
            }

            var signInResult = await signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: false);
            if (signInResult.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false);
                return Results.Ok(new { message = "Signed in." });
            }
            return Results.Unauthorized();
        });
                

        // Reset password custom logic
        authGroup.MapPost("/reset-password", async (ResetPasswordRequest request, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Results.NotFound(new { error = "Email not found." });
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (result.Succeeded)
            {
                return Results.Ok(new { message = "Password changed successfully." });
            }

            return Results.BadRequest(result.Errors);
        });

        // Refresh token placeholder
        authGroup.MapPost("/refresh-token", () =>
        {
            return Results.Ok(new { token = "refreshed-dummy-token" });
        });

        // Logout placeholder
        authGroup.MapPost("/logout", () =>
        {
            return Results.Ok();
        });

        // Forgot password placeholder
        authGroup.MapPost("/forgot-password", () =>
        {
            return Results.Ok(new { message = "Password reset link sent (placeholder)." });
        });

        // Validate token placeholder
        authGroup.MapGet("/validate-token", (HttpContext httpContext) =>
        {
            return Results.Ok(new { valid = httpContext.User.Identity?.IsAuthenticated ?? false });
        });

        // Get current user (me)
        authGroup.MapGet("/me", async (HttpContext httpContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            return Results.Ok(new { user.Id, user.Email, user.FullName, user.InstitutionalId, user.Role, user.AccountStatus });
        }).RequireAuthorization();
    }
}
