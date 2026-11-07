using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.UseCases.Images;

public sealed record UploadImageCommand(Guid UserId, Stream Content, long Length);

public sealed record UploadImageResult(Guid ImageId, ImageVersionDto Version);

public sealed class UploadImageHandler(
    IImageRepository images,
    IFileStorage storage,
    IImageProcessor processor)
{
    private const long MaxSizeBytes = 10 * 1024 * 1024;

    public async Task<UploadImageResult> HandleAsync(UploadImageCommand cmd, CancellationToken ct = default)
    {
        if (cmd.Length is <= 0 or > MaxSizeBytes)
            throw new ValidationException("Размер файла должен быть от 1 байта до 10 МБ");

        using var buffer = new MemoryStream();
        await cmd.Content.CopyToAsync(buffer, ct);
        if (buffer.Length > MaxSizeBytes)
            throw new ValidationException("Размер файла превышает 10 МБ");

        buffer.Position = 0;
        var info = await processor.ReadInfoAsync(buffer, ct);

        var bytes = buffer.ToArray();
        var path = await storage.SaveAsync(bytes, info.Extension, ct);

        var image = new Image(cmd.UserId);
        var version = image.AddVersion("original", null, path, info.ContentType,
            bytes.Length, info.Width, info.Height);

        await images.AddAsync(image, ct);
        await images.SaveChangesAsync(ct);

        return new UploadImageResult(image.Id, ImageVersionDto.From(version, image.CurrentVersionId));
    }
}