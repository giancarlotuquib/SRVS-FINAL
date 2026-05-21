using System;
using SRVS.Domain.Enums;

namespace SRVS.Domain.Entities;

public class Subject : EntityBase
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
