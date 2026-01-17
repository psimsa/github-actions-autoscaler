using Docker.DotNet;
using Docker.DotNet.Models;
using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Models;

namespace GithubActionsAutoscaler.Services;

public class DockerService : IDockerService
{
    private readonly IContainerManager _containerManager;
    private readonly ILogger<DockerService> _logger;
    private readonly IRepositoryFilter _repositoryFilter;
    private readonly ILabelMatcher _labelMatcher;
    private readonly int _maxRunners;
    private int _totalCount = 0;
    private readonly string _dockerImage;
    private readonly string _coordinatorHostname;

    private Task _containerGuardTask;

    public DockerService(
        IContainerManager containerManager,
        AppConfiguration configuration,
        IRepositoryFilter repositoryFilter,
        ILabelMatcher labelMatcher,
        ILogger<DockerService> logger
    )
    {
        _containerManager = containerManager;
        _logger = logger;
        _repositoryFilter = repositoryFilter;
        _labelMatcher = labelMatcher;
        var maxRunners = configuration.MaxRunners;
        _maxRunners = maxRunners > 0 ? maxRunners : 3;
        _containerGuardTask = ContainerGuardAsync(CancellationToken.None);
        _dockerImage = configuration.DockerImage;
        _coordinatorHostname = configuration.CoordinatorHostname;
    }

    public async Task<IList<ContainerListResponse>> GetAutoscalerContainersAsync()
    {
        return await _containerManager.ListContainersAsync();
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
                    "Workflow '{Workflow}' is self-hosted and repository {Repository} whitelisted, starting container",
                    workflow.Job.Name,
                    workflow.Repository.FullName
                );
                Interlocked.Increment(ref _totalCount);
                var containerName =
                    $"{_coordinatorHostname}-{workflow.Repository.Name}-{workflow.Job.RunId}-{_totalCount}";
                return await _containerManager.CreateAndStartContainerAsync(
                    workflow.Repository.FullName,
                    containerName,
                    workflow.Job.RunId,
                    _dockerImage
                );
            case "completed":
                await _containerManager.PruneVolumesAsync(CancellationToken.None);
                break;
            case null:
                break;
        }

        return true;
    }

    public async Task WaitForAvailableRunnerAsync()
    {
        while ((await _containerManager.ListContainersAsync()).Count >= _maxRunners)
            await Task.Delay(3_000);
    }

    private async Task ContainerGuardAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await _containerManager.StartCreatedContainersAsync(token);
            await _containerManager.CleanupOldContainersAsync(token);
            await Task.Delay(TimeSpan.FromMinutes(2), token);
        }
    }
}
