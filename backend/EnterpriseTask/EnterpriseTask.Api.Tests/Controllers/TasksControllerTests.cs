using EnterpriseTask.Api.Controllers;
using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EnterpriseTask.Api.Tests.Controllers;

public sealed class TasksControllerTests
{
    private static readonly Guid ActorUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TaskId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task GetById_ReturnsTaskWhenVisible()
    {
        var expected = CreateTask(TaskId);
        var controller = CreateController(new RecordingTaskQueries(expected), new FixedCurrentUserContext(ActorUserId));

        var result = await controller.GetById(TaskId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenTaskIsMissingOrNotVisible()
    {
        var controller = CreateController(new RecordingTaskQueries(task: null), new FixedCurrentUserContext(ActorUserId));

        var result = await controller.GetById(TaskId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ReturnsUnauthorizedWhenActorIsMissing()
    {
        var controller = CreateController(new RecordingTaskQueries(CreateTask(TaskId)), new MissingCurrentUserContext());

        var result = await controller.GetById(TaskId, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Duplicate_ReturnsCreatedTaskContract()
    {
        var duplicatedTaskId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var duplicatedTask = CreateTask(duplicatedTaskId);
        var commands = new NoopTaskCommands
        {
            DuplicateResult = new TaskDuplicateResult(TaskCommandResult.Success, duplicatedTaskId, duplicatedTask)
        };
        var controller = CreateController(new RecordingTaskQueries(duplicatedTask), new FixedCurrentUserContext(ActorUserId), commands);

        var result = await controller.Duplicate(TaskId, new DuplicateTaskRequest("Copied", ResetPeople: false, ResetAttachments: true), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TasksController.GetById), created.ActionName);
        Assert.NotNull(created.Value);
        var id = Assert.IsType<Guid>(created.Value.GetType().GetProperty("id")?.GetValue(created.Value));
        var task = Assert.IsType<TaskDto>(created.Value.GetType().GetProperty("task")?.GetValue(created.Value));
        Assert.Equal(duplicatedTaskId, id);
        Assert.Same(duplicatedTask, task);
    }

    [Fact]
    public async Task Archive_ReturnsNoContentWhenArchived()
    {
        var commands = new NoopTaskCommands { ArchiveResult = TaskCommandResult.Success };
        var controller = CreateController(new RecordingTaskQueries(CreateTask(TaskId)), new FixedCurrentUserContext(ActorUserId), commands);

        var result = await controller.Archive(TaskId, new ArchiveTaskRequest("Done"), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    private static TasksController CreateController(ITaskQueries queries, ICurrentUserContext currentUser, ITaskCommands? commands = null)
    {
        commands ??= new NoopTaskCommands();
        var policies = new AllowAllTaskPolicyQueries();
        return new TasksController(
            queries,
            commands,
            new CreateTaskHandler(commands, policies),
            new UpdateTaskStatusHandler(commands, policies),
            currentUser);
    }

    private static TaskDto CreateTask(Guid id)
    {
        return new TaskDto(
            id,
            "CV-DETAIL",
            ProjectId: null,
            ParentTaskId: null,
            "Detail task",
            Description: null,
            TaskType: null,
            DepartmentId: 1,
            StatusId: 1,
            PriorityId: 1,
            UrgencyLevel: null,
            SecurityLevel: "internal",
            ReporterId: ActorUserId,
            AssigneeId: ActorUserId,
            CollaboratorIds: [],
            WatcherIds: [],
            StartDate: null,
            DueDate: null,
            Progress: 0,
            Source: null,
            AttachmentNames: [],
            Tags: [],
            ProcessingNotes: [],
            ExtensionRequests: [],
            Subtasks: [],
            SubtaskProgressAutoSync: true,
            ParentCompletionSuggested: false,
            EstimatedHours: null,
            ActualHours: null,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: null);
    }

    private sealed class FixedCurrentUserContext(Guid actorUserId) : ICurrentUserContext
    {
        public bool TryGetUserId(out Guid userId)
        {
            userId = actorUserId;
            return true;
        }

        public UserScope GetRequiredScope() => new(actorUserId, DepartmentId: 1, IsAdmin: true, IsDirector: false, IsManager: false);
    }

    private sealed class MissingCurrentUserContext : ICurrentUserContext
    {
        public bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            return false;
        }

        public UserScope GetRequiredScope() => throw new InvalidOperationException("No current user.");
    }

    private sealed class RecordingTaskQueries(TaskDto? task) : ITaskQueries
    {
        public Task<IReadOnlyList<TaskDto>> GetTasksAsync(Guid actorUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<TaskDto>>(task is null ? [] : [task]);
        }

        public Task<TaskDto?> GetTaskAsync(Guid actorUserId, Guid taskId, CancellationToken cancellationToken)
        {
            return Task.FromResult(task?.Id == taskId ? task : null);
        }

        public Task<IReadOnlyList<TaskActivityDto>> GetActivitiesAsync(Guid actorUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<TaskActivityDto>>([]);
        }

        public Task<TaskFormOptionsDto> GetFormOptionsAsync(Guid actorUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TaskFormOptionsDto([], [], [], [], [], [], []));
        }
    }

    private sealed class NoopTaskCommands : ITaskCommands
    {
        public TaskDuplicateResult DuplicateResult { get; init; } = new(TaskCommandResult.Success, TaskId);

        public TaskCommandResult ArchiveResult { get; init; } = TaskCommandResult.Success;

        public Task<TaskCreateResult> CreateAsync(Guid actorUserId, CreateTaskRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new TaskCreateResult(TaskCommandResult.Success, TaskId));

        public Task<TaskCommandResult> UpdateAsync(Guid actorUserId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(TaskCommandResult.Success);

        public Task<TaskCommandResult> UpdateStatusAsync(Guid actorUserId, Guid taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(TaskCommandResult.Success);

        public Task<TaskCommandResult> TransferAssigneeAsync(Guid actorUserId, Guid taskId, TransferTaskAssigneeRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(TaskCommandResult.Success);

        public Task<TaskDuplicateResult> DuplicateAsync(Guid actorUserId, Guid taskId, DuplicateTaskRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(DuplicateResult);

        public Task<TaskCommandResult> ArchiveAsync(Guid actorUserId, Guid taskId, ArchiveTaskRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(ArchiveResult);

        public Task<TaskCreateResult> AddCommentAsync(Guid actorUserId, Guid taskId, AddTaskCommentRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new TaskCreateResult(TaskCommandResult.Success, TaskId));

        public Task<TaskCreateResult> RequestExtensionAsync(Guid actorUserId, Guid taskId, CreateTaskExtensionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new TaskCreateResult(TaskCommandResult.Success, TaskId));

        public Task<TaskCommandResult> ReviewExtensionAsync(Guid actorUserId, Guid taskId, Guid requestId, ReviewTaskExtensionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(TaskCommandResult.Success);

        public Task<TaskCreateResult> CreateSubTaskAsync(Guid actorUserId, Guid taskId, CreateSubTaskRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new TaskCreateResult(TaskCommandResult.Success, TaskId));

        public Task<TaskCommandResult> UpdateSubTaskAsync(Guid actorUserId, Guid taskId, Guid subTaskId, UpdateSubTaskRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(TaskCommandResult.Success);

        public Task<TaskCommandResult> DeleteSubTaskAsync(Guid actorUserId, Guid taskId, Guid subTaskId, CancellationToken cancellationToken) =>
            Task.FromResult(TaskCommandResult.Success);
    }

    private sealed class AllowAllTaskPolicyQueries : ITaskPolicyQueries
    {
        public Task<bool> HasPermissionAsync(Guid actorUserId, string permissionCode, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> CanUseDepartmentAsync(Guid actorUserId, long? departmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<TaskAccessResult> GetAccessAsync(
            Guid actorUserId,
            Guid taskId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new TaskAccessResult(Exists: true, HasPermission: true, CanAccess: true));
        }
    }
}
