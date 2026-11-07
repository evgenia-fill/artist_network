using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;
using PixelFlow.Application.UseCases.Images;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.Tests;

public sealed class ApplyImageTemplateHandlerTests
{
    [Fact]
    public async Task ApplyTemplate_CreatesOneVersionForEachImageAndNotifiesAfterSave()
    {
        var ownerId = Guid.NewGuid();
        var first = NewImage(ownerId);
        var second = NewImage(ownerId);
        var repository = new FakeRepository(first, second);
        var processor = new FakeProcessor();
        var notifier = new FakeNotifier(repository);
        var handler = CreateHandler(repository, processor, notifier);

        var results = await handler.HandleAsync(new ApplyImageTemplateCommand(
            ownerId, [first.Id, second.Id],
            [new RotateEditStep(RotationAngle.Degrees90), new FlipEditStep(FlipAxis.Horizontal)]));

        Assert.Equal(2, results.Count);
        Assert.Equal(2, processor.CallCount);
        Assert.Equal(2, repository.SaveCount);
        Assert.Equal(2, notifier.Sent.Count);
        Assert.Equal(new[] { 1, 2 }, notifier.Sent.Select(n => n.SaveCountWhenSent));
        Assert.All(results, result =>
        {
            Assert.Equal("template", result.Version.Operation);
            Assert.Equal(2, result.Version.Number);
            Assert.True(result.Version.IsCurrent);
            Assert.Contains("rotate", result.Version.Parameters!);
            Assert.Contains("flip", result.Version.Parameters!);
        });
        Assert.All(new[] { first, second }, image => Assert.Equal(2, image.Versions.Count));
    }

    [Fact]
    public async Task ApplyTemplate_DoesNotModifyAnyImageWhenOneIsNotOwned()
    {
        var ownerId = Guid.NewGuid();
        var first = NewImage(ownerId);
        var anotherUsersImage = NewImage(Guid.NewGuid());
        var repository = new FakeRepository(first, anotherUsersImage);
        var processor = new FakeProcessor();
        var handler = CreateHandler(repository, processor, new FakeNotifier(repository));

        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(
            new ApplyImageTemplateCommand(ownerId, [first.Id, anotherUsersImage.Id],
                [new RotateEditStep(RotationAngle.Degrees90)])));

        Assert.Equal(0, processor.CallCount);
        Assert.Equal(0, repository.SaveCount);
        Assert.Single(first.Versions);
    }

    [Fact]
    public async Task ApplyTemplate_RejectsDuplicateImageIdsBeforeProcessing()
    {
        var ownerId = Guid.NewGuid();
        var image = NewImage(ownerId);
        var repository = new FakeRepository(image);
        var processor = new FakeProcessor();
        var handler = CreateHandler(repository, processor, new FakeNotifier(repository));

        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(
            new ApplyImageTemplateCommand(ownerId, [image.Id, image.Id],
                [new FlipEditStep(FlipAxis.Vertical)])));

        Assert.Equal(0, processor.CallCount);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task ApplyTemplate_RejectsMoreThanTenImages()
    {
        var repository = new FakeRepository();
        var processor = new FakeProcessor();
        var handler = CreateHandler(repository, processor, new FakeNotifier(repository));

        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(
            new ApplyImageTemplateCommand(Guid.NewGuid(),
                Enumerable.Range(0, 11).Select(_ => Guid.NewGuid()).ToArray(),
                [new RotateEditStep(RotationAngle.Degrees90)])));

        Assert.Equal(0, processor.CallCount);
    }

    [Fact]
    public async Task ApplyTemplate_RejectsInvalidRotationBeforeProcessing()
    {
        var ownerId = Guid.NewGuid();
        var image = NewImage(ownerId);
        var repository = new FakeRepository(image);
        var processor = new FakeProcessor();
        var handler = CreateHandler(repository, processor, new FakeNotifier(repository));

        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(
            new ApplyImageTemplateCommand(ownerId, [image.Id],
                [new RotateEditStep((RotationAngle)45)])));

        Assert.Equal(0, processor.CallCount);
        Assert.Single(image.Versions);
    }

    private static Image NewImage(Guid ownerId)
    {
        var image = new Image(ownerId);
        image.AddVersion("original", null, "source.jpg", "image/jpeg", 67, 100, 100);
        return image;
    }

    private static ApplyImageTemplateHandler CreateHandler(
        FakeRepository repository, FakeProcessor processor, FakeNotifier notifier) =>
        new(repository, new FakeStorage(), processor, notifier);

    private sealed class FakeRepository : IImageRepository
    {
        private readonly Dictionary<Guid, Image> _images;

        public FakeRepository(params Image[] entries) => _images = entries.ToDictionary(i => i.Id);

        public int SaveCount { get; private set; }

        public Task<Image?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_images.GetValueOrDefault(id));

        public Task<IReadOnlyList<Image>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Image>>(_images.Values.Where(i => i.OwnerId == ownerId).ToArray());

        public Task AddAsync(Image image, CancellationToken ct = default)
        {
            _images[image.Id] = image;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStorage : IFileStorage
    {
        public Task<string> SaveAsync(byte[] content, string extension, CancellationToken ct = default) =>
            Task.FromResult($"{Guid.NewGuid():N}{extension}");

        public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default) =>
            Task.FromResult<Stream>(new MemoryStream([67, 68]));
    }

    private sealed class FakeProcessor : IImageProcessor
    {
        public int CallCount { get; private set; }

        public Task<ImageInfo> ReadInfoAsync(Stream input, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<ProcessedImage> RotateAsync(Stream input, RotationAngle angle, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<ProcessedImage> FlipAsync(Stream input, FlipAxis axis, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<ProcessedImage> ApplyTemplateAsync(
            Stream input, IReadOnlyList<ImageEditStep> steps, CancellationToken ct = default)
        {
            Assert.Collection(steps,
                step => Assert.IsType<RotateEditStep>(step),
                step => Assert.IsType<FlipEditStep>(step));
            CallCount++;
            return Task.FromResult(new ProcessedImage([67, 68], "image/jpeg", ".jpg", 100, 100));
        }
    }

    private sealed class FakeNotifier(FakeRepository repository) : IImageChangeNotifier
    {
        public List<(Guid ImageId, int SaveCountWhenSent)> Sent { get; } = [];

        public Task NotifyAsync(Guid imageId, ImageVersionDto currentVersion, CancellationToken ct = default)
        {
            Sent.Add((imageId, repository.SaveCount));
            return Task.CompletedTask;
        }
    }
}
