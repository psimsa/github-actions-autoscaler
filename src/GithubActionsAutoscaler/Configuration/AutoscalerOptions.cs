namespace GithubActionsAutoscaler.Configuration;

public sealed record AutoscalerOptions(
	AppOptions AppOptions,
	QueueOptions QueueOptions,
	RunnerOptions RunnerOptions
);
