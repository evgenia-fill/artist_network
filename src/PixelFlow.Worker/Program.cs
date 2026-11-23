using MassTransit;
using PixelFlow.Application.UseCases.Jobs;
using PixelFlow.Application.Models;
using PixelFlow.Infrastructure;
using PixelFlow.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
var processingOptions = builder.Configuration.GetSection("JobProcessing")
    .Get<JobProcessingOptions>() ?? new JobProcessingOptions();
processingOptions.Validate();
builder.Services.AddSingleton(processingOptions);
builder.Services.AddScoped<ProcessJobHandler>();
builder.Services.AddScoped<RecoverJobsHandler>();
builder.Services.AddHostedService<JobRecoveryService>();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<JobCreatedConsumer>();

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

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();