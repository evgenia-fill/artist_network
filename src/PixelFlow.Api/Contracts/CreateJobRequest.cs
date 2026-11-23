namespace PixelFlow.Api.Contracts;

public sealed record CreateJobRequest(IReadOnlyList<ImageTemplateStepRequest?>? Steps);