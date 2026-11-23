namespace PixelFlow.Application.Models;

public sealed class JobProcessingOptions
{
    public int MaxAttempts { get; set; } = 3;
    public int LeaseSeconds { get; set; } = 120;
    public int RetryInitialDelaySeconds { get; set; } = 5;
    public int RecoveryIntervalSeconds { get; set; } = 15;
    public int PendingReplayAfterSeconds { get; set; } = 30;
    public int RecoveryBatchSize { get; set; } = 100;

    public void Validate()
    {
        if (MaxAttempts < 1 || LeaseSeconds < 10 || RetryInitialDelaySeconds < 1 ||
            RecoveryIntervalSeconds < 1 || PendingReplayAfterSeconds < 1 ||
            RecoveryBatchSize is < 1 or > 1000)
            throw new InvalidOperationException("Некорректная конфигурация JobProcessing");
    }

    public TimeSpan RetryDelay(int attempts)
    {
        var seconds = RetryInitialDelaySeconds * Math.Pow(2, Math.Max(0, attempts - 1));
        return TimeSpan.FromSeconds(Math.Min(seconds, 3600));
    }
}
