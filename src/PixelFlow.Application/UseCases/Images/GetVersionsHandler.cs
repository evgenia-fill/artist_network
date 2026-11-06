using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;

namespace PixelFlow.Application.UseCases.Images;

public sealed record GetVersionsQuery(Guid UserId, Guid ImageId);

public sealed class GetVersionsHandler(IImageRepository images)
{
    public async Task<IReadOnlyList<ImageVersionDto>> HandleAsync(GetVersionsQuery query, CancellationToken ct = default)
    {
        var image = await images.GetByIdAsync(query.ImageId, ct);
        if (image is null || image.OwnerId != query.UserId)
            throw new NotFoundException("Изображение не найдено");

        return image.Versions
            .OrderBy(v => v.Number)
            .Select(v => ImageVersionDto.From(v, image.CurrentVersionId))
            .ToList();
    }
}