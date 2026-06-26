namespace EnterpriseTask.Domain.Tasks;

public sealed record TaskScopeContext(
    Guid ActorUserId,
    bool CanSeeAllData,
    bool CanSeeDepartmentData,
    long? ActorDepartmentId,
    IReadOnlyCollection<long> ScopedDepartmentIds,
    Guid? CreatedBy,
    Guid? ReporterId,
    Guid? AssigneeId,
    long? TaskDepartmentId,
    bool IsConfidential);
