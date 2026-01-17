namespace GithubActionsAutoscaler.Services;

public interface ILabelMatcher
{
    bool HasAllRequiredLabels(string[] jobLabels);
}
