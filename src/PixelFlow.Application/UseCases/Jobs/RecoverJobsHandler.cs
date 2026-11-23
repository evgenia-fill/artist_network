using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Models;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.UseCases.Jobs;

public sealed class RecoverJobsHandler(
    IJobRepository jobs,
    IJobEventPublisher events,
    JobProcessingOptions options)
{
    public async Task<int> HandleAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var stalePendingBefore = now.AddSeconds(-options.PendingReplayAfterSeconds);
        var recoverable = await jobs.FindRecoverableAsync(now, stalePendingBefore,
            options.RecoveryBatchSize, ct);
        var published = 0;

        foreach (var job in recoverable)
        {
            ct.ThrowIfCancellationRequested();

            if (job.Attempts >= options.MaxAttempts)
            {
                const string error = "Задача не завершилась до окончания lease: лимит попыток исчерпан";
                if (await jobs.MarkExhaustedFailedAsync(job.Id, options.MaxAttempts, error, now, ct))
                {
                    await events.PublishFailedAsync(job, error, ct);
                    published++;
                }
            }
            else
            {
                await events.PublishCreatedAsync(job, ct);
                published++;
            }
        }

        return published;
    }
}
