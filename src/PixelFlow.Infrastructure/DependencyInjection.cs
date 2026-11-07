using Microsoft.Extensions.DependencyInjection;
using PixelFlow.Application.Abstractions;
using PixelFlow.Infrastructure.Imaging;
using PixelFlow.Infrastructure.Persistence;
using PixelFlow.Infrastructure.Storage;

namespace PixelFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IImageProcessor, ImageSharpProcessor>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IImageRepository, InMemoryImageRepository>(); 
        return services;
    }
}