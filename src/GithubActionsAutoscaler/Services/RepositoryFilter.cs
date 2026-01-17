using GithubActionsAutoscaler.Configuration;

namespace GithubActionsAutoscaler.Services;

public class RepositoryFilter : IRepositoryFilter
{
    private readonly string _repoWhitelistPrefix;
    private readonly string[] _repoWhitelist;
    private readonly bool _isRepoWhitelistExactMatch;
    private readonly string _repoBlacklistPrefix;
    private readonly string[] _repoBlacklist;
    private readonly bool _isRepoBlacklistExactMatch;

    public RepositoryFilter(AppConfiguration configuration)
    {
        _repoWhitelistPrefix = configuration.RepoWhitelistPrefix;
        _repoWhitelist = configuration.RepoWhitelist;
        _isRepoWhitelistExactMatch = configuration.IsRepoWhitelistExactMatch;
        _repoBlacklistPrefix = configuration.RepoBlacklistPrefix;
        _repoBlacklist = configuration.RepoBlacklist;
        _isRepoBlacklistExactMatch = configuration.IsRepoBlacklistExactMatch;
    }

    public bool IsRepositoryAllowed(string repositoryFullName)
    {
        if (IsRepoBlacklisted(repositoryFullName))
        {
            return false;
        }

        return IsRepoWhitelisted(repositoryFullName);
    }

    private bool IsRepoBlacklisted(string repoName)
    {
        if (string.IsNullOrWhiteSpace(_repoBlacklistPrefix) && _repoBlacklist.Length == 0)
        {
            return false;
        }

        if (
            !string.IsNullOrWhiteSpace(_repoBlacklistPrefix)
            && repoName.StartsWith(_repoBlacklistPrefix)
        )
        {
            return true;
        }

        if (_isRepoBlacklistExactMatch)
        {
            return _repoBlacklist.Contains(repoName);
        }

        return _repoBlacklist.Any(repoName.StartsWith);
    }

    private bool IsRepoWhitelisted(string repositoryFullName)
    {
        if (
            !string.IsNullOrWhiteSpace(_repoWhitelistPrefix)
            && repositoryFullName.StartsWith(_repoWhitelistPrefix)
        )
        {
            return true;
        }

        if (_repoWhitelist.Length == 0)
        {
            return false;
        }

        if (_isRepoWhitelistExactMatch)
        {
            return _repoWhitelist.Contains(repositoryFullName);
        }

        return _repoWhitelist.Any(repo => repositoryFullName.StartsWith(repo) || repo.Equals("*"));
    }
}
