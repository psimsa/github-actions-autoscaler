namespace GithubActionsAutoscaler.Abstractions.Runner;

public enum RunnerStatus
{
	Creating,
	Starting,
	Running,
	Stopping,
	Stopped,
	Failed
}
