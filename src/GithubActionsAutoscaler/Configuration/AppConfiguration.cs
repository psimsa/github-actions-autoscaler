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
        var maxRunners = configuration.GetValue<int>("MaxRunners", 4);
        maxRunners = maxRunners switch
        {
            0 => 1,
            < 0 => int.MaxValue,
            _ => maxRunners,
        };

        var architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

        var otelConfig = new OpenTelemetryConfiguration();
        configuration.GetSection("OpenTelemetry").Bind(otelConfig);

        var envOtlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(envOtlpEndpoint))
        {
            otelConfig.OtlpEndpoint = envOtlpEndpoint;
        }

        var envServiceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
        if (!string.IsNullOrWhiteSpace(envServiceName))
        {
            otelConfig.ServiceName = envServiceName;
        }

        return new AppConfiguration()
        {
            AzureStorageQueue = configuration.GetValue<string>("AzureStorageQueue") ?? "",
            AzureStorage = configuration.GetValue<string>("AzureStorage") ?? "",
            UseWebEndpoint = configuration.GetValue<bool>("UseWebEndpoint"),
            DockerToken = configuration.GetValue<string>("DockerToken") ?? "",
            GithubToken = configuration.GetValue<string>("GithubToken") ?? "",
            MaxRunners = maxRunners,
            RepoAllowlistPrefix = configuration.GetValue<string>("RepoAllowlistPrefix") ?? "",
            RepoAllowlist =
                configuration
                    .GetValue<string>("RepoAllowlist")
                    ?.Split(
                        ',',
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                    )
                    .Distinct()
                    .ToArray() ?? [],
            IsRepoAllowlistExactMatch = configuration.GetValue<bool>(
                "IsRepoAllowlistExactMatch",
                true
            ),
            RepoDenylistPrefix = configuration.GetValue<string>("RepoDenylistPrefix") ?? "",
            RepoDenylist =
                configuration
                    .GetValue<string>("RepoDenylist")
                    ?.Split(
                        ',',
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                    )
                    .Distinct()
                    .ToArray() ?? [],
            IsRepoDenylistExactMatch = configuration.GetValue<bool>("IsRepoDenylistExactMatch"),
            DockerHost =
                configuration.GetValue<string>("DockerHost") ?? "unix:/var/run/docker.sock",
            Labels = (
                configuration
                    .GetValue<string>("Labels")
                    ?.ToLowerInvariant()
                    .Split(
                        ',',
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                    ) ?? []
            )
                .Concat(
                    [
                        "self-hosted",
                        architecture,
                        // os
                    ]
                )
                .Distinct()
                .ToArray(),
            OpenTelemetry = otelConfig,
            DockerImage =
                configuration.GetValue<string>("DockerImage") ?? "myoung34/github-runner:latest",
            AutoCheckForImageUpdates = configuration.GetValue<bool>(
                "AutoCheckForImageUpdates",
                true
            ),
            CoordinatorHostname =
                configuration.GetValue<string>("CoordinatorHostname") ?? Environment.MachineName,
        };
    }

    public class OpenTelemetryConfiguration
    {
        public bool Enabled { get; set; } = true;
        public string ServiceName { get; set; } = "github-actions-autoscaler";
        public string? OtlpEndpoint { get; set; }
    }
}
