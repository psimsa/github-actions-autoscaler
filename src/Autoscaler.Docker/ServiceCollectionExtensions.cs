using Autoscaler.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AutoscalerApi.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDockerService(this IServiceCollection services)
    {
        services.AddSingleton<IDockerService, DockerService>();
        return services;
    }
}
