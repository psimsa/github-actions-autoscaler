using GithubActionsAutoscaler.Abstractions.Runner;
using GithubActionsAutoscaler.Runner.Docker.Services;
using GithubActionsAutoscaler.Runner.Docker.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GithubActionsAutoscaler.Runner.Docker;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDockerRunnerProvider(
		this IServiceCollection services,
		DockerRunnerOptions options)
	{
		services.AddSingleton(options);
		services.AddSingleton<IImageManager, ImageManager>();
		services.AddSingleton<IContainerManager, ContainerManager>();
		services.AddSingleton<IRunnerManager, DockerRunnerManager>();
		services.AddSingleton<IValidateOptions<DockerRunnerOptions>, DockerRunnerOptionsValidator>();
		return services;
	}
}
