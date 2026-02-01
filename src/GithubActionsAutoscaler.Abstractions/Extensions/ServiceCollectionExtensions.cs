using GithubActionsAutoscaler.Abstractions.Queue;
using GithubActionsAutoscaler.Abstractions.Queue.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace GithubActionsAutoscaler.Abstractions.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddInMemoryQueueProvider(
		this IServiceCollection services,
		QueueOptions options)
	{
		services.AddSingleton(options);
		services.AddSingleton<IQueueProvider, InMemoryQueueProvider>();
		return services;
	}
}
