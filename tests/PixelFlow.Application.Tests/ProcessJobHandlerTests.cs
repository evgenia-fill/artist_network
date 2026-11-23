using PixelFlow.Application.Abstractions;
using PixelFlow.Application.Models;
using PixelFlow.Application.UseCases.Jobs;
using PixelFlow.Domain.Entities;

namespace PixelFlow.Application.Tests;

public sealed class ProcessJobHandlerTests
{
    [Fact]
    public async Task ProcessingFailure_SchedulesRetryWithoutFinalFailureEvent()
    {
        var image = NewImage();
        var job = NewJob(image);
        var jobs = new FakeJobs(job) { Outcome = JobFailureOutcome.RetryScheduled };
        var publisher = new FakePublisher();
        var handler = new ProcessJobHandler(jobs, new FakeImages(image), new FakeStorage(),
            new FailingProcessor(), publisher, new JobProcessingOptions());

        var before = DateTimeOffset.UtcNow;
        var result = await handler.HandleAsync(job.Id, "worker-a");

        Assert.Equal(JobProcessingResult.RetryScheduled, result);
        Assert.False(jobs.Permanent);
        Assert.Equal(3, jobs.MaxAttempts);
        Assert.True(jobs.RetryAt >= before.AddSeconds(4));
        Assert.Empty(publisher.Failed);
        Assert.Equal(0, jobs.SaveCount);
    }

    [Fact]
    public async Task ProcessingFailure_PublishesFailureOnlyAfterFinalAttempt()
    {
        var image = NewImage();
        var job = NewJob(image);
        var jobs = new FakeJobs(job) { Outcome = JobFailureOutcome.Failed };
        var publisher = new FakePublisher();
        var handler = new ProcessJobHandler(jobs, new FakeImages(image), new FakeStorage(),
            new FailingProcessor(), publisher, new JobProcessingOptions());

        var result = await handler.HandleAsync(job.Id, "worker-a");

        Assert.Equal(JobProcessingResult.Failed, result);
        Assert.Equal(new[] { job.Id }, publisher.Failed);
    }

    [Fact]
    public async Task StaleWorker_DoesNotPublishFailureAfterLeaseWasLost()
    {
        var image = NewImage();
        var job = NewJob(image);
        var jobs = new FakeJobs(job) { Outcome = JobFailureOutcome.Ignored };
        var publisher = new FakePublisher();
        var handler = new ProcessJobHandler(jobs, new FakeImages(image), new FakeStorage(),
            new FailingProcessor(), publisher, new JobProcessingOptions());

        var result = await handler.HandleAsync(job.Id, "worker-a");

        Assert.Equal(JobProcessingResult.Skipped, result);
        Assert.Empty(publisher.Failed);
    }

    [Fact]
    public async Task SuccessfulProcessing_SavesVersionAndPublishesCompleted()
    {
        var image = NewImage();
        var job = NewJob(image);
        var jobs = new FakeJobs(job);
        var publisher = new FakePublisher();
        var handler = new ProcessJobHandler(jobs, new FakeImages(image), new FakeStorage(),
            new SuccessfulProcessor(), publisher, new JobProcessingOptions());

        var result = await handler.HandleAsync(job.Id, "worker-a");

        Assert.Equal(JobProcessingResult.Completed, result);
        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.Equal(2, image.Versions.Count);
        Assert.Equal(1, jobs.SaveCount);
        Assert.Equal(new[] { job.Id }, publisher.Completed);
    }

    [Fact]
    public async Task MissingImage_FailsWithoutRetry()
    {
        var image = NewImage();
        var job = NewJob(image);
        var jobs = new FakeJobs(job) { Outcome = JobFailureOutcome.Failed };
        var publisher = new FakePublisher();
        var handler = new ProcessJobHandler(jobs, new MissingImages(), new FakeStorage(),
            new FailingProcessor(), publisher, new JobProcessingOptions());

        var result = await handler.HandleAsync(job.Id, "worker-a");

        Assert.Equal(JobProcessingResult.Failed, result);
        Assert.True(jobs.Permanent);
        Assert.Equal(new[] { job.Id }, publisher.Failed);
    }

    private static Image NewImage()
    {
        var image = new Image(Guid.NewGuid());
        image.AddVersion("original", null, "source.jpg", "image/jpeg", 5, 100, 100);
        return image;
    }

    private static Job NewJob(Image image) => new(image.Id, image.OwnerId, "template",
        "{\"steps\":[{\"type\":\"rotate\",\"degrees\":90}]}");

    private sealed class FakeJobs(Job job) : IJobRepository
    {
        public JobFailureOutcome Outcome { get; set; }
        public bool Permanent { get; private set; }
        public int MaxAttempts { get; private set; }
        public DateTimeOffset RetryAt { get; private set; }
        public int SaveCount { get; private set; }

        public Task<bool> TryAcquireAsync(Guid jobId, string owner, TimeSpan lease, int maxAttempts,
            CancellationToken ct = default) => Task.FromResult(true);

        public Task<Job?> GetByIdAsync(Guid jobId, CancellationToken ct = default) =>
            Task.FromResult<Job?>(job);

        public Task<JobFailureOutcome> ResolveFailureAsync(Guid jobId, string owner, string error,
            int maxAttempts, DateTimeOffset retryAt, bool permanent, CancellationToken ct = default)
        {
            Permanent = permanent;
            MaxAttempts = maxAttempts;
            RetryAt = retryAt;
            return Task.FromResult(Outcome);
        }

        public Task AddAsync(Job value, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Job>> FindRecoverableAsync(DateTimeOffset now,
            DateTimeOffset stalePendingBefore, int batchSize, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<bool> MarkExhaustedFailedAsync(Guid jobId, int maxAttempts, string error,
            DateTimeOffset now, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeImages(Image image) : IImageRepository
    {
        public Task<Image?> GetByIdAsync(Guid imageId, CancellationToken ct = default) =>
            Task.FromResult<Image?>(image);

        public Task<IReadOnlyList<Image>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task AddAsync(Image value, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeStorage : IFileStorage
    {
        public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default) =>
            Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));

        public Task<string> SaveAsync(byte[] content, string extension, CancellationToken ct = default) =>
            Task.FromResult("output.jpg");
    }

    private sealed class FailingProcessor : IImageProcessor
    {
        public Task<ProcessedImage> ApplyTemplateAsync(Stream input,
            IReadOnlyList<ImageEditStep> steps, CancellationToken ct = default) =>
            throw new IOException("Не удалось обработать файл");

        public Task<ImageInfo> ReadInfoAsync(Stream input, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<ProcessedImage> RotateAsync(Stream input, RotationAngle angle,
            CancellationToken ct = default) => throw new NotImplementedException();

        public Task<ProcessedImage> FlipAsync(Stream input, FlipAxis axis,
            CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class MissingImages : IImageRepository
    {
        public Task<Image?> GetByIdAsync(Guid imageId, CancellationToken ct = default) =>
            Task.FromResult<Image?>(null);

        public Task<IReadOnlyList<Image>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task AddAsync(Image value, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    private sealed class SuccessfulProcessor : IImageProcessor
    {
        public Task<ProcessedImage> ApplyTemplateAsync(Stream input,
            IReadOnlyList<ImageEditStep> steps, CancellationToken ct = default) =>
            Task.FromResult(new ProcessedImage([1, 2, 3], "image/jpeg", ".jpg", 100, 100));

        public Task<ImageInfo> ReadInfoAsync(Stream input, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<ProcessedImage> RotateAsync(Stream input, RotationAngle angle,
            CancellationToken ct = default) => throw new NotImplementedException();

        public Task<ProcessedImage> FlipAsync(Stream input, FlipAxis axis,
            CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakePublisher : IJobEventPublisher
    {
        public List<Guid> Failed { get; } = [];
        public List<Guid> Completed { get; } = [];

        public Task PublishFailedAsync(Job job, string error, CancellationToken ct = default)
        {
            Failed.Add(job.Id);
            return Task.CompletedTask;
        }

        public Task PublishCompletedAsync(Job job, Guid versionId, CancellationToken ct = default)
        {
            Completed.Add(job.Id);
            return Task.CompletedTask;
        }

        public Task PublishCreatedAsync(Job job, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }
}
