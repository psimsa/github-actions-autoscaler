using GithubActionsAutoscaler.Abstractions.Models;

namespace GithubActionsAutoscaler.Services;

public interface IWorkflowProcessor
{
	Task<bool> ProcessWorkflowAsync(Workflow? workflow);
}
