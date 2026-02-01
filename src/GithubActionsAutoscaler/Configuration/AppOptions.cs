namespace GithubActionsAutoscaler.Configuration;

public class AppOptions
{
	public OperationMode Mode { get; set; }
	public string GithubToken { get; set; } = "";
	public string CoordinatorHostname { get; set; } = "";
	public RepositoryFilterOptions RepositoryFilter { get; set; } = new();
	public string[] Labels { get; set; } = [];
	public OpenTelemetryOptions OpenTelemetry { get; set; } = new();
}
