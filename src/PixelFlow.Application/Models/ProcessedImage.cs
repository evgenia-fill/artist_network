namespace PixelFlow.Application.Models;

public sealed record ProcessedImage(byte[] Content, string ContentType, string Extension, int Width, int Height);