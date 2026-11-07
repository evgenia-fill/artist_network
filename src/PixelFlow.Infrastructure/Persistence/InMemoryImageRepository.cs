using System.Collections.Concurrent;
using PixelFlow.Application.Abstractions;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Infrastructure.Persistence;

public sealed class InMemoryImageRepository : IImageRepository
{
    private readonly ConcurrentDictionary<Guid, Image> _images = new();

    public Task<Image?> GetByIdAsync(Guid imageId, CancellationToken ct = default) =>
        Task.FromResult(_images.GetValueOrDefault(imageId));

    public Task<IReadOnlyList<Image>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Image>>(
            _images.Values.Where(i => i.OwnerId == ownerId).OrderBy(i => i.CreatedAt).ToList());

    public Task AddAsync(Image image, CancellationToken ct = default)
    {
        _images[image.Id] = image;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}