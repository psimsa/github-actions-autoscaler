using AutoscalerApi.Models;
using Docker.DotNet.Models;

namespace AutoscalerApi.Services;

public interface IDockerService
{
    Task<bool> ProcessWorkflow(Workflow? workflow);
    Task<IList<ContainerListResponse>> GetAutoscalerContainersAsync();
}