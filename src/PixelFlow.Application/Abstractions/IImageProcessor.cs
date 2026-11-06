using PixelFlow.Application.Models;
namespace PixelFlow.Application.Abstractions;

public interface IImageProcessor
{
    Task<ImageInfo> ReadInfoAsync(Stream input, CancellationToken ct = default);
    Task<ProcessedImage> RotateAsync(Stream input, RotationAngle angle, CancellationToken ct = default);
    Task<ProcessedImage> FlipAsync(Stream input, FlipAxis axis, CancellationToken ct = default);
}