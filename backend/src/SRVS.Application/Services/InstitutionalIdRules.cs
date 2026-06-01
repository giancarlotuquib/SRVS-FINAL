using SRVS.Domain.Enums;

namespace SRVS.Application.Services;

public static class InstitutionalIdRules
{
    public static bool IsValid(UserRoleType role, string institutionalId)
    {
        if (string.IsNullOrWhiteSpace(institutionalId) || !institutionalId.All(char.IsDigit))
        {
            return false;
        }

        return role switch
        {
            UserRoleType.Admin => institutionalId.Length == 5,
            UserRoleType.DepartmentHead or UserRoleType.Educator => institutionalId.Length == 5,
            UserRoleType.Viewer => institutionalId.Length == 10,
            _ => false
        };
    }
}