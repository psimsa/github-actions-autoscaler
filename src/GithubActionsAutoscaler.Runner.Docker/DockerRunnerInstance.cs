using GithubActionsAutoscaler.Abstractions.Runner;

namespace GithubActionsAutoscaler.Runner.Docker;

public sealed record DockerRunnerInstance(
	string Id,
	string Name,
	string Repository,
	long JobRunId,
	RunnerStatus Status,
	DateTimeOffset CreatedAt
) : IRunnerInstance;
