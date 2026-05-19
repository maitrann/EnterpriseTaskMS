namespace EnterpriseTask.Application.InterDepartmentRequests;

public interface IInterDepartmentRequestQueries
{
    Task<IReadOnlyList<InterDepartmentRequestDto>> GetRequestsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestDepartmentRefDto>> GetDepartmentOptionsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestOwnerRefDto>> GetOwnerOptionsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestSlaPolicyDto>> GetSlaPoliciesAsync(CancellationToken cancellationToken);
}

public interface IInterDepartmentRequestCommands
{
    Task<Guid> CreateAsync(CreateInterDepartmentRequestCommand request, CancellationToken cancellationToken);

    Task<bool> AcknowledgeAsync(Guid requestId, CancellationToken cancellationToken);

    Task<bool> AssignOwnerAsync(Guid requestId, AssignOwnerRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateStatusAsync(Guid requestId, UpdateRequestStatusRequest request, CancellationToken cancellationToken);

    Task<Guid?> AddMessageAsync(Guid requestId, AddRequestMessageRequest request, CancellationToken cancellationToken);

    Task<bool> CloseAsync(Guid requestId, CancellationToken cancellationToken);
}
