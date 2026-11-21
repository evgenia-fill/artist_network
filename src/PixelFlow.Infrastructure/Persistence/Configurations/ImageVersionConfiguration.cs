// ImageVersionConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Infrastructure.Persistence.Configurations;

public sealed class ImageVersionConfiguration : IEntityTypeConfiguration<ImageVersion>
{
    public void Configure(EntityTypeBuilder<ImageVersion> b)
    {
        b.ToTable("image_versions");
        b.HasKey(v => v.Id);
        b.Property(v => v.Id).ValueGeneratedNever();
        b.Property(v => v.Operation).HasMaxLength(50).IsRequired();
        b.Property(v => v.ContentType).HasMaxLength(50).IsRequired();
        b.Property(v => v.FilePath).IsRequired();
        b.Property(v => v.Parameters).HasColumnType("jsonb");
        b.HasIndex(v => new { v.ImageId, v.Number }).IsUnique();

        b.HasOne<ImageVersion>()
            .WithMany()
            .HasForeignKey(v => v.ParentVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}