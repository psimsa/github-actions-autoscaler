using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace AutoscalerApi;

public class AppConfiguration
{
    public bool UseWebEndpoint { get; set; }
    public string AzureStorage { get; set; } = "";
    public string AzureStorageQueue { get; set; } = "";
    public string DockerToken { get; set; } = "";
    public string DockerImage { get; set; }
    public string GithubToken { get; set; } = "";
    public int MaxRunners { get; set; }
    public string RepoWhitelistPrefix { get; set; } = "";
    public string[] RepoWhitelist { get; set; } = Array.Empty<string>();
    public bool IsRepoWhitelistExactMatch { get; set; }
    public string RepoBlacklistPrefix { get; set; } = "";
    public string[] RepoBlacklist { get; set; } = Array.Empty<string>();
    public bool IsRepoBlacklistExactMatch { get; set; }
    public string DockerHost { get; set; } = "";
    public string[] Labels { get; set; } = Array.Empty<string>();
    public string ApplicationInsightsConnectionString { get; set; } = "";
    public bool AutoCheckForImageUpdates { get; set; }


    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "<Pending>")]
    public static AppConfiguration FromConfiguration(IConfiguration configuration)
    {
        var maxRunners = configuration.GetValue<int>("MaxRunners", 4);
        maxRunners = maxRunners switch
        {
            0 => 1,
            < 0 => int.MaxValue,
            _ => maxRunners
        };

        var architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        /*var os = architecture switch
        {
            _ when RuntimeInformation.IsOSPlatform(OSPlatform.Windows) => "windows",
            _ when RuntimeInformation.IsOSPlatform(OSPlatform.Linux) => "linux",
            _ when RuntimeInformation.IsOSPlatform(OSPlatform.OSX) => "osx",
            _ => ""
        };*/


        return new AppConfiguration()
        {
            AzureStorageQueue = configuration.GetValue<string>("AzureStorageQueue"),
            AzureStorage = configuration.GetValue<string>("AzureStorage"),
            UseWebEndpoint = configuration.GetValue<bool>("UseWebEndpoint"),
            DockerToken = configuration.GetValue<string>("DockerToken"),
            GithubToken = configuration.GetValue<string>("GithubToken"),
            MaxRunners = maxRunners,
            RepoWhitelistPrefix = configuration.GetValue<string>("RepoWhitelistPrefix"),
            RepoWhitelist = configuration.GetValue<string>("RepoWhitelist").Split(',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray(),
            IsRepoWhitelistExactMatch = configuration.GetValue<bool>("IsRepoWhitelistExactMatch", true),
            RepoBlacklistPrefix = configuration.GetValue<string>("RepoBlacklistPrefix"),
            RepoBlacklist = configuration.GetValue<string>("RepoBlacklist").Split(',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray(),
            IsRepoBlacklistExactMatch = configuration.GetValue<bool>("IsRepoBlacklistExactMatch"),
            DockerHost = configuration.GetValue<string>("DockerHost") ?? "unix:/var/run/docker.sock",
            Labels = (configuration.GetValue<string>("Labels")?.ToLowerInvariant().Split(',',
                          StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                      ?? Array.Empty<string>())
                .Concat(new[]
                {
                    "self-hosted",
                    architecture,
                    // os
                }).Distinct().ToArray(),
            ApplicationInsightsConnectionString =
                configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING"),
            DockerImage = configuration.GetValue<string>("DockerImage") ?? "myoung34/github-runner:latest",
            AutoCheckForImageUpdates = configuration.GetValue<bool>("AutoCheckForImageUpdates", true)
        };
    }

}