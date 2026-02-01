namespace GithubActionsAutoscaler.Configuration;

public class QueueOptions
{
	public string Provider { get; set; } = "";
	public AzureQueueOptions AzureStorageQueue { get; set; } = new();
}

public class AzureQueueOptions
{
	public string ConnectionString { get; set; } = "";
	public string QueueName { get; set; } = "";
}
