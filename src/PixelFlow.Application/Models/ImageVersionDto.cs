using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.Models;

public sealed record ImageVersionDto(
    Guid Id, int Number, string Operation, string? Parameters,
    int Width, int Height, long Size, DateTimeOffset CreatedAt, bool IsCurrent)
{
    public static ImageVersionDto From(ImageVersion v, Guid? currentId) =>
        new(v.Id, v.Number, v.Operation, v.Parameters, v.Width, v.Height, v.Size, v.CreatedAt, v.Id == currentId);
}