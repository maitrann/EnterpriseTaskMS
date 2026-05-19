namespace EnterpriseTask.Application.Departments;

public sealed record DepartmentCardDto(
    string Name,
    string? Description,
    int Members,
    int ActiveTasks,
    int CompletedTasks,
    string Lead,
    string Sla,
    string Tone);
