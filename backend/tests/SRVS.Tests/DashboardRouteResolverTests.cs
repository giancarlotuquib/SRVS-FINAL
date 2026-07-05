using SRVS.Application.Services;
using SRVS.Domain.Enums;

namespace SRVS.Tests;

public class DashboardRouteResolverTests
{
    [Theory]
    [InlineData(UserRoleType.Admin, "/admin/dashboard")]
    [InlineData(UserRoleType.DepartmentHead, "/department-head/dashboard")]
    [InlineData(UserRoleType.Educator, "/educator/dashboard")]
    [InlineData(UserRoleType.Student, "/viewer/dashboard")]
    public void GetRoute_ReturnsRoleSpecificDashboard(UserRoleType role, string expectedRoute)
    {
        var route = DashboardRouteResolver.GetRoute(role);

        Assert.Equal(expectedRoute, route);
    }
}