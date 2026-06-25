namespace EnterpriseTask.Application.Common;

public static class RoleCodes
{
    public const string Admin = "admin";
    public const string Director = "director";
    public const string Manager = "manager";
    public const string Employee = "employee";
    public const string ExternalPartner = "external_partner";

    public static bool IsAdmin(string roleCode)
    {
        return Is(roleCode, Admin);
    }

    public static bool IsDirector(string roleCode)
    {
        return Is(roleCode, Director);
    }

    public static bool IsManager(string roleCode)
    {
        return Is(roleCode, Manager);
    }

    private static bool Is(string roleCode, string expectedCode)
    {
        return roleCode.Equals(expectedCode, StringComparison.OrdinalIgnoreCase);
    }
}
