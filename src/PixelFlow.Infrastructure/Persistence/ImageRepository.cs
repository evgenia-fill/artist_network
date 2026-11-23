using Microsoft.EntityFrameworkCore;
using PixelFlow.Application.Abstractions;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Infrastructure.Persistence;

public sealed class ImageRepository(AppDbContext db) : IImageRepository
{
    public Task<Image?> GetByIdAsync(Guid imageId, CancellationToken ct = default) =>
        db.Images.Include(i => i.Versions).FirstOrDefaultAsync(i => i.Id == imageId, ct);

    public async Task<IReadOnlyList<Image>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        await db.Images.Include(i => i.Versions)
            .Where(i => i.OwnerId == ownerId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Image image, CancellationToken ct = default) =>
        await db.Images.AddAsync(image, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}