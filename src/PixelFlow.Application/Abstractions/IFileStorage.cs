namespace PixelFlow.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(byte[] content, string extension, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);
}