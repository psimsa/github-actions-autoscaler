using Autoscaler.Domain;
using Docker.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoscalerApi.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDockerService(this IServiceCollection services, AppConfiguration configuration)
    {
        services.AddSingleton<IDockerService, DockerService>();

        var dockerConfig = !string.IsNullOrWhiteSpace(configuration.DockerHost)
            ? new DockerClientConfiguration(new Uri(configuration.DockerHost))
            : new DockerClientConfiguration();

        services.AddSingleton(_ => dockerConfig.CreateClient());

        return services;
    }
}
