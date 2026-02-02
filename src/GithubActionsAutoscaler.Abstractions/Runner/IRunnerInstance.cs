namespace GithubActionsAutoscaler.Abstractions.Runner;

public interface IRunnerInstance
{
	string Id { get; }
	string Name { get; }
	string Repository { get; }
	long JobRunId { get; }
	RunnerStatus Status { get; }
	DateTimeOffset CreatedAt { get; }
}
