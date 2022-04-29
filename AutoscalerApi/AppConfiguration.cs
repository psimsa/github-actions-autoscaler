using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace AutoscalerApi;

public class AppConfiguration
{
    public bool UseWebEndpoint { get; set; }
    public string AzureStorage { get; set; } = "";
    public string AzureStorageQueue { get; set; } = "";
    public string DockerToken { get; set; } = "";
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

    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "<Pending>")]
    public static AppConfiguration FromConfiguration(IConfiguration configuration)
    {
        var maxRunners = configuration.GetValue<int>("MaxRunners");
        maxRunners = maxRunners switch
        {
            0 => 1,
            < 0 => int.MaxValue,
            _ => maxRunners
        };
        var architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
        string os = "";
        os = architecture switch
        {
            _ when System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows) => "windows",
            _ when System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux) => "linux",
            _ when System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX) => "osx",
            _ => ""
        };


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
            IsRepoWhitelistExactMatch = configuration.GetValue<bool>("IsRepoWhitelistExactMatch"),
            RepoBlacklistPrefix = configuration.GetValue<string>("RepoBlacklistPrefix"),
            RepoBlacklist = configuration.GetValue<string>("RepoBlacklist").Split(',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray(),
            IsRepoBlacklistExactMatch = configuration.GetValue<bool>("IsRepoBlacklistExactMatch"),
            DockerHost = configuration.GetValue<string>("DockerHost") ?? "unix:/var/run/docker.sock",
            Labels = (configuration.GetValue<string>("Labels")?.ToLowerInvariant().Split(',',
                          StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                      ?? Array.Empty<string>())
                .Concat(new[] {"self-hosted", architecture.ToString().ToLowerInvariant(), os}).Distinct().ToArray()
        };
    }
}