using FluentAssertions;
using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Services;
using GithubActionsAutoscaler.Tests.Integration.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using System.Text;

namespace GithubActionsAutoscaler.Tests.Integration.Services;

public class RabbitMQQueueServiceTests : IClassFixture<RabbitMqFixture>
{
	private readonly RabbitMqFixture _fixture;
	private const string TestQueueName = "test-queue";

	public RabbitMQQueueServiceTests(RabbitMqFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task InitializeAsync_ConnectsSuccessfully()
	{
		var config = CreateConfig();
		var service = new RabbitMQQueueService(config, NullLogger<RabbitMQQueueService>.Instance);

		var act = async () => await service.InitializeAsync(CancellationToken.None);

		await act.Should().NotThrowAsync();
		await service.DisposeAsync();
	}

	[Fact]
	public async Task ReceiveMessageAsync_WhenMessageExists_ReturnsMessage()
	{
		var config = CreateConfig();
		var service = new RabbitMQQueueService(config, NullLogger<RabbitMQQueueService>.Instance);
		await service.InitializeAsync(CancellationToken.None);

		await PublishTestMessage("test message content");

		var message = await service.ReceiveMessageAsync(CancellationToken.None);

		message.Should().NotBeNull();
		message!.Body.Should().Be("test message content");

		await service.DisposeAsync();
	}

	[Fact]
	public async Task ReceiveMessageAsync_WhenQueueEmpty_ReturnsNull()
	{
		var config = CreateConfig("empty-queue");
		var service = new RabbitMQQueueService(config, NullLogger<RabbitMQQueueService>.Instance);
		await service.InitializeAsync(CancellationToken.None);

		var message = await service.ReceiveMessageAsync(CancellationToken.None);

		message.Should().BeNull();

		await service.DisposeAsync();
	}

	[Fact]
	public async Task DeleteMessageAsync_AcknowledgesMessage()
	{
		var config = CreateConfig("delete-test-queue");
		var service = new RabbitMQQueueService(config, NullLogger<RabbitMQQueueService>.Instance);
		await service.InitializeAsync(CancellationToken.None);

		await PublishTestMessage("message to delete", "delete-test-queue");

		var message = await service.ReceiveMessageAsync(CancellationToken.None);
		message.Should().NotBeNull();

		var act = async () => await service.DeleteMessageAsync(
			message!.MessageId,
			message.PopReceipt,
			CancellationToken.None);

		await act.Should().NotThrowAsync();

		var nextMessage = await service.ReceiveMessageAsync(CancellationToken.None);
		nextMessage.Should().BeNull();

		await service.DisposeAsync();
	}

	[Fact]
	public async Task AbandonMessageAsync_RequeuesMessage()
	{
		var config = CreateConfig("abandon-test-queue");
		var service = new RabbitMQQueueService(config, NullLogger<RabbitMQQueueService>.Instance);
		await service.InitializeAsync(CancellationToken.None);

		await PublishTestMessage("message to abandon", "abandon-test-queue");

		var message = await service.ReceiveMessageAsync(CancellationToken.None);
		message.Should().NotBeNull();

		await service.AbandonMessageAsync(
			message!.MessageId,
			message.PopReceipt,
			CancellationToken.None);

		var requeuedMessage = await service.ReceiveMessageAsync(CancellationToken.None);
		requeuedMessage.Should().NotBeNull();
		requeuedMessage!.Body.Should().Be("message to abandon");

		await service.DisposeAsync();
	}

	private AppConfiguration CreateConfig(string queueName = TestQueueName)
	{
		return new AppConfiguration
		{
			QueueProvider = QueueProvider.RabbitMQ,
			RabbitHost = _fixture.Host,
			RabbitPort = _fixture.Port,
			RabbitUsername = _fixture.Username,
			RabbitPassword = _fixture.Password,
			RabbitQueueName = queueName
		};
	}

	private async Task PublishTestMessage(string content, string queueName = TestQueueName)
	{
		var factory = new ConnectionFactory
		{
			HostName = _fixture.Host,
			Port = _fixture.Port,
			UserName = _fixture.Username,
			Password = _fixture.Password
		};

		using var connection = await factory.CreateConnectionAsync();
		using var channel = await connection.CreateChannelAsync();

		await channel.QueueDeclareAsync(
			queue: queueName,
			durable: true,
			exclusive: false,
			autoDelete: false,
			arguments: null);

		var body = Encoding.UTF8.GetBytes(content);
		await channel.BasicPublishAsync(
			exchange: "",
			routingKey: queueName,
			mandatory: false,
			body: body);
	}
}
