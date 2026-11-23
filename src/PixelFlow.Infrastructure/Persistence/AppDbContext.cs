using Microsoft.EntityFrameworkCore;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Image> Images => Set<Image>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Friendship> Friendships => Set<Friendship>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}