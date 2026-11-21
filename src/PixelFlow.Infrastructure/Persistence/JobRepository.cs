using Microsoft.EntityFrameworkCore;
using PixelFlow.Application.Abstractions;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Infrastructure.Persistence;

public sealed class JobRepository(AppDbContext db) : IJobRepository
{
    public Task<Job?> GetByIdAsync(Guid jobId, CancellationToken ct = default) =>
        db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);

    public async Task AddAsync(Job job, CancellationToken ct = default) =>
        await db.Jobs.AddAsync(job, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
    
    public async Task<bool> TryAcquireAsync(Guid jobId, string owner, TimeSpan lease, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var until = now.Add(lease);

        var affected = await db.Jobs
            .Where(j => j.Id == jobId &&
                        (j.Status == JobStatus.Pending ||
                         (j.Status == JobStatus.Processing && j.LeaseUntil < now)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.Status, JobStatus.Processing)
                .SetProperty(j => j.LeaseOwner, owner)
                .SetProperty(j => j.LeaseUntil, (DateTimeOffset?)until)
                .SetProperty(j => j.Attempts, j => j.Attempts + 1)
                .SetProperty(j => j.LastUpdated, now), ct);

        return affected == 1;
    }

    public async Task MarkFailedAsync(Guid jobId, string error, CancellationToken ct = default)
    {
        var message = error.Length > 2000 ? error[..2000] : error;
        var now = DateTimeOffset.UtcNow;

        await db.Jobs
            .Where(j => j.Id == jobId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.Status, JobStatus.Failed)
                .SetProperty(j => j.Error, message)
                .SetProperty(j => j.LeaseOwner, (string?)null)
                .SetProperty(j => j.LeaseUntil, (DateTimeOffset?)null)
                .SetProperty(j => j.LastUpdated, now), ct);
    }
}