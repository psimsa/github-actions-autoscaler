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

		Assert.NotNull(message);
		Assert.Equal("payload", message!.Content);
		Assert.Equal(1, message.DequeueCount);
	}

	[Fact]
	public async Task DeleteMessageAsync_RemovesInflightMessage()
	{
		var provider = new InMemoryQueueProvider();
		await provider.InitializeAsync();

		await provider.SendMessageAsync("payload");
		var message = await provider.ReceiveMessageAsync();
		Assert.NotNull(message);

		await provider.DeleteMessageAsync(message!);
		var next = await provider.ReceiveMessageAsync();

		Assert.Null(next);
	}
}
