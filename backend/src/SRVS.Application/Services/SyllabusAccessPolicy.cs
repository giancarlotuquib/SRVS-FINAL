using SRVS.Domain.Entities;
using SRVS.Domain.Enums;

namespace SRVS.Application.Services;

public static class SyllabusAccessPolicy
{
    public static bool CanView(SyllabusDocument document, UserRoleType role, string userId, string userDepartmentName = "")
    {
        if (role == UserRoleType.Admin)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(userDepartmentName) && !string.Equals(document.DepartmentName, userDepartmentName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return role switch
        {
            UserRoleType.DepartmentHead => true,
            UserRoleType.Educator => document.OwnerUserId == userId,
            UserRoleType.Student => document.Status == SyllabusStatus.Approved && document.IsPublished,
            _ => false
        };
    }

    public static bool CanDownload(SyllabusDocument document, UserRoleType role, string userId, string userDepartmentName = "")
    {
        return CanView(document, role, userId, userDepartmentName);
    }

    public static bool CanSubmit(SyllabusDocument document, UserRoleType role, string userId, string userDepartmentName = "")
    {
        if (role != UserRoleType.Admin && !string.IsNullOrEmpty(userDepartmentName) && !string.Equals(document.DepartmentName, userDepartmentName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return document.Status is SyllabusStatus.Draft or SyllabusStatus.Rejected
            && role is UserRoleType.Educator or UserRoleType.DepartmentHead
            && (document.OwnerUserId == userId || role == UserRoleType.DepartmentHead);
    }

    public static bool CanRevise(SyllabusDocument document, UserRoleType role, string userId, string userDepartmentName = "")
    {
        return CanSubmit(document, role, userId, userDepartmentName);
    }

    public static bool CanReview(SyllabusDocument document, UserRoleType role, string userDepartmentName = "")
    {
        if (role != UserRoleType.Admin && !string.IsNullOrEmpty(userDepartmentName) && !string.Equals(document.DepartmentName, userDepartmentName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return role == UserRoleType.DepartmentHead
            && document.Status == SyllabusStatus.Submitted;
    }
}
