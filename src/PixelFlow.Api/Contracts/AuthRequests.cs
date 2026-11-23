namespace PixelFlow.Api.Contracts;

public sealed record RegisterRequest(
    string Login,
    string Password);

public sealed record LoginRequest(
    string Login,
    string Password);

public sealed record AddFriendRequest(string Login);