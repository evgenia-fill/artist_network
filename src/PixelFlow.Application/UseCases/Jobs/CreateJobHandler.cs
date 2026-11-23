using System.Text.Json;
using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.UseCases.Jobs;

public sealed record CreateJobCommand(Guid UserId, Guid ImageId, IReadOnlyList<ImageEditStep> Steps);

public sealed class CreateJobHandler(
    IImageRepository images, IJobRepository jobs, IJobEventPublisher events)
{
    public async Task<JobDto> HandleAsync(CreateJobCommand cmd, CancellationToken ct = default)
    {
        if (cmd.Steps is null || cmd.Steps.Count is < 1 or > 5)
            throw new ValidationException("Задача должна содержать от 1 до 5 действий");

        var image = await images.GetByIdAsync(cmd.ImageId, ct);
        if (image is null || image.OwnerId != cmd.UserId)
            throw new NotFoundException("Изображение не найдено");

        var parameters = JsonSerializer.Serialize(new
        {
            steps = cmd.Steps.Select(step => step switch
            {
                RotateEditStep r => (object)new { type = "rotate", degrees = (int)r.Angle },
                FlipEditStep f => new { type = "flip", axis = f.Axis.ToString().ToLowerInvariant() },
                _ => throw new ValidationException("Неподдерживаемое действие")
            }).ToArray()
        });

        var job = new Job(image.Id, cmd.UserId, "template", parameters);
        await jobs.AddAsync(job, ct);
        await jobs.SaveChangesAsync(ct);

        await events.PublishCreatedAsync(job, ct);
        return JobDto.From(job);
    }
}