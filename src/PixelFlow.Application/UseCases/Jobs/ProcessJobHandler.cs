using System.Text.Json;
using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;

namespace PixelFlow.Application.UseCases.Jobs;

public sealed class ProcessJobHandler(
    IJobRepository jobs,
    IImageRepository images,
    IFileStorage storage,
    IImageProcessor processor,
    IJobEventPublisher events,
    JobProcessingOptions options)
{
    public async Task<JobProcessingResult> HandleAsync(Guid jobId, string workerId, CancellationToken ct = default)
    {
        if (!await jobs.TryAcquireAsync(jobId, workerId, TimeSpan.FromSeconds(options.LeaseSeconds), options.MaxAttempts, ct))
            return JobProcessingResult.Skipped;

        var job = await jobs.GetByIdAsync(jobId, ct)
            ?? throw new NotFoundException("Задача не найдена");

        Guid versionId;
        try
        {
            var image = await images.GetByIdAsync(job.ImageId, ct)
                ?? throw new NotFoundException("Изображение не найдено");

            var steps = ParseSteps(job.Parameters);
            var current = image.Versions.Single(v => v.Id == image.CurrentVersionId);

            await using var input = await storage.OpenReadAsync(current.FilePath, ct);
            var output = await processor.ApplyTemplateAsync(input, steps, ct);
            var path = await storage.SaveAsync(output.Content, output.Extension, ct);

            var version = image.AddVersion("template", job.Parameters, path, output.ContentType,
                output.Content.LongLength, output.Width, output.Height);
            job.Complete(version.Id);
            await jobs.SaveChangesAsync(ct);
            versionId = version.Id;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var permanent = ex is ValidationException or NotFoundException;
            var retryAt = DateTimeOffset.UtcNow.Add(options.RetryDelay(job.Attempts));
            var outcome = await jobs.ResolveFailureAsync(jobId, workerId, ex.Message,
                options.MaxAttempts, retryAt, permanent, ct);

            if (outcome == JobFailureOutcome.Failed)
            {
                await events.PublishFailedAsync(job, ex.Message, ct);
                return JobProcessingResult.Failed;
            }

            return outcome == JobFailureOutcome.RetryScheduled
                ? JobProcessingResult.RetryScheduled
                : JobProcessingResult.Skipped;
        }

        await events.PublishCompletedAsync(job, versionId, ct);
        return JobProcessingResult.Completed;
    }

    private static IReadOnlyList<ImageEditStep> ParseSteps(string? parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters))
            throw new ValidationException("У задачи нет параметров");

        using var doc = JsonDocument.Parse(parameters);
        var steps = new List<ImageEditStep>();

        foreach (var s in doc.RootElement.GetProperty("steps").EnumerateArray())
        {
            var type = s.GetProperty("type").GetString();
            switch (type)
            {
                case "rotate":
                    steps.Add(new RotateEditStep((RotationAngle)s.GetProperty("degrees").GetInt32()));
                    break;
                case "flip":
                    steps.Add(new FlipEditStep(
                        Enum.Parse<FlipAxis>(s.GetProperty("axis").GetString()!, ignoreCase: true)));
                    break;
                default:
                    throw new ValidationException($"Неизвестный тип шага: {type}");
            }
        }

        return steps;
    }
}
