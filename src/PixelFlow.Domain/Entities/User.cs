namespace PixelFlow.Domain.Entities;

public sealed class User
{
    private User() { }

    public User(string login, string passwordHash)
    {
        Id = Guid.NewGuid();
        Login = login;
        PasswordHash = passwordHash;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Login { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
}