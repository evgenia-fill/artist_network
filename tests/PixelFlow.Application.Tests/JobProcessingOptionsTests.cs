using PixelFlow.Application.Models;

namespace PixelFlow.Application.Tests;

public sealed class JobProcessingOptionsTests
{
    [Fact]
    public void RetryDelay_IncreasesExponentially()
    {
        var options = new JobProcessingOptions();

        Assert.Equal(TimeSpan.FromSeconds(5), options.RetryDelay(1));
        Assert.Equal(TimeSpan.FromSeconds(10), options.RetryDelay(2));
        Assert.Equal(TimeSpan.FromSeconds(20), options.RetryDelay(3));
    }
}
