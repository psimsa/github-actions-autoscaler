using GithubActionsAutoscaler.Abstractions.Services;

namespace GithubActionsAutoscaler.Tests.Unit.Services;

public class LabelMatcherTests
{
    [Fact]
    public void HasAllRequiredLabels_WithSelfHostedLabel_ReturnsTrue()
    {
        var labels = CreateConfiguration(["self-hosted", "linux", "x64"]);
        var matcher = new LabelMatcher(labels);

        var result = matcher.HasAllRequiredLabels(["self-hosted", "linux"]);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasAllRequiredLabels_WithoutSelfHostedLabel_ReturnsFalse()
    {
        var labels = CreateConfiguration(["self-hosted", "linux", "x64"]);
        var matcher = new LabelMatcher(labels);

        var result = matcher.HasAllRequiredLabels(["linux", "x64"]);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasAllRequiredLabels_WithMissingLabel_ReturnsFalse()
    {
        var labels = CreateConfiguration(["self-hosted", "linux"]);
        var matcher = new LabelMatcher(labels);

        var result = matcher.HasAllRequiredLabels(["self-hosted", "windows"]);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasAllRequiredLabels_IsCaseInsensitive()
    {
        var labels = CreateConfiguration(["self-hosted", "linux", "x64"]);
        var matcher = new LabelMatcher(labels);

        var result = matcher.HasAllRequiredLabels(["Self-Hosted", "LINUX"]);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasAllRequiredLabels_WithOnlySelfHosted_ReturnsTrue()
    {
        var labels = CreateConfiguration(["self-hosted"]);
        var matcher = new LabelMatcher(labels);

        var result = matcher.HasAllRequiredLabels(["self-hosted"]);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasAllRequiredLabels_WithEmptyJobLabels_ReturnsFalse()
    {
        var labels = CreateConfiguration(["self-hosted", "linux"]);
        var matcher = new LabelMatcher(labels);

        var result = matcher.HasAllRequiredLabels([]);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasAllRequiredLabels_WithSubsetOfLabels_ReturnsTrue()
    {
        var labels = CreateConfiguration(["self-hosted", "linux", "x64", "gpu"]);
        var matcher = new LabelMatcher(labels);

        var result = matcher.HasAllRequiredLabels(["self-hosted", "linux"]);

        result.Should().BeTrue();
    }

    private static string[] CreateConfiguration(string[] labels)
    {
        return labels;
    }
}
