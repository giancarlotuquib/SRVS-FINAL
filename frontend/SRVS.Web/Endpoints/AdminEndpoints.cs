using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;
using SRVS.Web.Data;
using SRVS.Web.DTOs;
using SRVS.Web.Components.Admin.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SRVS.Web.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var adminGroup = app.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization();

        // 1. User management: Get all users
        adminGroup.MapGet("/users", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var users = await dbContext.Users
                .Select(u => new AdminUserResponse
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    FullName = u.FullName,
                    SchoolId = u.Id,
                    DepartmentName = u.DepartmentName,
                    Role = u.Role,
                    AccountStatus = u.AccountStatus
                })
                .ToListAsync();

            return Results.Ok(users);
        })
        .WithName("GetAdminUsers")
        .WithSummary("Get all users")
        .WithDescription("Retrieves a complete list of registered users in the system.")
        .Produces<List<AdminUserResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 2. Activate user account
        adminGroup.MapPut("/users/{id}/activate", async (string id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var target = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (target is null) return Results.NotFound(new ErrorResponse { Error = "User not found." });

            target.AccountStatus = UserAccountStatus.Active;
            await dbContext.SaveChangesAsync();

            return Results.Ok(new MessageResponse { Message = "User activated successfully." });
        })
        .WithName("ActivateUser")
        .WithSummary("Activate user account")
        .WithDescription("Activates a user account by setting its status to Active.")
        .Produces<MessageResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 3. Deactivate user account
        adminGroup.MapPut("/users/{id}/deactivate", async (string id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var target = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (target is null) return Results.NotFound(new ErrorResponse { Error = "User not found." });

            target.AccountStatus = UserAccountStatus.Suspended;
            await dbContext.SaveChangesAsync();

            return Results.Ok(new MessageResponse { Message = "User deactivated successfully." });
        })
        .WithName("DeactivateUser")
        .WithSummary("Deactivate user account")
        .WithDescription("Deactivates a user account by setting its status to Suspended.")
        .Produces<MessageResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 4. Mark user account as deleted
        adminGroup.MapDelete("/users/{id}", async (string id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var target = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (target is null) return Results.NotFound(new ErrorResponse { Error = "User not found." });

            target.AccountStatus = UserAccountStatus.Deleted;
            await dbContext.SaveChangesAsync();

            return Results.Ok(new MessageResponse { Message = "User status set to deleted." });
        })
        .WithName("DeleteUser")
        .WithSummary("Delete user account")
        .WithDescription("Sets user account status to Deleted.")
        .Produces<MessageResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 5. Get pending registrations
        adminGroup.MapGet("/registrations/pending", async (
            [FromQuery] string? search,
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var query = dbContext.Users.Where(r => r.AccountStatus == UserAccountStatus.PendingApproval);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(r => r.FullName.Contains(s) || (r.Email != null && r.Email.Contains(s)) || r.Id.Contains(s));
            }

            var registrations = await query
                .Select(r => new RegistrationResponse
                {
                    Id = r.Id,
                    Email = r.Email,
                    FirstName = r.FirstName,
                    LastName = r.LastName,
                    FullName = r.FullName,
                    SchoolId = r.Id,
                    DepartmentName = r.DepartmentName,
                    RequestedRole = r.Role,
                    CreatedAtUtc = r.CreatedAtUtc,
                    Status = r.AccountStatus
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(registrations);
        })
        .WithName("GetPendingRegistrations")
        .WithSummary("Get pending user registrations")
        .WithDescription("Retrieves pending self-registration requests requiring admin approval.")
        .Produces<List<RegistrationResponse>>(StatusCodes.Status200OK);

        // 6. Get all registrations
        adminGroup.MapGet("/registrations/all", async (
            HttpContext httpContext,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var registrations = await dbContext.Users
                .Select(r => new RegistrationResponse
                {
                    Id = r.Id,
                    Email = r.Email,
                    FirstName = r.FirstName,
                    LastName = r.LastName,
                    FullName = r.FullName,
                    SchoolId = r.Id,
                    DepartmentName = r.DepartmentName,
                    RequestedRole = r.Role,
                    CreatedAtUtc = r.CreatedAtUtc,
                    Status = r.AccountStatus
                })
                .ToListAsync();

            return Results.Ok(registrations);
        })
        .WithName("GetAllRegistrations")
        .WithSummary("Get all registration requests")
        .WithDescription("Retrieves all registration requests regardless of approval status.")
        .Produces<List<RegistrationResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 7. Approve pending registration
        adminGroup.MapPut("/registrations/{id}/approve", async (
            string id,
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var registration = await userManager.FindByIdAsync(id);
            if (registration is null) return Results.NotFound(new ErrorResponse { Error = "Registration record not found." });

            registration.AccountStatus = UserAccountStatus.Active;
            await userManager.UpdateAsync(registration);

            return Results.Ok(new MessageResponse { Message = "Registration approved successfully." });
        })
        .WithName("ApproveRegistration")
        .WithSummary("Approve registration")
        .WithDescription("Approves a pending registration, enabling the user account.")
        .Produces<MessageResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        // 8. Reject pending registration
        adminGroup.MapPut("/registrations/{id}/reject", async (
            string id,
            [FromBody] RejectRegistrationRequest? request,
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var registration = await userManager.FindByIdAsync(id);
            if (registration is null) return Results.NotFound(new ErrorResponse { Error = "Registration record not found." });

            registration.AccountStatus = UserAccountStatus.Rejected;
            await userManager.UpdateAsync(registration);

            return Results.Ok(new MessageResponse { Message = "Registration rejected successfully." });
        })
        .WithName("RejectRegistration")
        .WithSummary("Reject registration")
        .WithDescription("Rejects a pending user registration request.")
        .Produces<MessageResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
