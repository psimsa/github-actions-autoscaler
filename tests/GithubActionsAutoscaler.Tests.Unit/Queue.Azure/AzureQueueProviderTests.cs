using GithubActionsAutoscaler.Abstractions.Queue;
using GithubActionsAutoscaler.Queue.Azure;
using GithubActionsAutoscaler.Queue.Azure.Validation;

namespace GithubActionsAutoscaler.Tests.Unit.Queue.Azure;

public class AzureQueueProviderTests
{
	[Fact]
	public void ValidateOptions_WithMissingConnectionString_ReturnsFailure()
	{
		var validator = new AzureQueueOptionsValidator();
		var options = new AzureQueueOptions { ConnectionString = "", QueueName = "queue" };

		var result = validator.Validate(null, options);

		Assert.False(result.Succeeded);
		Assert.Contains("connection string", result.FailureMessage);
	}

	[Fact]
	public void ValidateOptions_WithMissingQueueName_ReturnsFailure()
	{
		var validator = new AzureQueueOptionsValidator();
		var options = new AzureQueueOptions { ConnectionString = "conn", QueueName = "" };

		var result = validator.Validate(null, options);

		Assert.False(result.Succeeded);
		Assert.Contains("queue name", result.FailureMessage);
	}

	[Fact]
	public void ValidateOptions_WithValidOptions_ReturnsSuccess()
	{
		var validator = new AzureQueueOptionsValidator();
		var options = new AzureQueueOptions { ConnectionString = "conn", QueueName = "queue" };

		var result = validator.Validate(null, options);

		Assert.True(result.Succeeded);
	}

	[Fact]
	public void AzureQueueMessage_ImplementsQueueMessageContract()
	{
		IQueueMessage message = new AzureQueueMessage(
			"id",
			"receipt",
			"content",
			null,
			2
		);

		Assert.Equal("id", message.MessageId);
		Assert.Equal("receipt", message.PopReceipt);
		Assert.Equal("content", message.Content);
		Assert.Equal(2, message.DequeueCount);
	}
}
