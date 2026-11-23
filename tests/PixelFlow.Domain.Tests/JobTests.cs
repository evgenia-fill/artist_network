using PixelFlow.Domain.Entities;

namespace PixelFlow.Domain.Tests;

public class JobTests
{
    [Fact]
    public void Complete_SetsStatusProgressAndClearsLease()
    {
        var job = new Job(Guid.NewGuid(), Guid.NewGuid(), "template", null);
        var versionId = Guid.NewGuid();

        job.Complete(versionId);

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.Equal(100, job.Progress);
        Assert.Equal(versionId, job.ResultVersionId);
        Assert.Null(job.LeaseUntil);
    }
}