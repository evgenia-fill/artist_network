namespace PixelFlow.Domain.Entities;

public class Image
{
    private readonly List<ImageVersion> _versions = new();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OwnerId { get; private set; }
    public Guid? CurrentVersionId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public IReadOnlyList<ImageVersion> Versions => _versions;

    private Image() { }

    public Image(Guid ownerId) => OwnerId = ownerId;

    public ImageVersion AddVersion(string operation, string? parameters,
        string filePath, string contentType, long size, int width, int height)
    {
        var number = _versions.Count == 0 ? 1 : _versions.Max(v => v.Number) + 1;
        var version = new ImageVersion(Id, number, operation, parameters,
            CurrentVersionId, filePath, contentType, size, width, height);
        _versions.Add(version);
        CurrentVersionId = version.Id;
        return version;
    }

    public void Reset() =>
        CurrentVersionId = _versions.Single(v => v.Number == 1).Id;

    public void RestoreTo(Guid versionId)
    {
        if (_versions.All(v => v.Id != versionId))
            throw new KeyNotFoundException("Версия не найдена");
        CurrentVersionId = versionId;
    }
}