using MassTransit;
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