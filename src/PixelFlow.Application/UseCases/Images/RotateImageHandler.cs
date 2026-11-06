using System.Text.Json;
using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;

namespace PixelFlow.Application.UseCases.Images;

public sealed record RotateImageCommand(Guid UserId, Guid ImageId, RotationAngle Angle);

public sealed class RotateImageHandler(
    IImageRepository images, IFileStorage storage, IImageProcessor processor)
{
    public async Task<ImageVersionDto> HandleAsync(RotateImageCommand cmd, CancellationToken ct = default)
    {
        var image = await images.GetByIdAsync(cmd.ImageId, ct)
                    ?? throw new NotFoundException("Изображение не найдено");

        if (image.OwnerId != cmd.UserId)
            throw new NotFoundException("Изображение не найдено");

        var current = image.Versions.Single(v => v.Id == image.CurrentVersionId);

        await using var input = await storage.OpenReadAsync(current.FilePath, ct);
        var result = await processor.RotateAsync(input, cmd.Angle, ct);

        var path = await storage.SaveAsync(result.Content, result.Extension, ct);

        var version = image.AddVersion(
            operation: "rotate",
            parameters: JsonSerializer.Serialize(new { degrees = (int)cmd.Angle }),
            filePath: path,
            contentType: result.ContentType,
            size: result.Content.Length,
            width: result.Width,
            height: result.Height);

        await images.SaveChangesAsync(ct);
        return ImageVersionDto.From(version, image.CurrentVersionId);
    }
}