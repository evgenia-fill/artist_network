namespace PixelFlow.Domain.Entities;

public sealed class Friendship
{
    private Friendship() { }

    public Friendship(Guid userId, Guid friendId)
    {
        if (userId == friendId)
            throw new ArgumentException(
                "Нельзя добавить самого себя в друзья.");

        UserId = userId;
        FriendId = friendId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public Guid FriendId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}