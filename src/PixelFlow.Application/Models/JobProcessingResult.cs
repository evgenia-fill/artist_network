namespace PixelFlow.Application.Models;

public enum JobProcessingResult
{
    Skipped,
    Completed,
    RetryScheduled,
    Failed
}
