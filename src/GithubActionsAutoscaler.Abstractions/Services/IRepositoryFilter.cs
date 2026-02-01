namespace GithubActionsAutoscaler.Abstractions.Services;

public interface IRepositoryFilter
{
    bool IsRepositoryAllowed(string repositoryFullName);
}
