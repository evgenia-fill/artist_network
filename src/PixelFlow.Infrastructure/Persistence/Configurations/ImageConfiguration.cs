using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Infrastructure.Persistence.Configurations;

public sealed class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> b)
    {
        b.ToTable("images");
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).ValueGeneratedNever();   // Id создаётся в коде
        b.HasIndex(i => i.OwnerId);

        b.HasMany(i => i.Versions)
            .WithOne()
            .HasForeignKey(v => v.ImageId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(i => i.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}