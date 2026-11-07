using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using AppImageInfo = PixelFlow.Application.Models.ImageInfo;
using SharpImage = SixLabors.ImageSharp.Image;

namespace PixelFlow.Infrastructure.Imaging;

public sealed class ImageSharpProcessor : IImageProcessor
{
    private static readonly HashSet<string> AllowedMimeTypes =
        ["image/jpeg", "image/png", "image/webp"];

    public async Task<AppImageInfo> ReadInfoAsync(Stream input, CancellationToken ct = default)
    {
        try
        {
            var info = await SharpImage.IdentifyAsync(input, ct);
            var format = GetAllowedFormat(info.Metadata.DecodedImageFormat);

            return new AppImageInfo(info.Width, info.Height, format.DefaultMimeType, GetExtension(format));
        }
        catch (ImageFormatException)
        {
            throw new ValidationException("Файл не является поддерживаемым изображением (JPEG, PNG, WebP)");
        }
    }

    public Task<ProcessedImage> RotateAsync(Stream input, RotationAngle angle, CancellationToken ct = default)
    {
        var mode = angle switch
        {
            RotationAngle.Degrees90 => RotateMode.Rotate90,
            RotationAngle.Degrees180 => RotateMode.Rotate180,
            RotationAngle.Degrees270 => RotateMode.Rotate270,
            _ => throw new ValidationException("Допустимые углы: 90, 180, 270")
        };

        return ProcessAsync(input, image => image.Mutate(x => x.Rotate(mode)), ct);
    }

    public Task<ProcessedImage> FlipAsync(Stream input, FlipAxis axis, CancellationToken ct = default)
    {
        var mode = axis switch
        {
            FlipAxis.Horizontal => FlipMode.Horizontal,
            FlipAxis.Vertical => FlipMode.Vertical,
            _ => throw new ValidationException("Допустимые оси: horizontal, vertical")
        };

        return ProcessAsync(input, image => image.Mutate(x => x.Flip(mode)), ct);
    }

    private static async Task<ProcessedImage> ProcessAsync(
        Stream input, Action<SharpImage> operation, CancellationToken ct)
    {
        try
        {
            using var image = await SharpImage.LoadAsync(input, ct);
            var format = GetAllowedFormat(image.Metadata.DecodedImageFormat);

            image.Mutate(x => x.AutoOrient()); 
            operation(image);

            using var output = new MemoryStream();
            await image.SaveAsync(output, format, ct);

            return new ProcessedImage(
                output.ToArray(), format.DefaultMimeType, GetExtension(format),
                image.Width, image.Height);
        }
        catch (ImageFormatException)
        {
            throw new ValidationException("Не удалось обработать изображение");
        }
    }

    private static IImageFormat GetAllowedFormat(IImageFormat? format)
    {
        if (format is null || !AllowedMimeTypes.Contains(format.DefaultMimeType))
            throw new ValidationException("Поддерживаются только JPEG, PNG и WebP");
        return format;
    }

    private static string GetExtension(IImageFormat format) => "." + format.FileExtensions.First();
}