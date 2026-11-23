using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PixelFlow.Api.Contracts;
using PixelFlow.Domain.Entities;
using PixelFlow.Infrastructure.Persistence;

namespace PixelFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AppDbContext db,
    IPasswordHasher<User> hasher,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken ct)
    {
        var login = NormalizeLogin(request.Login);

        if (login is null ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 8)
        {
            return BadRequest(new
            {
                message = "Логин: 3–32 символа, латиница, цифры или _. Пароль: минимум 8 символов."
            });
        }

        if (await db.Users.AnyAsync(x => x.Login == login, ct))
            return Conflict(new { message = "Логин уже занят." });

        var user = new User(login, "temporary");
        var hash = hasher.HashPassword(user, request.Password);

        user = new User(login, hash);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return Ok(CreateAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken ct)
    {
        var login = NormalizeLogin(request.Login);

        if (login is null)
            return Unauthorized(new { message = "Неверный логин или пароль." });

        var user = await db.Users
            .SingleOrDefaultAsync(x => x.Login == login, ct);

        if (user is null ||
            hasher.VerifyHashedPassword(
                user, user.PasswordHash, request.Password)
            == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Неверный логин или пароль."
            });
        }

        return Ok(CreateAuthResponse(user));
    }

    private object CreateAuthResponse(User user)
    {
        var keyText = configuration["Auth:JwtKey"];

        if (string.IsNullOrWhiteSpace(keyText) ||
            Encoding.UTF8.GetByteCount(keyText) < 32)
        {
            throw new InvalidOperationException(
                "Auth:JwtKey должен содержать минимум 32 байта.");
        }

        var issuer = configuration["Auth:Issuer"] ?? "PixelFlow";
        var audience = configuration["Auth:Audience"] ?? "PixelFlow";

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.Login)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(keyText));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials);

        return new
        {
            token = new JwtSecurityTokenHandler()
                .WriteToken(token),

            user = new
            {
                id = user.Id,
                login = user.Login
            }
        };
    }

    private static string? NormalizeLogin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var login = value.Trim().ToLowerInvariant();

        if (login.Length is < 3 or > 32)
            return null;

        return login.All(c =>
            char.IsAsciiLetterOrDigit(c) || c == '_')
            ? login
            : null;
    }
}
