using PixelFlow.Application.Models;

namespace PixelFlow.Application.Abstractions;

public interface IImageChangeNotifier
{
    Task NotifyAsync(Guid imageId, ImageVersionDto currentVersion, CancellationToken ct = default);
}
