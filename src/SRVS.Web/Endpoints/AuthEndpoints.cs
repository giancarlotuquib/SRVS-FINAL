using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SRVS.Application.Services;
using SRVS.Web.Components.Account;
using Microsoft.AspNetCore.Identity;
using SRVS.Domain.Enums;
using SRVS.Web.Data;
using SyllabusRepository.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SRVS.Web.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/api/auth").WithTags("Auth");

        // Register
        authGroup.MapPost("/register", async (RegisterRequest request, UserManager<ApplicationUser> userManager) =>
        {
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
            return result.Succeeded ? Results.Ok(new { message = "Account created successfully." }) : Results.BadRequest(result.Errors);
        });



        // Login
        authGroup.MapPost("/login", async ([FromBody] LoginRequest request, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.InstitutionalId == request.SchoolId);
            if (user is null) return Results.Unauthorized();
            
            var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid) return Results.Unauthorized();

            if (user.Role != request.Role)
            {
                return Results.Json(new { correctRole = user.Role.ToString() }, statusCode: 403);
            }

            // TODO: generate actual JWT token instead of dummy
            var token = "dummy-jwt-token";
            return Results.Ok(new { token });
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
    }
}
