using Microsoft.AspNetCore.SignalR;
using PixelFlow.Api.Auth;
using PixelFlow.Application.Abstractions;

namespace PixelFlow.Api.Realtime;

public sealed class ImageUpdatesHub(IImageRepository images) : Hub
{
    public async Task Subscribe(Guid imageId)
    {
        var userId = DemoUserId.Resolve(Context.GetHttpContext()?.Request.Headers);
        var image = await images.GetByIdAsync(imageId, Context.ConnectionAborted);
        if (image is null || image.OwnerId != userId)
            throw new HubException("Изображение не найдено");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(imageId), Context.ConnectionAborted);
    }

    public Task Unsubscribe(Guid imageId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(imageId), Context.ConnectionAborted);

    public static string GroupName(Guid imageId) => $"image:{imageId:N}";
}