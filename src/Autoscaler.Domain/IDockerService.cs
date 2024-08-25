using Autoscaler.Domain.Models;

namespace Autoscaler.Domain;

public interface IDockerService
{
    Task<bool> ProcessWorkflow(Workflow? workflow);
    Task WaitForAvailableRunner();
}