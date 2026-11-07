using System.Text.Json;
using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.UseCases.Images;

public sealed record ApplyImageTemplateCommand(
    Guid UserId,
    IReadOnlyList<Guid> ImageIds,
    IReadOnlyList<ImageEditStep> Steps);

public sealed record AppliedImageTemplate(Guid ImageId, ImageVersionDto Version);

public sealed class ApplyImageTemplateHandler(
    IImageRepository images,
    IFileStorage storage,
    IImageProcessor processor,
    IImageChangeNotifier notifier)
{
    public async Task<IReadOnlyList<AppliedImageTemplate>> HandleAsync(
        ApplyImageTemplateCommand cmd, CancellationToken ct = default)
    {
        if (cmd.ImageIds is null || cmd.ImageIds.Count is < 1 or > 10 ||
            cmd.ImageIds.Any(id => id == Guid.Empty) ||
            cmd.ImageIds.Distinct().Count() != cmd.ImageIds.Count)
            throw new ValidationException("Нужно указать от 1 до 10 разных изображений");

        if (cmd.Steps is null || cmd.Steps.Count is < 1 or > 5)
            throw new ValidationException("Шаблон должен содержать от 1 до 5 действий");

        foreach (var step in cmd.Steps)
        {
            if (step is not (RotateEditStep { Angle: RotationAngle.Degrees90 or
                RotationAngle.Degrees180 or RotationAngle.Degrees270 } or
                FlipEditStep { Axis: FlipAxis.Horizontal or FlipAxis.Vertical }))
                throw new ValidationException("Шаблон содержит неподдерживаемое действие");
        }

        var ownedImages = new List<Image>(cmd.ImageIds.Count);
        foreach (var id in cmd.ImageIds)
        {
            var image = await images.GetByIdAsync(id, ct);
            if (image is null || image.OwnerId != cmd.UserId)
                throw new NotFoundException("Изображение не найдено");
            ownedImages.Add(image);
        }

        var parameters = JsonSerializer.Serialize(new
        {
            steps = cmd.Steps.Select(step => step switch
            {
                RotateEditStep rotate => (object)new { type = "rotate", degrees = (int)rotate.Angle },
                FlipEditStep flip => new { type = "flip", axis = flip.Axis.ToString().ToLowerInvariant() },
                _ => throw new ValidationException("Шаблон содержит неподдерживаемое действие")
            }).ToArray()
        });

        var results = new List<AppliedImageTemplate>(ownedImages.Count);
        foreach (var image in ownedImages)
        {
            ct.ThrowIfCancellationRequested();
            var current = image.Versions.Single(v => v.Id == image.CurrentVersionId);
            await using var input = await storage.OpenReadAsync(current.FilePath, ct);
            var output = await processor.ApplyTemplateAsync(input, cmd.Steps, ct);
            var path = await storage.SaveAsync(output.Content, output.Extension, ct);

            var version = image.AddVersion(
                operation: "template",
                parameters: parameters,
                filePath: path,
                contentType: output.ContentType,
                size: output.Content.LongLength,
                width: output.Width,
                height: output.Height);

            await images.SaveChangesAsync(ct);
            var dto = ImageVersionDto.From(version, image.CurrentVersionId);
            results.Add(new AppliedImageTemplate(image.Id, dto));
            await notifier.NotifyAsync(image.Id, dto, ct);
        }

        return results;
    }
}
