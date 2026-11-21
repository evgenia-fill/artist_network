namespace PixelFlow.Contracts;

public record JobCreated(
    Guid JobId,
    Guid ImageId,
    Guid UserId,
    string Operation,
    string? Parameters,
    DateTimeOffset CreatedAt);

public record JobProgressChanged(Guid JobId, Guid ImageId, Guid UserId, int Progress);

public record JobCompleted(Guid JobId, Guid ImageId, Guid UserId, Guid VersionId);

public record JobFailed(Guid JobId, Guid ImageId, Guid UserId, string Error);