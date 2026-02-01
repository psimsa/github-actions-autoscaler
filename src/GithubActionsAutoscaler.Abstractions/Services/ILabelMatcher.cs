namespace GithubActionsAutoscaler.Abstractions.Services;

public interface ILabelMatcher
{
    bool HasAllRequiredLabels(string[] jobLabels);
}
