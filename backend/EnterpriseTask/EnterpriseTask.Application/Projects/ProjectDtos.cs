namespace EnterpriseTask.Application.Projects;

public sealed record ProjectDto(
    Guid Id,
    string? Code,
    string Name,
    string? Description,
    long? DepartmentId,
    Guid? OwnerId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Status,
    Guid? CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
