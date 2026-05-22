using EnterpriseTask.Application.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class ProjectsController(IProjectQueries projectQueries) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await projectQueries.GetProjectsAsync(this.GetUserScope(), cancellationToken));
    }
}
