using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Login)
            .HasMaxLength(32)
            .IsRequired();

        b.Property(x => x.PasswordHash)
            .IsRequired();

        b.Property(x => x.CreatedAt)
            .IsRequired();

        b.HasIndex(x => x.Login).IsUnique();
    }
}