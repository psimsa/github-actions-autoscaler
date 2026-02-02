namespace GithubActionsAutoscaler.Runner.Docker;

public class DockerRunnerOptions
{
	public int MaxRunners { get; set; }
	public string Image { get; set; } = "";
	public string DockerHost { get; set; } = "";
	public string RegistryToken { get; set; } = "";
	public string AccessToken { get; set; } = "";
	public bool AutoCheckForImageUpdates { get; set; }
	public string ToolCacheVolumeName { get; set; } = "";
	public string CoordinatorHostname { get; set; } = "";
	public string[] Labels { get; set; } = [];
}
