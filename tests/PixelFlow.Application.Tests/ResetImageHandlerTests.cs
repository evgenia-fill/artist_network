using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;
using PixelFlow.Application.UseCases.Images;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.Tests;

public sealed class ResetImageHandlerTests
{
    [Fact]
    public async Task Reset_NotifiesSubscribersOnlyAfterSave()
    {
        var ownerId = Guid.NewGuid();
        var image = new Image(ownerId);
        var original = image.AddVersion("original", null, "original.jpg", "image/jpeg", 67, 100, 200);
        image.AddVersion("rotate", "{\"degrees\":90}", "rotated.jpg", "image/jpeg", 68, 200, 100);
        var repository = new FakeImageRepository(image);
        var notifier = new RecordingNotifier(repository);

        var result = await new ResetImageHandler(repository, notifier)
            .HandleAsync(new ResetImageCommand(ownerId, image.Id));

        Assert.Equal(original.Id, result.Id);
        Assert.True(result.IsCurrent);
        Assert.True(repository.WasSaved);
        Assert.True(notifier.WasCalledAfterSave);
        Assert.Equal(image.Id, notifier.NotifiedImageId);
        Assert.Equal(original.Id, notifier.NotifiedVersion?.Id);
    }

    [Fact]
    public async Task Reset_OfAnotherUsersImage_DoesNotNotify()
    {
        var image = new Image(Guid.NewGuid());
        image.AddVersion("original", null, "original.jpg", "image/jpeg", 67, 100, 200);
        var repository = new FakeImageRepository(image);
        var notifier = new RecordingNotifier(repository);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ResetImageHandler(repository, notifier)
                .HandleAsync(new ResetImageCommand(Guid.NewGuid(), image.Id)));

        Assert.False(repository.WasSaved);
        Assert.False(notifier.WasCalledAfterSave);
    }

    private sealed class FakeImageRepository(Image image) : IImageRepository
    {
        public bool WasSaved { get; private set; }

        public Task<Image?> GetByIdAsync(Guid imageId, CancellationToken ct = default) =>
            Task.FromResult<Image?>(imageId == image.Id ? image : null);

        public Task<IReadOnlyList<Image>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        {
            IReadOnlyList<Image> result = ownerId == image.OwnerId
                ? new[] { image }
                : Array.Empty<Image>();
            return Task.FromResult(result);
        }

        public Task AddAsync(Image item, CancellationToken ct = default) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            WasSaved = true;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingNotifier(FakeImageRepository repository) : IImageChangeNotifier
    {
        public bool WasCalledAfterSave { get; private set; }
        public Guid? NotifiedImageId { get; private set; }
        public ImageVersionDto? NotifiedVersion { get; private set; }

        public Task NotifyAsync(Guid imageId, ImageVersionDto currentVersion, CancellationToken ct = default)
        {
            WasCalledAfterSave = repository.WasSaved;
            NotifiedImageId = imageId;
            NotifiedVersion = currentVersion;
            return Task.CompletedTask;
        }
    }
}