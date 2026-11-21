using Microsoft.Extensions.DependencyInjection;
using PixelFlow.Application.UseCases.Images;
using PixelFlow.Application.UseCases.Jobs;

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
        services.AddScoped<UploadImageHandler>();
        services.AddScoped<GetImageFileHandler>();
        services.AddScoped<ApplyImageTemplateHandler>();
        services.AddScoped<CreateJobHandler>();
        services.AddScoped<GetJobHandler>();
        
        return services;
    }
}