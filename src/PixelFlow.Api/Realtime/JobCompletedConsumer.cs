using MassTransit;
using Microsoft.AspNetCore.SignalR;
using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Models;
using PixelFlow.Contracts;

namespace PixelFlow.Api.Realtime;

public sealed class JobCompletedConsumer(
    IHubContext<ImageUpdatesHub> hub,
    IImageRepository images,
    IImageChangeNotifier notifier,
    ILogger<JobCompletedConsumer> logger) : IConsumer<JobCompleted>
{
    public async Task Consume(ConsumeContext<JobCompleted> context)
    {
        var m = context.Message;
        var ct = context.CancellationToken;

        await hub.Clients.Group(ImageUpdatesHub.GroupName(m.ImageId)).SendAsync(
            "JobUpdated",
            new { jobId = m.JobId, imageId = m.ImageId, status = "Completed", progress = 100, versionId = m.VersionId },
            ct);

        var image = await images.GetByIdAsync(m.ImageId, ct);
        var version = image?.Versions.FirstOrDefault(v => v.Id == m.VersionId);
        if (image is not null && version is not null)
            await notifier.NotifyAsync(image.Id, ImageVersionDto.From(version, image.CurrentVersionId), ct);

        logger.LogInformation("Уведомление о завершении задачи {JobId} отправлено", m.JobId);
    }
}