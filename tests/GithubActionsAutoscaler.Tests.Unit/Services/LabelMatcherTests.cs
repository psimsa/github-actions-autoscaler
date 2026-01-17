using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Services;

namespace GithubActionsAutoscaler.Tests.Unit.Services;

public class LabelMatcherTests
{
    [Fact]
    public void HasAllRequiredLabels_WithSelfHostedLabel_ReturnsTrue()
    {
        var config = CreateConfiguration(["self-hosted", "linux", "x64"]);
        var matcher = new LabelMatcher(config);

        var result = matcher.HasAllRequiredLabels(["self-hosted", "linux"]);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasAllRequiredLabels_WithoutSelfHostedLabel_ReturnsFalse()
    {
        var config = CreateConfiguration(["self-hosted", "linux", "x64"]);
        var matcher = new LabelMatcher(config);

        var result = matcher.HasAllRequiredLabels(["linux", "x64"]);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasAllRequiredLabels_WithMissingLabel_ReturnsFalse()
    {
        var config = CreateConfiguration(["self-hosted", "linux"]);
        var matcher = new LabelMatcher(config);

        var result = matcher.HasAllRequiredLabels(["self-hosted", "windows"]);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasAllRequiredLabels_IsCaseInsensitive()
    {
        var config = CreateConfiguration(["self-hosted", "linux", "x64"]);
        var matcher = new LabelMatcher(config);

        var result = matcher.HasAllRequiredLabels(["Self-Hosted", "LINUX"]);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasAllRequiredLabels_WithOnlySelfHosted_ReturnsTrue()
    {
        var config = CreateConfiguration(["self-hosted"]);
        var matcher = new LabelMatcher(config);

        var result = matcher.HasAllRequiredLabels(["self-hosted"]);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasAllRequiredLabels_WithEmptyJobLabels_ReturnsFalse()
    {
        var config = CreateConfiguration(["self-hosted", "linux"]);
        var matcher = new LabelMatcher(config);

        var result = matcher.HasAllRequiredLabels([]);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasAllRequiredLabels_WithSubsetOfLabels_ReturnsTrue()
    {
        var config = CreateConfiguration(["self-hosted", "linux", "x64", "gpu"]);
        var matcher = new LabelMatcher(config);

        var result = matcher.HasAllRequiredLabels(["self-hosted", "linux"]);

        result.Should().BeTrue();
    }

    private static AppConfiguration CreateConfiguration(string[] labels)
    {
        return new AppConfiguration { Labels = labels, DockerImage = "test-image:latest" };
    }
}
