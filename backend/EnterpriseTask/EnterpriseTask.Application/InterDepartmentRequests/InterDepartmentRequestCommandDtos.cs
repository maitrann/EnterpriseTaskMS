namespace EnterpriseTask.Application.InterDepartmentRequests;

public sealed record CreateInterDepartmentRequestCommand(
    string Type,
    string Title,
    string Description,
    long? RequesterDepartmentId,
    Guid? RequesterUserId,
    long? TargetDepartmentId,
    string Priority,
    DateOnly DueDate,
    Dictionary<string, string>? FormValues,
    string? Note);

public sealed record AssignOwnerRequest(Guid OwnerId);

public sealed record UpdateRequestStatusRequest(string Status);

public sealed record AddRequestMessageRequest(Guid? AuthorUserId, string AuthorName, string AuthorRole, string? AuthorDepartment, string Body);
