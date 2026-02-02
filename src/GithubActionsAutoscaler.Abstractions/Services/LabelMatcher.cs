namespace GithubActionsAutoscaler.Abstractions.Services;

public class LabelMatcher : ILabelMatcher
{
    private readonly string[] _labels;

    public LabelMatcher(string[] labels)
    {
        _labels = labels;
    }

    public bool HasAllRequiredLabels(string[] jobLabels)
    {
        var hasSelfHostedLabel = jobLabels.Any(l =>
            l.Equals("self-hosted", StringComparison.OrdinalIgnoreCase)
        );
        if (!hasSelfHostedLabel)
        {
            return false;
        }

        return jobLabels.All(l => _labels.Contains(l.ToLowerInvariant()));
    }
}
