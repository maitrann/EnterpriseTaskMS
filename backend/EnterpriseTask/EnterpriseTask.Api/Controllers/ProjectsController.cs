using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class ProjectsController(
    IProjectQueries projectQueries,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await projectQueries.GetProjectsAsync(currentUser.GetRequiredScope(), cancellationToken));
    }
}
