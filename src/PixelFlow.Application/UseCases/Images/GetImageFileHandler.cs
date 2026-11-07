using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;

namespace PixelFlow.Application.UseCases.Images;

public sealed record GetImageFileQuery(Guid UserId, Guid ImageId);

public sealed class GetImageFileHandler(IImageRepository images, IFileStorage storage)
{
    public async Task<ImageFileResult> HandleAsync(GetImageFileQuery query, CancellationToken ct = default)
    {
        var image = await images.GetByIdAsync(query.ImageId, ct);
        if (image is null || image.OwnerId != query.UserId)
            throw new NotFoundException("Изображение не найдено");

        var current = image.Versions.Single(v => v.Id == image.CurrentVersionId);
        var stream = await storage.OpenReadAsync(current.FilePath, ct);
        return new ImageFileResult(stream, current.ContentType);
    }
}