using GithubActionsAutoscaler.Abstractions.Models;
using GithubActionsAutoscaler.Abstractions.Runner;
using GithubActionsAutoscaler.Abstractions.Services;

namespace GithubActionsAutoscaler.Services;

public class WorkflowProcessor : IWorkflowProcessor
{
	private readonly IRunnerManager _runnerManager;
	private readonly IRepositoryFilter _repositoryFilter;
	private readonly ILabelMatcher _labelMatcher;
	private readonly ILogger<WorkflowProcessor> _logger;

	public WorkflowProcessor(
		IRunnerManager runnerManager,
		IRepositoryFilter repositoryFilter,
		ILabelMatcher labelMatcher,
		ILogger<WorkflowProcessor> logger)
	{
		_runnerManager = runnerManager;
		_repositoryFilter = repositoryFilter;
		_labelMatcher = labelMatcher;
		_logger = logger;
	}

	public async Task<bool> ProcessWorkflowAsync(Workflow? workflow)
	{
		switch (workflow?.Action)
		{
			case "queued" when workflow.Job.Labels.All(l => l != "self-hosted"):
				_logger.LogInformation(
					"Removing non-selfhosted job {jobName} from queue",
					workflow.Job.Name
				);
				return true;
			case "queued" when !_labelMatcher.HasAllRequiredLabels(workflow.Job.Labels):
				_logger.LogInformation(
					"Job {jobName} does not have all necessary labels, returning to queue",
					workflow.Job.Name
				);
				return false;
			case "queued" when _repositoryFilter.IsRepositoryAllowed(workflow.Repository.FullName):
				_logger.LogInformation(
					"Workflow '{Workflow}' is self-hosted and repository {Repository} allowed, starting runner",
					workflow.Job.Name,
					workflow.Repository.FullName
				);
				await _runnerManager.WaitForAvailableSlotAsync();
				var created = await _runnerManager.CreateRunnerAsync(
					workflow.Repository.FullName,
					string.Empty,
					workflow.Job.RunId
				);
				return created != null;
			case "completed":
				await _runnerManager.CleanupOldRunnersAsync();
				break;
			case null:
				break;
		}

		return true;
	}
}
