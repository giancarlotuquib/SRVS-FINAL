using SRVS.Domain.Entities;
using SRVS.Domain.Enums;

namespace SRVS.Application.Services;

public static class SyllabusAccessPolicy
{
    public static bool CanView(SyllabusDocument document, UserRoleType role, Guid? departmentId, string userId)
    {
        return role switch
        {
            UserRoleType.Admin => true,
            UserRoleType.DepartmentHead => IsSameDepartment(document, departmentId),
            UserRoleType.Educator => document.OwnerUserId == userId || IsSameDepartment(document, departmentId),
            UserRoleType.Viewer => document.Status == SyllabusStatus.Approved && document.IsPublished,
            _ => false
        };
    }

    public static bool CanDownload(SyllabusDocument document, UserRoleType role, Guid? departmentId, string userId)
    {
        return CanView(document, role, departmentId, userId);
    }

    public static bool CanSubmit(SyllabusDocument document, UserRoleType role, Guid? departmentId, string userId)
    {
        return document.Status is SyllabusStatus.Draft or SyllabusStatus.Rejected
            && role is UserRoleType.Educator or UserRoleType.DepartmentHead
            && (document.OwnerUserId == userId || IsSameDepartment(document, departmentId));
    }

    public static bool CanRevise(SyllabusDocument document, UserRoleType role, Guid? departmentId, string userId)
    {
        return CanSubmit(document, role, departmentId, userId);
    }

    public static bool CanReview(SyllabusDocument document, UserRoleType role, Guid? departmentId)
    {
        return role == UserRoleType.DepartmentHead
            && document.Status == SyllabusStatus.Submitted
            && IsSameDepartment(document, departmentId);
    }

    private static bool IsSameDepartment(SyllabusDocument document, Guid? departmentId)
    {
        return departmentId.HasValue && document.DepartmentId == departmentId.Value;
    }
}
