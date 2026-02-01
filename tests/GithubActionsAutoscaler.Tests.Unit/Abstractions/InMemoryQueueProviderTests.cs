using GithubActionsAutoscaler.Abstractions.Queue.InMemory;

namespace GithubActionsAutoscaler.Tests.Unit.Abstractions;

public class InMemoryQueueProviderTests
{
	[Fact]
	public async Task SendMessageAsync_ThenReceiveMessageAsync_ReturnsMessage()
	{
		var provider = new InMemoryQueueProvider();
		await provider.InitializeAsync();

		await provider.SendMessageAsync("payload");
		var message = await provider.ReceiveMessageAsync();

		message.Should().NotBeNull();
		message!.Content.Should().Be("payload");
		message.DequeueCount.Should().Be(1);
	}

	[Fact]
	public async Task DeleteMessageAsync_RemovesInflightMessage()
	{
		var provider = new InMemoryQueueProvider();
		await provider.InitializeAsync();

		await provider.SendMessageAsync("payload");
		var message = await provider.ReceiveMessageAsync();
		message.Should().NotBeNull();

		await provider.DeleteMessageAsync(message!);
		var next = await provider.ReceiveMessageAsync();

		next.Should().BeNull();
	}
}
