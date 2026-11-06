using Microsoft.Extensions.DependencyInjection;
using PixelFlow.Application.UseCases.Images;

namespace PixelFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RotateImageHandler>();
        services.AddScoped<FlipImageHandler>();
        services.AddScoped<ResetImageHandler>();
        services.AddScoped<RestoreVersionHandler>();
        services.AddScoped<GetVersionsHandler>();
        
        return services;
    }
}