using Microsoft.Extensions.DependencyInjection;
using PixelFlow.Application.Abstractions;
using PixelFlow.Infrastructure.Imaging;
using PixelFlow.Infrastructure.Storage;

namespace PixelFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IImageProcessor, ImageSharpProcessor>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        return services;
    }
}