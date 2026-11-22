using MassTransit;
using Microsoft.AspNetCore.SignalR;
using PixelFlow.Contracts;

namespace PixelFlow.Api.Realtime;

public sealed class JobFailedConsumer(
    IHubContext<ImageUpdatesHub> hub,
    ILogger<JobFailedConsumer> logger) : IConsumer<JobFailed>
{
    public async Task Consume(ConsumeContext<JobFailed> context)
    {
        var m = context.Message;

        await hub.Clients.Group(ImageUpdatesHub.GroupName(m.ImageId)).SendAsync(
            "JobUpdated",
            new
            {
                jobId = m.JobId,
                imageId = m.ImageId,
                status = "Failed",
                progress = 0,
                error = "Не удалось обработать изображение"
            },
            context.CancellationToken);

        logger.LogWarning("Задача {JobId} завершилась ошибкой: {Error}", m.JobId, m.Error);
    }
}