namespace GithubActionsAutoscaler.Queue.Azure;

public class AzureQueueOptions
{
	public string ConnectionString { get; set; } = "";
	public string QueueName { get; set; } = "";
}
