namespace AutoscalerApi;

public class AppConfiguration
{
    public bool UseWebEndpoint { get; set; }
    public string AzureStorage { get; set; }
    public string AzureStorageQueue { get; set; }
    public string DockerToken { get; set; }
    public string GithubToken { get; set; }
    public int MaxRunners { get; set; }
    public string RepoPrefix { get; set; }
    public string[] RepoWhitelist { get; set; } = new string[0];
    public bool IsRepoWhitelistExactMatch { get; set; }
    public string DockerHost { get; set; }
}
