namespace GithubActionsAutoscaler.Abstractions.Queue;

public interface IQueueMessage
{
	string MessageId { get; }
	string PopReceipt { get; }
	string Content { get; }
	DateTimeOffset? InsertedOn { get; }
	int DequeueCount { get; }
}
