using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;

namespace PixelFlow.Application.UseCases.Images;

public sealed record RestoreVersionCommand(Guid UserId, Guid ImageId, Guid VersionId);

public sealed class RestoreVersionHandler(IImageRepository images)
{
    public async Task<ImageVersionDto> HandleAsync(RestoreVersionCommand cmd, CancellationToken ct = default)
    {
        var image = await images.GetByIdAsync(cmd.ImageId, ct);
        if (image is null || image.OwnerId != cmd.UserId)
            throw new NotFoundException("Изображение не найдено");

        var target = image.Versions.FirstOrDefault(v => v.Id == cmd.VersionId)
                     ?? throw new NotFoundException("Версия не найдена");

        image.RestoreTo(target.Id);
        await images.SaveChangesAsync(ct);

        return ImageVersionDto.From(target, image.CurrentVersionId);
    }
}