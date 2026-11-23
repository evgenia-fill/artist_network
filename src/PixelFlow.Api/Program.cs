using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PixelFlow.Api.Middleware;
using PixelFlow.Api.Realtime;
using PixelFlow.Application.Abstractions;
using PixelFlow.Application;
using PixelFlow.Domain.Entities;
using PixelFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Auth:JwtKey"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Не настроен Auth:JwtKey. Нужен ключ минимум 32 байта.");
}

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

var issuer = builder.Configuration["Auth:Issuer"] ?? "PixelFlow";
var audience = builder.Configuration["Auth:Audience"] ?? "PixelFlow";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IImageChangeNotifier, SignalRImageChangeNotifier>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<JobCompletedConsumer>();
    x.AddConsumer<JobFailedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"]!);
            h.Password(builder.Configuration["RabbitMq:Password"]!);
        });
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();
app.MapHub<ImageUpdatesHub>("/hubs/images");
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();