namespace GithubActionsAutoscaler.Abstractions.Queue.InMemory;

public sealed record InMemoryQueueMessage(
	string MessageId,
	string PopReceipt,
	string Content,
	DateTimeOffset? InsertedOn,
	int DequeueCount
) : IQueueMessage;
