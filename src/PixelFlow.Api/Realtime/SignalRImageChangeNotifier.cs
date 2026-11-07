using Microsoft.AspNetCore.SignalR;
using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Models;

namespace PixelFlow.Api.Realtime;

public sealed class SignalRImageChangeNotifier(
    IHubContext<ImageUpdatesHub> hub,
    ILogger<SignalRImageChangeNotifier> logger) : IImageChangeNotifier
{
    public async Task NotifyAsync(Guid imageId, ImageVersionDto currentVersion, CancellationToken ct = default)
    {
        try
        {
            await hub.Clients.Group(ImageUpdatesHub.GroupName(imageId)).SendAsync(
                "ImageChanged", new { imageId, currentVersion }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось отправить ImageChanged для {ImageId}", imageId);
        }
    }
}