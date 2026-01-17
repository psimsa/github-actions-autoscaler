using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace GithubActionsAutoscaler.Configuration;

public class AppConfiguration
{
	public bool UseWebEndpoint { get; set; }
	public string AzureStorage { get; set; } = "";
	public string AzureStorageQueue { get; set; } = "";
	public string DockerToken { get; set; } = "";
	public string DockerImage { get; set; } = "myoung34/github-runner:latest";
	public string GithubToken { get; set; } = "";
	public int MaxRunners { get; set; }
	public string RepoAllowlistPrefix { get; set; } = "";
	public string[] RepoAllowlist { get; set; } = [];
	public bool IsRepoAllowlistExactMatch { get; set; }
	public string RepoDenylistPrefix { get; set; } = "";
	public string[] RepoDenylist { get; set; } = [];
	public bool IsRepoDenylistExactMatch { get; set; }
	public string DockerHost { get; set; } = "";
	public string[] Labels { get; set; } = [];
	public OpenTelemetryConfiguration OpenTelemetry { get; set; } = new();
	public bool AutoCheckForImageUpdates { get; set; }
	public string CoordinatorHostname { get; set; } = Environment.MachineName;

	[UnconditionalSuppressMessage(
		"Trimming",
		"IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
		Justification = "<Pending>"
	)]
	public static AppConfiguration FromConfiguration(IConfiguration configuration)
	{
		return new AppConfiguration()
		{
			AzureStorageQueue = configuration.GetValue<string>("AzureStorageQueue") ?? "",
			AzureStorage = configuration.GetValue<string>("AzureStorage") ?? "",
			UseWebEndpoint = configuration.GetValue<bool>("UseWebEndpoint"),
			DockerToken = configuration.GetValue<string>("DockerToken") ?? "",
			GithubToken = configuration.GetValue<string>("GithubToken") ?? "",
			MaxRunners = GetMaxRunners(configuration),
			RepoAllowlistPrefix = configuration.GetValue<string>("RepoAllowlistPrefix") ?? "",
			RepoAllowlist = ParseCommaSeparatedList(configuration.GetValue<string>("RepoAllowlist")),
			IsRepoAllowlistExactMatch = configuration.GetValue<bool>("IsRepoAllowlistExactMatch", true),
			RepoDenylistPrefix = configuration.GetValue<string>("RepoDenylistPrefix") ?? "",
			RepoDenylist = ParseCommaSeparatedList(configuration.GetValue<string>("RepoDenylist")),
			IsRepoDenylistExactMatch = configuration.GetValue<bool>("IsRepoDenylistExactMatch"),
			DockerHost = configuration.GetValue<string>("DockerHost") ?? "unix:/var/run/docker.sock",
			Labels = BuildLabels(configuration),
			OpenTelemetry = BuildOpenTelemetryConfiguration(configuration),
			DockerImage = configuration.GetValue<string>("DockerImage") ?? "myoung34/github-runner:latest",
			AutoCheckForImageUpdates = configuration.GetValue<bool>("AutoCheckForImageUpdates", true),
			CoordinatorHostname = configuration.GetValue<string>("CoordinatorHostname") ?? Environment.MachineName,
		};
	}

	private static int GetMaxRunners(IConfiguration configuration)
	{
		var maxRunners = configuration.GetValue<int>("MaxRunners", 4);
		return maxRunners switch
		{
			0 => 1,
			< 0 => int.MaxValue,
			_ => maxRunners,
		};
	}

	private static string[] ParseCommaSeparatedList(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return [];

		return value
			.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
			.Distinct()
			.ToArray();
	}

	private static string[] BuildLabels(IConfiguration configuration)
	{
		var architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
		var configuredLabels = ParseCommaSeparatedList(
			configuration.GetValue<string>("Labels")?.ToLowerInvariant()
		);

		return configuredLabels
			.Concat(["self-hosted", architecture])
			.Distinct()
			.ToArray();
	}

	[UnconditionalSuppressMessage(
		"Trimming",
		"IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
		Justification = "<Pending>"
	)]
	private static OpenTelemetryConfiguration BuildOpenTelemetryConfiguration(IConfiguration configuration)
	{
		var otelConfig = new OpenTelemetryConfiguration();
		configuration.GetSection("OpenTelemetry").Bind(otelConfig);

		var envOtlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
		if (!string.IsNullOrWhiteSpace(envOtlpEndpoint))
			otelConfig.OtlpEndpoint = envOtlpEndpoint;

		var envServiceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
		if (!string.IsNullOrWhiteSpace(envServiceName))
			otelConfig.ServiceName = envServiceName;

		return otelConfig;
	}

	public class OpenTelemetryConfiguration
	{
		public bool Enabled { get; set; } = true;
		public string ServiceName { get; set; } = "github-actions-autoscaler";
		public string? OtlpEndpoint { get; set; }
	}
}
