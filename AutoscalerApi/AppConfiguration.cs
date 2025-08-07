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
        var maxRunners = GetMaxRunners(configuration);
        var architecture = GetArchitecture();
        var repoWhitelist = GetRepoWhitelist(configuration);
        var repoBlacklist = GetRepoBlacklist(configuration);
        var labels = GetLabels(configuration, architecture);

        return new AppConfiguration()
        {
            AzureStorageQueue = configuration.GetValue<string>("AzureStorageQueue") ?? string.Empty,
            AzureStorage = configuration.GetValue<string>("AzureStorage") ?? string.Empty,
            UseWebEndpoint = configuration.GetValue<bool>("UseWebEndpoint", false),
            DockerToken = configuration.GetValue<string>("DockerToken") ?? string.Empty,
            GithubToken = configuration.GetValue<string>("GithubToken") ?? string.Empty,
            MaxRunners = maxRunners,
            RepoWhitelistPrefix = configuration.GetValue<string>("RepoWhitelistPrefix") ?? string.Empty,
            RepoWhitelist = repoWhitelist,
            IsRepoWhitelistExactMatch = configuration.GetValue<bool>(
                "IsRepoWhitelistExactMatch",
                true
            ),
            RepoBlacklistPrefix = configuration.GetValue<string>("RepoBlacklistPrefix") ?? string.Empty,
            RepoBlacklist = repoBlacklist,
            IsRepoBlacklistExactMatch = configuration.GetValue<bool>("IsRepoBlacklistExactMatch", false),
            DockerHost = configuration.GetValue<string>("DockerHost") ?? "unix:/var/run/docker.sock",
            Labels = labels,
            ApplicationInsightsConnectionString = configuration.GetValue<string>(
                "APPLICATIONINSIGHTS_CONNECTION_STRING"
            ) ?? string.Empty,
            AutoCheckForImageUpdates = configuration.GetValue<bool>(
                "AutoCheckForImageUpdates",
                true
            ),
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

    private static string GetArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
    }

    private static string[] GetRepoWhitelist(IConfiguration configuration)
    {
        var repoWhitelist = configuration.GetValue<string>("RepoWhitelist") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(repoWhitelist))
            return Array.Empty<string>();

        return repoWhitelist.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToArray();
    }

    private static string[] GetRepoBlacklist(IConfiguration configuration)
    {
        var repoBlacklist = configuration.GetValue<string>("RepoBlacklist") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(repoBlacklist))
            return Array.Empty<string>();

        return repoBlacklist.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToArray();
    }

    private static string[] GetLabels(IConfiguration configuration, string architecture)
    {
        var labels = configuration.GetValue<string>("Labels") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(labels))
            return new[] { "self-hosted", architecture };

        return labels.ToLowerInvariant().Split(
                ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
            )
            .Concat(new[] { "self-hosted", architecture })
            .Distinct()
            .ToArray();
    }
}
