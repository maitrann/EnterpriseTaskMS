using EnterpriseTask.Api.Controllers;
using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Departments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EnterpriseTask.Api.Tests.Controllers;

public sealed class DepartmentsControllerTests
{
    private static readonly Guid ActorUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Create_PassesCurrentUserScopeToCommand()
    {
        var commands = new RecordingDepartmentCommands(
            new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.Success, 42));
        var controller = CreateController(commands);

        var result = await controller.Create(
            new DepartmentCreateRequest(1, "OPS", "Operations", null, null, null),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal(ActorUserId, commands.LastScope?.UserId);
    }

    [Fact]
    public async Task Update_MapsCycleConflictToProblemDetails()
    {
        var commands = new RecordingDepartmentCommands(
            new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.CycleDenied));
        var controller = CreateController(commands);

        var result = await controller.Update(
            10,
            new DepartmentUpdateRequest("OPS", "Operations", null, 11),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal(ActorUserId, commands.LastScope?.UserId);
    }

    [Fact]
    public async Task AssignManager_MapsMissingManagerToNotFoundProblem()
    {
        var commands = new RecordingDepartmentCommands(
            new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.ManagerNotFound));
        var controller = CreateController(commands);

        var result = await controller.AssignManager(
            10,
            new DepartmentManagerAssignmentRequest(Guid.NewGuid()),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal(ActorUserId, commands.LastScope?.UserId);
    }

    [Fact]
    public async Task Deactivate_MapsActiveChildrenConflictToProblemDetails()
    {
        var commands = new RecordingDepartmentCommands(
            new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.ActiveChildrenDenied));
        var controller = CreateController(commands);

        var result = await controller.Deactivate(10, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal(ActorUserId, commands.LastScope?.UserId);
    }

    private static DepartmentsController CreateController(RecordingDepartmentCommands commands)
    {
        return new DepartmentsController(
            new EmptyDepartmentQueries(),
            commands,
            new FixedCurrentUserContext(new UserScope(ActorUserId, 7, IsAdmin: true, IsDirector: false, IsManager: false)));
    }

    private sealed class FixedCurrentUserContext(UserScope scope) : ICurrentUserContext
    {
        public bool TryGetUserId(out Guid userId)
        {
            userId = scope.UserId;
            return true;
        }

        public UserScope GetRequiredScope() => scope;
    }

    private sealed class RecordingDepartmentCommands(DepartmentAdministrationCommandResult result)
        : IDepartmentAdministrationCommands
    {
        public UserScope? LastScope { get; private set; }

        public Task<DepartmentAdministrationCommandResult> CreateAsync(
            UserScope scope,
            DepartmentCreateRequest request,
            CancellationToken cancellationToken)
        {
            LastScope = scope;
            return Task.FromResult(result);
        }

        public Task<DepartmentAdministrationCommandResult> UpdateAsync(
            UserScope scope,
            long departmentId,
            DepartmentUpdateRequest request,
            CancellationToken cancellationToken)
        {
            LastScope = scope;
            return Task.FromResult(result);
        }

        public Task<DepartmentAdministrationCommandResult> AssignManagerAsync(
            UserScope scope,
            long departmentId,
            DepartmentManagerAssignmentRequest request,
            CancellationToken cancellationToken)
        {
            LastScope = scope;
            return Task.FromResult(result);
        }

        public Task<DepartmentAdministrationCommandResult> DeactivateAsync(
            UserScope scope,
            long departmentId,
            CancellationToken cancellationToken)
        {
            LastScope = scope;
            return Task.FromResult(result);
        }
    }

    private sealed class EmptyDepartmentQueries : IDepartmentQueries
    {
        public Task<IReadOnlyList<DepartmentCardDto>> GetCardsAsync(
            UserScope scope,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<DepartmentCardDto>>([]);
        }

        public Task<IReadOnlyList<DepartmentOptionDto>> GetOptionsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<DepartmentOptionDto>>([]);
        }

        public Task<IReadOnlyList<DepartmentListItemDto>> GetListAsync(
            bool includeInactive,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<DepartmentListItemDto>>([]);
        }

        public Task<IReadOnlyList<DepartmentTreeNodeDto>> GetTreeAsync(
            bool includeInactive,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<DepartmentTreeNodeDto>>([]);
        }
    }
}
