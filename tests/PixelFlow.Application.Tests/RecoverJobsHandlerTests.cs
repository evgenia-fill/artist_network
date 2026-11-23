using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Models;
using PixelFlow.Application.UseCases.Jobs;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.Tests;

public sealed class RecoverJobsHandlerTests
{
    [Fact]
    public async Task Recover_PublishesPendingJobAgain()
    {
        var job = NewJob();
        var repository = new FakeRepository(job);
        var publisher = new FakePublisher();
        var handler = new RecoverJobsHandler(repository, publisher, new JobProcessingOptions());

        var count = await handler.HandleAsync();

        Assert.Equal(1, count);
        Assert.Equal(new[] { job.Id }, publisher.Created);
        Assert.Empty(publisher.Failed);
    }

    [Fact]
    public async Task Recover_RedispatchesExpiredProcessingJobBelowAttemptLimit()
    {
        var job = NewJob();
        Set(job, nameof(Job.Status), JobStatus.Processing);
        Set(job, nameof(Job.Attempts), 2);
        var repository = new FakeRepository(job);
        var publisher = new FakePublisher();
        var handler = new RecoverJobsHandler(repository, publisher, new JobProcessingOptions());

        await handler.HandleAsync();

        Assert.Equal(new[] { job.Id }, publisher.Created);
        Assert.Empty(repository.ExpiredFailed);
    }

    [Fact]
    public async Task Recover_MarksExpiredJobFailedAfterLastAttempt()
    {
        var job = NewJob();
        Set(job, nameof(Job.Status), JobStatus.Processing);
        Set(job, nameof(Job.Attempts), 3);
        var repository = new FakeRepository(job);
        var publisher = new FakePublisher();
        var handler = new RecoverJobsHandler(repository, publisher, new JobProcessingOptions());

        var count = await handler.HandleAsync();

        Assert.Equal(1, count);
        Assert.Equal(new[] { job.Id }, repository.ExpiredFailed);
        Assert.Equal(new[] { job.Id }, publisher.Failed);
        Assert.Empty(publisher.Created);
    }

    [Fact]
    public async Task Recover_FailsPendingJobWhenMaxAttemptsChanged()
    {
        var job = NewJob();
        Set(job, nameof(Job.Attempts), 3);
        var repository = new FakeRepository(job);
        var publisher = new FakePublisher();
        var handler = new RecoverJobsHandler(repository, publisher, new JobProcessingOptions());

        await handler.HandleAsync();

        Assert.Equal(new[] { job.Id }, repository.ExpiredFailed);
        Assert.Equal(new[] { job.Id }, publisher.Failed);
        Assert.Empty(publisher.Created);
    }

    [Fact]
    public async Task Recover_DoesNotPublishFailureWhenAnotherWorkerAlreadyFinished()
    {
        var job = NewJob();
        Set(job, nameof(Job.Status), JobStatus.Processing);
        Set(job, nameof(Job.Attempts), 3);
        var repository = new FakeRepository(job) { MarkExpiredResult = false };
        var publisher = new FakePublisher();
        var handler = new RecoverJobsHandler(repository, publisher, new JobProcessingOptions());

        var count = await handler.HandleAsync();

        Assert.Equal(0, count);
        Assert.Empty(publisher.Failed);
    }

    private static Job NewJob() => new(Guid.NewGuid(), Guid.NewGuid(), "template", null);

    private static void Set<T>(Job job, string name, T value) =>
        typeof(Job).GetProperty(name)!.SetValue(job, value);

    private sealed class FakeRepository(params Job[] jobs) : IJobRepository
    {
        public bool MarkExpiredResult { get; set; } = true;
        public List<Guid> ExpiredFailed { get; } = [];

        public Task<IReadOnlyList<Job>> FindRecoverableAsync(DateTimeOffset now,
            DateTimeOffset stalePendingBefore, int batchSize, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Job>>(jobs);

        public Task<bool> MarkExhaustedFailedAsync(Guid id, int maxAttempts, string error,
            DateTimeOffset now, CancellationToken ct = default)
        {
            ExpiredFailed.Add(id);
            return Task.FromResult(MarkExpiredResult);
        }

        public Task<Job?> GetByIdAsync(Guid jobId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task AddAsync(Job job, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<bool> TryAcquireAsync(Guid jobId, string owner, TimeSpan lease, int maxAttempts,
            CancellationToken ct = default) => throw new NotImplementedException();

        public Task<JobFailureOutcome> ResolveFailureAsync(Guid jobId, string owner, string error,
            int maxAttempts, DateTimeOffset retryAt, bool permanent, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakePublisher : IJobEventPublisher
    {
        public List<Guid> Created { get; } = [];
        public List<Guid> Failed { get; } = [];

        public Task PublishCreatedAsync(Job job, CancellationToken ct = default)
        {
            Created.Add(job.Id);
            return Task.CompletedTask;
        }

        public Task PublishFailedAsync(Job job, string error, CancellationToken ct = default)
        {
            Failed.Add(job.Id);
            return Task.CompletedTask;
        }

        public Task PublishCompletedAsync(Job job, Guid versionId, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }
}
