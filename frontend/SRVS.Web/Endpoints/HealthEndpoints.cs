using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SRVS.Web.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/health")
            .WithTags("Health");

        api.MapGet("/", () => Results.Ok(new
        {
            status = "ok",
            timestampUtc = DateTimeOffset.UtcNow
        }))
        .AllowAnonymous()
        .WithName("GetHealth");

        return app;
    }
}
