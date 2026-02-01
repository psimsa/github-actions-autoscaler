using GithubActionsAutoscaler.Abstractions.Services;

namespace GithubActionsAutoscaler.Tests.Unit.Services;

public class RepositoryFilterTests
{
    [Fact]
    public void IsRepositoryAllowed_WithMatchingAllowlistPrefix_ReturnsTrue()
    {
        var config = CreateConfiguration(repoAllowlistPrefix: "myorg/");
        var filter = CreateFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/my-repo");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsRepositoryAllowed_WithNonMatchingAllowlistPrefix_ReturnsFalse()
    {
        var config = CreateConfiguration(repoAllowlistPrefix: "myorg/");
        var filter = CreateFilter(config);

        var result = filter.IsRepositoryAllowed("otherorg/my-repo");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsRepositoryAllowed_WithExactAllowlistMatch_ReturnsTrue()
    {
        var config = CreateConfiguration(
            repoAllowlist: ["myorg/repo1", "myorg/repo2"],
            isRepoAllowlistExactMatch: true
        );
        var filter = CreateFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/repo1");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsRepositoryAllowed_WithExactAllowlistNoMatch_ReturnsFalse()
    {
        var config = CreateConfiguration(
            repoAllowlist: ["myorg/repo1", "myorg/repo2"],
            isRepoAllowlistExactMatch: true
        );
        var filter = CreateFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/repo3");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsRepositoryAllowed_WithPrefixAllowlistMatch_ReturnsTrue()
    {
        var config = CreateConfiguration(
            repoAllowlist: ["myorg/"],
            isRepoAllowlistExactMatch: false
        );
        var filter = CreateFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/any-repo");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsRepositoryAllowed_WithWildcardAllowlist_ReturnsTrue()
    {
        var config = CreateConfiguration(repoAllowlist: ["*"], isRepoAllowlistExactMatch: false);
        var filter = CreateFilter(config);

        var result = filter.IsRepositoryAllowed("anyorg/any-repo");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsRepositoryAllowed_WithDenylistPrefix_ReturnsFalse()
    {
        var config = CreateConfiguration(
            repoAllowlistPrefix: "myorg/",
            repoDenylistPrefix: "myorg/blocked-"
        );
        var filter = CreateFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/blocked-repo");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsRepositoryAllowed_DenylistTakesPrecedenceOverAllowlist()
    {
        var config = CreateConfiguration(
            repoAllowlist: ["myorg/blocked-repo"],
            repoDenylist: ["myorg/blocked-repo"],
            isRepoAllowlistExactMatch: true,
            isRepoDenylistExactMatch: true
        );
        var filter = CreateFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/blocked-repo");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsRepositoryAllowed_WithEmptyAllowlistAndNoPrefix_ReturnsFalse()
    {
        var config = CreateConfiguration();
        var filter = CreateFilter(config);

        var result = filter.IsRepositoryAllowed("anyorg/any-repo");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsRepositoryAllowed_WithPrefixDenylistMatch_ReturnsFalse()
    {
        var config = CreateConfiguration(
            repoAllowlistPrefix: "myorg/",
            repoDenylist: ["myorg/blocked"],
            isRepoDenylistExactMatch: false
        );
        var filter = CreateFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/blocked-repo");

        result.Should().BeFalse();
    }

    private static RepositoryFilterConfiguration CreateConfiguration(
        string repoAllowlistPrefix = "",
        string[] repoAllowlist = null!,
        bool isRepoAllowlistExactMatch = true,
        string repoDenylistPrefix = "",
        string[] repoDenylist = null!,
        bool isRepoDenylistExactMatch = false
    )
    {
        return new RepositoryFilterConfiguration(
            repoAllowlistPrefix,
            repoAllowlist ?? [],
            isRepoAllowlistExactMatch,
            repoDenylistPrefix,
            repoDenylist ?? [],
            isRepoDenylistExactMatch
        );
    }

    private static RepositoryFilter CreateFilter(RepositoryFilterConfiguration config)
    {
        return new RepositoryFilter(
            config.RepoAllowlistPrefix,
            config.RepoAllowlist,
            config.IsRepoAllowlistExactMatch,
            config.RepoDenylistPrefix,
            config.RepoDenylist,
            config.IsRepoDenylistExactMatch
        );
    }

    private sealed record RepositoryFilterConfiguration(
        string RepoAllowlistPrefix,
        string[] RepoAllowlist,
        bool IsRepoAllowlistExactMatch,
        string RepoDenylistPrefix,
        string[] RepoDenylist,
        bool IsRepoDenylistExactMatch
    );
}
