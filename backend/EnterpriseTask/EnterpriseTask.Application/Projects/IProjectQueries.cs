namespace EnterpriseTask.Application.Projects;

public interface IProjectQueries
{
    Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(CancellationToken cancellationToken);
}
