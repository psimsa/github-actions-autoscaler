using Azure.Storage.Queues;
using FluentAssertions;
using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Services;
using GithubActionsAutoscaler.Tests.Integration.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace GithubActionsAutoscaler.Tests.Integration.Services;

public class AzureQueueServiceTests : IClassFixture<AzuriteFixture>
{
	private readonly AzuriteFixture _fixture;
	private const string TestQueueName = "azure-test-queue";

	public AzureQueueServiceTests(AzuriteFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task InitializeAsync_CreatesQueueAndConnects()
	{
		var config = CreateConfig();
		var service = new AzureQueueService(config, NullLogger<AzureQueueService>.Instance);

		var act = async () => await service.InitializeAsync(CancellationToken.None);

		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task ReceiveMessageAsync_WhenMessageExists_ReturnsDecodedMessage()
	{
		var config = CreateConfig("receive-test-queue");
		var service = new AzureQueueService(config, NullLogger<AzureQueueService>.Instance);
		await service.InitializeAsync(CancellationToken.None);

		var messageContent = """{"action": "queued", "test": true}""";
		await SendTestMessage(messageContent, "receive-test-queue");

		var message = await service.ReceiveMessageAsync(CancellationToken.None);

		message.Should().NotBeNull();
		message!.Body.Should().Be(messageContent);
	}

	[Fact]
	public async Task ReceiveMessageAsync_WhenQueueEmpty_ReturnsNull()
	{
		var config = CreateConfig("empty-azure-queue");
		var service = new AzureQueueService(config, NullLogger<AzureQueueService>.Instance);
		await service.InitializeAsync(CancellationToken.None);

		var message = await service.ReceiveMessageAsync(CancellationToken.None);

		message.Should().BeNull();
	}

	[Fact]
	public async Task DeleteMessageAsync_RemovesMessageFromQueue()
	{
		var config = CreateConfig("delete-azure-queue");
		var service = new AzureQueueService(config, NullLogger<AzureQueueService>.Instance);
		await service.InitializeAsync(CancellationToken.None);

		await SendTestMessage("message to delete", "delete-azure-queue");

		var message = await service.ReceiveMessageAsync(CancellationToken.None);
		message.Should().NotBeNull();

		await service.DeleteMessageAsync(
			message!.MessageId,
			message.PopReceipt,
			CancellationToken.None);

		var queueClient = new QueueClient(_fixture.ConnectionString, "delete-azure-queue");
		var properties = await queueClient.GetPropertiesAsync();
		properties.Value.ApproximateMessagesCount.Should().Be(0);
	}

	private AppConfiguration CreateConfig(string queueName = TestQueueName)
	{
		return new AppConfiguration
		{
			QueueProvider = QueueProvider.Azure,
			AzureStorage = _fixture.ConnectionString,
			AzureStorageQueue = queueName
		};
	}

	private async Task SendTestMessage(string content, string queueName = TestQueueName)
	{
		var queueClient = new QueueClient(_fixture.ConnectionString, queueName);
		await queueClient.CreateIfNotExistsAsync();

		var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
		await queueClient.SendMessageAsync(base64Content);
	}
}
