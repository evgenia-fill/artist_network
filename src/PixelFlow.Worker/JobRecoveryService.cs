using PixelFlow.Application.UseCases.Jobs;

namespace PixelFlow.Worker;

public sealed class JobRecoveryService(
    IServiceScopeFactory scopes,
    PixelFlow.Application.Models.JobProcessingOptions options,
    ILogger<JobRecoveryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.RecoveryIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<RecoverJobsHandler>();
                var count = await handler.HandleAsync(stoppingToken);
                if (count > 0)
                    logger.LogInformation("Восстановление очереди: опубликовано {Count} событий", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Не удалось восстановить задачи из базы данных");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
