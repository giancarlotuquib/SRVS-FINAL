using SRVS.Application.Services;
using SRVS.Domain.Enums;

namespace SRVS.Tests;

public class InstitutionalIdRulesTests
{
    [Theory]
    [InlineData(UserRoleType.Admin, "00000", true)]
    [InlineData(UserRoleType.Admin, "0000", false)]
    [InlineData(UserRoleType.DepartmentHead, "12345", true)]
    [InlineData(UserRoleType.Educator, "54321", true)]
    [InlineData(UserRoleType.Student, "1234567890", true)]
    [InlineData(UserRoleType.Student, "12345", false)]
    [InlineData(UserRoleType.Educator, "ABCDE", false)]
    public void IsValid_ReturnsExpectedResult(UserRoleType role, string institutionalId, bool expected)
    {
        var result = InstitutionalIdRules.IsValid(role, institutionalId);

        Assert.Equal(expected, result);
    }
}