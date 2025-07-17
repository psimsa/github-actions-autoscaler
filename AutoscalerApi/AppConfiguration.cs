using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace AutoscalerApi;

public class AppConfiguration
{
    public bool UseWebEndpoint { get; set; }
    public string AzureStorage { get; set; } = "";
    public string AzureStorageQueue { get; set; } = "";
    public string DockerToken { get; set; } = "";
    public string DockerImage { get; set; } = "myoung34/github-runner:latest";
    public string GithubToken { get; set; } = "";
    public int MaxRunners { get; set; } = 4;
    public string RepoWhitelistPrefix { get; set; } = "";
    public string[] RepoWhitelist { get; set; } = Array.Empty<string>();
    public bool IsRepoWhitelistExactMatch { get; set; } = true;
    public string RepoBlacklistPrefix { get; set; } = "";
    public string[] RepoBlacklist { get; set; } = Array.Empty<string>();
    public bool IsRepoBlacklistExactMatch { get; set; } = false;
    public string DockerHost { get; set; } = "unix:/var/run/docker.sock";
    public string[] Labels { get; set; } = Array.Empty<string>();
    public string ApplicationInsightsConnectionString { get; set; } = "";
    public bool AutoCheckForImageUpdates { get; set; } = true;

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
        /*var os = architecture switch
        {
            _ when RuntimeInformation.IsOSPlatform(OSPlatform.Windows) => "windows",
            _ when RuntimeInformation.IsOSPlatform(OSPlatform.Linux) => "linux",
            _ when RuntimeInformation.IsOSPlatform(OSPlatform.OSX) => "osx",
            _ => ""
        };*/

        var repoWhitelist = configuration.GetValue<string>("RepoWhitelist") ?? string.Empty;
        var repoBlacklist = configuration.GetValue<string>("RepoBlacklist") ?? string.Empty;
        var labels = configuration.GetValue<string>("Labels") ?? string.Empty;

        return new AppConfiguration()
        {
            AzureStorageQueue = configuration.GetValue<string>("AzureStorageQueue") ?? string.Empty,
            AzureStorage = configuration.GetValue<string>("AzureStorage") ?? string.Empty,
            UseWebEndpoint = configuration.GetValue<bool>("UseWebEndpoint", false),
            DockerToken = configuration.GetValue<string>("DockerToken") ?? string.Empty,
            GithubToken = configuration.GetValue<string>("GithubToken") ?? string.Empty,
            MaxRunners = maxRunners,
            RepoWhitelistPrefix = configuration.GetValue<string>("RepoWhitelistPrefix") ?? string.Empty,
            RepoWhitelist = string.IsNullOrWhiteSpace(repoWhitelist)
                ? Array.Empty<string>()
                : repoWhitelist.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Distinct()
                    .ToArray(),
            IsRepoWhitelistExactMatch = configuration.GetValue<bool>(
                "IsRepoWhitelistExactMatch",
                true
            ),
            RepoBlacklistPrefix = configuration.GetValue<string>("RepoBlacklistPrefix") ?? string.Empty,
            RepoBlacklist = string.IsNullOrWhiteSpace(repoBlacklist)
                ? Array.Empty<string>()
                : repoBlacklist.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Distinct()
                    .ToArray(),
            IsRepoBlacklistExactMatch = configuration.GetValue<bool>("IsRepoBlacklistExactMatch", false),
            DockerHost = configuration.GetValue<string>("DockerHost") ?? "unix:/var/run/docker.sock",
            Labels = string.IsNullOrWhiteSpace(labels)
                ? new[] { "self-hosted", architecture }
                : labels.ToLowerInvariant().Split(
                        ',',
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                    )
                    .Concat(new[] { "self-hosted", architecture })
                    .Distinct()
                    .ToArray(),
            ApplicationInsightsConnectionString = configuration.GetValue<string>(
                "APPLICATIONINSIGHTS_CONNECTION_STRING"
            ) ?? string.Empty,
            AutoCheckForImageUpdates = configuration.GetValue<bool>(
                "AutoCheckForImageUpdates",
                true
            ),
        };
    }
}
