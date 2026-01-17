namespace GithubActionsAutoscaler.Models;

public record QueueMessage(string MessageId, string PopReceipt, string Body);
