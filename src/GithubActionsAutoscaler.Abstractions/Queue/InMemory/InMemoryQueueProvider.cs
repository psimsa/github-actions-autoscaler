using System.Collections.Concurrent;

namespace GithubActionsAutoscaler.Abstractions.Queue.InMemory;

public class InMemoryQueueProvider : IQueueProvider
{
	private readonly ConcurrentQueue<InMemoryQueueMessage> _queue = new();
	private readonly ConcurrentDictionary<string, InMemoryQueueMessage> _inflight = new();
	private int _messageSequence = 0;

	public Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	public Task<IQueueMessage?> ReceiveMessageAsync(CancellationToken cancellationToken = default)
	{
		if (!_queue.TryDequeue(out var message))
		{
			return Task.FromResult<IQueueMessage?>(null);
		}

		var updated = message with
		{
			DequeueCount = message.DequeueCount + 1,
			PopReceipt = Guid.NewGuid().ToString("N")
		};

		_inflight[updated.MessageId] = updated;
		return Task.FromResult<IQueueMessage?>(updated);
	}

	public Task<IQueueMessage?> PeekMessageAsync(CancellationToken cancellationToken = default)
	{
		return Task.FromResult<IQueueMessage?>(_queue.TryPeek(out var message) ? message : null);
	}

	public Task SendMessageAsync(string content, CancellationToken cancellationToken = default)
	{
		var messageId = Interlocked.Increment(ref _messageSequence).ToString();
		var message = new InMemoryQueueMessage(
			messageId,
			Guid.NewGuid().ToString("N"),
			content,
			DateTimeOffset.UtcNow,
			0
		);
		_queue.Enqueue(message);
		return Task.CompletedTask;
	}

	public Task DeleteMessageAsync(IQueueMessage message, CancellationToken cancellationToken = default)
	{
		_inflight.TryRemove(message.MessageId, out _);
		return Task.CompletedTask;
	}

	public Task<int> GetApproximateMessageCountAsync(CancellationToken cancellationToken = default)
	{
		return Task.FromResult(_queue.Count);
	}
}
