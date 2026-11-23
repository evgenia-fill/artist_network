using MassTransit;
using PixelFlow.Application.Abstractions;
using PixelFlow.Contracts;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Infrastructure.Messaging;

public sealed class MassTransitJobEventPublisher(IPublishEndpoint bus) : IJobEventPublisher
{
    public Task PublishCreatedAsync(Job job, CancellationToken ct = default) =>
        bus.Publish(new JobCreated(
            job.Id, job.ImageId, job.UserId, job.Operation, job.Parameters, job.CreatedAt), ct);

    public Task PublishCompletedAsync(Job job, Guid versionId, CancellationToken ct = default) =>
        bus.Publish(new JobCompleted(job.Id, job.ImageId, job.UserId, versionId), ct);

    public Task PublishFailedAsync(Job job, string error, CancellationToken ct = default) =>
        bus.Publish(new JobFailed(job.Id, job.ImageId, job.UserId, error), ct);
}