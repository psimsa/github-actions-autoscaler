using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Services;

namespace GithubActionsAutoscaler.Tests.Unit.Services;

public class RepositoryFilterTests
{
    [Fact]
    public void IsRepositoryAllowed_WithMatchingWhitelistPrefix_ReturnsTrue()
    {
        var config = CreateConfiguration(repoWhitelistPrefix: "myorg/");
        var filter = new RepositoryFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/my-repo");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsRepositoryAllowed_WithNonMatchingWhitelistPrefix_ReturnsFalse()
    {
        var config = CreateConfiguration(repoWhitelistPrefix: "myorg/");
        var filter = new RepositoryFilter(config);

        var result = filter.IsRepositoryAllowed("otherorg/my-repo");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsRepositoryAllowed_WithExactWhitelistMatch_ReturnsTrue()
    {
        var config = CreateConfiguration(
            repoWhitelist: ["myorg/repo1", "myorg/repo2"],
            isRepoWhitelistExactMatch: true
        );
        var filter = new RepositoryFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/repo1");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsRepositoryAllowed_WithExactWhitelistNoMatch_ReturnsFalse()
    {
        var config = CreateConfiguration(
            repoWhitelist: ["myorg/repo1", "myorg/repo2"],
            isRepoWhitelistExactMatch: true
        );
        var filter = new RepositoryFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/repo3");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsRepositoryAllowed_WithPrefixWhitelistMatch_ReturnsTrue()
    {
        var config = CreateConfiguration(
            repoWhitelist: ["myorg/"],
            isRepoWhitelistExactMatch: false
        );
        var filter = new RepositoryFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/any-repo");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsRepositoryAllowed_WithWildcardWhitelist_ReturnsTrue()
    {
        var config = CreateConfiguration(repoWhitelist: ["*"], isRepoWhitelistExactMatch: false);
        var filter = new RepositoryFilter(config);

        var result = filter.IsRepositoryAllowed("anyorg/any-repo");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsRepositoryAllowed_WithBlacklistPrefix_ReturnsFalse()
    {
        var config = CreateConfiguration(
            repoWhitelistPrefix: "myorg/",
            repoBlacklistPrefix: "myorg/blocked-"
        );
        var filter = new RepositoryFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/blocked-repo");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsRepositoryAllowed_BlacklistTakesPrecedenceOverWhitelist()
    {
        var config = CreateConfiguration(
            repoWhitelist: ["myorg/blocked-repo"],
            repoBlacklist: ["myorg/blocked-repo"],
            isRepoWhitelistExactMatch: true,
            isRepoBlacklistExactMatch: true
        );
        var filter = new RepositoryFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/blocked-repo");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsRepositoryAllowed_WithEmptyWhitelistAndNoPrefix_ReturnsFalse()
    {
        var config = CreateConfiguration();
        var filter = new RepositoryFilter(config);

        var result = filter.IsRepositoryAllowed("anyorg/any-repo");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsRepositoryAllowed_WithPrefixBlacklistMatch_ReturnsFalse()
    {
        var config = CreateConfiguration(
            repoWhitelistPrefix: "myorg/",
            repoBlacklist: ["myorg/blocked"],
            isRepoBlacklistExactMatch: false
        );
        var filter = new RepositoryFilter(config);

        var result = filter.IsRepositoryAllowed("myorg/blocked-repo");

        result.Should().BeFalse();
    }

    private static AppConfiguration CreateConfiguration(
        string repoWhitelistPrefix = "",
        string[] repoWhitelist = null!,
        bool isRepoWhitelistExactMatch = true,
        string repoBlacklistPrefix = "",
        string[] repoBlacklist = null!,
        bool isRepoBlacklistExactMatch = false
    )
    {
        return new AppConfiguration
        {
            RepoWhitelistPrefix = repoWhitelistPrefix,
            RepoWhitelist = repoWhitelist ?? [],
            IsRepoWhitelistExactMatch = isRepoWhitelistExactMatch,
            RepoBlacklistPrefix = repoBlacklistPrefix,
            RepoBlacklist = repoBlacklist ?? [],
            IsRepoBlacklistExactMatch = isRepoBlacklistExactMatch,
            DockerImage = "test-image:latest",
        };
    }
}
