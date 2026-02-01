using System.Diagnostics;
using GithubActionsAutoscaler.Abstractions.Models;
using GithubActionsAutoscaler.Abstractions.Runner;
using GithubActionsAutoscaler.Abstractions.Services;
using GithubActionsAutoscaler.Telemetry;

namespace GithubActionsAutoscaler.Services;

public class WorkflowProcessor : IWorkflowProcessor
{
	private readonly IRunnerManager _runnerManager;
	private readonly IRepositoryFilter _repositoryFilter;
	private readonly ILabelMatcher _labelMatcher;
	private readonly ILogger<WorkflowProcessor> _logger;
	private readonly AutoscalerMetrics? _metrics;

	public WorkflowProcessor(
		IRunnerManager runnerManager,
		IRepositoryFilter repositoryFilter,
		ILabelMatcher labelMatcher,
		ILogger<WorkflowProcessor> logger,
		AutoscalerMetrics? metrics = null)
	{
		_runnerManager = runnerManager;
		_repositoryFilter = repositoryFilter;
		_labelMatcher = labelMatcher;
		_logger = logger;
		_metrics = metrics;
	}

	public async Task<bool> ProcessWorkflowAsync(Workflow? workflow)
	{
		switch (workflow?.Action)
		{
		case "queued" when workflow.Job.Labels.All(l => l != "self-hosted"):
			Activity.Current?.AddEvent(
				new ActivityEvent("Removing non-selfhosted job from queue")
			);
			return true;
		case "queued" when !_labelMatcher.HasAllRequiredLabels(workflow.Job.Labels):
			Activity.Current?.AddEvent(
				new ActivityEvent("Job missing required labels, returning to queue")
			);
			return false;
		case "queued" when _repositoryFilter.IsRepositoryAllowed(workflow.Repository.FullName):
			Activity.Current?.AddEvent(
				new ActivityEvent("Starting runner for workflow")
			);
			await _runnerManager.WaitForAvailableSlotAsync();
			var created = await _runnerManager.CreateRunnerAsync(
				workflow.Repository.FullName,
				string.Empty,
				workflow.Job.RunId
			);
			if (created != null)
			{
				_metrics?.RecordJobStarted(workflow.Repository.FullName);
			}
			return created != null;
		case "completed":
			await _runnerManager.CleanupOldRunnersAsync();
			_metrics?.RecordJobCompleted(workflow.Repository.FullName);
			break;
			case null:
				break;
		}

		return true;
	}
}
