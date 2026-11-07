using PixelFlow.Domain.Entities;

namespace PixelFlow.Domain.Tests;

public sealed class ImageTests
{
    [Fact]
    public void AddVersion_SetsSequentialNumberAndParent()
    {
        var image = new Image(Guid.NewGuid());
        var original = image.AddVersion("original", null, "original.jpg", "image/jpeg", 67, 100, 200);
        var rotated = image.AddVersion("rotate", "{\"degrees\":90}", "rotated.jpg", "image/jpeg", 68, 200, 100);

        Assert.Equal(1, original.Number);
        Assert.Equal(2, rotated.Number);
        Assert.Equal(original.Id, rotated.ParentVersionId);
        Assert.Equal(rotated.Id, image.CurrentVersionId);
        Assert.Equal(2, image.Versions.Count);
    }

    [Fact]
    public void RestoreTo_ChangesCurrentVersionWithoutRemovingHistory()
    {
        var image = new Image(Guid.NewGuid());
        var original = image.AddVersion("original", null, "original.jpg", "image/jpeg", 67, 100, 200);
        image.AddVersion("flip", "{\"axis\":\"horizontal\"}", "flipped.jpg", "image/jpeg", 69, 100, 200);

        image.RestoreTo(original.Id);

        Assert.Equal(original.Id, image.CurrentVersionId);
        Assert.Equal(2, image.Versions.Count);
    }

    [Fact]
    public void Reset_ReturnsToOriginalVersion()
    {
        var image = new Image(Guid.NewGuid());
        var original = image.AddVersion("original", null, "original.jpg", "image/jpeg", 67, 100, 200);
        image.AddVersion("rotate", "{\"degrees\":180}", "rotated.jpg", "image/jpeg", 68, 100, 200);

        image.Reset();

        Assert.Equal(original.Id, image.CurrentVersionId);
    }

    [Fact]
    public void RestoreTo_UnknownVersion_Throws()
    {
        var image = new Image(Guid.NewGuid());
        image.AddVersion("original", null, "original.jpg", "image/jpeg", 67, 100, 200);

        Assert.Throws<KeyNotFoundException>(() => image.RestoreTo(Guid.NewGuid()));
    }
}
