using SRVS.Domain.Enums;

namespace SRVS.Application.Services;

public static class DashboardRouteResolver
{
    public static string GetRoute(UserRoleType role)
    {
        return role switch
        {
            UserRoleType.Admin => "/admin/dashboard",
            UserRoleType.DepartmentHead => "/department-head/dashboard",
            UserRoleType.Educator => "/educator/dashboard",
            UserRoleType.Student => "/viewer/dashboard",
            _ => "/dashboard"
        };
    }
}