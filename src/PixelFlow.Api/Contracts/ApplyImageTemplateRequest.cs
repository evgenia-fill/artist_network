namespace PixelFlow.Api.Contracts;

public sealed record ApplyImageTemplateRequest(
    IReadOnlyList<Guid>? ImageIds,
    IReadOnlyList<ImageTemplateStepRequest?>? Steps);