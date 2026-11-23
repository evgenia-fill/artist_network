using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Infrastructure.Persistence.Configurations;

public sealed class FriendshipConfiguration
    : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> b)
    {
        b.ToTable("friendships");

        b.HasKey(x => new { x.UserId, x.FriendId });

        b.Property(x => x.CreatedAt).IsRequired();

        b.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.FriendId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.FriendId);

        b.ToTable(t =>
            t.HasCheckConstraint(
                "ck_friendships_not_self",
                "user_id <> friend_id"));
    }
}