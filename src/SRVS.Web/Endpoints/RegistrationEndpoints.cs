using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SRVS.Application.Abstractions;
using SRVS.Domain.Entities;
using SRVS.Web.Data;
using SRVS.Web.Components.Admin.Models;

namespace SRVS.Web.Endpoints;

public static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/registrations")
            .WithTags("Registrations")
            .RequireAuthorization();

        api.MapGet("/", async (
            string? search,
            IRegistrationApprovalService registrationApprovalService,
            CancellationToken cancellationToken) =>
        {
            var queue = await registrationApprovalService.GetQueueAsync(search, cancellationToken);
            return Results.Ok(queue);
        })
        .WithName("GetRegistrationQueue");

        api.MapPost("/{registrationRequestId:guid}/approve", async (
            Guid registrationRequestId,
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager,
            IRegistrationApprovalService registrationApprovalService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            await registrationApprovalService.ApproveAsync(registrationRequestId, user.Id, user.FullName, cancellationToken);
            return Results.NoContent();
        })
        .WithName("ApproveRegistration");

        api.MapPost("/{registrationRequestId:guid}/reject", async (
            Guid registrationRequestId,
            RejectRegistrationRequest request,
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager,
            IRegistrationApprovalService registrationApprovalService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            await registrationApprovalService.RejectAsync(registrationRequestId, user.Id, user.FullName, request.ReviewRemarks ?? string.Empty, cancellationToken);
            return Results.NoContent();
        })
        .WithName("RejectRegistration");

        return app;
    }
}
