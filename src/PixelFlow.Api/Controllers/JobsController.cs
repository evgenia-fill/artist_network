using Microsoft.AspNetCore.Mvc;
using PixelFlow.Api.Auth;
using PixelFlow.Application.Models;
using PixelFlow.Application.UseCases.Jobs;

namespace PixelFlow.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public sealed class JobsController(GetJobHandler getJob) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await getJob.HandleAsync(new GetJobQuery(DemoUserId.Resolve(Request.Headers), id), ct));
}