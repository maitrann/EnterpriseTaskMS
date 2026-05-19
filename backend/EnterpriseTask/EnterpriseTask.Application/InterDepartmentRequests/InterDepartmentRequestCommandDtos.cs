namespace EnterpriseTask.Application.InterDepartmentRequests;

public sealed record CreateInterDepartmentRequestCommand(
    string Type,
    string Title,
    string Description,
    long? RequesterDepartmentId,
    long? RequesterUserId,
    long? TargetDepartmentId,
    string Priority,
    DateOnly DueDate,
    Dictionary<string, string>? FormValues,
    string? Note);

public sealed record AssignOwnerRequest(long OwnerId);

public sealed record UpdateRequestStatusRequest(string Status);

public sealed record AddRequestMessageRequest(long? AuthorUserId, string AuthorName, string AuthorRole, string? AuthorDepartment, string Body);
