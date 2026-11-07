using Microsoft.AspNetCore.Http;

namespace PixelFlow.Api.Auth;

public static class DemoUserId
{
    private static readonly Guid DefaultId = Guid.Parse("11111111-1111-1111-1111-111111111111"); // заглушка, потом будет реальный жвт токен

    public static Guid Resolve(IHeaderDictionary? headers)
    {
        if (headers is not null &&
            headers.TryGetValue("X-User-Id", out var value) &&
            Guid.TryParse(value.ToString(), out var userId))
            return userId;

        return DefaultId;
    }
}
