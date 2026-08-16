namespace SRVS.Domain.Constants;

public static class EngineeringDepartments
{
    public const string CivilEngineering = "Civil Engineering";
    public const string ComputerEngineering = "Computer Engineering";
    public const string ElectricalEngineering = "Electrical Engineering";
    public const string ElectronicsEngineering = "Electronics Engineering";
    public const string IndustrialEngineering = "Industrial Engineering";
    public const string MechanicalEngineering = "Mechanical Engineering";

    public static readonly IReadOnlyList<string> All = new[]
    {
        CivilEngineering,
        ComputerEngineering,
        ElectricalEngineering,
        ElectronicsEngineering,
        IndustrialEngineering,
        MechanicalEngineering
    };

    public static string GetDepartmentCode(string departmentName) => departmentName switch
    {
        CivilEngineering => "BSCE",
        ComputerEngineering => "BSCpE",
        ElectricalEngineering => "BSEE",
        ElectronicsEngineering => "BSECE",
        IndustrialEngineering => "BSIE",
        MechanicalEngineering => "BSME",
        _ => "ENG"
    };
}
