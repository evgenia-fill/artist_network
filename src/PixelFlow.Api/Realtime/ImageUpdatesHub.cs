using Microsoft.AspNetCore.SignalR;
using PixelFlow.Application.Abstractions;
using System.Security.Claims;

namespace PixelFlow.Api.Realtime;

public sealed class ImageUpdatesHub(IImageRepository images) : Hub
{
    public async Task Subscribe(Guid imageId)
    {
        var userIdText = Context.User?
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdText, out var userId))
            throw new HubException("Не авторизован");
        var image = await images.GetByIdAsync(imageId, Context.ConnectionAborted);
        if (image is null || image.OwnerId != userId)
            throw new HubException("Изображение не найдено");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(imageId), Context.ConnectionAborted);
    }

    public Task Unsubscribe(Guid imageId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(imageId), Context.ConnectionAborted);

    public static string GroupName(Guid imageId) => $"image:{imageId:N}";
}