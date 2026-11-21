using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.Abstractions;

public interface IJobEventPublisher
{
    Task PublishCreatedAsync(Job job, CancellationToken ct = default);
}