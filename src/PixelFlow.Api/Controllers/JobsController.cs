using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelFlow.Application.Models;
using PixelFlow.Application.UseCases.Jobs;

[Authorize]
public sealed class JobsController(GetJobHandler getJob) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDto>> Get(
        Guid id,
        CancellationToken ct)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var result = await getJob.HandleAsync(
            new GetJobQuery(id, userId),
            ct
        );

        return Ok(result);
    }
}