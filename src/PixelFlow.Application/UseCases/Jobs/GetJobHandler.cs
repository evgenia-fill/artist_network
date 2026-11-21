using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;

namespace PixelFlow.Application.UseCases.Jobs;

public sealed record GetJobQuery(Guid UserId, Guid JobId);

public sealed class GetJobHandler(IJobRepository jobs)
{
    public async Task<JobDto> HandleAsync(GetJobQuery query, CancellationToken ct = default)
    {
        var job = await jobs.GetByIdAsync(query.JobId, ct);
        if (job is null || job.UserId != query.UserId)
            throw new NotFoundException("Задача не найдена");
        return JobDto.From(job);
    }
}