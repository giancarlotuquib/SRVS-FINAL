namespace SRVS.Domain.Entities;

public class UserDepartment : EntityBase
{
    public string UserId { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

    public Department? Department { get; set; }

    public string AccessScope { get; set; } = string.Empty;
}