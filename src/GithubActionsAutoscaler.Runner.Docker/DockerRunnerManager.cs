using GithubActionsAutoscaler.Abstractions.Runner;
using GithubActionsAutoscaler.Abstractions.Services;
using GithubActionsAutoscaler.Runner.Docker.Services;
using GithubActionsAutoscaler.Abstractions.Telemetry;

namespace GithubActionsAutoscaler.Runner.Docker;

public class DockerRunnerManager : IRunnerManager
{
	private readonly IContainerManager _containerManager;
	private readonly IRepositoryFilter _repositoryFilter;
	private readonly ILabelMatcher _labelMatcher;
	private readonly DockerRunnerOptions _options;
	private readonly string _coordinatorHostname;
	private readonly AutoscalerMetrics? _metrics;
	private int _totalCount = 0;

	public DockerRunnerManager(
		IContainerManager containerManager,
		IRepositoryFilter repositoryFilter,
		ILabelMatcher labelMatcher,
		DockerRunnerOptions options,
		AutoscalerMetrics? metrics = null)
	{
		_containerManager = containerManager;
		_repositoryFilter = repositoryFilter;
		_labelMatcher = labelMatcher;
		_options = options;
		_coordinatorHostname = options.CoordinatorHostname;
		_metrics = metrics;
	}

	public async Task<IReadOnlyList<IRunnerInstance>> GetRunnersAsync(
		CancellationToken cancellationToken = default)
	{
		var containers = await _containerManager.ListContainersAsync();
		_metrics?.UpdateActiveRunners(containers.Count);
		var runners = containers
			.Select(container =>
			{
				var repository = container.Labels.TryGetValue("autoscaler.repository", out var repo)
					? repo
					: string.Empty;
				var runId = container.Labels.TryGetValue("autoscaler.jobrun", out var run)
					&& long.TryParse(run, out var parsed)
					? parsed
					: 0;
				var createdAt = container.Created.ToUniversalTime();
				return (IRunnerInstance)
					new DockerRunnerInstance(
						container.ID,
						container.Names.FirstOrDefault() ?? string.Empty,
						repository,
						runId,
						RunnerStatus.Running,
						createdAt
					);
			})
			.ToList();

		return runners;
	}

	public async Task<int> GetRunnerCountAsync(CancellationToken cancellationToken = default)
	{
		var count = (await _containerManager.ListContainersAsync()).Count;
		_metrics?.UpdateActiveRunners(count);
		return count;
	}

	public Task<int> GetMaxRunnersAsync()
	{
		return Task.FromResult(_options.MaxRunners > 0 ? _options.MaxRunners : 3);
	}

	public async Task<bool> CanCreateRunnerAsync(CancellationToken cancellationToken = default)
	{
		var count = (await _containerManager.ListContainersAsync()).Count;
		_metrics?.UpdateActiveRunners(count);
		return count < await GetMaxRunnersAsync();
	}

	public async Task<IRunnerInstance?> CreateRunnerAsync(
		string repositoryFullName,
		string runnerName,
		long jobRunId,
		CancellationToken cancellationToken = default)
	{
		if (!_repositoryFilter.IsRepositoryAllowed(repositoryFullName))
		{
			return null;
		}

		if (!_labelMatcher.HasAllRequiredLabels(_options.Labels))
		{
			return null;
		}

		if (string.IsNullOrWhiteSpace(runnerName))
		{
			Interlocked.Increment(ref _totalCount);
			runnerName = $"{_coordinatorHostname}-{repositoryFullName}-{jobRunId}-{_totalCount}";
		}

		var created = await _containerManager.CreateAndStartContainerAsync(
			repositoryFullName,
			runnerName,
			jobRunId,
			_options.Image
		);
		if (!created)
		{
			return null;
		}

		return new DockerRunnerInstance(
			runnerName,
			runnerName,
			repositoryFullName,
			jobRunId,
			RunnerStatus.Running,
			DateTimeOffset.UtcNow
		);
	}

	public Task StopRunnerAsync(string runnerId, CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	public Task CleanupOldRunnersAsync(CancellationToken cancellationToken = default)
	{
		return _containerManager.CleanupOldContainersAsync(cancellationToken);
	}

	public async Task WaitForAvailableSlotAsync(CancellationToken cancellationToken = default)
	{
		while ((await _containerManager.ListContainersAsync()).Count >= await GetMaxRunnersAsync())
		{
			_metrics?.UpdateActiveRunners((await _containerManager.ListContainersAsync()).Count);
			await Task.Delay(3_000, cancellationToken);
		}
	}
}
