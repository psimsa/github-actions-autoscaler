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

		result.Succeeded.Should().BeFalse();
		result.FailureMessage.Should().Contain("connection string");
	}

	[Fact]
	public void ValidateOptions_WithMissingQueueName_ReturnsFailure()
	{
		var validator = new AzureQueueOptionsValidator();
		var options = new AzureQueueOptions { ConnectionString = "conn", QueueName = "" };

		var result = validator.Validate(null, options);

		result.Succeeded.Should().BeFalse();
		result.FailureMessage.Should().Contain("queue name");
	}

	[Fact]
	public void ValidateOptions_WithValidOptions_ReturnsSuccess()
	{
		var validator = new AzureQueueOptionsValidator();
		var options = new AzureQueueOptions { ConnectionString = "conn", QueueName = "queue" };

		var result = validator.Validate(null, options);

		result.Succeeded.Should().BeTrue();
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

		message.MessageId.Should().Be("id");
		message.PopReceipt.Should().Be("receipt");
		message.Content.Should().Be("content");
		message.DequeueCount.Should().Be(2);
	}
}
