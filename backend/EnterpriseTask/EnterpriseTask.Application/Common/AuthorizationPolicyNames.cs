namespace EnterpriseTask.Application.Common;

public static class AuthorizationPolicyNames
{
    public const string AuthenticatedUser = "AuthenticatedUser";
    public const string AdminOnly = "AdminOnly";
    public const string ElevatedDataReader = "ElevatedDataReader";
    public const string DepartmentDataReader = "DepartmentDataReader";
}
