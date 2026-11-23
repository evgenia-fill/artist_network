using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Infrastructure.Persistence.Configurations;

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> b)
    {
        b.ToTable("jobs");
        b.HasKey(j => j.Id);
        b.Property(j => j.Id).ValueGeneratedNever();
        b.Property(j => j.Operation).HasMaxLength(50).IsRequired();
        b.Property(j => j.Parameters).HasColumnType("jsonb");
        b.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(j => j.Error).HasMaxLength(2000);
        b.Property(j => j.LeaseOwner).HasMaxLength(100);
        b.Property(j => j.LastUpdated).IsConcurrencyToken();

        b.HasIndex(j => j.UserId);
        b.HasIndex(j => j.ImageId);
        b.HasIndex(j => new { j.Status, j.LeaseUntil });
        b.HasIndex(j => new { j.Status, j.NextAttemptAt });   

        b.HasOne<Image>().WithMany().HasForeignKey(j => j.ImageId).OnDelete(DeleteBehavior.Cascade);
    }
}