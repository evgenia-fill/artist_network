using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.Abstractions;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid jobId, CancellationToken ct = default);
    Task AddAsync(Job job, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<bool> TryAcquireAsync(Guid jobId, string owner, TimeSpan lease, CancellationToken ct = default);
    Task MarkFailedAsync(Guid jobId, string error, CancellationToken ct = default);
}