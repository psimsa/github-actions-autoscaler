namespace GithubActionsAutoscaler.Runner.Docker.Services;

public sealed record RunnerContainer(string Id, long Created, string Status);
