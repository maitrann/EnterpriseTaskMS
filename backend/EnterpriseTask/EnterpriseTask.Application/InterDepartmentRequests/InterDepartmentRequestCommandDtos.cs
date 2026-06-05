namespace EnterpriseTask.Application.InterDepartmentRequests;

public sealed record CreateInterDepartmentRequestCommand(
    string Type,
    string Title,
    string Description,
    long? RequesterDepartmentId,
    long? TargetDepartmentId,
    string Priority,
    DateOnly DueDate,
    Dictionary<string, string>? FormValues,
    string? Note);

public sealed record AssignOwnerRequest(Guid OwnerId);

public sealed record UpdateRequestStatusRequest(string Status);

public sealed record AddRequestMessageRequest(string Body);

public enum InterDepartmentRequestCommandResult
{
    Success,
    NotFound,
    Forbidden,
    InvalidState
}

public sealed record InterDepartmentRequestCreateResult(InterDepartmentRequestCommandResult Result, Guid? Id = null);
