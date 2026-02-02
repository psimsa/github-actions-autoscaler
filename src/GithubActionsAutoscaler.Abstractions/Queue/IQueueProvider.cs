namespace GithubActionsAutoscaler.Abstractions.Queue;

public interface IQueueProvider
{
	Task InitializeAsync(CancellationToken cancellationToken = default);
	Task<IQueueMessage?> ReceiveMessageAsync(CancellationToken cancellationToken = default);
	Task<IQueueMessage?> PeekMessageAsync(CancellationToken cancellationToken = default);
	Task SendMessageAsync(string content, CancellationToken cancellationToken = default);
	Task DeleteMessageAsync(IQueueMessage message, CancellationToken cancellationToken = default);
	Task<int> GetApproximateMessageCountAsync(CancellationToken cancellationToken = default);
}
