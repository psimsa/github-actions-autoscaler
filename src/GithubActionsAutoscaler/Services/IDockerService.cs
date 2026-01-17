using Docker.DotNet.Models;
using GithubActionsAutoscaler.Models;

namespace GithubActionsAutoscaler.Services;

public interface IDockerService
{
    Task<bool> ProcessWorkflowAsync(Workflow? workflow);
    Task<IList<ContainerListResponse>> GetAutoscalerContainersAsync();
    Task WaitForAvailableRunnerAsync();
}
