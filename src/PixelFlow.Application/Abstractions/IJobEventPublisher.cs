using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.Abstractions;

public interface IJobEventPublisher
{
    Task PublishCreatedAsync(Job job, CancellationToken ct = default);
    Task PublishCompletedAsync(Job job, Guid versionId, CancellationToken ct = default);
    Task PublishFailedAsync(Job job, string error, CancellationToken ct = default);
}