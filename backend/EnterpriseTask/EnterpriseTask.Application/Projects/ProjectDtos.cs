namespace EnterpriseTask.Application.Projects;

public sealed record ProjectDto(
    long Id,
    string? Code,
    string Name,
    string? Description,
    long? DepartmentId,
    long? OwnerId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Status,
    long? CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
