using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Application.InterDepartmentRequests;

public interface IInterDepartmentRequestQueries
{
    Task<IReadOnlyList<InterDepartmentRequestDto>> GetRequestsAsync(UserScope scope, CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestDepartmentRefDto>> GetDepartmentOptionsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestOwnerRefDto>> GetOwnerOptionsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestSlaPolicyDto>> GetSlaPoliciesAsync(CancellationToken cancellationToken);
}

public interface IInterDepartmentRequestCommands
{
    Task<InterDepartmentRequestCreateResult> CreateAsync(UserScope scope, CreateInterDepartmentRequestCommand request, CancellationToken cancellationToken);

    Task<InterDepartmentRequestCommandResult> AcknowledgeAsync(UserScope scope, Guid requestId, CancellationToken cancellationToken);

    Task<InterDepartmentRequestCommandResult> AssignOwnerAsync(UserScope scope, Guid requestId, AssignOwnerRequest request, CancellationToken cancellationToken);

    Task<InterDepartmentRequestCommandResult> UpdateStatusAsync(UserScope scope, Guid requestId, UpdateRequestStatusRequest request, CancellationToken cancellationToken);

    Task<InterDepartmentRequestCreateResult> AddMessageAsync(UserScope scope, Guid requestId, AddRequestMessageRequest request, CancellationToken cancellationToken);

    Task<InterDepartmentRequestCommandResult> CloseAsync(UserScope scope, Guid requestId, CancellationToken cancellationToken);
}
