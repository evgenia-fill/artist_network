using PixelFlow.Application.Models;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.Abstractions;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid jobId, CancellationToken ct = default);
    Task AddAsync(Job job, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<bool> TryAcquireAsync(Guid jobId, string owner, TimeSpan lease, int maxAttempts, CancellationToken ct = default);
    Task<JobFailureOutcome> ResolveFailureAsync(Guid jobId, string owner, string error,
        int maxAttempts, DateTimeOffset retryAt, bool permanent, CancellationToken ct = default);
    Task<IReadOnlyList<Job>> FindRecoverableAsync(DateTimeOffset now,
        DateTimeOffset stalePendingBefore, int batchSize, CancellationToken ct = default);
    Task<bool> MarkExhaustedFailedAsync(Guid jobId, int maxAttempts, string error,
        DateTimeOffset now, CancellationToken ct = default);
}
