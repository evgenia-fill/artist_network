using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PixelFlow.Application.Abstractions;
using PixelFlow.Infrastructure.Imaging;
using PixelFlow.Infrastructure.Persistence;
using PixelFlow.Infrastructure.Storage;

namespace PixelFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IImageProcessor, ImageSharpProcessor>();
        services.AddSingleton<IFileStorage>(new LocalFileStorage(configuration["Storage:Path"] ?? "uploads"));
        services.AddDbContext<AppDbContext>(o => o
            .UseNpgsql(configuration.GetConnectionString("Default"))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IImageRepository, ImageRepository>();
        return services;
    }
}