using Microsoft.EntityFrameworkCore;
using PixelFlow.Application.Abstractions;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Infrastructure.Persistence;

public sealed class JobRepository(AppDbContext db) : IJobRepository
{
    public Task<Job?> GetByIdAsync(Guid jobId, CancellationToken ct = default) =>
        db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);

    public async Task AddAsync(Job job, CancellationToken ct = default) =>
        await db.Jobs.AddAsync(job, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}