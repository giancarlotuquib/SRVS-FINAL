using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;
using SRVS.Web.Data;
using SRVS.Web.Components.Admin.Models;

namespace SRVS.Web.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var adminGroup = app.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization();

        // User management endpoints
        adminGroup.MapGet("/users", async (HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var users = await dbContext.Users
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FullName,
                    u.InstitutionalId,
                    u.Role,
                    u.AccountStatus,
                    u.DepartmentId
                })
                .ToListAsync();

            return Results.Ok(users);
        });

        adminGroup.MapPut("/users/{id}/activate", async (string id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var target = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (target is null) return Results.NotFound();

            target.AccountStatus = UserAccountStatus.Active;
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        });

        adminGroup.MapPut("/users/{id}/deactivate", async (string id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var target = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (target is null) return Results.NotFound();

            target.AccountStatus = UserAccountStatus.Suspended;
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        });

        adminGroup.MapDelete("/users/{id}", async (string id, HttpContext httpContext, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var target = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (target is null) return Results.NotFound();

            target.AccountStatus = UserAccountStatus.Deleted;
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        });

        // Registration management endpoints
        adminGroup.MapGet("/registrations/pending", async (
            string? search,
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var registrations = await dbContext.RegistrationRequests
                .Where(r => r.Status == RegistrationStatus.Pending)
                .ToListAsync(cancellationToken);
            return Results.Ok(registrations);
        })
        .WithName("GetPendingRegistrations");

        adminGroup.MapGet("/registrations/all", async (
            HttpContext httpContext,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var registrations = await dbContext.RegistrationRequests
                .Select(r => new
                {
                    r.Id,
                    r.Email,
                    r.FullName,
                    r.InstitutionalId,
                    r.RequestedRole,
                    r.CreatedAtUtc,
                    r.Status
                })
                .ToListAsync();

            return Results.Ok(registrations);
        })
        .WithName("GetAllRegistrations");

        adminGroup.MapPut("/registrations/{id}/approve", async (
            Guid id,
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var registration = await dbContext.RegistrationRequests.FindAsync(id, cancellationToken);
            if (registration is null) return Results.NotFound();

            registration.Status = RegistrationStatus.Approved;
            registration.ReviewedByUserId = user.Id;
            registration.ReviewedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        })
        .WithName("ApproveRegistration");

        adminGroup.MapPut("/registrations/{id}/reject", async (
            Guid id,
            RejectRegistrationRequest request,
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null) return Results.Unauthorized();
            if (user.Role != UserRoleType.Admin) return Results.Forbid();

            var registration = await dbContext.RegistrationRequests.FindAsync(id, cancellationToken);
            if (registration is null) return Results.NotFound();

            registration.Status = RegistrationStatus.Rejected;
            registration.ReviewRemarks = request.ReviewRemarks;
            registration.ReviewedByUserId = user.Id;
            registration.ReviewedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        })
        .WithName("RejectRegistration");
    }
}
