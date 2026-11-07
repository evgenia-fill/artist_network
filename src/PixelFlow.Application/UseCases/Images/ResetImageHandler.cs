using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;

namespace PixelFlow.Application.UseCases.Images;

public sealed record ResetImageCommand(Guid UserId, Guid ImageId);

public sealed class ResetImageHandler(IImageRepository images, IImageChangeNotifier notifier)
{
    public async Task<ImageVersionDto> HandleAsync(ResetImageCommand cmd, CancellationToken ct = default)
    {
        var image = await images.GetByIdAsync(cmd.ImageId, ct);
        if (image is null || image.OwnerId != cmd.UserId)
            throw new NotFoundException("Изображение не найдено");

        image.Reset();
        await images.SaveChangesAsync(ct);

        var current = image.Versions.Single(v => v.Id == image.CurrentVersionId);
        var dto = ImageVersionDto.From(current, image.CurrentVersionId);
        await notifier.NotifyAsync(image.Id, dto, ct);
        return dto;
    }
}