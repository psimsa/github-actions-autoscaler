namespace GithubActionsAutoscaler.Abstractions.Runner;

public interface IRunnerManager
{
	Task<IReadOnlyList<IRunnerInstance>> GetRunnersAsync(CancellationToken cancellationToken = default);
	Task<int> GetRunnerCountAsync(CancellationToken cancellationToken = default);
	Task<int> GetMaxRunnersAsync();
	Task<bool> CanCreateRunnerAsync(CancellationToken cancellationToken = default);
	Task<IRunnerInstance?> CreateRunnerAsync(
		string repositoryFullName,
		string runnerName,
		long jobRunId,
		CancellationToken cancellationToken = default);
	Task StopRunnerAsync(string runnerId, CancellationToken cancellationToken = default);
	Task CleanupOldRunnersAsync(CancellationToken cancellationToken = default);
}
