namespace EnterpriseTask.Domain.Tasks;

public static class TaskScopePolicy
{
    public static bool CanAccess(TaskScopeContext context)
    {
        if (context.CanSeeAllData)
        {
            return true;
        }

        var isRelatedActor =
            IsActor(context, context.CreatedBy) ||
            IsActor(context, context.ReporterId) ||
            IsActor(context, context.AssigneeId);

        if (context.IsConfidential)
        {
            return isRelatedActor;
        }

        if (isRelatedActor)
        {
            return true;
        }

        if (!context.CanSeeDepartmentData || context.TaskDepartmentId is null)
        {
            return false;
        }

        return context.ActorDepartmentId == context.TaskDepartmentId ||
            context.ScopedDepartmentIds.Contains(context.TaskDepartmentId.Value);
    }

    private static bool IsActor(TaskScopeContext context, Guid? userId)
    {
        return userId == context.ActorUserId;
    }
}
