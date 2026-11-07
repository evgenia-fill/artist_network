using PixelFlow.Api.Middleware;
using PixelFlow.Api.Realtime;
using PixelFlow.Application.Abstractions;
using PixelFlow.Application;
using PixelFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IImageChangeNotifier, SignalRImageChangeNotifier>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();
app.MapHub<ImageUpdatesHub>("/hubs/images");
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();