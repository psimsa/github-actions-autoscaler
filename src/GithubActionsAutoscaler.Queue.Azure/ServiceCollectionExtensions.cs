using GithubActionsAutoscaler.Abstractions.Queue;
using GithubActionsAutoscaler.Queue.Azure.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GithubActionsAutoscaler.Queue.Azure;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddAzureQueueProvider(
		this IServiceCollection services,
		AzureQueueOptions options)
	{
		services.AddSingleton(options);
		services.AddSingleton<IQueueProvider, AzureQueueProvider>();
		services.AddSingleton<IValidateOptions<AzureQueueOptions>, AzureQueueOptionsValidator>();
		return services;
	}
}
