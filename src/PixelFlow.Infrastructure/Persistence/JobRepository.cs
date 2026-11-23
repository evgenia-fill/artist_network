using Microsoft.EntityFrameworkCore;
using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Models;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Infrastructure.Persistence;

public sealed class JobRepository(AppDbContext db) : IJobRepository
{
    public Task<Job?> GetByIdAsync(Guid jobId, CancellationToken ct = default) =>
        db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);

    public async Task AddAsync(Job job, CancellationToken ct = default) =>
        await db.Jobs.AddAsync(job, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task<bool> TryAcquireAsync(Guid jobId, string owner, TimeSpan lease, int maxAttempts, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var until = now.Add(lease);

        var affected = await db.Jobs
            .Where(j => j.Id == jobId && j.Attempts < maxAttempts &&
                        ((j.Status == JobStatus.Pending &&
                          (j.NextAttemptAt == null || j.NextAttemptAt <= now)) ||
                         (j.Status == JobStatus.Processing && j.LeaseUntil <= now)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.Status, JobStatus.Processing)
                .SetProperty(j => j.LeaseOwner, owner)
                .SetProperty(j => j.LeaseUntil, (DateTimeOffset?)until)
                .SetProperty(j => j.NextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(j => j.Attempts, j => j.Attempts + 1)
                .SetProperty(j => j.LastUpdated, now), ct);

        return affected == 1;
    }

    public async Task<JobFailureOutcome> ResolveFailureAsync(Guid jobId, string owner, string error,
        int maxAttempts, DateTimeOffset retryAt, bool permanent, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var message = Truncate(error);
        var owned = db.Jobs.Where(j => j.Id == jobId && j.Status == JobStatus.Processing &&
                                        j.LeaseOwner == owner && j.LeaseUntil > now);

        var terminal = await owned.Where(j => permanent || j.Attempts >= maxAttempts)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.Status, JobStatus.Failed)
                .SetProperty(j => j.Error, message)
                .SetProperty(j => j.LeaseOwner, (string?)null)
                .SetProperty(j => j.LeaseUntil, (DateTimeOffset?)null)
                .SetProperty(j => j.NextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(j => j.LastUpdated, now), ct);

        if (terminal == 1)
            return JobFailureOutcome.Failed;

        var retry = await owned.Where(j => !permanent && j.Attempts < maxAttempts)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.Status, JobStatus.Pending)
                .SetProperty(j => j.Error, message)
                .SetProperty(j => j.LeaseOwner, (string?)null)
                .SetProperty(j => j.LeaseUntil, (DateTimeOffset?)null)
                .SetProperty(j => j.NextAttemptAt, (DateTimeOffset?)retryAt)
                .SetProperty(j => j.LastUpdated, now), ct);

        return retry == 1 ? JobFailureOutcome.RetryScheduled : JobFailureOutcome.Ignored;
    }

    public async Task<IReadOnlyList<Job>> FindRecoverableAsync(DateTimeOffset now,
        DateTimeOffset stalePendingBefore, int batchSize, CancellationToken ct = default) =>
        await db.Jobs.AsNoTracking()
            .Where(j =>
                (j.Status == JobStatus.Pending &&
                 ((j.NextAttemptAt != null && j.NextAttemptAt <= now) ||
                  (j.NextAttemptAt == null && j.LastUpdated <= stalePendingBefore))) ||
                (j.Status == JobStatus.Processing && j.LeaseUntil <= now))
            .OrderBy(j => j.LastUpdated)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task<bool> MarkExhaustedFailedAsync(Guid jobId, int maxAttempts, string error,
        DateTimeOffset now, CancellationToken ct = default)
    {
        var message = Truncate(error);
        var affected = await db.Jobs
            .Where(j => j.Id == jobId && j.Attempts >= maxAttempts &&
                        (j.Status == JobStatus.Pending ||
                         (j.Status == JobStatus.Processing && j.LeaseUntil <= now)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.Status, JobStatus.Failed)
                .SetProperty(j => j.Error, message)
                .SetProperty(j => j.LeaseOwner, (string?)null)
                .SetProperty(j => j.LeaseUntil, (DateTimeOffset?)null)
                .SetProperty(j => j.NextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(j => j.LastUpdated, now), ct);

        return affected == 1;
    }

    private static string Truncate(string value) => value.Length > 2000 ? value[..2000] : value;
}
