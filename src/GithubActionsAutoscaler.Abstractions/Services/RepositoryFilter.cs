namespace GithubActionsAutoscaler.Abstractions.Services;

public class RepositoryFilter : IRepositoryFilter
{
    private readonly string _repoAllowlistPrefix;
    private readonly string[] _repoAllowlist;
    private readonly bool _isRepoAllowlistExactMatch;
    private readonly string _repoDenylistPrefix;
    private readonly string[] _repoDenylist;
    private readonly bool _isRepoDenylistExactMatch;

    public RepositoryFilter(
        string repoAllowlistPrefix,
        string[] repoAllowlist,
        bool isRepoAllowlistExactMatch,
        string repoDenylistPrefix,
        string[] repoDenylist,
        bool isRepoDenylistExactMatch
    )
    {
        _repoAllowlistPrefix = repoAllowlistPrefix;
        _repoAllowlist = repoAllowlist;
        _isRepoAllowlistExactMatch = isRepoAllowlistExactMatch;
        _repoDenylistPrefix = repoDenylistPrefix;
        _repoDenylist = repoDenylist;
        _isRepoDenylistExactMatch = isRepoDenylistExactMatch;
    }

    public bool IsRepositoryAllowed(string repositoryFullName)
    {
        if (IsRepoDenied(repositoryFullName))
        {
            return false;
        }

        return IsRepoInAllowlist(repositoryFullName);
    }

    private bool IsRepoDenied(string repoName)
    {
        if (string.IsNullOrWhiteSpace(_repoDenylistPrefix) && _repoDenylist.Length == 0)
        {
            return false;
        }

        if (
            !string.IsNullOrWhiteSpace(_repoDenylistPrefix)
            && repoName.StartsWith(_repoDenylistPrefix)
        )
        {
            return true;
        }

        if (_isRepoDenylistExactMatch)
        {
            return _repoDenylist.Contains(repoName);
        }

        return _repoDenylist.Any(repoName.StartsWith);
    }

    private bool IsRepoInAllowlist(string repositoryFullName)
    {
        if (
            !string.IsNullOrWhiteSpace(_repoAllowlistPrefix)
            && repositoryFullName.StartsWith(_repoAllowlistPrefix)
        )
        {
            return true;
        }

        if (_repoAllowlist.Length == 0)
        {
            return false;
        }

        if (_isRepoAllowlistExactMatch)
        {
            return _repoAllowlist.Contains(repositoryFullName);
        }

        return _repoAllowlist.Any(repo => repositoryFullName.StartsWith(repo) || repo.Equals("*"));
    }
}
