using System.Diagnostics.CodeAnalysis;

namespace AutoscalerApi;

public class AppConfiguration
{
    public bool UseWebEndpoint { get; set; }
    public string AzureStorage { get; set; } = "";
    public string AzureStorageQueue { get; set; } = "";
    public string DockerToken { get; set; } = "";
    public string GithubToken { get; set; } = "";
    public int MaxRunners { get; set; }
    public string RepoPrefix { get; set; } = "";
    public string[] RepoWhitelist { get; set; } = Array.Empty<string>();
    public bool IsRepoWhitelistExactMatch { get; set; }
    public string DockerHost { get; set; } = "";

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static AppConfiguration FromConfiguration(IConfiguration configuration)
    {
        var maxRunners = configuration.GetValue<int>("MaxRunners");
        maxRunners = maxRunners switch
        {
            0 => 1,
            < 0 => int.MaxValue,
            _ => maxRunners
        };

        return new AppConfiguration()
        {
            AzureStorageQueue = configuration.GetValue<string>("AzureStorageQueue"),
            AzureStorage = configuration.GetValue<string>("AzureStorage"),
            UseWebEndpoint = configuration.GetValue<bool>("UseWebEndpoint"),
            DockerToken = configuration.GetValue<string>("DockerToken"),
            GithubToken = configuration.GetValue<string>("GithubToken"),
            MaxRunners = maxRunners,
            RepoPrefix = configuration.GetValue<string>("RepoPrefix"),
            RepoWhitelist = configuration.GetValue<string>("RepoWhitelist").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            IsRepoWhitelistExactMatch = configuration.GetValue<bool>("IsRepoWhitelistExactMatch"),
            DockerHost = configuration.GetValue<string>("DockerHost"),
        };
    }
}
