namespace GithubActionsAutoscaler.Abstractions.Queue;

public class QueueOptions
{
	public string Provider { get; set; } = "";
	public string AzureStorage { get; set; } = "";
	public string AzureStorageQueue { get; set; } = "";
}
