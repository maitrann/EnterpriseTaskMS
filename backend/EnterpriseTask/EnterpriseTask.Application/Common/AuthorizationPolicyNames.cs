namespace EnterpriseTask.Application.Common;

public static class AuthorizationPolicyNames
{
    public const string AuthenticatedUser = "AuthenticatedUser";
    public const string AdminOnly = "AdminOnly";
    public const string ElevatedDataReader = "ElevatedDataReader";
    public const string DepartmentDataReader = "DepartmentDataReader";
    public const string TaskCreate = "TaskCreate";
    public const string TaskUpdate = "TaskUpdate";
    public const string TaskAssign = "TaskAssign";
    public const string CommentCreate = "CommentCreate";
}
