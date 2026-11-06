using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.Abstractions;

public interface IImageRepository
{
    Task<Image?> GetByIdAsync(Guid imageId, CancellationToken ct = default);
    Task<IReadOnlyList<Image>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task AddAsync(Image image, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}