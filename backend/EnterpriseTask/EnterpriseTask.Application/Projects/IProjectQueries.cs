using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Application.Projects;

public interface IProjectQueries
{
    Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(UserScope scope, CancellationToken cancellationToken);
}
