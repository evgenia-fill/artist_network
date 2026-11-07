using System.Text.Json;
using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;

namespace PixelFlow.Application.UseCases.Images;

public sealed record FlipImageCommand(Guid UserId, Guid ImageId, FlipAxis Axis);

public sealed class FlipImageHandler(
    IImageRepository images, IFileStorage storage, IImageProcessor processor,
    IImageChangeNotifier notifier)
{
    public async Task<ImageVersionDto> HandleAsync(FlipImageCommand cmd, CancellationToken ct = default)
    {
        var image = await images.GetByIdAsync(cmd.ImageId, ct);
        if (image is null || image.OwnerId != cmd.UserId)
            throw new NotFoundException("Изображение не найдено");

        var current = image.Versions.Single(v => v.Id == image.CurrentVersionId);

        await using var input = await storage.OpenReadAsync(current.FilePath, ct);
        var result = await processor.FlipAsync(input, cmd.Axis, ct);

        var path = await storage.SaveAsync(result.Content, result.Extension, ct);

        var version = image.AddVersion(
            operation: "flip",
            parameters: JsonSerializer.Serialize(new { axis = cmd.Axis.ToString().ToLowerInvariant() }),
            filePath: path,
            contentType: result.ContentType,
            size: result.Content.Length,
            width: result.Width,
            height: result.Height);

        await images.SaveChangesAsync(ct);
        var dto = ImageVersionDto.From(version, image.CurrentVersionId);
        await notifier.NotifyAsync(image.Id, dto, ct);
        return dto;
    }
}