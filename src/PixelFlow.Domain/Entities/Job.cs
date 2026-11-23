namespace PixelFlow.Domain.Entities;

public enum JobStatus { Pending, Processing, Completed, Failed }

public class Job
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ImageId { get; private set; }
    public Guid UserId { get; private set; }
    public string Operation { get; private set; } = default!;
    public string? Parameters { get; private set; }
    public JobStatus Status { get; private set; } = JobStatus.Pending;
    public int Attempts { get; private set; }
    public int Progress { get; private set; }
    public string? Error { get; private set; }
    public Guid? ResultVersionId { get; private set; }
    public string? LeaseOwner { get; private set; }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastUpdated { get; private set; } = DateTimeOffset.UtcNow;

    private Job() { }

    public Job(Guid imageId, Guid userId, string operation, string? parameters)
    {
        ImageId = imageId;
        UserId = userId;
        Operation = operation;
        Parameters = parameters;
    }
    
    public void Complete(Guid resultVersionId)
    {
        Status = JobStatus.Completed;
        Progress = 100;
        ResultVersionId = resultVersionId;
        Error = null;
        LeaseOwner = null;
        LeaseUntil = null;
        LastUpdated = DateTimeOffset.UtcNow;
    }
}