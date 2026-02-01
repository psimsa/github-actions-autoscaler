namespace GithubActionsAutoscaler.Configuration;

public class OpenTelemetryOptions
{
	public bool Enabled { get; set; }
	public string ServiceName { get; set; } = "";
	public string? OtlpEndpoint { get; set; }
}
