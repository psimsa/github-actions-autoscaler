using GithubActionsAutoscaler.Configuration;

namespace GithubActionsAutoscaler.Services;

public class LabelMatcher : ILabelMatcher
{
    private readonly string[] _labels;

    public LabelMatcher(AppConfiguration configuration)
    {
        _labels = configuration.Labels;
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
