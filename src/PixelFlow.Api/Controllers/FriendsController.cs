using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixelFlow.Api.Contracts;
using PixelFlow.Domain.Entities;
using PixelFlow.Infrastructure.Persistence;

namespace PixelFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/friends")]
public sealed class FriendsController(AppDbContext db) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(
            ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var friends = await db.Friendships
            .Where(x => x.UserId == CurrentUserId)
            .Join(
                db.Users,
                friendship => friendship.FriendId,
                user => user.Id,
                (friendship, user) => new
                {
                    user.Id,
                    user.Login,
                    friendship.CreatedAt
                })
            .OrderBy(x => x.Login)
            .ToListAsync(ct);

        return Ok(friends);
    }

    [HttpPost]
    public async Task<IActionResult> Add(
        AddFriendRequest request,
        CancellationToken ct)
    {
        var login = request.Login?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(login))
            return BadRequest(new { message = "Укажите логин." });

        var friend = await db.Users
            .SingleOrDefaultAsync(x => x.Login == login, ct);

        if (friend is null)
            return NotFound(new { message = "Пользователь не найден." });

        var userId = CurrentUserId;

        if (friend.Id == userId)
            return BadRequest(new
            {
                message = "Нельзя добавить самого себя."
            });

        if (await db.Friendships.AnyAsync(
            x => x.UserId == userId && x.FriendId == friend.Id, ct))
        {
            return Conflict(new
            {
                message = "Этот пользователь уже в друзьях."
            });
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync(ct);

        db.Friendships.Add(new Friendship(userId, friend.Id));
        db.Friendships.Add(new Friendship(friend.Id, userId));

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Ok(new
        {
            friend.Id,
            friend.Login
        });
    }
}
