using MassTransit;
using PixelFlow.Application.UseCases.Jobs;
using PixelFlow.Contracts;

namespace PixelFlow.Worker;

public sealed class JobCreatedConsumer(
    ProcessJobHandler handler,
    ILogger<JobCreatedConsumer> logger) : IConsumer<JobCreated>
{
    public async Task Consume(ConsumeContext<JobCreated> context)
    {
        var jobId = context.Message.JobId;
        logger.LogInformation("Получена задача {JobId}", jobId);

        var leaseOwner = $"{Environment.ProcessId}-{Guid.NewGuid():N}";
        var result = await handler.HandleAsync(jobId, leaseOwner, context.CancellationToken);

        logger.LogInformation("Задача {JobId}: {Result}", jobId, result);
    }
}