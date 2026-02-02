namespace GithubActionsAutoscaler.Configuration;

public class RunnerOptions
{
	public string Provider { get; set; } = "";
	public int MaxRunners { get; set; }
	public DockerRunnerOptions Docker { get; set; } = new();
}

public class DockerRunnerOptions
{
	public string Host { get; set; } = "";
	public string Image { get; set; } = "";
	public string RegistryToken { get; set; } = "";
	public bool AutoCheckForImageUpdates { get; set; }
	public string ToolCacheVolumeName { get; set; } = "";
}
