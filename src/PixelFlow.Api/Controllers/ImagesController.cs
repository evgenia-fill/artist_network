using Microsoft.AspNetCore.Mvc;
using PixelFlow.Api.Auth;
using PixelFlow.Api.Contracts;
using PixelFlow.Application.Exceptions;
using PixelFlow.Application.Models;
using PixelFlow.Application.UseCases.Images;

namespace PixelFlow.Api.Controllers;

[ApiController]
[Route("api/images")]
public sealed class ImagesController(
    UploadImageHandler upload,
    GetImageFileHandler getFile,
    RotateImageHandler rotate,
    FlipImageHandler flip,
    ResetImageHandler reset,
    RestoreVersionHandler restore,
    GetVersionsHandler getVersions,
    ApplyImageTemplateHandler applyTemplate) : ControllerBase
{
    private Guid CurrentUserId => DemoUserId.Resolve(Request.Headers);

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var result = await upload.HandleAsync(new UploadImageCommand(CurrentUserId, stream, file.Length), ct);
        return CreatedAtAction(nameof(GetFile), new { id = result.ImageId }, result);
    }

    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> GetFile(Guid id, CancellationToken ct)
    {
        var result = await getFile.HandleAsync(new GetImageFileQuery(CurrentUserId, id), ct);
        return File(result.Content, result.ContentType);
    }

    [HttpPost("{id:guid}/rotate")]
    public async Task<ActionResult<ImageVersionDto>> Rotate(Guid id, RotateImageRequest request, CancellationToken ct)
    {
        if (request.Degrees is not (90 or 180 or 270))
            throw new ValidationException("Допустимые углы: 90, 180, 270");

        var version = await rotate.HandleAsync(
            new RotateImageCommand(CurrentUserId, id, (RotationAngle)request.Degrees), ct);
        return Ok(version);
    }

    [HttpPost("{id:guid}/flip")]
    public async Task<ActionResult<ImageVersionDto>> Flip(Guid id, FlipImageRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<FlipAxis>(request.Axis, ignoreCase: true, out var axis) || !Enum.IsDefined(axis))
            throw new ValidationException("Допустимые оси: horizontal, vertical");

        var version = await flip.HandleAsync(new FlipImageCommand(CurrentUserId, id, axis), ct);
        return Ok(version);
    }

    [HttpPost("batch/apply-template")]
    public async Task<ActionResult<IReadOnlyList<AppliedImageTemplate>>> ApplyTemplate(
        ApplyImageTemplateRequest request, CancellationToken ct)
    {
        if (request.ImageIds is null || request.Steps is null)
            throw new ValidationException("Укажите imageIds и steps");

        var steps = request.Steps.Select(ParseStep).ToArray();
        var result = await applyTemplate.HandleAsync(
            new ApplyImageTemplateCommand(CurrentUserId, request.ImageIds, steps), ct);
        return Ok(result);
    }

    private static ImageEditStep ParseStep(ImageTemplateStepRequest? step)
    {
        if (step is null)
            throw new ValidationException("Шаг шаблона не может быть null");

        if (string.Equals(step.Type, "rotate", StringComparison.OrdinalIgnoreCase))
        {
            if (step.Degrees is not (90 or 180 or 270) || step.Axis is not null)
                throw new ValidationException("Поворот: degrees должен быть 90, 180 или 270");
            return new RotateEditStep((RotationAngle)step.Degrees.Value);
        }

        if (string.Equals(step.Type, "flip", StringComparison.OrdinalIgnoreCase))
        {
            if (step.Degrees is not null ||
                (!string.Equals(step.Axis, "horizontal", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(step.Axis, "vertical", StringComparison.OrdinalIgnoreCase)))
                throw new ValidationException("Отражение: axis должен быть horizontal или vertical");
            return new FlipEditStep(Enum.Parse<FlipAxis>(step.Axis!, ignoreCase: true));
        }

        throw new ValidationException("Допустимые действия: rotate, flip");
    }

    [HttpPost("{id:guid}/reset")]
    public async Task<ActionResult<ImageVersionDto>> Reset(Guid id, CancellationToken ct) =>
        Ok(await reset.HandleAsync(new ResetImageCommand(CurrentUserId, id), ct));

    [HttpGet("{id:guid}/versions")]
    public async Task<ActionResult<IReadOnlyList<ImageVersionDto>>> Versions(Guid id, CancellationToken ct) =>
        Ok(await getVersions.HandleAsync(new GetVersionsQuery(CurrentUserId, id), ct));

    [HttpPost("{id:guid}/versions/{versionId:guid}/restore")]
    public async Task<ActionResult<ImageVersionDto>> Restore(Guid id, Guid versionId, CancellationToken ct) =>
        Ok(await restore.HandleAsync(new RestoreVersionCommand(CurrentUserId, id, versionId), ct));
}