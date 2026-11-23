using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.Models;

public sealed record JobDto(
    Guid Id, Guid ImageId, string Operation, string Status, int Attempts, int Progress,
    string? Error, Guid? ResultVersionId, DateTimeOffset CreatedAt, DateTimeOffset LastUpdated,
    DateTimeOffset? NextAttemptAt)
{
    public static JobDto From(Job j) =>
        new(j.Id, j.ImageId, j.Operation, j.Status.ToString(), j.Attempts, j.Progress,
            j.Error, j.ResultVersionId, j.CreatedAt, j.LastUpdated, j.NextAttemptAt);
}