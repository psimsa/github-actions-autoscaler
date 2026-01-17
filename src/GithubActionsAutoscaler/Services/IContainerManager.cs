using Docker.DotNet.Models;

namespace GithubActionsAutoscaler.Services;

public interface IContainerManager
{
    Task<IList<ContainerListResponse>> ListContainersAsync();
    Task<bool> CreateAndStartContainerAsync(
        string repositoryFullName,
        string containerName,
        long jobRunId,
        string image
    );
    Task StartCreatedContainersAsync(CancellationToken token);
    Task CleanupOldContainersAsync(CancellationToken token);
    Task PruneVolumesAsync(CancellationToken token);
}
