using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SRVS.Web.Components.Admin.Models;

namespace SRVS.Web.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        var reportGroup = app.MapGroup("/api/reports").WithTags("Reports");

        // Placeholder endpoints – replace with real report generation logic
        reportGroup.MapGet("/enrollment", () =>
        {
            // TODO: implement enrollment report
            return Results.Ok(new { message = "Enrollment report not implemented yet." });
        }).WithName("GetEnrollmentReport");

        reportGroup.MapGet("/performance", () =>
        {
            // TODO: implement performance report
            return Results.Ok(new { message = "Performance report not implemented yet." });
        }).WithName("GetPerformanceReport");
    }
}
