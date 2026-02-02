using GithubActionsAutoscaler.Abstractions.Queue;

namespace GithubActionsAutoscaler.Queue.Azure;

public sealed record AzureQueueMessage(
	string MessageId,
	string PopReceipt,
	string Content,
	DateTimeOffset? InsertedOn,
	int DequeueCount
) : IQueueMessage;
