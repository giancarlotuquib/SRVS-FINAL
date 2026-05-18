using SRVS.Application.Services;
using SRVS.Domain.Enums;

namespace SRVS.Tests;

public class InstitutionalIdRulesTests
{
    [Theory]
    [InlineData(UserRoleType.DepartmentHead, "12345", true)]
    [InlineData(UserRoleType.Educator, "54321", true)]
    [InlineData(UserRoleType.Viewer, "1234567890", true)]
    [InlineData(UserRoleType.Viewer, "12345", false)]
    [InlineData(UserRoleType.Educator, "ABCDE", false)]
    public void IsValid_ReturnsExpectedResult(UserRoleType role, string institutionalId, bool expected)
    {
        var result = InstitutionalIdRules.IsValid(role, institutionalId);

        Assert.Equal(expected, result);
    }
}