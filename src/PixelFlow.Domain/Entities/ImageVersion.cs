namespace PixelFlow.Domain.Entities;

public class ImageVersion
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ImageId { get; private set; }
    public int Number { get; private set; }
    public string Operation { get; private set; } = default!;
    public string? Parameters { get; private set; }
    public string FilePath { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long Size { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public Guid? ParentVersionId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private ImageVersion() { }  

    public ImageVersion(Guid imageId, int number, string operation, string? parameters,
        Guid? parentVersionId, string filePath, string contentType, long size, int width, int height)
    {
        ImageId = imageId; Number = number; Operation = operation; Parameters = parameters;
        ParentVersionId = parentVersionId; FilePath = filePath; ContentType = contentType;
        Size = size; Width = width; Height = height;
    }
}