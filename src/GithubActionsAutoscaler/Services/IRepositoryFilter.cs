namespace GithubActionsAutoscaler.Services;

public interface IRepositoryFilter
{
    bool IsRepositoryAllowed(string repositoryFullName);
}
